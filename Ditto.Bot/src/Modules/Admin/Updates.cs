using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data.Discord;
using Ditto.Bot.Database.Models;
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
    public class Updates : DiscordModule
    {
        public class BuildInfo
        {
            public BuildInfo(bool fetch, string localHash, string remoteHash)
            {
                FetchSuccess = fetch;
                LocalHash = localHash;
                RemoteHash = remoteHash;
            }

            public bool FetchSuccess { get; set; }
            public string LocalHash { get; set; }
            public string RemoteHash { get; set; }
            public bool HasUpdates => !string.Equals(LocalHash.Trim(), RemoteHash.Trim(), StringComparison.CurrentCultureIgnoreCase);
        }

        static Updates()
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
                                    var _ = new Updates()
                                    {
                                        Context = new CommandContextEx(client, message as IUserMessage)
                                        {
                                            Channel = channel,
                                            Guild = guild
                                        }
                                    }.Build(); //commitHash
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

        private static Tuple<bool, string> RunGit(string args, bool checkForErrors = true)
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

            bool errorReceived = false;
            var outputBuilder = new StringBuilder();

            process.OutputDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e?.Data))
                {
                    outputBuilder.Append(e.Data)
                                 .Append(Environment.NewLine);
                }
            };
            process.ErrorDataReceived += (o, e) =>
            {
                if (!string.IsNullOrEmpty(e?.Data) && checkForErrors)
                {
                    Log.Debug($"Updates: GIT Error | {e.Data}");
                    errorReceived = true;
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return new Tuple<bool, string>(
                !errorReceived,
                outputBuilder.ToString()
            );
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task<BuildInfo> CheckForUpdates(bool reactions = true)
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                // Git fetch to retrieve all updates
                var fetch = RunGit("fetch");
                if (fetch.Item1)
                {
                    // Compare the local and remote hash.
                    var localHash = RunGit("rev-parse HEAD");
                    var remoteHash = RunGit("rev-parse origin/master");
                    var buildInfo = new BuildInfo(fetch.Item1, localHash.Item2.Trim(), remoteHash.Item2.Trim());

                    if (!buildInfo.HasUpdates && reactions)
                    {
                        await Context.ApplyResultReaction(CommandResult.SuccessAlt1).ConfigureAwait(false);
                    }
                    return buildInfo;
                }
                else
                {
                    // Git fetch failed.
                    Log.Error($"Updates | GIT fetch failed. {fetch.Item2 ?? string.Empty}");
                    if (reactions)
                        await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
            else if (reactions)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }

            return new BuildInfo(false, null, null);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        [Alias("upgrade")]
        public async Task Update()
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates().ConfigureAwait(false);
                if (buildInfo.HasUpdates)
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


                    var pull = RunGit("pull origin master", false);
                    if (pull.Item1)
                    {
                        var startInfo = new ProcessStartInfo()
                        {
                            FileName = "bash",
                            Arguments = $"{Ditto.Settings.Paths.BaseDir}/{Ditto.Settings.Paths.ScriptDir}/Run.{(global::Ditto.Data.BaseClass.IsLinux() ? "sh" : "bat")}",
                            WorkingDirectory = $"{Ditto.Settings.Paths.BaseDir}/{Ditto.Settings.Paths.ScriptDir}",
                            UseShellExecute = false,
                            CreateNoWindow = false,
                        };
                        if (global::Ditto.Data.BaseClass.IsWindows())
                        {
                            startInfo.FileName = "cmd";
                            startInfo.Arguments = "/c " + startInfo.Arguments;
                        }

                        // Start a new instance and close the current process.
                        Log.Debug($"Updating bot...");
                        await Ditto.StopAsync().ConfigureAwait(false);
                        using var process = new Process() { StartInfo = startInfo };
                        process.Start();
                        Program.Close();
                    }
                    else
                    {
                        Log.Error($"Updates | GIT pull failed. {pull.Item2 ?? string.Empty}");
                        await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                    }
                }
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task<IEnumerable<EmbedField>> UpdateList(string fromHash = null, bool post = true)
        {
            var values = new List<EmbedField>();
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates(false).ConfigureAwait(false);
                if (buildInfo.FetchSuccess)
                {
                    var commits = RunGit($"log {fromHash ?? buildInfo.LocalHash}..{buildInfo.RemoteHash} --pretty=tformat:\"%h|%an|%cI|%s\"");
                    if (commits.Item1)
                    {
                        var commitData = commits.Item2;
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
                    }
                }
            }
            else if(post)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
            return values;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Build(string fromHash = null)
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates(false).ConfigureAwait(false);
                if (buildInfo.FetchSuccess)
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
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
        }
    }
}
