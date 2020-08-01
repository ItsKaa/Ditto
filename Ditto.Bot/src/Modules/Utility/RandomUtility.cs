using Discord;
using Discord.Commands;
using Ditto.Attributes;
using Ditto.Bot.Data.API.Rest;
using Ditto.Bot.Helpers;
using Ditto.Common;
using Ditto.Data.Commands;
using Ditto.Data.Discord;
using Ditto.Extensions;
using Ditto.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
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
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
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
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
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
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);

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
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
        }
        
        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task Joke()
        {
            var joke = new DadJokeApi().Joke();
            if (!string.IsNullOrEmpty(joke))
            {
                await Context.Channel.SendMessageAsync($"{Context.User.Mention} {joke}").ConfigureAwait(false);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
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
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
            else if (type == QuoteType.Trump)
            {
                var quote = new TrumpQuoteApi().Quote(text?.Length > 0 ? text : null);
                if (!string.IsNullOrEmpty(quote))
                {
                    await Context.EmbedAsync(new EmbedBuilder()
                            .WithDescription(quote)
                            .WithFooter(new EmbedFooterBuilder()
                            {
                                Text = @"by Donald Trump"
                            })
                    ).ConfigureAwait(false);
                }
                else
                {
                    await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                }
            }
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public Task TrumpQuote([Multiword] string text = "")
           => Quote(QuoteType.Trump, text);

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task Fortune()
        {
            var fortune = new FortuneCookieApi().Fortune();
            if (!string.IsNullOrEmpty(fortune))
            {
                await Context.Channel.SendMessageAsync(
                    $"{Context.User.Mention} {fortune}"
                ).ConfigureAwait(false);
            }
            else
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
            }
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        public async Task AdorableAvatar()
        {
            var avatarUrl = new AdorableApi().RandomAvatar();
            await Context.Channel.EmbedAsync(
                new EmbedBuilder()
                .WithImageUrl(avatarUrl.ToString())
                .WithAuthor(Context.User)
            ).ConfigureAwait(false);
        }

        [DiscordCommand(CommandSourceLevel.Group | CommandSourceLevel.Guild, CommandAccessLevel.All)]
        [Alias("thonk")]
        public async Task Thonkify([Multiword] string text)
        {
            var tracking = new System.Drawing.Bitmap(ImageHelper.Base64ToImage("iVBORw0KGgoAAAANSUhEUgAAAAYAAAOACAYAAAAZzQIQAAAALElEQVR4nO3BAQ0AAADCoPdPbQ8HFAAAAAAAAAAAAAAAAAAAAAAAAAAAAPwZV4AAAfA8WFIAAAAASUVORK5CYII="));

            var images = new List<System.Drawing.Bitmap>();
            try
            {
                foreach (var c in text)
                {
                    var filePath = "data/images/thonkify/" + ((c >= 'A' && c <= 'Z') ? $"letters/{c}cap" : (c >= 'a' && c <= 'z') ? $"letters/{c.ToString().ToUpper()}low" : "misc/nothing");
                    if (File.Exists(filePath + ".png"))
                    {
                        filePath += ".png";
                    }
                    else if (File.Exists(filePath + ".gif"))
                    {
                        filePath += ".gif";
                    }
                    else
                    {
                        continue;
                    }

                    images.Add(new System.Drawing.Bitmap(filePath));
                    images.Add(tracking);
                }

                using var bmp = ImageHelper.CombineBitmap(images, System.Drawing.Color.Transparent);
                using var ms = new MemoryStream();

                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                // Ensure the stream is at the beginning, very important otherwise discord will send a 0kb file.
                ms.Position = 0;

                await Context.Channel.SendFileAsync(ms, $"thonkify.png").ConfigureAwait(false);
            }
            catch { }
            finally
            {
                foreach (var image in images)
                {
                    image.Dispose();
                }
                tracking.Dispose();
            }
        }

        [Priority(1), DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        [Help(null, "Cut a GIF file in parts.")]
        public async Task ChunkifyGif(
            [Help("url", "The url of the file, optional when a file is attached to the message.", optional: true)]
            string sourceUrl,
            [Help("count", "The number to \"cut\", meaning the column and row count", "Examples: 2 = 2x2, 5 = 5x5.")]
            int chunkCount,
            [Help("size", "The maximum width or height of the result.", optional: true)]
            int maxChunkSize,
            [Help("strechToFit", "Stretch the image (width/height) to the designated maxChunkSize, defaults to true", optional: true)]
            bool strechToFit = true
            )
        {
            SixLabors.ImageSharp.Image originalGifImage = null;
            try
            {
                // Read the image file from the network.
                Stream inputStream = await WebHelper.GetStreamAsync(sourceUrl).ConfigureAwait(false);

                // Convert to MemoryStream because this image class doesn't like HttpBaseStreams
                using (var ms = new MemoryStream())
                {
                    inputStream.CopyTo(ms);
                    ms.Position = 0;
                    originalGifImage = SixLabors.ImageSharp.Image.Load(ms);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex);
            }

            if (originalGifImage == null || originalGifImage.Frames.Count == 0)
            {
                await Context.ApplyResultReaction(CommandResult.Failed);
                return;
            }


            var streams = new List<Stream>();
            var chunkSourceSize = new Size(Convert.ToInt32(originalGifImage.Width / chunkCount), Convert.ToInt32(originalGifImage.Height / chunkCount));
            var chunkDestSize = chunkSourceSize;
            if (maxChunkSize != int.MinValue)
            {
                if (!strechToFit)
                {
                    double multiplier = Convert.ToDouble(Math.Max(chunkSourceSize.Width, chunkSourceSize.Height)) / Math.Min(chunkSourceSize.Width, chunkSourceSize.Height);
                    var minValue = Convert.ToInt32(maxChunkSize / multiplier);

                    chunkDestSize = chunkSourceSize.Width > chunkSourceSize.Height
                        ? new Size(maxChunkSize, minValue)
                        : new Size(minValue, maxChunkSize);
                }
                else
                {
                    chunkDestSize = new Size(maxChunkSize, maxChunkSize);
                }
            }

            for (int y = 0; y < chunkCount; y++)
            {
                for (int x = 0; x < chunkCount; x++)
                {

                    var gifImage = new Image<Rgba32>(chunkDestSize.Width, chunkDestSize.Height);
                    for (int i = 0; i < originalGifImage.Frames.Count; i++)
                    {
                        var gifFrameImage = originalGifImage.Frames.CloneFrame(i);
                        gifFrameImage.Mutate(img => img.Crop(new Rectangle(x * chunkSourceSize.Width, y * chunkSourceSize.Height, chunkSourceSize.Width, chunkSourceSize.Height)));
                        gifFrameImage.Mutate(img => img.Resize(chunkDestSize));
                        gifImage.Frames.AddFrame(gifFrameImage.Frames.RootFrame);
                    }

                    var ms = new MemoryStream();
                    gifImage.SaveAsGif(ms);
                    streams.Add(ms);
                }
            }

            // Post result images one by one
            for (int i = 0; i < streams.Count; i++)
            {
                var stream = streams.ElementAt(i);
                stream.Position = 0;
                await Context.Channel.SendFileAsync(stream, $"chunk_{i + 1}.gif").ConfigureAwait(false);
            }

            await Context.ApplyResultReaction(CommandResult.Success);
        }

        [Priority(3), DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task ChunkifyGif(int chunkCount, int maxChunkSize, bool strechToFit = true)
        {
            if (Context.Message.Attachments.Count == 0)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }
            else
            {
                var url = Context.Message.Attachments.ElementAt(0).ProxyUrl;
                await ChunkifyGif(url, chunkCount, maxChunkSize, strechToFit).ConfigureAwait(false);
            }
        }

        [Priority(2), DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task ChunkifyGif(int chunkCount, bool strechToFit = true)
            => ChunkifyGif(chunkCount, int.MinValue, strechToFit);

        [Priority(0), DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task ChunkifyGif(string sourceUrl, int chunkCount, bool strechToFit = true)
            => ChunkifyGif(sourceUrl, chunkCount, int.MinValue, strechToFit);
    }
}
