// ReSharper disable once CheckNamespace
namespace System
{
    internal static class ConsoleExtended
    {
        public static void WriteTitle(string message, params object[] args)
        {
            Console.Title = string.Format(message, args);
            Console.WriteLine(message, args);
        }
    }
}
