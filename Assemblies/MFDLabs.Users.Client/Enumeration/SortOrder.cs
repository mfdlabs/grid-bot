﻿using System.Runtime.Serialization;

namespace MFDLabs.Users.Client.Enumeration
{
    [DataContract]
    public enum SortOrder
    {
        [EnumMember(Value = "Asc")]
        Asc = 0,

        [EnumMember(Value = "Desc")]
        Desc = 1,
    }
}