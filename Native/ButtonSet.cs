using System.Windows.Forms;

namespace Screna.Native
{
    class ButtonSet
    {
        MouseButtons m_Set;

        public ButtonSet() { m_Set = MouseButtons.None; }

        public void Add(MouseButtons element) { m_Set |= element; }

        public void Remove(MouseButtons element) { m_Set &= ~element; }

        public bool Contains(MouseButtons element) { return (m_Set & element) != MouseButtons.None; }
    }
}