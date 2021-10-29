using System.Reflection;
using System.ComponentModel;

namespace MFDLabs.ErrorHandling.Extensions
{
    public static class EnumExtensions
    {
        public static string ToDescription<T>(this T self)
        {
            FieldInfo fi = self.GetType().GetField(self.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0) return attributes[0].Description;
            else return self.ToString();
        }
    }
}
