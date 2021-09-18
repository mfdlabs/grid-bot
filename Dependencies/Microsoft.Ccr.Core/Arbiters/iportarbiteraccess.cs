namespace Microsoft.Ccr.Core.Arbiters
{
    public interface IPortArbiterAccess
    {
        IPortElement TestForElement();

        IPortElement[] TestForMultipleElements(int count);

        void PostElement(IPortElement element);

        PortMode Mode { get; set; }
    }
}
