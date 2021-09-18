using System.Runtime.Serialization;

namespace MFDLabs.Http.ServiceClient
{
    [DataContract]
    public class PayloadError
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }
    }
}
