using System;
using System.Linq;
using System.Collections.Generic;

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
