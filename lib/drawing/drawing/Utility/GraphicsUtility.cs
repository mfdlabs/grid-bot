using System;
using System.Drawing;

namespace Drawing
{
    internal static class GraphicsUtility
    {
        internal static void CleanupRender(Graphics graphics, IntPtr hdc)
        {
            graphics.ReleaseHdc(hdc);
            graphics.Dispose();
        }
    }
}
