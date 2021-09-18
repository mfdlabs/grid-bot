using MFDLabs.Diagnostics;
using MFDLabs.Drawing;
using System;

using HWND = System.IntPtr;

namespace MFDLabs.Grid.Screenshot.Relay
{
    public class Program
    {
        public static void Main()
        {
            HWND hwnd = ProcessHelper.GetWindowHandle(
                global::MFDLabs.Grid.Screenshot.Relay.Properties.Settings.Default.GridServerExecutableNameNoExtension
            );
            try
            {
                var bitMap = BitmapExtensions.GetBitmapForWindowByWindowHandle(hwnd);
                bitMap.Save(
                    global::MFDLabs.Grid.Screenshot.Relay.Properties.Settings.Default.OutputScreenshotFileName
                );
            }
            catch (Exception)
            {
                Console.WriteLine("The grid server was not running.");
            }
            return;
        }
    }
}
