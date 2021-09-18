namespace MFDLabs.RequestContext
{
    public interface IRequestContextLoader
    {
        IRequestContext GetCurrentContext();
    }
}
