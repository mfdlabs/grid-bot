namespace Microsoft.Ccr.Core
{
    // Token: 0x0200000A RID: 10
    public interface IPort
    {
        // Token: 0x0600005F RID: 95
        void PostUnknownType(object item);

        // Token: 0x06000060 RID: 96
        bool TryPostUnknownType(object item);
    }
}
