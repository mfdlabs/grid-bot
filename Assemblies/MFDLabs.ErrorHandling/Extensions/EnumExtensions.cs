using System.ComponentModel;

namespace MFDLabs.ErrorHandling.Extensions
{
    public static class EnumExtensions
    {
        public static string ToDescription<T>(this T self)
        {
            var fi = self.GetType().GetField(self.ToString());

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : self.ToString();
        }
    }
}
