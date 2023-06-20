using Discord;
using Discord.Interactions;
using Ditto.Bot.Services;
using Ditto.Data.Discord;
using System;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Admin
{
    [Group("admin", "Administrative commands")]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public class AdminSlash : DiscordBaseSlashModule
    {
        public AdminService AdminService { get; }
        public AdminSlash(InteractionService interactionService, AdminService adminService) : base(interactionService)
        {
            AdminService = adminService;
        }

        [SlashCommand("disconnect", "Trigger a disconnect of the bot (bot owner only)")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireOwner]
        public async Task Disconnect()
        {
            await RespondAsync("Disconnecting...");
            var responseMessageId = (await Context.Interaction.GetOriginalResponseAsync())?.Id ?? ulong.MaxValue;

            Func<Task> disconnectAction = null;
            disconnectAction = new Func<Task>(async () =>
            {
                try
                {
                    if (await Ditto.Client.GetChannelAsync(Context.Channel.Id) is ITextChannel textChannel
                        && await textChannel.GetMessageAsync(responseMessageId) is IUserMessage userMessage)
                    {
                        await userMessage.ModifyAsync(msg => msg.Content = "Reconnected!");
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to reply during reconnection on Disconnect command", ex);
                }

                Ditto.Connected -= disconnectAction;
            });
            Ditto.Connected += disconnectAction;
            await AdminService.Disconnect();
        }

        [SlashCommand("cache-channel", "Set the global cache channel for the bot for temporary files (bot owner only)")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireOwner]
        public async Task CacheChannel(ITextChannel textChannel)
        {
            await AdminService.SetGlobalCacheChannel(textChannel);
            await RespondAsync($"Changed the global chat channel to {textChannel?.Mention}", ephemeral: true);
        }

        [SlashCommand("debug-log", "Set the debug logging state (bot owner only)")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireOwner]
        public Task DebugLogging(bool enable)
        {
            AdminService.DebugLogging(enable);
            return RespondAsync($"{(enable ? "Enabled" : "Disabled")} debug logging.", ephemeral: true);
        }
    }
}
