using MFDLabs.Drawing.Models;
using MFDLabs.Drawing.NativeWin32;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace MFDLabs.Drawing
{
    public class BitmapExtensions
    {
        public static Bitmap GetBitmapForWindowByWindowHandle(IntPtr hwnd)
        {
            NativeMethods.GetWindowRect(hwnd, out BaseRectangle rect);
            GetWindowBitmapAndDeviceHandle(rect, out Bitmap bitmap, out Graphics graphics, out IntPtr hdc);
            NativeMethods.PrintWindow(hwnd, hdc, 0);
            GraphicsUtility.CleanupRender(graphics, hdc);
            return bitmap;
        }

        private static void GetWindowBitmapAndDeviceHandle(BaseRectangle rect, out Bitmap bitmap, out Graphics graphics, out IntPtr hdc)
        {
            bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(bitmap);
            hdc = graphics.GetHdc();
        }
    }
}
