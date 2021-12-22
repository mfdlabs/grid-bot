using System;
using System.Drawing;

namespace MFDLabs.Drawing
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
