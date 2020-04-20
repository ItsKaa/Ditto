using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data.Discord;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.Admin.Data;
using Ditto.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Extensions.Discord;
using Ditto.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    public class Build : DiscordModule
    {
        static Build()
        {
            // Check for a link update message
            Ditto.Connected += async() =>
            {
                Link link = null;
                await Ditto.Database.DoAsync(uow =>
                {
                    link = uow.Links.Get(l => l.Type == Database.Data.LinkType.Update);
                }, false).ConfigureAwait(false);

                if (link != null)
                {
                    if (!string.IsNullOrEmpty(link.Value))
                    {
                        var values = link.Value.Split("|", StringSplitOptions.RemoveEmptyEntries);
                        var commitHash = values.LastOrDefault();
                        if (!string.IsNullOrEmpty(commitHash) && ulong.TryParse(values.FirstOrDefault(), out ulong messageId))
                        {
                            IMessage message = null;
                            IGuild guild = null;
                            ITextChannel channel = null;
                            await Ditto.Client.DoAsync(async client =>
                            {
                                guild = client.GetGuild(link.GuildId);
                                channel = await guild.GetTextChannelAsync(link.ChannelId).ConfigureAwait(false);
                                message = await channel.GetMessageAsync(messageId, options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit }).ConfigureAwait(false);
                                if (message != null)
                                {
                                    await message.SetResultAsync(CommandResult.Success).ConfigureAwait(false);
                                }

                                // Post build info in a secondary task
                                try
                                {
                                    var _ = new Build()
                                    {
                                        Context = new CommandContextEx(client, message as IUserMessage)
                                        {
                                            Channel = channel,
                                            Guild = guild
                                        }
                                    }.Info(); //commitHash
                                }
                                catch(Exception ex)
                                {
                                    Log.Error(ex);
                                }

                                // Remove link
                                await Ditto.Database.DoAsync(uow =>
                                {
                                    uow.Links.Remove(link);
                                }, true).ConfigureAwait(false);

                            }).ConfigureAwait(false);
                        }
                    }
                }
            };
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(0)]
        public Task _(string fromHash = null)
        {
            return Info(fromHash);
        }

        private static string RunGit(string args)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "git",
                    Arguments = args,
                    WorkingDirectory = $"{Ditto.Settings.Paths.BaseDir}/{Ditto.Settings.Paths.ScriptDir}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e?.Data))
                {
                    outputBuilder.Append(e.Data)
                                 .Append(Environment.NewLine);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            return outputBuilder.ToString();
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(1)]
        public async Task<BuildInfo> CheckForUpdates(bool reactions = true)
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                // Git fetch to retrieve all updates
                var fetch = RunGit("fetch");

                // Compare the local and remote hash.
                var localHash = RunGit("rev-parse HEAD");
                var remoteHash = RunGit("rev-parse origin/master");
                if (!string.IsNullOrEmpty(localHash) && !string.IsNullOrEmpty(remoteHash))
                {
                    var buildInfo = new BuildInfo(localHash.Trim(), remoteHash.Trim());
                    if(reactions && buildInfo.LocalHash == null || buildInfo.RemoteHash == null)
                    {
                        await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    }
                    else if (reactions && buildInfo.IsEqual)
                    {
                        // Branch is already up to date
                        await Context.ApplyResultReaction(CommandResult.SuccessAlt1).ConfigureAwait(false);
                    }
                    return buildInfo;
                }
                else
                {
                    // Git fetch or rev-parse failed.
                    Log.Error($"Git failed to find branch info | Fetch: '{fetch}'; Local Hash: '{localHash}'; Remote Hash '{remoteHash}'.");
                    if (reactions)
                        await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
            else if (reactions)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }

            return new BuildInfo(null, null);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(1)]
        [Alias("upgrade")]
        public async Task Update()
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates().ConfigureAwait(false);
                if (!buildInfo.IsEqual && (await UpdateList(null, false).ConfigureAwait(false)).Count() > 0)
                {
                    // Add a database link containing the current local branch.
                    await Ditto.Database.DoAsync((uow) =>
                    {
                        var link = uow.Links.GetOrAdd((link) => link.Type == Database.Data.LinkType.Update, new Database.Models.Link()
                        {
                            Type = Database.Data.LinkType.Update,
                            ChannelId = Context.Channel.Id,
                            GuildId = Context.Guild.Id,
                            Date = DateTime.Now,
                            Value = $"{Context.Message.Id}|{buildInfo.LocalHash}",
                        });

                        link.ChannelId = Context.Channel.Id;
                        link.GuildId = Context.Guild.Id;
                        link.Date = DateTime.Now;
                        link.Value = $"{Context.Message.Id}|{buildInfo.LocalHash}";
                    }, true).ConfigureAwait(false);


                    var pull = RunGit("pull origin master");

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = "bash",
                        Arguments = $"{Ditto.Settings.Paths.BaseDir}/{Ditto.Settings.Paths.ScriptDir}/Run.{(BaseClass.IsLinux() ? "sh" : "bat")}",
                        WorkingDirectory = $"{Ditto.Settings.Paths.BaseDir}/{Ditto.Settings.Paths.ScriptDir}",
                        UseShellExecute = false,
                        CreateNoWindow = false,
                    };
                    if (BaseClass.IsWindows())
                    {
                        startInfo.FileName = "cmd";
                        startInfo.Arguments = "/c " + startInfo.Arguments;
                    }

                    // Start a new instance and close the current process.
                    Log.Info($"Updating bot...");
                    await Ditto.StopAsync().ConfigureAwait(false);
                    using var process = new Process() { StartInfo = startInfo };
                    process.Start();
                    Program.Close();
                }
                else
                {
                    await Context.ApplyResultReaction(CommandResult.SuccessAlt1).ConfigureAwait(false);
                }
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Alias("list")]
        [Priority(1)]
        public async Task<IEnumerable<EmbedField>> UpdateList(string fromHash = null, bool post = true)
        {
            var values = new List<EmbedField>();
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates(false).ConfigureAwait(false);

                var commitData = RunGit($"log {fromHash ?? buildInfo.LocalHash}..{buildInfo.RemoteHash} --pretty=tformat:\"%h|%an|%cI|%s\"");
                if (!string.IsNullOrEmpty(commitData))
                {
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Updates")
                        .WithOkColour(Context.Guild);

                    var entries = commitData.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                    foreach (var data in entries.Select(x => x.Split("|")))
                    {
                        var date = DateTime.Parse(data[2]);
                        var fieldBuilder = new EmbedFieldBuilder()
                        {
                            Name = $"{date:dd-MM-yyyy hh:mm:ss} ({data[0]})",
                            Value = data[3].TrimTo(200),
                        };

                        values.Add(fieldBuilder.Build());
                        embedBuilder.AddField(fieldBuilder);
                    }

                    if (post)
                        await Context.Channel.EmbedAsync(embedBuilder, options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit }).ConfigureAwait(false);
                }
                else if(post)
                {
                    await Context.ApplyResultReaction(CommandResult.SuccessAlt1).ConfigureAwait(false);
                }
            }
            else if(post)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            return values;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(1)]
        public async Task Info(string fromHash = null)
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates(false).ConfigureAwait(false);
                if (buildInfo.LocalHash != null && buildInfo.RemoteHash != null)
                {
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle($"\\{EmotesHelper.GetString(Emotes.HammerPick)} Build Info")
                        .WithOkColour(Context.Guild)
                    ;

                    var updateList = await UpdateList(fromHash, false).ConfigureAwait(false);
                    if (updateList.Count() > 0)
                    {
                        embedBuilder.WithDescription($"You are {updateList.Count()} commits behind.").WithFields(
                                updateList.Select(x => new EmbedFieldBuilder().WithName(x.Name).WithValue(x.Value))
                        );
                    }
                    else
                    {
                        embedBuilder.WithDescription($"Ditto is running on the last available version \\{EmotesHelper.GetString(Emotes.HeavyCheckMark)}");
                    }

                    await Context.Channel.EmbedAsync(embedBuilder, options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit }).ConfigureAwait(false);
                }
                else
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
        }
    }
}
