using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Screna
{
    public class ResizedImageProvider : IImageProvider
    {
        readonly float ResizeWidth, ResizeHeight;

        readonly IImageProvider ImageSource;
        readonly Color BackgroundColor;

        public ResizedImageProvider(IImageProvider ImageSource, int TargetWidth, int TargetHeight, Color BackgroundColor)
        {
            this.ImageSource = ImageSource;
            this.BackgroundColor = BackgroundColor;

            this.Height = TargetHeight;
            this.Width = TargetWidth;

            int OriginalWidth = ImageSource.Width,
                OriginalHeight = ImageSource.Height;

            var Ratio = Math.Min((float)TargetWidth / OriginalWidth, (float)TargetHeight / OriginalHeight);

            ResizeWidth = OriginalWidth * Ratio;
            ResizeHeight = OriginalHeight * Ratio;
        }

        public Bitmap Capture()
        {
            var BMP = ImageSource.Capture();

            var ResizedBMP = new Bitmap(Width, Height);

            using (var g = Graphics.FromImage(ResizedBMP))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                if (BackgroundColor != Color.Transparent)
                    g.FillRectangle(new SolidBrush(BackgroundColor), 0, 0, Width, Height);

                g.DrawImage(BMP, 0, 0, ResizeWidth, ResizeHeight);
            }

            return ResizedBMP;
        }

        public int Height { get; }

        public int Width { get; }

        public void Dispose() { }
    }
}