using System;
using System.Collections.Generic;
using System.Linq;

namespace MFDLabs
{
    public class Converters
    {
        public static List<T> EnumToList<T>() 
            => Enum.GetValues(typeof(T))
                .Cast<T>()
                .ToList();
    }
}
