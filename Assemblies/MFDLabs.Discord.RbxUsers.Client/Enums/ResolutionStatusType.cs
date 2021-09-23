using System.Runtime.Serialization;

namespace MFDLabs.Discord.RbxUsers.Client
{
    [DataContract]
    public enum ResolutionStatusType
    {
        [EnumMember(Value = "error")]
        Error,

        [EnumMember(Value = "ok")]
        Success
    }
}
