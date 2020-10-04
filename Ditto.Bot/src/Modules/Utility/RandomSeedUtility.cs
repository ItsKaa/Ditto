using Discord;
using Ditto.Bot.Helpers;
using Ditto.Data.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Imaging;
using Ditto.Helpers;
using System.IO;
using Ditto.Attributes;
using Ditto.Data.Commands;
using Ditto.Data;
using SixLabors.ImageSharp.Formats.Bmp;
using System.Drawing.Drawing2D;

namespace Ditto.Bot.Modules.Utility
{
    public class RandomSeedUtility : DiscordModule<Utility>
    {
        private static Randomizer GetTodayRandomizer(params ulong[] seed)
        {
            var date = DateTime.UtcNow.Date;
            seed = seed.Append((ulong)date.Ticks).Reverse().ToArray();
            return new Randomizer(seed);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Ship(IUser user1, IUser user2)
        {
            var levels = new Dictionary<int, Tuple<string, string>>
                {
                    { 100, new Tuple<string, string>("Soulmates", "#DF1E63") },
                    { 90, new Tuple<string, string>("Amazing", "#F54783") },
                    { 80, new Tuple<string, string>("Great", "#FD4558") },
                    { 70, new Tuple<string, string>("Good", "#F4893A") },
                    { 69, new Tuple<string, string>("Nice.", "#FF0000") },
                    { 68, new Tuple<string, string>("Fine", "#E9AC15") },
                    { 60, new Tuple<string, string>("Fine", "#E9AC15") },
                    { 50, new Tuple<string, string>("Average", "#E9CC15") },
                    { 40, new Tuple<string, string>("Poor", "#74D5FF") },
                    { 30, new Tuple<string, string>("Bad", "#4688E1") },
                    { 20, new Tuple<string, string>("Very Bad", "#7098CF") },
                    { 10, new Tuple<string, string>("Aweful", "#7D8CA2") },
                    { 1, new Tuple<string, string>("Horrid", "#585E68") },
                    { 0, new Tuple<string, string>("Abysmal", "#000000") }
                };

            var heartLevels = new Dictionary<int, string>
                {
                    { 100, "HeartArrow.png" },      // 💘
                    { 90,  "HeartSparkling.png" },  // 💖
                    { 50,  "Heart.png" },           // ❤
                    { 0,   "HeartBroken.png" },     // 💔
                };

            var randomizer = new Randomizer((ulong)DateTime.UtcNow.Date.Ticks, user1.Id, user2.Id);
            var percent = GetTodayRandomizer(user1.Id, user2.Id).New(0, 100);
            var highestKey = levels.Keys.OrderBy(x => x).LastOrDefault(k => percent >= k);
            var resultMessage = levels[highestKey].Item1;
            var resultColorBrush = new SolidBrush(ColorTranslator.FromHtml(levels[highestKey].Item2));
            var resultHeartText = heartLevels[heartLevels.Keys.OrderBy(x => x).LastOrDefault(k => percent >= k)];

            // Can't seem to render UTF-32 characters on Linux, so instead using an image.
            var heartImg = System.Drawing.Image.FromFile($@"data/images/hearts/{resultHeartText}");
            heartImg = ImageHelper.ReplacePixelColours(heartImg, resultColorBrush.Color);


            const int circleSize = 128;
            const int margin = 200;
            const int borderMargin = 50;
            const int circleMarginY = 50;
            const int imgWidth = (circleSize * 2) + (borderMargin * 2) + margin;
            const int imgHeight = circleSize + circleMarginY + 30;
            const int scale = 4;
            const float circleBorderSize = 1.5f;
            var circleColour = System.Drawing.Color.Gray;

            var bmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format32bppArgb);
            var bmpScaled = new Bitmap(imgWidth * scale, imgHeight * scale, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmpScaled))
            {
                g.SetupForHighQuality();

                const float resultStartOffset = 50f * scale;
                const float resultPercentageMargin = 20f * scale;
                const float resultMessageMargin = 12.5f * scale;
                const float nameMarginY = 5f;
                var resultHeartSize = new Size(heartImg.Width * scale, heartImg.Height * scale);

                // There seems to be a bug in .net core, but this does work on Windows.
                FontCollection fontCollection = new InstalledFontCollection();
                if (BaseClass.IsWindows())
                {
                    var privateFontCollection = new PrivateFontCollection();
                    privateFontCollection.AddFontFile("data/fonts/Pinky Cupid.otf");
                    fontCollection = privateFontCollection;
                }

                FontFamily fallbackFontFamily = new FontFamily("Arial");
                FontFamily pinkyCupidFontFamily = fontCollection.Families.FirstOrDefault(x => string.Equals(x.Name, "Pinky Cupid", StringComparison.CurrentCultureIgnoreCase)) ?? fallbackFontFamily;
                //FontFamily pinkyCupidFontFamily = fallbackFontFamily;

                var fontHeader = new Font(pinkyCupidFontFamily, 22f * scale);
                var headerText = "~Compatibility Meter~";
                var headerSize = g.MeasureString(headerText, fontHeader);
                g.DrawString(headerText, fontHeader, Brushes.Salmon, ((imgWidth * scale) - headerSize.Width) / 2f, 0);

                //var fontResultHeart = new Font("Segoe UI Emoji", 32f);
                //var resultHeartSize = g.MeasureString(resultHeartText, fontResultHeart);
                //g.DrawString(resultHeartText, fontResultHeart, resultColorBrush, (imgWidth - resultHeartSize.Width) / 2f, 50f);


                var fontPercentage = new Font(pinkyCupidFontFamily, 18f * scale);
                var percentageText = $"~{percent}%~";
                var percentageSize = g.MeasureString(percentageText, fontPercentage);
                g.DrawString(percentageText, fontPercentage, resultColorBrush, ((imgWidth * scale) - percentageSize.Width) / 2f, resultStartOffset + resultHeartSize.Height + resultPercentageMargin);

                var fontResultMessage = new Font(pinkyCupidFontFamily, 18f * scale);
                var resultMessageSize = g.MeasureString(resultMessage, fontPercentage);
                g.DrawString(resultMessage, fontResultMessage, resultColorBrush, ((imgWidth * scale) - resultMessageSize.Width) / 2f, resultStartOffset + percentageSize.Height + resultHeartSize.Height + resultMessageMargin);

                var fontName = new Font(pinkyCupidFontFamily, 18f * scale);
                var nameText1 = (user1 as IGuildUser)?.Nickname ?? user1.Username;
                var nameText2 = (user2 as IGuildUser)?.Nickname ?? user2.Username;
                var nameSize1 = g.MeasureString(nameText1, fontName);
                var nameSize2 = g.MeasureString(nameText2, fontName);
                g.DrawString(nameText1, fontName, Brushes.Gray, ((borderMargin + (circleSize / 2)) * scale) - (nameSize1.Width / 2f), (circleMarginY + circleSize + nameMarginY) * scale);
                g.DrawString(nameText2, fontName, Brushes.Gray, ((imgWidth - borderMargin - (circleSize / 2)) * scale) - (nameSize2.Width / 2f), (circleMarginY + circleSize + nameMarginY) * scale);
            }

            using (var g = Graphics.FromImage(bmp))
            {
                g.SetupForHighQuality();

                // Copy scaled image on top of this one
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmpScaled, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmpScaled.Width, bmpScaled.Height, GraphicsUnit.Pixel);

                // Now render the images.
                Bitmap avatar1 = null, avatar2 = null;
                try
                {

                    var avatarUrl1 = user1.GetAvatarUrl(Discord.ImageFormat.Jpeg, 128);
                    var avatarUrl2 = user2.GetAvatarUrl(Discord.ImageFormat.Jpeg, 128);
                    var avatarStream1 = await WebHelper.GetStreamAsync(avatarUrl1).ConfigureAwait(false);
                    var avatarStream2 = await WebHelper.GetStreamAsync(avatarUrl2).ConfigureAwait(false);

                    avatar1 = new Bitmap(avatarStream1);
                    avatar2 = new Bitmap(avatarStream2);
                }
                catch { }

                if (avatar1 == null)
                {
                    avatar1 = new Bitmap(128, 128);
                }
                if (avatar2 == null)
                {
                    avatar2 = new Bitmap(128, 128);
                }

                g.DrawImage(heartImg, ((imgWidth - heartImg.Width) / 2f) - 0, 60f);

                ImageHelper.DrawImageInCircle(g, avatar1, new PointF(borderMargin, circleMarginY), circleColour, circleBorderSize);
                ImageHelper.DrawImageInCircle(g, avatar2, new PointF(imgWidth - borderMargin - circleSize, circleMarginY), circleColour, circleBorderSize);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            await Context.Channel.SendFileAsync(ms, $"ship.png").ConfigureAwait(false);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public Task Ship(IUser user2)
            => Ship(Context.User, user2);

    }
}
