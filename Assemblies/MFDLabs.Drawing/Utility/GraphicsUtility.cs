using System;
using System.Drawing;

namespace MFDLabs.Drawing
{
    internal class GraphicsUtility
    {
        internal static void CleanupRender(Graphics graphics, IntPtr hdc)
        {
            graphics.ReleaseHdc(hdc);
            graphics.Dispose();
        }
    }
}
