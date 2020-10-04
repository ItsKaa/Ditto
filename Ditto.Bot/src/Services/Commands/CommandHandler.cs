using System.Collections.Generic;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System;
using Discord.Commands;
using System.Collections.Concurrent;
using Discord;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Data.Commands;
using Ditto.Attributes;
using Ditto.Data;
using Ditto.Extensions.Discord;
using Ditto.Bot.Data.Discord;
using Ditto.Bot.Helpers;
using ModuleInfo = Ditto.Bot.Data.Reflection.ModuleInfo;

namespace Ditto.Bot.Services.Commands
{
    public partial class CommandHandler : IDisposable
    {
        private readonly ObjectLock<DiscordClientEx> _discordClient;

        public bool Running { get; private set; }
        public ConcurrentDictionary<ulong, List<ModuleInfo>> Modules { get; private set; }
        public CommandConverter CommandConverter { get; private set; }
        public CommandMethodParser CommandMethodParser { get;set; }

        public CommandHandler(ObjectLock<DiscordClientEx> client)
        {
            _discordClient = client;
            Modules = new ConcurrentDictionary<ulong, List<ModuleInfo>>();
            CommandConverter = new CommandConverter();
            CommandMethodParser = new CommandMethodParser(this);
            Running = false;
        }

        public async Task SetupAsync()
        {
            // Initialize modules
            await ReloadAsync(0).ConfigureAwait(false);
            await LoadModulesAsync().ConfigureAwait(false);
        }
        
        public void Dispose()
        {
            try
            {
                _discordClient?.Do((client) =>
                {
                    if (client != null)
                    {
                        client.MessageReceived -= MessageReceivedHandler;
                        client.JoinedGuild -= JoinedGuildHandler;
                        client.LeftGuild -= LeftGuildHandler;
                    }
                });
            }
            catch { }
            try { Modules?.Clear(); } catch { }
        }

        public async Task StartAsync()
        {
            if (!Running)
            {
                Running = true;
                var time = DateTime.Now;

                // reload all our modules
                await ReloadAsync(null).ConfigureAwait(false);

                await _discordClient.DoAsync((client) =>
                {
                    client.MessageReceived += MessageReceivedHandler;
                    client.JoinedGuild += JoinedGuildHandler;
                    client.LeftGuild += LeftGuildHandler;
                }).ConfigureAwait(false);

                Log.Info("Started after {0:0}ms", (DateTime.Now - time).TotalMilliseconds);
            }
        }

        private async Task LoadModulesAsync(ModuleInfo moduleInfo = null)
        {
            if (moduleInfo == null)
            {
                if (Modules.TryGetValue(0, out List<ModuleInfo> modules))
                {
                    foreach (var module in modules)
                    {
                        await LoadModulesAsync(module);
                    }
                    GC.Collect();
                }
            }
            else
            {
                try
                {
                    // Ignore if the module has declared the attribute [DontAutoLoad]
                    if (null == moduleInfo.Type.GetCustomAttribute<DontAutoLoadAttribute>())
                    {
                        var instance = moduleInfo.Type.CreateInstance() as ModuleBaseClass;
                        instance.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex);
                }

                // Load child modules
                foreach (var submodule in moduleInfo.SubModules)
                {
                    await LoadModulesAsync(submodule);
                }
            }
        }
        

        private async Task JoinedGuildHandler(SocketGuild guild)
        {
            if (guild != null)
            {
                Log.Info($"Joined the guild {guild.Name} ({guild.Id})");
                await ReloadAsync(guild.Id, false).ConfigureAwait(false);
            }
        }

        private async Task LeftGuildHandler(SocketGuild guild)
        {
            if (guild != null)
            {
                Log.Info($"Left the guild {guild.Name} ({guild.Id})");
                Modules.TryRemove(guild.Id, out List<ModuleInfo> modules);
                await Ditto.Database.WriteAsync((uow) =>
                {
                    uow.Configs.Remove(i => i.GuildId == guild.Id);
                    uow.Commands.Remove(i => i.GuildId == guild.Id);
                    uow.Modules.Remove(i => i.GuildId == guild.Id);
                    //uow.Playlists.Remove(i => i.GuildId == guild.Id);
                    uow.Links.Remove(r => r.GuildId == guild.Id);

                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reload our command settings, will determine aliases and allowed sourced/accessibility.
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="fillDatabase">if true, any command that is not in our database will be automatically added, by default this is off and should only be used when manually editing the database.</param>
        public async Task ReloadAsync(IGuild guild, bool fillDatabase = false)
        {
            if (guild == null)
            {
                Modules.Clear();
                await ReloadAsync(0, false).ConfigureAwait(false); // default, for private messages.
                foreach (var g in await _discordClient.DoAsync((client) => client.Guilds))
				{
					await ReloadAsync(g, fillDatabase).ConfigureAwait(false);
				}
            }
            else
            {
                await ReloadAsync(guild.Id, fillDatabase).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reload our command settings, will determine aliases and allowed sourced/accessibility.
        /// </summary>
        /// <param name="guildId">see IGuild.Id</param>
        /// <param name="fillDatabase">if true, any command that is not in our database will be automatically added, by default this is off and should only be used when manually editing the database.</param>
        public async Task ReloadAsync(ulong guildId, bool fillDatabase = false)
        {	
            // Load our modules
            var internalModules = ReflectionHelper.GetModules().ToList();

            // Sort by priority
            internalModules.Sort((right, left) =>
            {
                return left.Priority.CompareTo(right.Priority);
            });

            // Get our database settings
            IEnumerable<Database.Models.Command> commands = null;
            IEnumerable<Database.Models.Module> modules = null;
            await Ditto.Database.ReadAsync((uow) =>
            {
                commands = uow.Commands.GetAll();
                modules = uow.Modules.GetAll();
            });

            // Loop through methods and add all non-existing commands to the database if we're debugging
            var addList = new List<Database.Models.Command>();
            foreach (var method in ReflectionHelper.EnumerateMethods(internalModules))
            {
                var command = commands.FirstOrDefault(a => a.Name == method.GetFullName() && a.GuildId == guildId);
                if (command != null)
                {
                    method.Accessibility = command.AccessLevel;
                    method.Source = command.SourceLevel;
                    method.Priority = command.Priority;
                    method.Aliases = command.Aliases.ToList();
                }
                else
                {
                    if (fillDatabase)
                    {
                        addList.Add(new Database.Models.Command()
                        {
                            GuildId = guildId,
                            Name = method.GetFullName(),
                            Aliases = method.Aliases,
                            Priority = method.Priority,
                            AccessLevel = method.Accessibility,
                            SourceLevel = method.Source,
                            Enabled = true
                        });
                    }
                }
            }
            if (addList.Count > 0)
            {
                await Ditto.Database.WriteAsync(uow =>
                {
                    uow.Commands.AddRange(addList);
                });
            }
            Modules.TryRemove(guildId, out List<ModuleInfo> values);
            Modules.TryAdd(guildId, internalModules);
        }

        private Task MessageReceivedHandler(SocketMessage socketMessage)
        {
            var _ = Task.Run(async () =>
            {	
                var userMessage = socketMessage as SocketUserMessage;
                var content = userMessage?.Content?.TrimStart();
                if (socketMessage == null || userMessage == null || socketMessage.Author == null || socketMessage.Author.IsBot || !Ditto.Running)
                    return;
                
                // Create our context
                var context = await _discordClient.DoAsync((client) => new CommandContextEx(client, userMessage)).ConfigureAwait(false);
                var userTag = await _discordClient.DoAsync((client) =>
                    content.ParseDiscordTags().FirstOrDefault(x => x.IsSuccess
                        && x.Type == DiscordTagType.USER
                        && x.Id == client.CurrentUser?.Id)
                );

                var startsWithPrefix = content.StartsWith(Ditto.Cache.Db.Prefix(context.Guild));
                if (userTag != null && !startsWithPrefix)
                {
                    content = content.Remove(userTag.Index, userTag.Length).TrimStart();
                    if (content.StartsWith(Ditto.Cache.Db.Prefix(context.Guild)))
                    {
                        content = content.Remove(0, 1);
                    }
                }
                else if (startsWithPrefix)
                {
                    content = content.Remove(0, 1);
                }
                else
                {
                    return;
                }


                // Validate our method
                var parseResults = (await CommandMethodParser.ParseMethodsAsync(context, content).ConfigureAwait(false)).ToList();

                // Get the one without an error
                var parseResult = parseResults.FirstOrDefault(a => string.IsNullOrEmpty(a.ErrorMessage));
                if (parseResult != null)
                {
                    // check if there are multiple options and ask for input
                    var params2d = parseResult.Parameters;
                    var params1d = params2d.GetColumn(0).ToList();
                    if (params2d.ColumnCount() > 1)
                    {
                        for (int i = 0; i < params2d.Count; i++)
                        {
                            var row = params2d[i];
                            if (row.Length > 1)
                            {
                                // Request user input
                                var options = new List<string>(row.Length);
                                foreach (var col in row)
                                {
                                    if (col != null)
                                        options.Add(col.ToString());
                                }

                                // Select one
                                var resultId = await context.SendOptionDialogueAsync("Please select one of the following items by adding a reaction with the number of your choice.",
                                    options,
                                    true, null, null,
                                    30000
                                ).ConfigureAwait(false);
                                if (resultId < 0)
                                    return;
                                params1d[i] = row[resultId - 1];
                            }
                        }
                    }

                    await ExecuteMethodAsync(context, parseResult.Method.MethodInfo, params1d).ConfigureAwait(false);
                }
                else if (parseResults.Count > 0)
                {
                    // Log our error message
                    var firstError = parseResults.FirstOrDefault()?.ErrorMessage;
                    if (!string.IsNullOrEmpty(firstError))
                    {
                        Log.Error(firstError);
                    }
                }
            });
            return Task.CompletedTask;
        }

        private async Task ExecuteMethodAsync(CommandContextEx context, MethodInfo methodInfo, object[] parameters, bool silent = false)
        {
            if (parameters == null)
                parameters = new object[] { };
            if (methodInfo == null)
                return;

            try
            {
                if (methodInfo.DeclaringType.CreateInstance() is ModuleBaseClass instance)
                {
                    instance.Context = context;
                    var @return = methodInfo.Invoke(instance, parameters);
                    if (@return is Task)
                    {
                        await ((Task)@return).ConfigureAwait(false);
                    }
                    var cmdAttribute = methodInfo.GetCustomAttribute<DiscordCommandAttribute>();
                    if (cmdAttribute?.DeleteUserMessage == true)
                    {
                        await context.Message.DeleteAfterAsync(cmdAttribute?.DeleteUserMessageTimer ?? TimeSpan.Zero);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    Log.Error("Error occurred at ExecuteMethodAsync()", ex);
                }
            }
        }

        private Task ExecuteMethodAsync(CommandContextEx context, MethodInfo methodInfo, IEnumerable<object> parameters, bool silent = false)
            => ExecuteMethodAsync(context, methodInfo, parameters.ToArray(), silent);

        internal static MethodInfo FindMethodInfo(ModuleInfo moduleInfo, string commandString)
            => moduleInfo.Methods.FirstOrDefault(a => a.Aliases.Any(n => commandString.StartsWith(n, StringComparison.InvariantCultureIgnoreCase)))?.MethodInfo;
    }
}
