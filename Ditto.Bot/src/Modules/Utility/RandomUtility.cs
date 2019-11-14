﻿using Discord;
using Ditto.Attributes;
using Ditto.Bot.Data.API.Rest;
using Ditto.Common;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ditto.Bot.Modules.Utility
{
    public class RandomUtility : DiscordModule<Utility>
    {
        public enum QuoteType
        {
            Default,
            Trump,
            Random = Default,
            Any = Random,
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Flip()
        {
            // Flip our coin
            var coin = Randomizer.NewBoolean();
            var coinFileName = coin ? "obverse" : "reverse";
            
            // Get a random base directory in data/images/coins
            var basePath = Path.GetDirectoryName($"{Globals.AppDirectory}/data/images/coins/");
            var directories = Directory.GetDirectories(basePath);
            var rngDirectory = directories[Randomizer.New(0, directories.Length-1)];

            // Get a random file with the appropriate name
            var files = Directory.GetFiles(rngDirectory, $"{coinFileName}.*", SearchOption.AllDirectories).ToList();
            if (files.Count == 0)
            {
                await Context.EmbedAsync(new EmbedBuilder()
                    .WithDescription($"💢 {Context.User.Mention} Something went wrong")
                    .WithErrorColour(Context.Guild)
                ).ConfigureAwait(false);
                return;
            }
            var rngFile = files[Randomizer.New(0, files.Count-1)];

            var fileMsg = await Context.Channel.SendFileAsync(rngFile).ConfigureAwait(false);
            await fileMsg.DeleteAsync();
            await Context.EmbedAsync(new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithDescription($"Flipped **{(coin ? "Heads" : "Tails")}**")
                .WithImageUrl(fileMsg.Attachments.FirstOrDefault()?.Url ?? "")
            ).ConfigureAwait(false);
        }
        
        public async Task NumbersApi(NumbersApi.Result result)
        {
            if (result == null)
            {
                // error
                await Context.EmbedAsync("Please enter a valid number", ContextMessageOption.ReplyWithError).ConfigureAwait(false);
            }
            else
            {
                await Context.EmbedAsync(new EmbedBuilder()
                    .WithTitle($"📊 Numbers: {result.Type}") // Numbers: Trivia
                    .WithDescription(result.Text)
                    , ContextMessageOption.ReplyUser).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task NumberFact(ulong? number = null)
        {
            //use http://numbersapi.com
            await NumbersApi(await Common.NumbersApi.Math(
                number ?? Randomizer.New(0UL, long.MaxValue),
                Common.NumbersApi.NotFoundOption.Ceil,
                false
            ));
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task YearFact(int? number = null)
        {
            //use http://numbersapi.com
            var year = number ?? Randomizer.New(-1225, 2060);
            await NumbersApi(await Common.NumbersApi.Year(year,
                year >= 0 ? Common.NumbersApi.NotFoundOption.Floor : Common.NumbersApi.NotFoundOption.Ceil,
                false
            ));
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task DateFact(uint? day = null, uint? month = null)
        {
            //use http://numbersapi.com
            await NumbersApi(await Common.NumbersApi.Date(
                month ?? Randomizer.New(0U, ushort.MaxValue),
                day ?? Randomizer.New(0, uint.MaxValue),
                Common.NumbersApi.NotFoundOption.Ceil, false
            ));
        }
        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task DateFact(DateTime date)
            => DateFact((uint)date.Day, (uint)date.Month);

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Dog(string masterBreed = "", string subBreed = "")
        {
            var dogApi = new DogApi();
            const int columnLength = 20;

            var url = dogApi.Random(masterBreed, subBreed);
            if (url != null)
            {
                await Context.Channel.SendMessageAsync(
                    $"{Context.User.Mention} {url}"
                ).ConfigureAwait(false);
            }
            else
            {
                var breeds = dogApi.GetBreeds();
                string lines = "";
                for (int i = 0; i < breeds.Count(); i += 3)
                {
                    lines += $"{breeds.ElementAt(i).Key.PadRight(columnLength)}"
                        + $"{(breeds.Count > i + 1 ? breeds.ElementAt(i + 1).Key.PadRight(columnLength) : "")}"
                        + $"{(breeds.Count > i + 2 ? breeds.ElementAt(i + 2).Key : "")}"
                        + "\n"
                    ;
                }
                await Context.EmbedAsync(
                    $"Could not find the specified breed, please use one of the following:\n```Markdown\n{lines}\n```",
                    ContextMessageOption.ReplyWithError
                ).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Cat([Multiword] string query = null)
        {
            var url = new TheCatApi().Random();
            if (url?.IsWellFormedOriginalString() == true)
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} {url}").ConfigureAwait(false);
            }
            else
            {
                await Context.EmbedAsync("An error occured while calling this method.", ContextMessageOption.ReplyWithError);
            }
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task RandomUser()
        {
            // https://randomuser.me
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Praise(IUser target, string reason)
        {
            // http://webknox.com/api#!/jokes/praise_GET
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task Insult(IUser target, string reason)
        {

            // http://webknox.com/api#!/jokes/insult_GET
            return Task.CompletedTask;
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task Joke()
        {
            var joke = new DadJokeApi().Joke();
            await Context.Channel.SendMessageAsync($"{Context.User.Mention} {joke}").ConfigureAwait(false);
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task Quote(QuoteType type = QuoteType.Random, [Multiword] string text = "")
        {
            if (type == QuoteType.Default)
            {
                var quote = new QuoteApi().Quote();
                if (quote != null)
                {
                    await Context.EmbedAsync(new EmbedBuilder()
                        .WithDescription(quote.QuoteText)
                        .WithFooter(quote.QuoteAuthor.Length > 0 ? $"by {quote.QuoteAuthor}" : "")
                    ).ConfigureAwait(false);
                }
                else
                {
                    await Context.EmbedAsync("An error occured while calling this method.", ContextMessageOption.ReplyWithError);
                }
            }
            else if (type == QuoteType.Trump)
            {
                var quote = new TrumpQuoteApi().Quote(text?.Length > 0 ? text : null);
                await Context.EmbedAsync(new EmbedBuilder()
                        .WithDescription(quote)
                        .WithFooter(new EmbedFooterBuilder()
                        {
                            Text = @"by Donald Trump"
                        })
                ).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task TrumpQuote([Multiword] string text = "")
           => Quote(QuoteType.Trump, text);

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task Fortune()
        {
            await Context.Channel.SendMessageAsync(
                $"{Context.User.Mention} {new FortuneCookieApi().Fortune()}"
            ).ConfigureAwait(false);
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task AdorableAvatar()
        {
            // https://api.adorable.io/avatars/face/eyes7/nose1/mouth9/ffaaaa
            return Task.CompletedTask;
        }

    }
}
