using System;
using System.Drawing;
using System.Drawing.Imaging;
using MFDLabs.Drawing.Extensions;
using MFDLabs.Drawing.Models;
using MFDLabs.Drawing.NativeWin32;

namespace MFDLabs.Drawing
{
    public class BitmapExtensions
    {
        public static Bitmap GetBitmapForWindowByWindowHandle(IntPtr hwnd)
        {
            NativeMethods.GetWindowRect(hwnd, out BaseRectangle rect);
            GetWindowBitmapAndDeviceHandle(rect, out Bitmap bitmap, out Graphics graphics, out IntPtr hdc);
            NativeMethods.PrintWindow(hwnd, hdc, 0);
            graphics.CleanUp(hdc);
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
