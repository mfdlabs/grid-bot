using System;
using System.Drawing;
using System.Drawing.Imaging;
using MFDLabs.Drawing.Extensions;
using MFDLabs.Drawing.Models;
using MFDLabs.Drawing.NativeWin32;

namespace MFDLabs.Drawing
{
    public static class BitmapExtensions
    {
        public static Bitmap GetBitmapForWindowByWindowHandle(this IntPtr hwnd)
        {
            NativeMethods.GetWindowRect(hwnd, out var rect);
            rect.GetWindowBitmapAndDeviceHandle(out var bitmap, out var graphics, out var hdc);
            NativeMethods.PrintWindow(hwnd, hdc, 0);
            graphics.CleanUp(hdc);
            return bitmap;
        }

        private static void GetWindowBitmapAndDeviceHandle(this BaseRectangle rect, out Bitmap bitmap, out Graphics graphics, out IntPtr hdc)
        {
            bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            graphics = Graphics.FromImage(bitmap);
            hdc = graphics.GetHdc();
        }
    }
}
