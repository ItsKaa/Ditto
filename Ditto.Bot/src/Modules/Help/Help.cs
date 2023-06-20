using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Services;
using Ditto.Data.Commands;
using Ditto.Extensions;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Help
{
    [Alias("description", "desc", "describe", "explain", "detail", "details", "info", "h", "?")]
    public sealed class Help : DiscordTextModule
    {
        public Help(DatabaseCacheService cache, DatabaseService database) : base(cache, database)
        {
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Local)]
        public async Task _([Multiword] string text = "")
        {
            var parseResults = (await Bot.Ditto.CommandHandler.CommandMethodParser.ParseMethodsAsync(Context, text, false).ConfigureAwait(false))
                .Where(r => !(r.Method.MethodInfo.Name == "_" && r.Method.Accessibility.Has(CommandAccessLevel.Global))) // Remove global Helper class method.
                .ToList();

            if (parseResults.Count == 0)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }

            // Display help
            // TODO: Display paged list, currently using first element
            var result = parseResults.FirstOrDefault();
            var embedBuilder = new EmbedBuilder().WithAuthor($"[Help]");

            var methodName = result.Method.ToString();
            var methodDescription = string.Empty;

            var methodHelpAttribute = result.Method.MethodInfo.GetCustomAttributes<HelpAttribute>()?.FirstOrDefault();
            if (methodHelpAttribute != null)
            {
                methodDescription = methodHelpAttribute.LongDescription ?? methodHelpAttribute.ShortDescription;
            }

            var moduleNameString = result.Module.GetHelpNameString(" > ");
            embedBuilder.Author = new EmbedAuthorBuilder().WithName($"[Help] {moduleNameString} > {methodName}");
            embedBuilder.Description = methodDescription;
            embedBuilder.Fields = new System.Collections.Generic.List<EmbedFieldBuilder>();

            var parameters = result.Method.MethodInfo.GetParameters().ToList();
            foreach(var parameterInfo in parameters)
            {
                var paramHelpAttribute = parameterInfo.GetCustomAttributes<HelpAttribute>()?.FirstOrDefault();
                if(paramHelpAttribute != null)
                {
                    // Generate info based on attribute information
                    embedBuilder.Fields.Add(new EmbedFieldBuilder()
                        .WithName($"`{paramHelpAttribute.Name.ToTitleCase()}`{((paramHelpAttribute.IsOptional || parameterInfo.IsOptional) ? " (optional)" : "")}")
                        .WithValue(paramHelpAttribute.ShortDescription ?? paramHelpAttribute.LongDescription)
                        .WithIsInline(false)
                    );

                    if (!string.IsNullOrEmpty(paramHelpAttribute.Extra))
                    {
                        embedBuilder.Fields.Add(new EmbedFieldBuilder()
                            .WithName(ParseExtraString(paramHelpAttribute.Extra, parameterInfo))
                            .WithValue(Globals.Character.HiddenSpace)
                            .WithIsInline(false)
                        );
                    }
                }
                else
                {
                    // Auto generate based on types and parameter name
                    embedBuilder.Fields.Add(new EmbedFieldBuilder()
                        .WithName($"`{parameterInfo.Name}`{(parameterInfo.IsOptional ? " (optional)" : "")}")
                        .WithValue(GetHelpString(parameterInfo.ParameterType))
                        .WithIsInline(false)
                    );

                    var extra = GetHelpString(parameterInfo.ParameterType, true);
                    if (!string.IsNullOrEmpty(extra))
                    {
                        embedBuilder.Fields.Add(new EmbedFieldBuilder()
                            .WithName(extra)
                            .WithValue(Globals.Character.HiddenSpace)
                            .WithIsInline(false)
                        );
                    }
                }
            }

            await Context.EmbedAsync(embedBuilder).ConfigureAwait(false);
        }

        public string GetHelpString(Type type, bool extra = false)
        {
            if(type == typeof(IRole))
            {
                if (extra)
                    return "e.g.: @Members";
                else
                    return "Mentionable discord role.";
            }
            else if(type == typeof(IUser))
            {
                if (extra)
                    return "e.g.: @Kaa#2195";
                else
                    return "Mentionable discord user.";
            }

            return (extra ? null : Globals.Character.HiddenSpace);
        }

        public string ParseExtraString(string extraString, System.Reflection.ParameterInfo parameterInfo)
        {
            if(parameterInfo.ParameterType.IsEnum)
            {
                extraString = extraString.Replace("%values%", string.Join(", ", parameterInfo.ParameterType.GetEnumNames().Select(n => n.ToLower())));
            }

            return extraString;
        }
    }
}
