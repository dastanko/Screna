using Screna.Native;
using System.Drawing;
using System.Windows.Forms;

namespace Screna
{
    /// <summary>
    /// Draws MouseClicks and/or Keystrokes on an Image
    /// </summary>
    public class MouseKeyHook : IOverlay
    {
        MouseListener ClickHook;
        KeyListener KeyHook;

        bool MouseClicked = false,
            Control = false,
            Shift = false,
            Alt = false;

        Keys LastKeyPressed = Keys.None;

        public Pen ClickStrokePen { get; set; }
        public double ClickRadius { get; set; }
        public Font KeyStrokeFont { get; set; }
        public Brush KeyStrokeBrush { get; set; }
        public Point KeyStrokeLocation { get; set; }

        public MouseKeyHook(bool CaptureMouseClicks, bool CaptureKeystrokes)
        {
            ClickStrokePen = new Pen(Color.Black, 1);
            ClickRadius = 40;
            KeyStrokeFont = new Font(FontFamily.GenericMonospace, 60);
            KeyStrokeBrush = Brushes.Black;
            KeyStrokeLocation = new Point(100, 100);

            if (CaptureMouseClicks)
            {
                ClickHook = new MouseListener();
                ClickHook.MouseDown += (s, e) => MouseClicked = true;
            }

            if (CaptureKeystrokes)
            {
                KeyHook = new KeyListener();
                KeyHook.KeyDown += OnKeyPressed;
            }
        }

        void OnKeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Shift:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                    LastKeyPressed = Keys.Shift;
                    break;

                case Keys.Control:
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                    LastKeyPressed = Keys.Control;
                    break;

                case Keys.Alt:
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                    LastKeyPressed = Keys.Alt;
                    break;

                default:
                    LastKeyPressed = e.KeyCode;
                    break;
            }

            Control = e.Control;
            Shift = e.Shift;
            Alt = e.Alt;
        }

        public void Draw(Graphics g, Point Offset = default(Point))
        {
            if (MouseClicked)
            {
                var curPos = MouseCursor.CursorPosition;
                float d = (float)(ClickRadius * 2);

                g.DrawArc(ClickStrokePen,
                    curPos.X - 40 - Offset.X,
                    curPos.Y - 40 - Offset.Y,
                    d, d,
                    0, 360);

                MouseClicked = false;
            }

            if (LastKeyPressed != Keys.None)
            {
                string ToWrite = null;

                if (Control) ToWrite += "Ctrl+";
                if (Shift) ToWrite += "Shift+";
                if (Alt) ToWrite += "Alt+";

                ToWrite += LastKeyPressed.ToString();

                g.DrawString(ToWrite,
                    KeyStrokeFont,
                    KeyStrokeBrush,
                    KeyStrokeLocation.X,
                    KeyStrokeLocation.Y);

                LastKeyPressed = Keys.None;
            }
        }

        public void Dispose()
        {
            if (ClickHook != null)
            {
                ClickHook.Dispose();
                ClickHook = null;
            }

            if (KeyHook != null)
            {
                KeyHook.Dispose();
                KeyHook = null;
            }
        }
    }
}
