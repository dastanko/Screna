using System.Drawing;
using System.Windows.Forms;

namespace Screna
{
    public class ScreenProvider : IImageProvider
    {
        Screen Screen;
        IOverlay[] Overlays;

        public ScreenProvider(Screen Screen, params IOverlay[] Overlays)
        {
            this.Screen = Screen;
            this.Overlays = Overlays;
        }

        public Bitmap Capture()
        {
            var BMP = ScreenShot.Capture(Screen);

            using (var g = Graphics.FromImage(BMP))
                foreach (var overlay in Overlays)
                    overlay.Draw(g, Rectangle.Location);

            return BMP;
        }

        public int Height => Screen.Bounds.Height;

        public int Width => Screen.Bounds.Height;
        
        Rectangle Rectangle => Screen.Bounds;

        public void Dispose()
        {
            foreach (var overlay in Overlays)
                overlay.Dispose();
        }
    }
}
