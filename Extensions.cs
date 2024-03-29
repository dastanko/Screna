﻿using Screna.Native;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Screna
{
    public static class Extensions
    {
        /// <summary>
        /// Writes a Bitmap to Clipboard while taking care of Transparency
        /// </summary>
        public static void WriteToClipboard(this Bitmap BMP, bool PreserveTransparency = true)
        {
            if (PreserveTransparency)
            {
                using (var PngStream = new MemoryStream())
                {
                    BMP.Save(PngStream, ImageFormat.Png);
                    var pngClipboardData = new DataObject("PNG", PngStream);

                    using (var whiteS = new Bitmap(BMP.Width, BMP.Height, PixelFormat.Format24bppRgb))
                    {
                        using (var graphics = Graphics.FromImage(whiteS))
                        {
                            graphics.Clear(Color.White);
                            graphics.DrawImage(BMP, 0, 0, BMP.Width, BMP.Height);
                        }

                        // Add fallback for applications that don't support PNG from clipboard (eg. Photoshop or Paint)
                        pngClipboardData.SetData(DataFormats.Bitmap, whiteS);

                        Clipboard.Clear();
                        Clipboard.SetDataObject(pngClipboardData, true);
                    }
                }
            }
            else Clipboard.SetImage(BMP);
        }

        /// <summary>
        /// Removes the Pixels on Edges matching TrimColor(default is Transparent) from the Image
        /// </summary>
        public static unsafe Bitmap CropEmptyEdges(this Bitmap Image, Color TrimColor = default(Color))
        {
            if (Image == null)
                return null;

            int sizeX = Image.Width,
                sizeY = Image.Height;

            var r = new RECT(-1, -1, -1, -1);

            using (var b = new UnsafeBitmap(Image))
            {
                PixelData* pixel;

                for (int x = 0, y = 0; ; )
                {
                    pixel = b[x, y];

                    if (r.Left == -1)
                    {
                        if ((TrimColor.A == 0 && pixel->Alpha != 0) ||
                            (TrimColor.R != pixel->Red &
                             TrimColor.G != pixel->Green &
                             TrimColor.B != pixel->Blue))
                        {
                            r.Left = x;

                            x = 0;
                            y = 0;

                            continue;
                        }

                        if (y == sizeY - 1)
                        {
                            x++;
                            y = 0;
                        }
                        else y++;

                        continue;
                    }

                    if (r.Top == -1)
                    {
                        if ((TrimColor.A == 0 && pixel->Alpha != 0) ||
                            (TrimColor.R != pixel->Red &
                             TrimColor.G != pixel->Green &
                             TrimColor.B != pixel->Blue))
                        {
                            r.Top = y;

                            x = sizeX - 1;
                            y = 0;

                            continue;
                        }

                        if (x == sizeX - 1)
                        {
                            y++;
                            x = 0;
                        }
                        else x++;

                        continue;
                    }

                    if (r.Right == -1)
                    {
                        if ((TrimColor.A == 0 && pixel->Alpha != 0) ||
                            (TrimColor.R != pixel->Red &
                             TrimColor.G != pixel->Green &
                             TrimColor.B != pixel->Blue))
                        {
                            r.Right = x + 1;

                            x = 0;
                            y = sizeY - 1;

                            continue;
                        }

                        if (y == sizeY - 1)
                        {
                            x--;
                            y = 0;
                        }
                        else y++;

                        continue;
                    }

                    if (r.Bottom == -1)
                    {
                        if ((TrimColor.A == 0 && pixel->Alpha != 0) ||
                            (TrimColor.R != pixel->Red &
                             TrimColor.G != pixel->Green &
                             TrimColor.B != pixel->Blue))
                        {
                            r.Bottom = y + 1;
                            break;
                        }

                        if (x == sizeX - 1)
                        {
                            y--;
                            x = 0;
                        }
                        else x++;
                    }
                }
            }

            if (r.Left >= r.Right || r.Top >= r.Bottom)
                return null;

            Bitmap final = Image.Clone(r.ToRectangle(), Image.PixelFormat);

            Image.Dispose();

            return final;
        }

        public static void Draw(this IOverlay overlay, Image img, Point Offset = default(Point))
        {
            using (var g = Graphics.FromImage(img))
                overlay.Draw(g, Offset);
        }

        public static Rectangle ToRectangle(this RECT r) => new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

        /// <summary>
        /// Creates a Transparent Bitmap from a combination of a Bitmap on a White Background and another on a Black Background
        /// </summary>
        public static unsafe Bitmap DifferentiateAlpha(Bitmap WhiteBitmap, Bitmap BlackBitmap)
        {
            if (WhiteBitmap == null || BlackBitmap == null ||
                WhiteBitmap.Width != BlackBitmap.Width ||
                WhiteBitmap.Height != BlackBitmap.Height)
                return null;

            int sizeX = WhiteBitmap.Width,
                sizeY = WhiteBitmap.Height;

            var final = new Bitmap(sizeX, sizeY, PixelFormat.Format32bppArgb);

            var empty = true;

            using (var a = new UnsafeBitmap(WhiteBitmap))
            {
                using (var b = new UnsafeBitmap(BlackBitmap))
                {
                    using (var f = new UnsafeBitmap(final))
                    {
                        for (int x = 0, y = 0; x < sizeX && y < sizeY; )
                        {
                            PixelData* pixelA = a[x, y],
                                pixelB = b[x, y],
                                pixelF = f[x, y];

                            pixelF->Alpha = ToByte((pixelB->Red - pixelA->Red + 255 + pixelB->Green -
                                        pixelA->Green + 255 + pixelB->Blue - pixelA->Blue +
                                        255) / 3);

                            if (pixelF->Alpha > 0)
                            {
                                // Following math creates an image optimized to be displayed on a black background
                                pixelF->Red = ToByte(255 * pixelB->Red / pixelF->Alpha);
                                pixelF->Green = ToByte(255 * pixelB->Green / pixelF->Alpha);
                                pixelF->Blue = ToByte(255 * pixelB->Blue / pixelF->Alpha);
                            }

                            if (empty && pixelF->Alpha > 0)
                                empty = false;

                            if (x == sizeX - 1)
                            {
                                y++;
                                x = 0;
                                continue;
                            }
                            x++;
                        }
                    }
                }
            }

            return empty ? null : final;
        }

        static byte ToByte(int i) => (byte)(i > 255 ? 255 : (i < 0 ? 0 : i));

        internal static unsafe void Bgr32ToBgr24(byte[] source, int srcOffset, byte[] destination, int destOffset, int pixelCount)
        {
            fixed (byte* sourcePtr = source, destinationPtr = destination)
            {
                var sourceStart = sourcePtr + srcOffset;
                var destinationStart = destinationPtr + destOffset;
                var sourceEnd = sourceStart + 4 * pixelCount;

                var src = sourceStart;
                var dest = destinationStart;

                while (src < sourceEnd)
                {
                    *(dest++) = *(src++);
                    *(dest++) = *(src++);
                    *(dest++) = *(src++);
                    src++;
                }
            }
        }

        public static void FlipVertical(byte[] source, int srcOffset, byte[] destination, int destOffset, int height, int stride)
        {
            var src = srcOffset;
            var dest = destOffset + (height - 1) * stride;

            for (var y = 0; y < height; y++)
            {
                Buffer.BlockCopy(source, src, destination, dest, stride);
                src += stride;
                dest -= stride;
            }
        }

        /// <summary>
        /// Splits frame rate value to integer <c>rate</c> and <c>scale</c> values used in some AVI headers
        /// and VfW APIs.
        /// </summary>
        /// <param name="frameRate">
        /// Frame rate. Rounded to 3 fractional digits.
        /// </param>
        /// <param name="rate">
        /// When the method returns, contains rate value.
        /// </param>
        /// <param name="scale">
        /// When the method returns, contains scale value.
        /// </param>
        internal static void SplitFrameRate(decimal frameRate, out uint rate, out uint scale)
        {
            if (decimal.Round(frameRate) == frameRate)
            {
                rate = (uint)decimal.Truncate(frameRate);
                scale = 1;
            }
            else if (decimal.Round(frameRate, 1) == frameRate)
            {
                rate = (uint)decimal.Truncate(frameRate * 10m);
                scale = 10;
            }
            else if (decimal.Round(frameRate, 2) == frameRate)
            {
                rate = (uint)decimal.Truncate(frameRate * 100m);
                scale = 100;
            }
            else
            {
                rate = (uint)decimal.Truncate(frameRate * 1000m);
                scale = 1000;
            }

            // Make mutually prime (needed for some hardware players)
            while (rate % 2 == 0 && scale % 2 == 0)
            {
                rate /= 2;
                scale /= 2;
            }
            while (rate % 5 == 0 && scale % 5 == 0)
            {
                rate /= 5;
                scale /= 5;
            }
        }
    }
}