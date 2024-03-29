﻿using Discord.Commands;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Translation;
using Ditto.Translation.Attributes;
using Ditto.Translation.Data;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Translation
{
    [Alias("translation")]
    public class TranslationModule : DiscordModule
    {
        public static Task<TranslationResult> Translate(Language sourceLanguage, Language targetLanguage, string input)
        {
            var translator = new GoogleTranslator();
            return translator.TranslateAsync(input, sourceLanguage, targetLanguage);
        }

        private async Task ProcessTranslation(Language sourceLanguage, Language targetLanguage, string input)
        {
            if (sourceLanguage == null || targetLanguage == null)
            {
                await Context.ApplyResultReaction(CommandResult.InvalidParameters).ConfigureAwait(false);
            }
            else
            {
                var result = await Translate(sourceLanguage, targetLanguage, input).ConfigureAwait(false);
                if (result != null)
                {
                    await Context.TextChannel.SendMessageAsync(result.MergedTranslation).ConfigureAwait(false);
                }
                else
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
        }


        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.LocalAndParents, deleteUserMessage: true)]
        [Alias("tl"), Priority(2)]
        public async Task Translate(Languages sourceLanguage, Languages targetLanguage, [Multiword] string input)
        {
            var fromLanguage = GoogleTranslator.GetLanguageByName(sourceLanguage.GetAttribute<LanguageFullNameAttribute>().FullName);
            var toLanguage = GoogleTranslator.GetLanguageByName(targetLanguage.GetAttribute<LanguageFullNameAttribute>().FullName);

            await ProcessTranslation(fromLanguage, toLanguage, input).ConfigureAwait(false);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.Global | CommandAccessLevel.LocalAndParents, deleteUserMessage: true)]
        [Alias("tl"), Priority(1)]
        public async Task Translate(string sourceLanguageISO, string targetLanguageISO, [Multiword] string input)
        {
            var fromLanguage = GoogleTranslator.GetLanguageByISO(sourceLanguageISO);
            if (fromLanguage == null)
            {
                fromLanguage = GoogleTranslator.GetLanguageByName(sourceLanguageISO);
            }

            var toLanguage = GoogleTranslator.GetLanguageByISO(targetLanguageISO);
            if (toLanguage == null)
            {
                toLanguage = GoogleTranslator.GetLanguageByName(targetLanguageISO);
            }

            await ProcessTranslation(fromLanguage, toLanguage, input).ConfigureAwait(false);
        }
    }
}
