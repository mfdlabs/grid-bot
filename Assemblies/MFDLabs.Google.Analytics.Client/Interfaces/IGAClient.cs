using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Google.Analytics.Client
{
    public interface IGAClient
    {
        void TrackPageView(string clientID, string documentLocationUrl, string source = "Server", bool shouldClose = false);
        Task TrackPageViewAsync(string clientID, string documentLocationUrl, string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default);
        void TrackEvent(string clientID, string category, string eventName, string label = "None", int value = 0, string source = "Server", bool shouldClose = false);
        Task TrackEventAsync(string clientID, string category, string eventName, string label = "None", int value = 0, string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default);
        void TrackTransaction(string clientID, string transactionID, string transactionAffiliation, double transactionRevenue = 0.0f, double transactionTax = 0.0f, string source = "Server", bool shouldClose = false);
        Task TrackTransactionAsync(string clientID, string transactionID, string transactionAffiliation, double transactionRevenue = 0.0f, double transactionTax = 0.0f, string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default);
        void TrackItem(string clientID, string transactionID, string itemName, double itemPrice = 0.0f, int itemQuantity = 1, string itemCategory = "None", string source = "Server", bool shouldClose = false);
        Task TrackItemAsync(string clientID, string transactionID, string itemName, double itemPrice = 0.0f, int itemQuantity = 1, string itemCategory = "None", string source = "Server", bool shouldClose = false, CancellationToken cancellationToken = default);
    }
}
