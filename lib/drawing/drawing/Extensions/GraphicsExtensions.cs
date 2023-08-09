using System;
using System.Drawing;

namespace Drawing.Extensions
{
    public static class GraphicsExtensions
    {
        public static void CleanUp(this Graphics graphics, IntPtr hdc) => GraphicsUtility.CleanupRender(graphics, hdc);
    }
}
