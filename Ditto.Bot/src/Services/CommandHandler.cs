using System.Collections.Generic;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System;
using Discord.Commands;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TB.ComponentModel;
using System.Globalization;
using Discord;
using System.Collections.Immutable;
using Ditto.Data.Discord;
using Ditto.Helpers;
using Ditto.Extensions;
using Ditto.Data.Commands;
using Ditto.Attributes;
using Ditto.Data;
using Ditto.Extensions.Discord;
using Ditto.Bot.Data.Discord;
using Ditto.Bot.Data.Reflection;
using Ditto.Bot.Helpers;
using ModuleInfo = Ditto.Bot.Data.Reflection.ModuleInfo;

namespace Ditto.Bot.Services
{
    public partial class CommandHandler : IDisposable
    {
        private readonly ObjectLock<DiscordClientEx> _discordClient;

        //private static readonly Regex _regexParameterSeperator = new Regex(@"[\""].+?[\""]|[^ ]+", RegexOptions.Compiled);
        private static readonly Regex _regexParameterSeperator = new Regex(@"[\""`]+.+?[\""`]+|[^ ]+", RegexOptions.Compiled);

        private readonly ConcurrentDictionary<Type, TypeReader> _defaultTypeReaders;
        private readonly ImmutableList<Tuple<Type, Type>> _entityTypeReaders;
        private ConcurrentDictionary<ulong, List<ModuleInfo>> _modules = new ConcurrentDictionary<ulong, List<ModuleInfo>>();
        public bool Running { get; private set; } = false;

        public CommandHandler(ObjectLock<DiscordClientEx> client)
        {
            _discordClient = client;
            _defaultTypeReaders = new ConcurrentDictionary<Type, TypeReader>(DiscordHelper.GetDefaultTypeReaders());
            _entityTypeReaders = DiscordHelper.GetDefaultEntityTypeReaders().ToImmutableList();
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
            try { _modules?.Clear(); } catch { }
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
                if (_modules.TryGetValue(0, out List<ModuleInfo> modules))
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
                _modules.TryRemove(guild.Id, out List<ModuleInfo> modules);
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
                _modules.Clear();
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
            _modules.TryRemove(guildId, out List<ModuleInfo> values);
            _modules.TryAdd(guildId, internalModules);
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

                if (userTag != null)
                {
                    content = content.Remove(userTag.Index, userTag.Length).TrimStart();
                    if (content.StartsWith(Ditto.Cache.Db.Prefix(context.Guild)))
                    {
                        content = content.Remove(0, 1);
                    }
                }
                else if (content.StartsWith(Ditto.Cache.Db.Prefix(context.Guild)))
                {
                    content = content.Remove(0, 1);
                }
                else
                {
                    return;
                }


                // Validate our method
                var parseResults = (await ParseMethodsAsync(context, content).ConfigureAwait(false)).ToList();
				
                // Determine the best possible method
                parseResults.Sort((right, left) => // Reversed order for descending
                {
                    // Sort methods named _ to the bottom
                    if (left.Method.MethodInfo.Name == "_"
                        && right.Method.MethodInfo.Name != "_")
                    {
                        return -1;
                    }

                    // Highest priority goes first
                    if (left.Priority > right.Priority)
                    {
                        return left.Priority.CompareTo(right.Priority);
                    }
                    else if (left.Score == right.Score)
                    {
                        // Sort by most parameters parsed.
                        return (left.Parameters?.Count() ?? -1).CompareTo((right.Parameters?.Count() ?? -1));
                    }
                    return left.Score.CompareTo(right.Score);
                });

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
                Log.Error("Error occured at ExecuteMethodAsync()", ex);
            }
        }
        private Task ExecuteMethodAsync(CommandContextEx context, MethodInfo methodInfo, IEnumerable<object> parameters, bool silent = false)
            => ExecuteMethodAsync(context, methodInfo, parameters.ToArray(), silent);


        private async Task<IEnumerable<ParseResult>> ParseMethodsAsync(ICommandContextEx context, string input)
        {
            var list = new List<ParseResult>();
            foreach (var module in (context.Guild == null ? _modules[0] : _modules[context.Guild.Id]))
            {
                var parseResults = ParseMethodsInternal(module, input, ParsingState.BASE);
                foreach (var parseResult in parseResults)
                {
                    if (parseResult != null && parseResult.Method != null && parseResult.InputMessage != null)
                    {
                        var score = new Tuple<int?, List<object[]>>(null, null);
                        string errorMessage = null;
                        try
                        {
                            var commandName = parseResult.InputMessage;
                            //if (parseResult.Method.MethodInfo?.Name == "_")
                            if(parseResult.Method.Accessibility.Has(CommandAccessLevel.Global))
                            {
                                var firstCommand = commandName.Split(' ').FirstOrDefault();
                                if (firstCommand != null && firstCommand.Length > 0)
                                {
                                    var firstAlias = parseResult.Method.Aliases.Where(a => a != "_").FirstOrDefault(e => e.Equals(firstCommand, StringComparison.CurrentCultureIgnoreCase));
                                    if (firstAlias == null)
                                    {
                                        // add underscore
                                        commandName = "_ " + commandName;
                                    }
                                }
                            }

                            score = await GetMethodScoreAndParametersAsync(
                                context,
                                parseResult.Method.MethodInfo,
                                commandName
                            ).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            errorMessage = ex.Message;
                        }

                        // Validate command - part 1: if guild == null, verify that the command accepts non-guild sources
                        if (context.Guild == null
                            && parseResult.Method.Source.Has(CommandSourceLevel.DM)
                            && parseResult.Method.Source.Has(CommandSourceLevel.Group))
                        {
                            continue;
                        }

                        // Validate command - part 2, check the command settings
                        try
                        {
                            if ((await (parseResult.Method.MethodInfo.GetCustomAttribute(typeof(DiscordCommandAttribute)) as DiscordCommandAttribute).VerifyAsync(context)).HasError)
                                throw new Exception();
                        }
                        catch { continue; }

                        list.Add(new ParseResult()
                        {
                            InputMessage = parseResult.InputMessage,
                            Method = parseResult.Method,
                            Parameters = score.Item2,
                            Score = score.Item1 ?? -1,
                            Priority = parseResult.Priority,
                            ErrorMessage = errorMessage
                        });
                    }
                }
            }
            return list;
        }
        private IEnumerable<ParseResult> ParseMethodsInternal(ModuleInfo moduleInfo, string input, ParsingState state)
        {
            var list = new List<ParseResult>();

            // method or module + method
            foreach (var method in moduleInfo.Methods)
            {
                bool found = false;
                var methodNameMatch = method.Aliases.FirstOrDefault(n => input.StartsWith(n, StringComparison.OrdinalIgnoreCase));
                if ((method.MethodInfo.Name == "_" || (method.Aliases.Contains("") && methodNameMatch != null)) || !string.IsNullOrWhiteSpace(methodNameMatch))
                {
                    if (method.Accessibility.Has(state == ParsingState.BASE ? CommandAccessLevel.Global : CommandAccessLevel.Parents))
                    {
                        found = true;
                        list.Add(new ParseResult()
                        {
                            InputMessage = input,
                            Method = method,
                            Priority = method.Priority
                        });
                    }
                }

                // try to not get duplicates
                if (!found)
                {
                    // check module level method
                    var moduleNameMatch = moduleInfo.Aliases.FirstOrDefault(n => input.StartsWith(n, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(moduleNameMatch))
                    {
                        var inputModule = input.Remove(0, moduleNameMatch.Length).TrimStart(' ');
                        methodNameMatch = method.Aliases.FirstOrDefault(n => inputModule.StartsWith(n, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrWhiteSpace(methodNameMatch))
                        {
                            if (method.Accessibility.Has(CommandAccessLevel.Local))
                            {
                                list.Add(new ParseResult()
                                {
                                    InputMessage = inputModule,
                                    Method = method
                                });
                            }

                        }
                    }
                }
            }

            // Find parent module
            var moduleMatch = moduleInfo.Aliases.FirstOrDefault(n => input.StartsWith(n, StringComparison.OrdinalIgnoreCase));
            string moduleInput = input;
            var parent = false;
            if (!string.IsNullOrWhiteSpace(moduleMatch))
            {
                moduleInput = moduleInput.Remove(0, moduleMatch.Length).TrimStart(' ');
                parent = true;
            }

            // Submodule + method a.k.a. "youtube search":
            foreach (var submodule in moduleInfo.SubModules)
            {
                foreach (var name in submodule.Aliases)
                {
                    if (moduleInput.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    {
                        list.AddRange(ParseMethodsInternal(submodule, moduleInput, ParsingState.PARENT));
                    }
                }

                // Submodule.submodule, added at 20-11-2017
                // TODO: issues with "Local" :/
                foreach (var subsubmodule in submodule.SubModules)
                {
                    list.AddRange(ParseMethodsInternal(subsubmodule, moduleInput, ParsingState.BASE));
                }

                // Submodule method
                foreach (var submethod in submodule.Methods)
                {
                    var submoduleMatch = submethod.Aliases.FirstOrDefault(n => moduleInput.StartsWith(n, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(submoduleMatch))
                    {
                        if (submethod.Accessibility.Has(parent ? CommandAccessLevel.Parents : CommandAccessLevel.Global))
                        {
                            list.Add(new ParseResult()
                            {
                                InputMessage = moduleInput,
                                Method = submethod
                            });
                        }
                    }
                }

            }
            return list;
        }

        private async Task<Tuple<int?, List<object[]>>> GetMethodScoreAndParametersAsync(ICommandContext context, MethodInfo methodInfo, string commandString)
        {
            // Parse inputs
            var parameterInfo = methodInfo.GetParameters();
            var commandInputs = _regexParameterSeperator.Matches(commandString)
                .Select(m =>
                {
                    var value = m.Value;
                    if (value.StartsWith("\"")) value = value.Remove(0, 1);
                    if (value.EndsWith("\"")) value = value.Remove(value.Length - 1, 1);
                    //if (value.StartsWith('"') || value.StartsWith('`')) value = value.Remove(0, 1);
                    //if (value.EndsWith('"') || value.EndsWith('`')) value = value.Remove(value.Length - 1, 1);
                    return value;
                })
                .AfterIndex(1) // Take first element = method identifier
                .ToList();

            var objects = new List<object[]>(parameterInfo.Count());
            for (int i = 0; i < parameterInfo.Count(); i++)
            {
                objects.Add(new object[] { null });
            }

            int score = 0;
            
            // Check if we have enough parameters
            var optionalCount = parameterInfo.Sum(x => x.IsOptional);
            if ((parameterInfo.Count() - optionalCount) <= commandInputs.Count())
            {
                // Possible functions:
                // [string,int,int,string] one two 1 2 three four => ["one two", 1, 2, "three four"]
                // [string,int,string] one two 1 2 three four => ["one two 1", 2, "three four"]
                // [string,int] one two 1 2 => ["one two 1", 2]

                // loop parameters, if we encounted a multiword parameter, check the next parameter
                for (int i = 0; i < parameterInfo.Length; i++)
                {
                    var param = parameterInfo[i];
                    if (param.IsDefined(typeof(MultiwordAttribute)))
                    {
                        // check parameters
                        var paramsMultiword = parameterInfo.After(param).Reverse().ToList(); // After or From??
                        if (paramsMultiword.Count == 0)
                        {
                            // append everything to objects
                            objects[i][0] = string.Join(' ', commandInputs.FromIndex(i, true));
                            score += Globals.Command.Score.ParseSuccess;
                        }
                        else
                        {
                            if (paramsMultiword.Any(x => x.IsDefined(typeof(MultiwordAttribute))))
                            {
                                // Multiple "Multiword" attributes detected, use our best guess a.k.a. go with whatever parses first.
                                score += Globals.Command.Score.DoubleMultiword;
                            }
                            //else
                            {
                                // Only one Multiword tag is defined, loop from behind and parse our input values, if its invalid, use optional (or ignore, only for the first one)
                                var inputsMultiword = commandInputs.AsEnumerable().Reverse().ToList();

                                // j => paramMultiword
                                // k => inputMultiword or successfull parses
                                // l => objects[] index, due to parsing fails and/or optionals this can be different from 'k'.
                                for (int j = 0, k = 0, l = 0; j < paramsMultiword.Count(); j++)
                                {
                                    var paramMultiword = paramsMultiword[j];
                                    if (inputsMultiword.Count() < k)
                                    {
                                        // Check if optional, if not: fail.
                                        if (paramMultiword.IsOptional)
                                        {
                                            objects[objects.Count - l - 1][0] = paramMultiword.GetDefaultValue();
                                            score += Globals.Command.Score.Optional;
                                            l++;
                                        }
                                        else
                                        {
                                            throw new ArgumentOutOfRangeException(string.Format("Not enough arguments passed for the method '{0}'", methodInfo.Name));
                                        }
                                    }

                                    // Set our object
                                    // TODO: Parse value, if fail, try the next one
                                    try
                                    {
                                        var inputMultiword = inputsMultiword.ElementAt(k);
                                        objects[objects.Count - l - 1] = await ConvertObjectAsync(context, paramMultiword, inputMultiword).ConfigureAwait(false);
                                        inputsMultiword.RemoveAt(k);
                                        score += Globals.Command.Score.ParseSuccess;
                                        //k++;
                                        l++;
                                    }
                                    catch (Exception ex)
                                    {
                                        // TODO: If optional, try that first?
                                        if (paramMultiword.IsOptional)
                                        {
                                            objects[objects.Count - l - 1][0] = paramMultiword.GetDefaultValue();
                                            score += Globals.Command.Score.Optional;
                                            l++;
                                        }
                                        else
                                        {
                                            // Parsing failed, only ignore the first element in the inputs
                                            score += Globals.Command.Score.ParseFail;
                                            throw new ArgumentException(string.Format("Invalid arguments, {0}", ex));
                                        }
                                    }
                                }

                                // Now fill our multiword value
                                inputsMultiword.Reverse();
                                var inputValues = string.Join(' ', inputsMultiword);
                                objects[i][0] = inputValues;
                                score += Globals.Command.Score.ParseSuccess;
                            }
                        }
                        return new Tuple<int?, List<object[]>>(score, objects);
                    }
                    else
                    {
                        // no multiword, try to parse and fill
                        try
                        {
                            var inputMultiword = commandInputs.ElementAt(0);
                            objects[i] = await ConvertObjectAsync(context, param, inputMultiword).ConfigureAwait(false);
                            commandInputs.RemoveAt(0);
                            score += Globals.Command.Score.ParseSuccess;
                        }
                        catch (Exception ex)
                        {
                            // If available, use the default value.
                            if (param.IsOptional)
                            {
                                objects[i][0] = param.GetDefaultValue();
                                score += Globals.Command.Score.Optional;
                            }
                            else
                            {
                                // Parsing failed, score drop
                                score += Globals.Command.Score.ParseFail;
                                throw ex;
                            }
                        }
                    }
                }
                return new Tuple<int?, List<object[]>>(score, objects);
            }
            else
            {
                // Invalid amount of parameters
                throw new ArgumentException("Invalid parameter count");
            }
        }

        public async Task<object> ConvertObjectAsync(ICommandContext context, Type type, string input)
        {
            var typeReader = GetDefaultTypeReader(type);
            var typeResult = await typeReader.ReadAsync(context, input, null).ConfigureAwait(false);
            if (typeResult.IsSuccess)
            {
                if (typeResult.Values.Count > 1)
                {
                    return typeResult.Values.OrderByDescending(x => x.Score).Select(x => x.Value).ToArray();
                }
                return typeResult.Values.OrderByDescending(a => a.Score).FirstOrDefault().Value;
            }
            // try default
            return UniversalTypeConverter.Convert(input, type, CultureInfo.CurrentCulture, ConversionOptions.Default);
        }

        private async Task<object[]> ConvertObjectAsync(ICommandContext context, System.Reflection.ParameterInfo parameterInfo, string input)
        {
            return new[] { await ConvertObjectAsync(context, parameterInfo.ParameterType, input).ConfigureAwait(false) };
        }

        private TypeReader GetDefaultTypeReader(Type type)
        {
            if (_defaultTypeReaders.TryGetValue(type, out var reader))
                return reader;
            var typeInfo = type.GetTypeInfo();

            //Is this an enum?
            if (typeInfo.IsEnum)
            {
                reader = DiscordHelper.GetTypeReader(type);
                _defaultTypeReaders[type] = reader;
                return reader;
            }

            //Is this an entity?
            for (int i = 0; i < _entityTypeReaders.Count; i++)
            {
                if (type == _entityTypeReaders[i].Item1 || typeInfo.ImplementedInterfaces.Contains(_entityTypeReaders[i].Item1))
                {
                    reader = Activator.CreateInstance(_entityTypeReaders[i].Item2.MakeGenericType(type)) as TypeReader;
                    _defaultTypeReaders[type] = reader;
                    return reader;
                }
            }
            return null;
        }

        internal static MethodInfo FindMethodInfo(ModuleInfo moduleInfo, string commandString)
            => moduleInfo.Methods.FirstOrDefault(a => a.Aliases.Any(n => commandString.StartsWith(n, StringComparison.InvariantCultureIgnoreCase)))?.MethodInfo;
    }





    public partial class CommandHandler
    {
        private class ParseResult
        {
            public string InputMessage { get; set; }
            public string ErrorMessage { get; set; }

            public List<object[]> Parameters { get; set; }
            //public MethodInfo Method { get; set; }
            public ModuleMethod Method { get; set; }
            public int Score { get; set; }
            public int Priority { get; set; }
        }

        private enum ParsingState
        {
            BASE,
            PARENT
        }
    }
}
