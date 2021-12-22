using System;

namespace MFDLabs.ErrorHandling.Extensions
{
    public static class ExceptionExtensions
    {
        public static ExceptionDetail ToDetail(this Exception ex) => new(ex);
        public static string ToDetailedString(this Exception ex) => ex.ToDetail().ToString();
    }
}
