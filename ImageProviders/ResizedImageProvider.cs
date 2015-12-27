using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Screna
{
    public class ResizedImageProvider : IImageProvider
    {
        int TargetWidth, TargetHeight;
        float ResizeWidth, ResizeHeight;
        
        IImageProvider ImageSource;
        Color BackgroundColor;

        public ResizedImageProvider(IImageProvider ImageSource, int TargetWidth, int TargetHeight, Color BackgroundColor)
        {
            this.ImageSource = ImageSource;
            this.BackgroundColor = BackgroundColor;

            this.TargetHeight = TargetHeight;
            this.TargetWidth = TargetWidth;

            int OriginalWidth = ImageSource.Width,
                OriginalHeight = ImageSource.Height;

            float Ratio = Math.Min((float)TargetWidth / OriginalWidth, (float)TargetHeight / OriginalHeight);

            ResizeWidth = OriginalWidth * Ratio;
            ResizeHeight = OriginalHeight * Ratio;
        }

        public Bitmap Capture()
        {
            var BMP = ImageSource.Capture();

            var ResizedBMP = new Bitmap(TargetWidth, TargetHeight);

            using (var g = Graphics.FromImage(ResizedBMP))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;

                if (BackgroundColor != Color.Transparent)
                    g.FillRectangle(new SolidBrush(BackgroundColor), 0, 0, TargetWidth, TargetHeight);

                g.DrawImage(BMP, 0, 0, ResizeWidth, ResizeHeight);
            }

            return ResizedBMP;
        }

        public int Height { get { return TargetHeight; } }

        public int Width { get { return TargetWidth; } }
        
        public void Dispose() { }
    }
}