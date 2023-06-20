﻿using Discord;
using Ditto.Bot.Database.Models;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions.Discord;
using Ditto.Extensions;
using System;
using System.Linq;
using System.Diagnostics;
using System.Text;
using Ditto.Bot.Modules.Admin.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ditto.Helpers;
using Ditto.Data;

namespace Ditto.Bot.Modules.Admin
{
    public class Build : DiscordModule
    {
        private const string RepositoryAlias = "origin";
        //private const string BranchName = "master";
        private const string BranchName = "slash-commands";
        private const string Branch = RepositoryAlias + "/" + BranchName;

        static Build()
        {
            // Check for a link update message
            Ditto.Connected += async () =>
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
                            IUserMessage message = null;
                            IGuild guild = null;
                            ITextChannel channel = null;

                            guild = Ditto.Client.GetGuild(link.GuildId);
                            channel = await guild.GetTextChannelAsync(link.ChannelId).ConfigureAwait(false);
                            message = await channel.GetMessageAsync(messageId) as IUserMessage;
                            if (message != null && message.Author.Id != Ditto.Client.CurrentUser.Id)
                            {
                                await message.SetResultAsync(CommandResult.Success).ConfigureAwait(false);
                            }

                            // Post build info in a secondary task
                            try
                            {
                                var buildInfo = CheckForUpdates();
                                var _ = Info(buildInfo.Item2 ?? new BuildInfo(null, null), guild).ContinueWith(async embedTask
                                    =>
                                    {
                                        var embed = await embedTask;
                                        if (message.Author.IsBot && message.Author.Id == Ditto.Client.CurrentUser.Id)
                                        {
                                            await message.ModifyAsync(x =>
                                            {
                                                x.Content = "";
                                                x.Embed = embed;
                                                x.Flags = MessageFlags.None;
                                            });
                                        }
                                        else
                                        {
                                            await channel.SendMessageAsync(embed: embed);
                                        }
                                    }
                                );
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }

                            // Remove link
                            await Ditto.Database.DoAsync(uow =>
                            {
                                uow.Links.Remove(link);
                            }, true).ConfigureAwait(false);

                        }
                    }
                }
            };
        }

        public static string RunGit(string args)
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

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                throw;
            }

            return outputBuilder.ToString();
        }

        public static (bool, BuildInfo?) CheckForUpdates()
        {
            // Git fetch to retrieve all updates
            RunGit("fetch");

            // Compare the local and remote hash.
            var localHash = RunGit("rev-parse HEAD");
            var remoteHash = RunGit($"rev-parse {Branch}");
            if (!string.IsNullOrEmpty(localHash) && !string.IsNullOrEmpty(remoteHash))
            {
                var buildInfo = new BuildInfo(localHash.Trim(), remoteHash.Trim());
                if (buildInfo.LocalHash == null || buildInfo.RemoteHash == null)
                {
                    return (false, null);
                }
                else if (buildInfo.IsEqual)
                {
                    // Branch is already up to date
                    return (true, buildInfo);
                }
                else
                {
                    return (true, buildInfo);
                }
            }

            return (false, null);
        }

        public static async Task<(Embed, IEnumerable<EmbedField>)> UpdateList(BuildInfo buildInfo, string fromHash = null, ITextChannel textChannel = null)
        {
            var commitData = RunGit($"log {fromHash ?? buildInfo.LocalHash}..{buildInfo.RemoteHash} --pretty=tformat:\"%h|%an|%cI|%s\"");
            if (!string.IsNullOrEmpty(commitData))
            {
                var values = new List<EmbedField>();
                var embedBuilder = new EmbedBuilder()
                    .WithTitle("Updates");
                if (textChannel?.Guild != null)
                {
                    embedBuilder = embedBuilder.WithOkColour(textChannel.Guild);
                }

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

                if (textChannel != null)
                    await textChannel.EmbedAsync(embedBuilder, options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit }).ConfigureAwait(false);

                return (embedBuilder.Build(), values);
            }

            return (null, Enumerable.Empty<EmbedField>());
        }

        public static async Task<Embed> Info(BuildInfo buildInfo, IGuild guild, string fromHash = null)
        {
            if (buildInfo.LocalHash != null && buildInfo.RemoteHash != null)
            {
                var embedBuilder = new EmbedBuilder()
                    .WithTitle($"\\{EmotesHelper.GetString(Emotes.HammerPick)} Build Info")
                    .WithOkColour(guild)
                ;

                var updateList = (await UpdateList(buildInfo, fromHash)).Item2;
                if (updateList.Any())
                {
                    embedBuilder.WithDescription($"You are {updateList.Count()} commits behind.").WithFields(
                            updateList.Select(x => new EmbedFieldBuilder().WithName(x.Name).WithValue(x.Value))
                    );
                }
                else
                {
                    embedBuilder.WithDescription($"Ditto is running on the last available version \\{EmotesHelper.GetString(Emotes.HeavyCheckMark)}");
                }

                return embedBuilder.Build();
            }

            return null;
        }

        public static async Task Update(BuildInfo buildInfo, ITextChannel channel, IUserMessage userMessage, IGuild guild)
        {
            // Add a database link containing the current local branch.
            await Ditto.Database.DoAsync((uow) =>
            {
                var link = uow.Links.GetOrAdd((link) => link.Type == Database.Data.LinkType.Update, new Database.Models.Link()
                {
                    Type = Database.Data.LinkType.Update,
                    ChannelId = channel.Id,
                    GuildId = guild.Id,
                    Date = DateTime.Now,
                    Value = $"{userMessage.Id}|{buildInfo.LocalHash}",
                });

                link.ChannelId = channel.Id;
                link.GuildId = guild.Id;
                link.Date = DateTime.Now;
                link.Value = $"{userMessage.Id}|{buildInfo.LocalHash}";
            }, true).ConfigureAwait(false);


            RunGit($"pull {RepositoryAlias} {BranchName}");
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

    }
}
