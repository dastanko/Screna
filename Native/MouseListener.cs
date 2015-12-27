using System;
using System.Drawing;
using System.Windows.Forms;
using Screna.Native;

namespace Screna.Native
{
    public class MouseListener : BaseListener
    {
        readonly ButtonSet m_DoubleDown, m_SingleDown;
        Point m_PreviousPosition;
        readonly int m_SystemDoubleClickTime;
        MouseButtons m_PreviousClicked;
        Point m_PreviousClickedPosition;
        int m_PreviousClickedTime;

        public MouseListener()
            : base(HookHelper.HookGlobalMouse)
        {
            m_PreviousPosition = new Point(-1, -1);
            m_DoubleDown = new ButtonSet();
            m_SingleDown = new ButtonSet();

            m_SystemDoubleClickTime = User32.GetDoubleClickTime();
        }

        void StartDoubleClickWaiting(MouseEventExtArgs e)
        {
            m_PreviousClicked = e.Button;
            m_PreviousClickedTime = e.Timestamp;
            m_PreviousClickedPosition = e.Point;
        }

        void StopDoubleClickWaiting()
        {
            m_PreviousClicked = MouseButtons.None;
            m_PreviousClickedTime = 0;
            m_PreviousClickedPosition = new Point(0, 0);
        }

        bool IsDoubleClick(MouseEventExtArgs e)
        {
            return
                e.Button == m_PreviousClicked &&
                e.Point == m_PreviousClickedPosition && // Click-move-click exception, see Patch 11222
                e.Timestamp - m_PreviousClickedTime <= m_SystemDoubleClickTime;
        }

        protected override bool Callback(CallbackData data)
        {
            var e = GetEventArgs(data);

            if (e.IsMouseKeyDown) ProcessDown(ref e);

            if (e.IsMouseKeyUp) ProcessUp(ref e);

            if (e.WheelScrolled) ProcessWheel(ref e);

            if (HasMoved(e.Point)) ProcessMove(ref e);

            return !e.Handled;
        }

        MouseEventExtArgs GetEventArgs(CallbackData data) { return MouseEventExtArgs.FromRawDataGlobal(data); }

        void ProcessWheel(ref MouseEventExtArgs e)
        {
            OnWheel(e);
            OnWheelExt(e);
        }

        void ProcessDown(ref MouseEventExtArgs e)
        {
            if (IsDoubleClick(e)) e = e.ToDoubleClickEventArgs();

            OnDown(e);
            OnDownExt(e);
            if (e.Handled) return;

            if (e.Clicks == 2) m_DoubleDown.Add(e.Button);

            if (e.Clicks == 1) m_SingleDown.Add(e.Button);
        }

        void ProcessUp(ref MouseEventExtArgs e)
        {
            if (m_SingleDown.Contains(e.Button))
            {
                OnUp(e);
                OnUpExt(e);
                if (e.Handled) return;
                OnClick(e);
                m_SingleDown.Remove(e.Button);
            }

            if (m_DoubleDown.Contains(e.Button))
            {
                e = e.ToDoubleClickEventArgs();
                OnUp(e);
                OnDoubleClick(e);
                m_DoubleDown.Remove(e.Button);
            }

            if (e.Clicks == 2) StopDoubleClickWaiting();
            else if (e.Clicks == 1) StartDoubleClickWaiting(e);
        }

        void ProcessMove(ref MouseEventExtArgs e)
        {
            m_PreviousPosition = e.Point;

            OnMove(e);
            OnMoveExt(e);
        }

        bool HasMoved(Point actualPoint) { return m_PreviousPosition != actualPoint; }

        public event MouseEventHandler MouseMove;
        public event EventHandler<MouseEventExtArgs> MouseMoveExt;
        public event MouseEventHandler MouseClick;
        public event MouseEventHandler MouseDown;
        public event EventHandler<MouseEventExtArgs> MouseDownExt;
        public event MouseEventHandler MouseUp;
        public event EventHandler<MouseEventExtArgs> MouseUpExt;
        public event MouseEventHandler MouseWheel;
        public event EventHandler<MouseEventExtArgs> MouseWheelExt;
        public event MouseEventHandler MouseDoubleClick;

        void OnMove(MouseEventArgs e)
        {
            var handler = MouseMove;
            if (handler != null) handler(this, e);
        }

        void OnMoveExt(MouseEventExtArgs e)
        {
            var handler = MouseMoveExt;
            if (handler != null) handler(this, e);
        }

        void OnClick(MouseEventArgs e)
        {
            var handler = MouseClick;
            if (handler != null) handler(this, e);
        }

        void OnDown(MouseEventArgs e)
        {
            var handler = MouseDown;
            if (handler != null) handler(this, e);
        }

        void OnDownExt(MouseEventExtArgs e)
        {
            var handler = MouseDownExt;
            if (handler != null) handler(this, e);
        }

        void OnUp(MouseEventArgs e)
        {
            var handler = MouseUp;
            if (handler != null) handler(this, e);
        }

        void OnUpExt(MouseEventExtArgs e)
        {
            var handler = MouseUpExt;
            if (handler != null) handler(this, e);
        }

        void OnWheel(MouseEventArgs e)
        {
            var handler = MouseWheel;
            if (handler != null) handler(this, e);
        }

        void OnWheelExt(MouseEventExtArgs e)
        {
            var handler = MouseWheelExt;
            if (handler != null) handler(this, e);
        }

        void OnDoubleClick(MouseEventArgs e)
        {
            var handler = MouseDoubleClick;
            if (handler != null) handler(this, e);
        }
    }
}