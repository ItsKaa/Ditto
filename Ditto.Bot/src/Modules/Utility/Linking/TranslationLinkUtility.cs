using Cauldron.Core.Collections;
using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using Ditto.Translation;
using Ditto.Translation.Attributes;
using Ditto.Translation.Data;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility.Linking
{
    [Alias("translation")]
    public class TranslationLinkUtility : DiscordModule<LinkUtility>
    {
        private class TranslationLink
        {
            private static readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
            public Link Link { get; private set; }
            public ITextChannel TargetChannel { get; private set; }
            public ITextChannel SourceChannel { get; private set; }
            public Language TargetLanguage { get; private set; }
            public Language SourceLanguage { get; private set; }
            public ulong LastMessageId { get; private set; } = ulong.MaxValue;

            public TranslationLink(Link link)
            {
                Link = link;
                Reload().GetAwaiter().GetResult();
            }

            public async Task Reload()
            {
                TargetChannel = Link.Channel;
                var valueSplit = Link.Value.Split("|");
                var channelIdString = valueSplit[0];
                var sourceLanguageISO = valueSplit[1];
                var targetLanguageISO = valueSplit[2];
                var lastMessageIdString = valueSplit[3];

                TargetLanguage = GoogleTranslator.GetLanguageByISO(targetLanguageISO);
                SourceLanguage = GoogleTranslator.GetLanguageByISO(sourceLanguageISO);
                
                if (ulong.TryParse(channelIdString, out ulong channelId))
                {
                    SourceChannel = await Link.Guild.GetTextChannelAsync(channelId).ConfigureAwait(false);
                }

                if (ulong.TryParse(lastMessageIdString, out ulong lastMessageId))
                {
                    LastMessageId = lastMessageId;
                }
            }

            public async Task UpdateAsync(ulong lastMessageId)
            {
                await _mutex.WaitAsync().ConfigureAwait(false);
                try
                {
                    var linkValue = $"{SourceChannel.Id}|{SourceLanguage.ISO639}|{TargetLanguage.ISO639}|{lastMessageId}";
                    Link.Value = linkValue;
                    await Ditto.Database.DoAsync(uow =>
                    {
                        uow.Links.Update(Link);
                    }).ConfigureAwait(false);
                }
                catch { }
                finally
                {
                    _mutex.Release();
                }
            }
        }

        private static ConcurrentList<TranslationLink> _links = new ConcurrentList<TranslationLink>();
        private static GoogleTranslator _translator = new GoogleTranslator();

        static TranslationLinkUtility()
        {
            Ditto.Connected += () =>
            {
                Task.Run(async () =>
                {
                    // Setup the proxy if it is enabled.
                    if (Ditto.Settings.ProxySettings.Enabled)
                    {
                        _translator.Proxy = new WebProxy(Ditto.Settings.ProxySettings.Host, Ditto.Settings.ProxySettings.Port);
                        if (!string.IsNullOrEmpty(Ditto.Settings.ProxySettings.Username))
                        {
                            _translator.Proxy.Credentials = new NetworkCredential(Ditto.Settings.ProxySettings.Username, Ditto.Settings.ProxySettings.Password);
                        }
                     }

                    _links.Clear();
                    await Ditto.Database.ReadAsync(uow =>
                    {
                        _links.AddRange(uow.Links.GetAllWithLinks(l => l.Type == LinkType.Translation).Select(l => new TranslationLink(l)));
                    }).ConfigureAwait(false);

                    foreach (var link in _links.ToList())
                    {
                        var messages = new List<IMessage>();

                        if(link.LastMessageId == ulong.MinValue)
                        {
                            messages.AddRange((await link.SourceChannel.GetMessagesAsync(Discord.DiscordConfig.MaxMessagesPerBatch,
                                    options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit | RetryMode.RetryTimeouts }
                            ).ToListAsync().ConfigureAwait(false)).SelectMany(x => x).OrderBy(x => x.CreatedAt));
                        }
                        else if(link.LastMessageId != ulong.MaxValue)
                        {
                            var lastMessageId = link.LastMessageId;
                            while(true)
                            {
                                var msg = (await link.SourceChannel.GetMessagesAsync(lastMessageId, Direction.After, Discord.DiscordConfig.MaxMessagesPerBatch,
                                        options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit | RetryMode.RetryTimeouts }
                                ).ToListAsync().ConfigureAwait(false)).SelectMany(x => x).OrderBy(x => x.CreatedAt);

                                if(!msg.Any())
                                {
                                    break;
                                }

                                lastMessageId = msg.LastOrDefault().Id;
                                messages.AddRange(msg);
                            }
                        }

                        foreach(var msg in messages)
                        {
                            await HandleMessageAsync(msg).ConfigureAwait(false);
                        }
                    }

                    await Ditto.Client.DoAsync(c =>
                    {
                        c.MessageReceived -= Ditto_MessageReceived;
                        c.MessageReceived += Ditto_MessageReceived;
                    }).ConfigureAwait(false);

                });

                return Task.CompletedTask;
            };
            
            // Empty link handler since it's a one time only configuration
            LinkUtility.TryAddHandler(LinkType.Translation, (link, channel, cancellationToken) => Task.FromResult(Enumerable.Empty<string>()));
        }

        private static Task Ditto_MessageReceived(Discord.WebSocket.SocketMessage socketMessage)
        {
            Task.Run(async () =>
            {
                await HandleMessageAsync(socketMessage).ConfigureAwait(false);
            });

            return Task.CompletedTask;
        }

        private static async Task HandleMessageAsync(IMessage message)
        {
            if (message == null || message.Channel == null || message.Author.IsBot == true)
            {
                return;
            }

            foreach (var link in _links.ToList().Where(x => x.SourceChannel?.Id == message.Channel.Id))
            {
                if (message.Content.StartsWith(Ditto.Cache.Db.Prefix(link.Link.Guild)))
                {
                    continue;
                }

                if (link.Link == null || link.SourceChannel == null || link.TargetChannel == null || link.SourceLanguage == null || link.TargetLanguage == null)
                {
                    continue;
                }

                var messageContent = message.Content;
                var messageWithoutTags = messageContent;
                var messageTagStrings = new List<string>();
                int counter = 0;

                // Parse discord tags (user, channel, role)
                while (true)
                {
                    var parseResult = messageContent.ParseDiscordTags().Where(x => x.IsSuccess).FirstOrDefault();
                    if (parseResult == null)
                    {
                        break;
                    }

                    messageTagStrings.Add(messageContent.Substring(parseResult.Index, parseResult.Length));
                    messageWithoutTags = messageContent.Remove(parseResult.Index, parseResult.Length);
                    messageContent = messageContent.Remove(parseResult.Index, parseResult.Length);
                    messageContent = messageContent.Insert(parseResult.Index, $"{{{counter++}}}");
                }

                // Parse discord emojis.
                string imageUrl = null ;
                while(true)
                {

                    var parseResult = messageContent.ParseDiscordEmojis().FirstOrDefault();
                    if(parseResult == null)
                    {
                        break;
                    }

                    if (parseResult.Type == DiscordTagType.EMOJI_ANIMATED)
                    {
                        imageUrl = $"https://cdn.discordapp.com/emojis/{parseResult.Id}.gif?size=48";
                    }
                    else if (parseResult.Type == DiscordTagType.EMOJI)
                    {
                        imageUrl = $"https://cdn.discordapp.com/emojis/{parseResult.Id}.webp?size=48";
                    }

                    messageTagStrings.Add(messageContent.Substring(parseResult.Index, parseResult.Length));
                    messageWithoutTags = messageContent.Remove(parseResult.Index, parseResult.Length);
                    messageContent = messageContent.Remove(parseResult.Index, parseResult.Length);
                    messageContent = messageContent.Insert(parseResult.Index, $"{{{counter++}}}");
                }

                // Handle Tenor URLs
                var tenorMatch = Globals.RegularExpression.TenorGif.Match(messageContent);
                if (tenorMatch.Success)
                {
                    var tenorUrl = tenorMatch.Value;
                    messageContent = messageContent.Replace(tenorUrl, "");
                    messageWithoutTags = messageWithoutTags.Replace(tenorUrl, "");

                    var tenorGifUrl = await WebHelper.GetResponseUrlAsync($"{tenorUrl}.gif").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(tenorGifUrl))
                    {
                        imageUrl = tenorGifUrl;
                    }
                }

                // Translate or use original message when unnecessary.
                TranslationResult translationResult = null;
                if (!string.IsNullOrEmpty(messageWithoutTags.Trim()))
                {
                    try
                    {
                        translationResult = await _translator.TranslateLiteAsync(messageContent, link.SourceLanguage, link.TargetLanguage).ConfigureAwait(false);
                    }
                    catch
                    {
                        continue;
                    }

                    // Ignore if it's null for whatever reason.
                    if (translationResult == null)
                        continue;

                    // Ignore if the text has a 50%+ chance of being the target langauge and only if it goes to the same channel.
                    var targetLanguageDetection = translationResult?.LanguageDetections.FirstOrDefault(x => x.Language == translationResult.TargetLanguage);
                    if (targetLanguageDetection != null && targetLanguageDetection.Confidence > 0.50 && link.TargetChannel == link.SourceChannel)
                        continue;

                }

                // Retrieve data from the translation result
                var sourceLanguageISO = link.SourceLanguage.ISO639;
                var targetLanguageISO = link.TargetLanguage.ISO639;
                string translatedMessage = null;
                if (translationResult != null)
                {
                    translatedMessage = translationResult?.MergedTranslation ?? string.Empty;

                    // Replace the tags with it's original value
                    counter = 0;
                    foreach (var tag in messageTagStrings)
                    {
                        translatedMessage = translatedMessage.Replace($"{{{counter}}}", messageTagStrings.ElementAt(counter));
                        counter++;
                    }

                    sourceLanguageISO = translationResult.SourceLanguage.ISO639;
                    targetLanguageISO = translationResult.TargetLanguage.ISO639;
                }

                // Get the actual full name for the language
                var sourceLanguage = GoogleTranslator.GetLanguageByISO(sourceLanguageISO);
                var targetLanguage = GoogleTranslator.GetLanguageByISO(targetLanguageISO);

                var sourceLanguageName = sourceLanguage.FullName;
                var targetLanguageName = targetLanguage.FullName;
                if (targetLanguage.Equals(Language.ChineseSimplified))
                {
                    sourceLanguageName = GoogleTranslator.GetLanguageNameInChineseSimplified(sourceLanguage);
                    targetLanguageName = GoogleTranslator.GetLanguageNameInChineseSimplified(targetLanguage);
                }

                // Build the message description if it contains values.
                var messageDescription = "";
                if (!string.IsNullOrEmpty(messageWithoutTags.Trim()))
                {
                    messageDescription = $"`{sourceLanguageName}`\n{messageWithoutTags.Trim()}\n\n`{targetLanguageName}`\n";
                }
                messageDescription += translatedMessage;

                // Send embedded message to the target channel.
                var authorGuildUser = message.Author as IGuildUser;
                if (authorGuildUser == null)
                {
                    authorGuildUser = await message.Channel.GetUserAsync(message.Author.Id).ConfigureAwait(false) as IGuildUser;
                }

                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithIconUrl(message.Author.GetAvatarUrl())
                        .WithName(authorGuildUser?.DisplayName ?? authorGuildUser?.Nickname ?? message.Author?.Username ?? "Unknown User")
                    )
                    .WithDescription(messageDescription)
                    .WithColor(Ditto.Cache.Db.EmbedMusicPlayingColour(link.Link.Guild));

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    embedBuilder = embedBuilder.WithImageUrl(imageUrl);
                }

                // Download the attachments if they are included so we can forward them in the same message.
                var fileStreams = new List<Stream>();
                var files = new List<FileAttachment>();
                foreach (var attachment in message.Attachments)
                {
                    var attachmentUrl = attachment?.Url;
                    if (!string.IsNullOrEmpty(attachmentUrl))
                    {
                        try
                        {
                            var stream = await WebHelper.GetStreamAsync(attachmentUrl).ConfigureAwait(false);
                            files.Add(new FileAttachment(stream, attachment.Filename));
                            fileStreams.Add(stream);
                        }
                        catch { }
                    }
                }

                // Handle reply text
                var messageText = "";
                if (message.Reference?.MessageId.IsSpecified == true)
                {
                    var replyMessage = await message.Channel.GetMessageAsync(message.Reference.MessageId.Value).ConfigureAwait(false);
                    var replyAuthorName = (replyMessage.Author as IGuildUser)?.DisplayName ?? replyMessage.Author.Username;
                    var replyContent = replyMessage.Content;

                    if (replyMessage.Embeds.Any())
                    {
                        replyContent = replyMessage.Embeds.FirstOrDefault().Description;
                    }

                    if (string.IsNullOrEmpty(replyContent) && replyMessage.Attachments.Any())
                    {
                        if (targetLanguage.Equals(Language.ChineseSimplified))
                        {
                            replyContent = "<文件附件>";
                        }
                        else
                        {
                            replyContent = "<attachment>";
                        }
                    }

                    messageText = $"⤷ **{replyAuthorName}**: `{replyContent?.TrimTo(125) ?? ""}`";
                    if (replyMessage.Channel is ITextChannel textChannel)
                    {
                        messageText += $"\nhttps://discord.com/channels/{textChannel.GuildId}/{replyMessage.Channel.Id}/{replyMessage.Id}";
                    }
                }

                // Send the translated message.
                if (files.Count > 0)
                {
                    await link.TargetChannel.SendFilesAsync(
                        attachments: files,
                        text: messageText,
                        embed: embedBuilder.Build(),
                        options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }
                    ).ConfigureAwait(false);
                }
                else
                {
                    await link.TargetChannel.SendMessageAsync(messageText,
                        embed: embedBuilder.Build(),
                        options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }
                    ).ConfigureAwait(false);
                }

                // Close the handls of the streams
                foreach(var stream in fileStreams)
                {
                    stream.Close();
                }

                // Update link with new string value
                await link.UpdateAsync(message.Id).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        [Alias("add", "link", "hook", "register")]
        public async Task Add(ITextChannel sourceTextChannel, ITextChannel targetTextChannel, Languages sourceLanguage, Languages targetLanguage, ulong? fromMessageId = null)
        {
            if (!(await Ditto.Client.DoAsync(
                    async c => (await c.GetPermissionsAsync(sourceTextChannel)).HasAccess() && (await c.GetPermissionsAsync(targetTextChannel)).HasAccess()
                ).ConfigureAwait(false)))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            // Only allow using channels of the current guild.
            if (sourceTextChannel != null && sourceTextChannel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }
            else if (targetTextChannel != null && targetTextChannel.Guild != Context.Guild)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            var fromLanguage = GoogleTranslator.GetLanguageByName(sourceLanguage.GetAttribute<LanguageFullNameAttribute>().FullName);
            var toLanguage = GoogleTranslator.GetLanguageByName(targetLanguage.GetAttribute<LanguageFullNameAttribute>().FullName);
            if(fromLanguage == null || toLanguage == null)
            {
                await Context.ApplyResultReaction(CommandResult.InvalidParameters).ConfigureAwait(false);
                return;
            }

            string value = $"{sourceTextChannel.Id}|{fromLanguage.ISO639}|{toLanguage.ISO639}|{(fromMessageId == null ? $"{ulong.MinValue}" : $"{fromMessageId}")}";
            if (_links.FirstOrDefault(x => x.Link.Value.StartsWith(value, StringComparison.CurrentCultureIgnoreCase)) != null)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
            else
            {
                var link = await LinkUtility.TryAddLinkAsync(LinkType.Translation, targetTextChannel, value, null);
                if(link == null)
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
                else
                {
                    _links.Add(new TranslationLink(link));
                    await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
                }
            }
        }


    }
}
