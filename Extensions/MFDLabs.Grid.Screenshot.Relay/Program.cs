using MFDLabs.Diagnostics;
using MFDLabs.Drawing;
using System;

namespace MFDLabs.Grid.Screenshot.Relay
{
    internal static class Program
    {
        public static void Main()
        {
            var hwnd = ProcessHelper.GetWindowHandle(
                global::MFDLabs.Grid.Screenshot.Relay.Properties.Settings.Default.GridServerExecutableNameNoExtension
            );
            try
            {
                var bitMap = hwnd.GetBitmapForWindowByWindowHandle();
                bitMap.Save(
                    global::MFDLabs.Grid.Screenshot.Relay.Properties.Settings.Default.OutputScreenshotFileName
                );
            }
            catch (Exception)
            {
                Console.WriteLine("The grid server was not running.");
            }
        }
    }
}
