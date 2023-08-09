using System;

namespace Instrumentation.LegacySupport
{
    internal class NoCountersSpecifiedForCategoryException : Exception
    {
        public NoCountersSpecifiedForCategoryException(string categoryName)
            : base(categoryName)
        { }
    }
}
