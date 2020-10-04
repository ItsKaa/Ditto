using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

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
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }

        public static string ImageToBase64(Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to base 64 string
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }

        public static System.Drawing.Bitmap CombineBitmap(List<Bitmap> images, System.Drawing.Color backgroundColour)
        {
            System.Drawing.Bitmap finalImage = null;

            try
            {
                int width = 0;
                int height = 0;

                foreach (var bitmap in images)
                {
                    //update the size of the final bitmap
                    width += bitmap.Width;
                    height = bitmap.Height > height ? bitmap.Height : height;
                }

                //create a bitmap to hold the combined image
                finalImage = new System.Drawing.Bitmap(width, height);

                //get a graphics object from the image so we can draw on it
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalImage))
                {
                    //set background color
                    g.Clear(backgroundColour);

                    //go through each image and draw it on the final image
                    int offset = 0;
                    foreach (System.Drawing.Bitmap image in images)
                    {
                        g.DrawImage(image,
                          new System.Drawing.Rectangle(offset, 0, image.Width, image.Height));
                        offset += image.Width;
                    }
                }

                return finalImage;
            }
            catch (Exception)
            {
                finalImage?.Dispose();
                throw;
            }
            //finally
            //{
            //    //clean up memory
            //    foreach (System.Drawing.Bitmap image in images)
            //    {
            //        image.Dispose();
            //    }
            //}
        }

        /// <summary>
        /// Replaces all color values of an image, but maintains alpha levels.
        /// </summary>
        public static Bitmap ReplacePixelColours(Image source, Color targetColor)
        {
            var srcBitmap = source as Bitmap ?? new Bitmap(source);
            Bitmap newBitmap = new Bitmap(srcBitmap.Width, srcBitmap.Height);
            for (int x = 0; x < srcBitmap.Width; x++)
            {
                for (int y = 0; y < srcBitmap.Height; y++)
                {
                    var pixel = srcBitmap.GetPixel(x, y);
                    var colour = Color.FromArgb(pixel.A, targetColor);
                    newBitmap.SetPixel(x, y, colour);
                }
            }
            return newBitmap;
        }

        public static void SetupForHighQuality(this Graphics graphics)
        {
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
        }

        public static void DrawImageInCircle(Graphics graphics, Image image, PointF position, Color? borderColor = null, float borderSize = 1.0f)
        {
            // Clip the drawing area to the polygon and draw the image.
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(position.X, position.Y, image.Width, image.Height);

            GraphicsState state = graphics.Save();
            graphics.SetClip(path);
            graphics.DrawImage(image, new RectangleF(position.X, position.Y, image.Width, image.Height), new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            graphics.Restore(state);

            if (borderColor != null)
            {
                var pen = new Pen(borderColor ?? Color.Black, borderSize);
                graphics.DrawEllipse(pen, position.X, position.Y, image.Width, image.Height);
            }
        }

    }
}
