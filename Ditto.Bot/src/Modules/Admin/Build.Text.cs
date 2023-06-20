using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Modules.Admin.Data;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    [Alias("build")]
    public class BuildText : DiscordTextModule
    {
        public GitService GitService { get; }

        public BuildText(DatabaseCacheService cache, DatabaseService database, GitService gitService) : base(cache, database)
        {
            GitService = gitService;
        }


        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(0)]
        public Task _(int headPosition = 0)
        {
            return Info(headPosition);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(1)]
        public async Task<BuildInfo> CheckForUpdates(bool reactions = true)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return new BuildInfo(null, null);
            }

            var buildInfoUpdates = GitService.CheckForUpdates();
            if (buildInfoUpdates.Item2 == null)
            {
                // Something went wrong with the git command
                if (reactions)
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
            else if (!buildInfoUpdates.Item1)
            {
                // Branch already up to date
                if (reactions)
                    await Context.ApplyResultReaction(CommandResult.SuccessAlt1).ConfigureAwait(false);
            }

            return buildInfoUpdates.Item2 ?? new BuildInfo(null, null);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(1)]
        [Alias("upgrade")]
        public async Task Update()
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates();
                if (!buildInfo.IsEqual && (await List(null, false)).Any())
                {
                    await GitService.Update(buildInfo, Context.TextChannel, Context.Message, Context.Guild);
                }
                else
                {
                    await Context.ApplyResultReaction(CommandResult.SuccessAlt1);
                }
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Alias("list")]
        [Priority(1)]
        public async Task<IEnumerable<EmbedField>> List(string fromHash = null, bool post = true)
        {
            var values = new List<EmbedField>();
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates(false);
                values = (await GitService.UpdateList(buildInfo, fromHash, post ? Context.TextChannel : null)).Item2?.ToList();
                if (values == null)
                {
                    await Context.ApplyResultReaction(CommandResult.SuccessAlt1);
                }
            }
            else if(post)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission);
            }

            return values;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Priority(1)]
        public async Task Info(int headPosition = 0)
        {
            if (Permissions.IsAdministratorOrBotOwner(Context))
            {
                var buildInfo = await CheckForUpdates(false).ConfigureAwait(false);
                if (await GitService.Info(buildInfo, Context.Guild, $"HEAD~{Math.Max(0, headPosition)}") is Embed embed)
                {
                    await Context.Channel.SendMessageAsync(embed: embed, options: new RequestOptions() { RetryMode = RetryMode.RetryRatelimit });
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
