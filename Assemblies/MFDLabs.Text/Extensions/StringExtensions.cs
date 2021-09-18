namespace MFDLabs.Text.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);
        public static bool IsNullWhiteSpaceOrEmpty(this string str) => str.IsNullOrEmpty() || str.IsNullOrWhiteSpace();
    }
}
