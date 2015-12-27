using System;
using System.Drawing;

namespace Screna
{
    /// <summary>
    /// Draws over a Capured image.
    /// </summary>
    public interface IOverlay : IDisposable
    {
        void Draw(Graphics g, Point Offset = default(Point));
    }
}
