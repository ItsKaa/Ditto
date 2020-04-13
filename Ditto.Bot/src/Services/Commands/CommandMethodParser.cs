using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Database.Models;
using Ditto.Bot.Services.Commands.Data;
using Ditto.Data.Commands;
using Ditto.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ParseResult = Ditto.Bot.Services.Commands.Data.ParseResult;
using ModuleInfo = Ditto.Bot.Data.Reflection.ModuleInfo;

namespace Ditto.Bot.Services.Commands
{
    public class CommandMethodParser
    {
        public CommandScorer CommandScorer { get; private set; }
        public CommandHandler CommandHandler { get; private set; }
        public CommandMethodParser(CommandHandler commandHandler)
        {
            CommandHandler = commandHandler;
            CommandScorer = new CommandScorer(CommandHandler.CommandConverter);
        }

        /// <summary>
        /// Parses commands that accept the provided <paramref name="input"/> string as the method name and arguments.
        /// </summary>
        /// <returns>A sorted parse result list based on priority and successful parameters.</returns>
        public async Task<IEnumerable<ParseResult>> ParseMethodsAsync(ICommandContextEx context, string input, bool validateInput = true)
        {
            var list = new List<ParseResult>();
            foreach (var module in (context.Guild == null ? CommandHandler.Modules[0] : CommandHandler.Modules[context.Guild.Id]))
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
                            if (parseResult.Method.Accessibility.Has(CommandAccessLevel.Global))
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

                            score = await CommandScorer.GetMethodScoreAndParametersAsync(
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
                        if (validateInput)
                        {
                            if (context.Guild == null && !(parseResult.Method.Source.Has(CommandSourceLevel.DM) || parseResult.Method.Source.Has(CommandSourceLevel.Group)))
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
                        }

                        list.Add(new ParseResult()
                        {
                            InputMessage = parseResult.InputMessage,
                            Method = parseResult.Method,
                            Module = parseResult.Module,
                            Parameters = score.Item2,
                            Score = score.Item1 ?? -1,
                            Priority = parseResult.Priority,
                            ErrorMessage = errorMessage
                        });
                    }
                }
            }

            return GetSortedResults(list);
        }

        private IEnumerable<ParseResult> ParseMethodsInternal(ModuleInfo moduleInfo, string input, ParsingState state)
        {
            var inputItem1 = input?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            var list = new List<ParseResult>();

            // method or module + method
            foreach (var method in moduleInfo.Methods)
            {
                bool found = false;
                var methodNameMatch = method.Aliases.FirstOrDefault(n => inputItem1.Equals(n, StringComparison.OrdinalIgnoreCase));
                if ((method.MethodInfo.Name == "_" || (method.Aliases.Contains("") && methodNameMatch != null)) || !string.IsNullOrWhiteSpace(methodNameMatch))
                {
                    if (method.Accessibility.Has(state == ParsingState.BASE ? CommandAccessLevel.Global : CommandAccessLevel.Parents))
                    {
                        found = true;
                        list.Add(new ParseResult()
                        {
                            InputMessage = input,
                            Method = method,
                            Module = moduleInfo,
                            Priority = method.Priority,
                        });
                    }
                    else
                    {
                        // Assume the class alias for the local "underscore" named methods.
                        if (method.MethodInfo.Name == "_"
                           && state == ParsingState.BASE
                           && method.Accessibility.Has(CommandAccessLevel.Local)
                           && moduleInfo.Aliases.Any(n => n.Equals(inputItem1, StringComparison.CurrentCultureIgnoreCase))
                          )
                        {
                            found = true;
                            list.Add(new ParseResult()
                            {
                                InputMessage = input,
                                Method = method,
                                Module = moduleInfo,
                                Priority = method.Priority
                            });
                        }
                    }
                }

                // try to not get duplicates
                if (!found)
                {
                    // check module level method
                    var moduleNameMatch = moduleInfo.Aliases.FirstOrDefault(n => inputItem1.Equals(n, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(moduleNameMatch))
                    {
                        var inputModule = input.Remove(0, moduleNameMatch.Length).TrimStart(' ');
                        var inputModuleItem1 = inputModule.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                        methodNameMatch = method.Aliases.FirstOrDefault(n => inputModuleItem1.Equals(n, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrWhiteSpace(methodNameMatch))
                        {
                            if (method.Accessibility.Has(CommandAccessLevel.Local))
                            {
                                list.Add(new ParseResult()
                                {
                                    InputMessage = inputModule,
                                    Method = method,
                                    Priority = method.Priority,
                                    Module = moduleInfo,
                                });
                            }

                        }
                    }
                }
            }

            // Find parent module
            var moduleMatch = moduleInfo.Aliases.FirstOrDefault(n => inputItem1.Equals(n, StringComparison.OrdinalIgnoreCase));
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
                var moduleInputItem1 = moduleInput?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                foreach (var submethod in submodule.Methods)
                {
                    var submoduleMatch = submethod.Aliases.FirstOrDefault(n => n.Equals(moduleInputItem1, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(submoduleMatch))
                    {
                        if (submethod.Accessibility.Has(parent ? CommandAccessLevel.Parents : CommandAccessLevel.Global))
                        {
                            list.Add(new ParseResult()
                            {
                                InputMessage = moduleInput,
                                Method = submethod,
                                Module = moduleInfo,
                                Priority = submethod.Priority,
                            });
                        }
                    }
                }

            }
            return list;
        }

        /// <summary>
        /// Returns a sorted collection of the parsed method results based on the priority and parameter count.
        /// </summary>
        private IEnumerable<ParseResult> GetSortedResults(IEnumerable<ParseResult> parseResults)
        {
            // Determine the best possible method
            var parseResultsList = parseResults.ToList();
            parseResultsList.Sort((right, left) => // Reversed order for descending
            {
                // Sort methods named _ to the bottom
                if (left.Method.MethodInfo.Name == "_"
                    && right.Method.MethodInfo.Name != "_")
                {
                    return -1;
                }

                // Sort global _ method below local.
                if (left.Method.MethodInfo.Name == "_"
                    && right.Method.MethodInfo.Name == "_"
                    && left.Method.Accessibility.Has(CommandAccessLevel.Global) && !right.Method.Accessibility.Has(CommandAccessLevel.Global)
                    )
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
            return parseResultsList;
        }
    }
}
