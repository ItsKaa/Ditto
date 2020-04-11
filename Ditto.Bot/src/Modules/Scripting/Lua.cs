using Cauldron.Core.Collections;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Ditto.Attributes;
using Ditto.Bot.Modules.Scripting.Data;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions.Discord;
using MoonSharp.Interpreter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Scripting
{
    public class Lua : DiscordModule
    {
        private static ConcurrentDictionary<IGuild, ConcurrentList<LuaScript>> _scripts = new ConcurrentDictionary<IGuild, ConcurrentList<LuaScript>>();
        private static bool Initialized { get; set; } = false;

        static Lua()
        {
            Ditto.Connected += () =>
            {
                // Load lua scripts from directory
                var _ = Ditto.Client.DoAsync(client =>
                {
                    client.MessageReceived += DiscordMessageReceived;
                    client.UserJoined += DiscordUserJoined;
                    client.GuildMemberUpdated += GuildMemberUpdated;
                });


                if (!Initialized)
                {
                    Initialized = true;

                    // Load all files to _script
                    Task.Run(async () =>
                    {
                        foreach (var filePath in Directory.GetFiles($"{Globals.AppDirectory}\\data\\lua", "*.*", SearchOption.AllDirectories))
                        {
                            var guildId = Convert.ToUInt64(Path.GetDirectoryName(filePath).Split("\\").Last());
                            LuaHandler.CreateLuaScript(out LuaScript luaScript, null, null, null);
                            luaScript.Guild = await Ditto.Client.DoAsync(client => client.GetGuild(guildId)).ConfigureAwait(false);
                            luaScript.FileName = Path.GetFileName(filePath);
                            ExecuteMethod(luaScript, LuaScriptMethods.Initialise);
                            LuaHandler.ApplyScriptVariables(luaScript, true);
                            await LuaHandler.RunAsync(luaScript).ConfigureAwait(false);

                            var luaScripts = _scripts.GetOrAdd(luaScript.Guild, new ConcurrentList<LuaScript>());
                            luaScripts.Add(luaScript);
                        }
                    });

                    // Manual events
                    Task.Run(async () =>
                    {
                        var tasks = new ConcurrentDictionary<int, Tuple<Task, DateTime>>();
                        var tickDelay = TimeSpan.FromSeconds(10);

                        var dateStart = DateTime.MinValue;
                        while (Ditto.Running)
                        {
                            dateStart = DateTime.Now;
                            foreach (var scriptPair in _scripts)
                            {
                                foreach (var script in scriptPair.Value)
                                {
                                    if (tasks.TryGetValue(script.Id, out Tuple<Task, DateTime> tuple))
                                    {
                                        if ((tuple.Item1.Status == TaskStatus.RanToCompletion || tuple.Item1.Status == TaskStatus.Canceled || tuple.Item1.Status == TaskStatus.Faulted)
                                            && (dateStart - tuple.Item2) > tickDelay)
                                        {
                                            tasks.TryRemove(script.Id, out tuple);
                                        }
                                    }

                                    if (!tasks.TryGetValue(script.Id, out tuple))
                                    {
                                        tasks.TryAdd(script.Id, new Tuple<Task, DateTime>(Task.Run(() =>
                                        {
                                            ExecuteMethod(script, LuaScriptMethods.Tick);
                                        }), dateStart));
                                    }
                                }
                            }

                            await Task.Delay(250).ConfigureAwait(false);
                        }
                    });
                }
                return Task.CompletedTask;
            };
            Ditto.Exit += () =>
            {
                // Load lua scripts from directory
                return Ditto.Client.DoAsync(client =>
                {
                    if (client != null)
                    {
                        client.MessageReceived -= DiscordMessageReceived;
                        client.UserJoined -= DiscordUserJoined;
                        client.GuildMemberUpdated -= GuildMemberUpdated;
                    }
                });
            };
        }

        private static Task GuildMemberUpdated(SocketGuildUser socketGuildUserOld, SocketGuildUser socketGuildUser)
        {
            var _ = Task.Run(() =>
            {
                var addedRoles = socketGuildUser.Roles.Where(r => socketGuildUserOld.Roles.FirstOrDefault(rr => rr.Id == r.Id) == null);
                var removedRoles = socketGuildUserOld.Roles.Where(r => socketGuildUser.Roles.FirstOrDefault(rr => rr.Id == r.Id) == null);
                foreach (var role in addedRoles)
                {
                    ExecuteMethods(
                        LuaScriptMethods.RoleChanged,
                        socketGuildUser.Guild,
                        user: socketGuildUser,
                        role: role,
                        roleAdded: true
                    );
                }
                foreach (var role in removedRoles)
                {
                    ExecuteMethods(
                        LuaScriptMethods.RoleChanged,
                        guild: socketGuildUser.Guild,
                        user: socketGuildUser,
                        role: role,
                        roleAdded: false
                    );
                }
            });
            return Task.CompletedTask;
        }

        private static Task DiscordUserJoined(SocketGuildUser socketGuildUser)
        {
            var _ = Task.Run(() =>
            {
                ExecuteMethods(
                    LuaScriptMethods.UserJoined,
                    guild: socketGuildUser.Guild,
                    user: socketGuildUser
                );
            });
            return Task.CompletedTask;
        }

        private static Task DiscordMessageReceived(SocketMessage socketMessage)
        {
            var _ = Task.Run(() =>
            {
                if (socketMessage.Channel is ITextChannel textChannel)
                {
                    ExecuteMethods(
                        LuaScriptMethods.MessageReceived,
                        guild: textChannel.Guild,
                        user: socketMessage.Author,
                        user_message: socketMessage as IUserMessage
                    );
                }
            });
            return Task.CompletedTask;
        }

        public Lua()
        {
        }

        public static Closure GetMethodFromScript(LuaScript luaScript, LuaScriptMethods scriptMethod)
        {
            foreach (var value in Enum.GetValues(typeof(LuaScriptMethods)).OfType<LuaScriptMethods>())
            {
                // Enum.Parse
                if (scriptMethod.HasFlag(value))
                {
                    try
                    {
                        var scriptMethodKey = luaScript.Script.Globals.Keys.FirstOrDefault(k =>
                            k.Type == DataType.String && string.Equals(k.String, value.ToString(), StringComparison.CurrentCultureIgnoreCase)
                        );

                        if (scriptMethodKey != null && luaScript.Script.Globals[scriptMethodKey.String] is Closure func)
                        {
                            return func;
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        public static bool ExecuteMethod(LuaScript luaScript, LuaScriptMethods scriptMethods)
        {
            var scriptResult = LuaHandler.Run(luaScript);
            if (scriptResult != null)
            {
                var func = GetMethodFromScript(luaScript, scriptMethods);
                if (func != null)
                {
                    var funcResult = luaScript.Script.Call(func);
                    return true;
                }
            }
            return false;
        }

        public static void ExecuteMethods(
            LuaScriptMethods scriptMethods,
            IGuild guild,
            IUserMessage user_message = null,
            IUser user = null,
            IRole role = null,
            bool roleAdded = false
            )
        {
            if (_scripts.TryGetValue(guild, out ConcurrentList<LuaScript> luaScripts))
            {
                foreach (var luaScript in luaScripts)
                {
                    if (user != null)
                    {
                        luaScript.Lua.User = user;
                    }
                    luaScript.Lua.Guild = guild;
                    luaScript.Lua.UserMessage = user_message;
                    luaScript.Lua.Role = role;
                    luaScript.Lua.RoleAdded = roleAdded;
                    ExecuteMethod(luaScript, scriptMethods);
                }
            }
        }

        private LuaScript Validate(string luaCode, bool createFile)
        {
            return !LuaHandler.CreateLuaScript(out LuaScript luaScript, luaCode, (createFile ? Path.GetRandomFileName() : null), Context)
                ? null
                : luaScript;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Alias("Verify", "Check", "Status", "Compile")]
        public async Task<LuaScript> Validate(string luaCode)
        {
            if (Context.GuildUser?.GuildPermissions.Administrator == false)
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
                return null;
            }

            var luaScript = Validate(luaCode, false);
            await Context.ApplyResultReaction(luaScript != null ? CommandResult.Success : CommandResult.Failed).ConfigureAwait(false);
            return luaScript;
        }

        // >lua link ```Markdown
        //    [...]
        // ```
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        [Alias("Add", "Subscribe", "Create", "->", "=>", ">", "+", "+=")]
        public Task Link([Multiword] string luaCode)
        {
            if (Context.GuildUser?.GuildPermissions.Administrator == false)
            {
                return Task.CompletedTask;
            }

            var luaScript = Validate(luaCode, true);
            if (luaScript != null)
            {
                //luaScript.FileName = Path.GetTempFileName() + ".lua";
                //if(File.Exists(luaScript.FilePath))
                //{
                //    File.Delete(luaScript.FilePath);
                //}
                //await File.WriteAllTextAsync(luaScript.FilePath, luaScript.Code, Encoding.UTF8).ConfigureAwait(false);
                //luaScript.Code = null;
                _scripts.GetOrAdd(Context.Guild, new ConcurrentList<LuaScript>()).Add(luaScript);
            }
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.LocalAndParents)]
        public async Task Run([Multiword] string luaCode)
        {
            if (Context.GuildUser?.GuildPermissions.Administrator == true)
            {
                var luaScript = Validate(luaCode, false);
                ExecuteMethod(luaScript, LuaScriptMethods.Main);
                await Context.ApplyResultReaction(luaScript != null ? CommandResult.Success : CommandResult.Failed).ConfigureAwait(false);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.FailedUserPermission).ConfigureAwait(false);
            }
        }

    }
}
