using Discord.Commands;
using Ditto.Attributes;
using Ditto.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ditto.Bot.Services.Commands
{
    public class CommandScorer
    {
        public CommandConverter CommandConverter { get; private set; }
        public CommandScorer(CommandConverter commandConverter)
        {
            CommandConverter = commandConverter;
        }

        public async Task<Tuple<int?, List<object[]>>> GetMethodScoreAndParametersAsync(ICommandContext context, MethodInfo methodInfo, string commandString)
        {
            // Parse inputs
            var parameterInfo = methodInfo.GetParameters();
            var commandInputs = Globals.RegularExpression.CommandParameterSeperator.Matches(commandString)
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
                            objects[i][0] = string.Join(' ', commandInputs.FromIndex(i, false));
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
                                var inputsMultiword = commandInputs.FromIndex(i, false).AsEnumerable().Reverse().ToList();

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
                                        objects[objects.Count - l - 1] = await CommandConverter.ConvertObjectAsync(context, paramMultiword, inputMultiword).ConfigureAwait(false);
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
                            //var inputMultiword = commandInputs.ElementAt(0);
                            var inputMultiword = commandInputs.ElementAt(i);
                            objects[i] = await CommandConverter.ConvertObjectAsync(context, param, inputMultiword).ConfigureAwait(false);
                            //commandInputs.RemoveAt(0);
                            score += Globals.Command.Score.ParseSuccess;
                        }
                        catch (Exception)
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
                                throw;
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
    }
}
