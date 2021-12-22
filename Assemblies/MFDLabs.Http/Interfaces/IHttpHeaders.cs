using System.Collections.Generic;

namespace MFDLabs.Http
{
    public interface IHttpHeaders
    {
        IEnumerable<string> Keys { get; }
        string ContentType { get; set; }

        void Add(string name, string value);
        void AddOrUpdate(string name, string value);
        ICollection<string> Get(string name);
        bool Remove(string name);
    }
}
