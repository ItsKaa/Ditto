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
using Tweetinvi;

namespace Ditto.Bot.Modules.Utility
{
    public class RandomSeedUtility : DiscordModule<Utility>
    {
        private static readonly Dictionary<int, Tuple<string, string>> _shipLevels = new Dictionary<int, Tuple<string, string>>
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

        private static readonly Dictionary<int, string> _shipHeartLevels = new Dictionary<int, string>
        {
            { 100, "HeartArrow.png" },      // 💘
            { 90,  "HeartSparkling.png" },  // 💖
            { 50,  "Heart.png" },           // ❤
            { 0,   "HeartBroken.png" },     // 💔
        };

        private static Randomizer GetTodayRandomizer(params ulong[] seed)
        {
            var date = DateTime.UtcNow.Date;
            
            // Important to order the seed in case the order differs.
            seed = seed.Append((ulong)date.Ticks)
                .OrderBy(x => x)
                .ToArray();
            return new Randomizer(seed);
        }

        [DiscordCommand(CommandSourceLevel.All, CommandAccessLevel.All)]
        public async Task Ship(IUser user1, IUser user2, IUser user3 = null)
        {
            bool hasThreeUsers = user1 != null && user2 != null && user3 != null;

            // Validation check.
            if (user1 == null || user2 == null)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }

            // Check for duplicate users when calculating a three way ship.
            if(hasThreeUsers
                && user1 == user2 || user1 == user3 || user2 == user3)
            {
                await Context.ApplyResultReaction(CommandResult.Failed).ConfigureAwait(false);
                return;
            }


            // Randomize shipping.
            var randomizer = new Randomizer((ulong)DateTime.UtcNow.Date.Ticks, user1.Id, user2.Id);
            var percent = GetTodayRandomizer(user1.Id, user2.Id, user3?.Id ?? 0).New(0, 100);
            var highestKey = _shipLevels.Keys.OrderBy(x => x).LastOrDefault(k => percent >= k);
            var resultMessage = _shipLevels[highestKey].Item1;
            var resultColorBrush = new SolidBrush(ColorTranslator.FromHtml(_shipLevels[highestKey].Item2));
            var resultHeartText = _shipHeartLevels[_shipHeartLevels.Keys.OrderBy(x => x).LastOrDefault(k => percent >= k)];

            // Constant variables to setup the rendering bit.
            const int circleSize = 128;
            const int margin = 200;
            const int borderMargin = 50;
            const int circleMarginY = 50;
            const int imgWidth = (circleSize * 2) + (borderMargin * 2) + margin;
            float resultStartOffset = 50f + (hasThreeUsers ? 15f : 0f);
            var circleColour = System.Drawing.Color.Gray;
            const int scale = 4;
            // Dynamic
            int imgHeight = (circleSize + circleMarginY + 30) * (hasThreeUsers ? 2 : 1);
            const float circleBorderSize = 1.5f;

            // Can't seem to render UTF-32 characters on Linux, so instead using an image.
            var heartImg = System.Drawing.Image.FromFile($@"data/images/hearts/{resultHeartText}");
            heartImg = ImageHelper.ReplacePixelColours(heartImg, resultColorBrush.Color);

            // Setup fonts
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


            var bmp = new Bitmap(imgWidth, imgHeight, PixelFormat.Format32bppArgb);
            var bmpScaled = new Bitmap(imgWidth * scale, imgHeight * scale, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmpScaled))
            {
                g.SetupForHighQuality();

                float resultStartOffsetScaled = resultStartOffset * scale;
                const float resultPercentageMargin = 20f * scale;
                const float resultMessageMargin = 12.5f * scale;
                const float nameMarginY = 5f;
                var resultHeartSize = new Size(heartImg.Width * scale, heartImg.Height * scale);

                var fontHeader = new Font(pinkyCupidFontFamily, 22f * scale);
                var headerText = "~Compatibility Meter~";
                var headerSize = g.MeasureString(headerText, fontHeader);
                g.DrawString(headerText, fontHeader, Brushes.Salmon, ((imgWidth * scale) - headerSize.Width) / 2f, 0);

                // Moved to an image due to UTF-32 rendering issues on linux.
                //var fontResultHeart = new Font("Segoe UI Emoji", 32f);
                //var resultHeartSize = g.MeasureString(resultHeartText, fontResultHeart);
                //g.DrawString(resultHeartText, fontResultHeart, resultColorBrush, (imgWidth - resultHeartSize.Width) / 2f, 50f);

                var fontPercentage = new Font(pinkyCupidFontFamily, 18f * scale);
                var percentageText = $"~{percent}%~";
                var percentageSize = g.MeasureString(percentageText, fontPercentage);
                g.DrawString(percentageText, fontPercentage, resultColorBrush, ((imgWidth * scale) - percentageSize.Width) / 2f, resultStartOffsetScaled + resultHeartSize.Height + resultPercentageMargin);

                var fontResultMessage = new Font(pinkyCupidFontFamily, 18f * scale);
                var resultMessageSize = g.MeasureString(resultMessage, fontPercentage);
                g.DrawString(resultMessage, fontResultMessage, resultColorBrush, ((imgWidth * scale) - resultMessageSize.Width) / 2f, resultStartOffsetScaled + percentageSize.Height + resultHeartSize.Height + resultMessageMargin);

                var fontName = new Font(pinkyCupidFontFamily, 18f * scale);
                var nameText1 = (user1 as IGuildUser)?.Nickname ?? user1.Username;
                var nameText2 = (user2 as IGuildUser)?.Nickname ?? user2.Username;
                var nameSize1 = g.MeasureString(nameText1, fontName);
                var nameSize2 = g.MeasureString(nameText2, fontName);
                g.DrawString(nameText1, fontName, Brushes.Gray, ((borderMargin + (circleSize / 2)) * scale) - (nameSize1.Width / 2f), (circleMarginY + circleSize + nameMarginY) * scale);
                g.DrawString(nameText2, fontName, Brushes.Gray, ((imgWidth - borderMargin - (circleSize / 2)) * scale) - (nameSize2.Width / 2f), (circleMarginY + circleSize + nameMarginY) * scale);

                if (hasThreeUsers)
                {
                    var nameText3 = (user3 as IGuildUser)?.Nickname ?? user3?.Username;
                    var nameSize3 = g.MeasureString(nameText3, fontName);
                    g.DrawString(nameText3, fontName, Brushes.Gray, ((imgWidth * scale) - nameSize3.Width) / 2, (circleSize + circleSize + circleMarginY + 50 + nameMarginY) * scale);
                }
            }

            using (var g = Graphics.FromImage(bmp))
            {
                g.SetupForHighQuality();

                // Copy scaled image on top of this one
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmpScaled, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmpScaled.Width, bmpScaled.Height, GraphicsUnit.Pixel);

                // Avatar circles
                Bitmap avatar1 = null, avatar2 = null, avatar3 = null;
                try
                {
                    var avatarUrl1 = user1.GetAvatarUrl(Discord.ImageFormat.Jpeg, 128);
                    var avatarUrl2 = user2.GetAvatarUrl(Discord.ImageFormat.Jpeg, 128);
                    var avatarUrl3 = user3?.GetAvatarUrl(Discord.ImageFormat.Jpeg, 128);


                    var avatarStream1 = await WebHelper.GetStreamAsync(avatarUrl1).ConfigureAwait(false);
                    avatar1 = new Bitmap(avatarStream1);

                    var avatarStream2 = await WebHelper.GetStreamAsync(avatarUrl2).ConfigureAwait(false);
                    avatar2 = new Bitmap(avatarStream2);

                    if (hasThreeUsers)
                    {
                        var avatarStream3 = await WebHelper.GetStreamAsync(avatarUrl3).ConfigureAwait(false);
                        avatar3 = new Bitmap(avatarStream3);
                    }
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
                if(avatar3 == null)
                {
                    avatar3 = new Bitmap(128, 128);
                }

                ImageHelper.DrawImageInCircle(g, avatar1, new PointF(borderMargin, circleMarginY), circleColour, circleBorderSize);
                ImageHelper.DrawImageInCircle(g, avatar2, new PointF(imgWidth - borderMargin - circleSize, circleMarginY), circleColour, circleBorderSize);
                ImageHelper.DrawImageInCircle(g, avatar3, new PointF((imgWidth - circleSize) / 2, circleSize + circleMarginY + 50), circleColour, circleBorderSize);

                // Heart image
                g.DrawImage(heartImg, ((imgWidth - heartImg.Width) / 2f), (resultStartOffset + 10f));
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

