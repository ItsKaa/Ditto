using Cauldron.Core.Collections;
using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Translation;
using Ditto.Translation.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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
            LinkUtility.TryAddHandler(LinkType.Translation, (link, channel) => Task.FromResult(Enumerable.Empty<string>()));
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
                while(true)
                {

                    var parseResult = messageContent.ParseDiscordEmojis().FirstOrDefault();
                    if(parseResult == null)
                    {
                        break;
                    }

                    messageTagStrings.Add(messageContent.Substring(parseResult.Index, parseResult.Length));
                    messageWithoutTags = messageContent.Remove(parseResult.Index, parseResult.Length);
                    messageContent = messageContent.Remove(parseResult.Index, parseResult.Length);
                    messageContent = messageContent.Insert(parseResult.Index, $"{{{counter++}}}");
                }

                // Translate or use original message when unnecessary.
                string translatedMessage = messageContent;
                if (!string.IsNullOrEmpty(messageWithoutTags.Trim()))
                {
                    TranslationResult result = null;
                    try
                    {
                        result = await _translator.TranslateLiteAsync(messageContent, link.SourceLanguage, link.TargetLanguage).ConfigureAwait(false);
                    }
                    catch(Exception ex)
                    {
                        Log.Debug(ex);
                        return;
                    }
                    translatedMessage = result?.MergedTranslation;
                }

                // Replace the tags with it's original value
                counter = 0;
                foreach (var tag in messageTagStrings)
                {
                    translatedMessage = translatedMessage.Replace($"{{{counter}}}", messageTagStrings.ElementAt(counter));
                    counter++;
                }

                // Send embedded message to the target channel.
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor(message.Author)
                    .WithDescription(translatedMessage)
                    .WithFooter($"🔀 {link.SourceLanguage.FullName} -> {link.TargetLanguage.FullName}, ⏰ Posted at {message.CreatedAt.UtcDateTime:hh\\:mm} UTC")
                    .WithColor(Ditto.Cache.Db.EmbedMusicPlayingColour(link.Link.Guild));

                // Send the translated message.
                await link.TargetChannel.EmbedAsync(embedBuilder, options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);

                // Send a secondary message with the attachment if its included.
                if (message.Attachments.Count > 0)
                {
                    var attachmentUrl = message.Attachments.ElementAt(0)?.Url;
                    if (!string.IsNullOrEmpty(attachmentUrl))
                    {
                        await link.TargetChannel.SendMessageAsync(attachmentUrl, options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry }).ConfigureAwait(false);
                    }
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

            var fromLanguage = GoogleTranslator.GetLanguageByName(sourceLanguage.ToString());
            var toLanguage = GoogleTranslator.GetLanguageByName(targetLanguage.ToString());

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
