using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Screna
{
    /// <summary>
    /// Provides images.
    /// Must provide in PixelFormat.Format32bppRgb
    /// </summary>
    public interface IImageProvider : IDisposable
    {
        Bitmap Capture();

        int Height { get; }
        int Width { get; }
    }
}