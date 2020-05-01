using Cauldron.Core.Collections;
using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Data;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Modules.Admin;
using Ditto.Bot.Modules.Utility.Linking;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility
{
    [Alias("rolemenu")]
    public class RoleMenuUtility : DiscordModule<LinkUtility>
    {
        private static ConcurrentList<Link> _links = new ConcurrentList<Link>();

        static RoleMenuUtility()
        {
            Ditto.Connected += () =>
            {
                Task.Run(async () =>
                {
                    _links.Clear();
                    await Ditto.Database.ReadAsync(uow =>
                    {
                        _links.AddRange(uow.Links.GetAllWithLinks(l => l.Type == LinkType.RoleMenu));
                    }).ConfigureAwait(false);

                    foreach(var link in _links)
                    {
                        await HandleLinkAsync(link).ConfigureAwait(false);
                    }

                });
                return Task.CompletedTask;
            };

            // Empty link handler since we base updates on reactions from the IGuild message.
            LinkUtility.TryAddHandler(LinkType.RoleMenu, (link, channel) => Task.FromResult(Enumerable.Empty<string>()));
        }

        private static async Task<IUserMessage> GetMessageAsync(IGuild guild, ulong messageId, ITextChannel textChannel = null)
        {
            IUserMessage message = null;
            var textChannels = new List<ITextChannel>();
            if (textChannel != null)
            {
                textChannels.Add(textChannel);
            }
            else
            {
                textChannels.AddRange((await guild.GetChannelsAsync().ConfigureAwait(false)).OfType<ITextChannel>());
            }

            await Ditto.Client.DoAsync(async client =>
            {
                foreach (var channel in textChannels)
                {
                    if ((await client.GetPermissionsAsync(channel).ConfigureAwait(false)).ViewChannel)
                    {
                        message = await channel.GetMessageAsync(messageId).ConfigureAwait(false) as IUserMessage;
                        if (message != null)
                            return;
                    }
                }
            }).ConfigureAwait(false);

            return message;
        }

        private static async Task<bool> HandleLinkAsync(Link link)
        {
            IUserMessage message = null;
            if (ulong.TryParse(link.Value, out ulong messageId))
            {
                message = await GetMessageAsync(link.Guild, messageId, link.Channel).ConfigureAwait(false);
            }

            if (message == null)
                return false;

            // Handle message
            Ditto.ReactionHandler.Remove(message);

            var funcReaction = new Func<Discord.WebSocket.SocketReaction, bool, Task>(async (Discord.WebSocket.SocketReaction r, bool added) =>
            {
                try
                {
                    if (r.User.Value?.IsBot != true)
                    {
                        var link = _links.FirstOrDefault(l => ulong.TryParse(l.Value, out ulong value) && value == r.MessageId);
                        if (link != null)
                        {
                            // Use the ID or Unicode value depending on the type.
                            LinkItem linkItem = r.Emote is Emote guildEmote
                                ? link.Links.FirstOrDefault(l => ulong.TryParse(l.Identity.Split('=', System.StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), out ulong value) && value == guildEmote.Id)
                                : link.Links.FirstOrDefault(l => string.Equals(r.Emote.Name, l.Identity.Split('=', System.StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), StringComparison.CurrentCultureIgnoreCase));

                            if (linkItem != null && ulong.TryParse(linkItem.Identity.Split('=', System.StringSplitOptions.RemoveEmptyEntries).LastOrDefault(), out ulong roleId))
                            {
                                var role = link.Guild.GetRole(roleId);
                                if (role != null)
                                {
                                    if (await Permissions.CanBotManageRoles(link.Guild).ConfigureAwait(false))
                                    {
                                        if (r.User.Value is IGuildUser guildUser)
                                        {
                                            try
                                            {
                                                // Add or remove the role from the user.
                                                bool success = false;
                                                if (added)
                                                {
                                                    if (!guildUser.RoleIds.Contains(role.Id))
                                                    {
                                                        await guildUser.AddRoleAsync(role).ConfigureAwait(false);
                                                        success = true;
                                                    }
                                                }
                                                else
                                                {
                                                    if (guildUser.RoleIds.Contains(role.Id))
                                                    {
                                                        await guildUser.RemoveRoleAsync(role).ConfigureAwait(false);
                                                        success = true;
                                                    }
                                                }

                                                // Attempt to send the user a DM message, after setting the roles for better error handling.
                                                if (success)
                                                {
                                                    try
                                                    {
                                                        var dmChannel = await guildUser.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                                                        if (dmChannel != null)
                                                        {
                                                            if (added)
                                                                await dmChannel.SendMessageAsync($"**{link.Guild.Name}**: You have given yourself the role `{role.Name}`!").ConfigureAwait(false);
                                                            else
                                                                await dmChannel.SendMessageAsync($"**{link.Guild.Name}**: You have removed the role `{role.Name}` from yourself!").ConfigureAwait(false);
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Log.Warn($"Role menu: Could not adjust role of user '{r.User.Value.Username}' | {ex}");
                                            }
                                        }
                                        else
                                        {
                                            Log.Warn($"Role menu: Could not find user '{r.User.Value.Username}'.");
                                        }
                                    }
                                    else
                                    {
                                        Log.Warn($"Role menu: Cannot modify roles in guild <{link.Guild.Name}:{link.Guild.Id}>.");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Huh | {ex}");
                }
            });

            return Ditto.ReactionHandler.Add(message, r => funcReaction(r, true), r => funcReaction(r, false));
        }


        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        public async Task New(ulong messageId)
        {
            if (!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if (!(await Permissions.CanBotManageRoles(Context).ConfigureAwait(false)))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            IUserMessage message = await GetMessageAsync(Context.Guild, messageId).ConfigureAwait(false);
            var textChannel = message?.Channel as ITextChannel;

            // Verify message found
            if (message == null || textChannel == null)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }

            // Add link
            var link = await LinkUtility.TryAddLinkAsync(LinkType.RoleMenu, textChannel, messageId.ToString()).ConfigureAwait(false);
            if (link == null)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }

            // Add the link to our collection.
            _links.Add(link);

            // Handle link
            var result = await HandleLinkAsync(link).ConfigureAwait(false);
            await Context.ApplyResultReaction(result == true ? CommandResult.Success : CommandResult.Failed).ConfigureAwait(false);
        }

        [DiscordCommand(CommandSourceLevel.Guild, CommandAccessLevel.Local)]
        public async Task Set(ulong messageId, IEmote emote, IRole role)
        {
            if(!Permissions.IsAdministratorOrBotOwner(Context))
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return;
            }

            if(!(await Permissions.CanBotManageRoles(Context).ConfigureAwait(false)))
            {
                await Context.ApplyResultReaction(CommandResult.FailedBotPermission).ConfigureAwait(false);
                return;
            }

            var link = _links.FirstOrDefault(l => ulong.TryParse(l.Value, out ulong value) && value == messageId);
            if(link == null)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }

            // Verify that the link does not exist yet
            var linkValue = $"{((emote as Emote)?.Id)?.ToString() ?? emote.Name}={role.Id}";

            if(null != link.Links.FirstOrDefault(l => string.Equals(l.Identity, linkValue, StringComparison.CurrentCultureIgnoreCase)))
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }

            link.Links.Add(new LinkItem()
            {
                Identity = linkValue,
                Link = link
            });

            await Ditto.Database.WriteAsync(uow =>
            {
                uow.Links.UpdateRange(link);
            });

            // Find the message and react with said emote.
            var message = await GetMessageAsync(link.Guild, messageId, link.Channel).ConfigureAwait(false);
            if(message != null)
            {
                await message.AddReactionAsync(emote).ConfigureAwait(false);
            }

            await Context.ApplyResultReaction(CommandResult.Success).ConfigureAwait(false);
        }
    }
}
