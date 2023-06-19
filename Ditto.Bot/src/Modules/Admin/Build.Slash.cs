﻿using Discord.Interactions;
using Ditto.Bot.Modules.Admin.Data;
using Ditto.Data.Discord;
using System.Threading.Tasks;
using System;
using Discord;
using System.Collections.Generic;
using System.Linq;

namespace Ditto.Bot.Modules.Admin
{
    [Group("build", "Commands to handle builds / git")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireOwner]
    public class BuildSlash : DiscordSlashModule
    {
        public BuildSlash(InteractionService interactionService) : base(interactionService)
        {
        }

        private async Task<(bool, BuildInfo)> CheckForUpdates(bool respond = false)
        {
            var result = false;
            var buildInfoUpdates = Build.CheckForUpdates();
            if (buildInfoUpdates.Item2 == null)
            {
                if (respond)
                    await RespondAsync("Failed to execute the git command.", ephemeral: true);
            }
            else if (!buildInfoUpdates.Item1)
            {
                if (respond)
                    await RespondAsync("Branch already up to date.", ephemeral: true);
            }
            else
            {
                result = true;
            }

            return (result, buildInfoUpdates.Item2 ?? new BuildInfo(null, null));
        }

        [SlashCommand("update", "Update the bot to the latest version. (bot owner only)")]
        public async Task Update()
        {
            var buildInfo = await CheckForUpdates(true);
            if (!buildInfo.Item1)
                return;

            if (!buildInfo.Item2.IsEqual && (await List(null, false)).Any())
            {
                await DeferAsync();
                var message = await GetOriginalResponseAsync();
                await Build.Update(buildInfo.Item2, Context.Channel as ITextChannel, message, Context.Guild);
            }
        }

        [SlashCommand("list", "List the changes, if any. (bot owner only)")]
        public async Task<IEnumerable<EmbedField>> List(string fromHash = null, bool post = true)
        {
            var buildInfo = await CheckForUpdates(false);
            var data = (await Build.UpdateList(buildInfo.Item2, fromHash));
            if (post)
            {
                if (data.Item1 == null)
                {
                    await RespondAsync("No changes detected");
                }
                else
                {
                    await RespondAsync(embeds: new [] { data.Item1 });
                }
            }

            return data.Item2 ?? Enumerable.Empty<EmbedField>();
        }

        [SlashCommand("info", "Retrieve the latest build information. (bot owner only)")]
        public async Task Info(string fromHash = null)
        {
            var buildInfo = await CheckForUpdates(true);
            if (!buildInfo.Item1)
                return;

            if (await Build.Info(buildInfo.Item2, Context.Guild, fromHash) is Embed embed)
            {
                await RespondAsync(embed: embed, ephemeral: true, options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit });
            }
            else
            {
                throw new Exception("Failed to execute the git command.");
            }
        }

    }
}