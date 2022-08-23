using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace Ditto.Bot.Helpers
{
    public static class ImageHelper
    {
        public static Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                return Image.Load(ms);
            }
        }
        public static string ImageToBase64(Image image, IImageEncoder encoder)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, encoder);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to base 64 string
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        public static Image<Rgba32> CombineImages(List<Image> images, Color backgroundColour)
        {
            int width = 0;
            int height = 0;

            foreach (var image in images)
            {
                //update the size of the final bitmap
                width += image.Width;
                height = image.Height > height ? image.Height : height;
            }

            var finalImage = new Image<Rgba32>(width, height);
            var position = new Point(0, 0);
            foreach (var image in images)
            {
                finalImage.Mutate(m => m.DrawImage(image, position, 1.0f));
                position.Offset(image.Width, 0);
            }

            return finalImage;
        }

        /// <summary>
        /// Replaces all color values of an image, but maintains alpha levels.
        /// </summary>
        public static Image<Rgba32> ReplacePixelColours(Image image, Color targetColor)
        {
            var sourceImage = image.CloneAs<Rgba32>();
            var imageResult = new Image<Rgba32>(image.Width, image.Height);
            for (int x = 0; x < sourceImage.Width; x++)
            {
                for (int y = 0; y < sourceImage.Height; y++)
                {
                    var pixel = sourceImage[x, y];
                    var colour = targetColor.WithAlpha(pixel.A / 255f);
                    imageResult[x, y] = colour.ToPixel<Rgba32>();
                }
            }
            return imageResult;
        }

        public static void DrawImageInCircle(Image destinationImage, Image sourceImage, PointF position, Color? borderColor = null, float borderSize = 1.0f)
        {
            // Clip the drawing area to the polygon and draw the image.
            var clipPosition = position;
            clipPosition.X += sourceImage.Width / 2f;
            clipPosition.Y += sourceImage.Height / 2f;

            var path = new EllipsePolygon(clipPosition.X, clipPosition.Y, sourceImage.Width, sourceImage.Height);
            destinationImage.Mutate(x => x.Clip(path, x => x.DrawImage(sourceImage, new Point((int)position.X, (int)position.Y), 1.0f)));

            if (borderColor != null)
            {
                destinationImage.Mutate(x => x.Draw(borderColor ?? Color.Black, borderSize, path));
            }
        }

    }
}
