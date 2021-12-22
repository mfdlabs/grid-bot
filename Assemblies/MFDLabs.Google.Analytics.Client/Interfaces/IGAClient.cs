using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Google.Analytics.Client
{
    public interface IGaClient
    {
        void TrackPageView(string clientId,
            string documentLocationUrl,
            string source = "Server",
            bool shouldClose = false);
        Task TrackPageViewAsync(string clientId,
            string documentLocationUrl,
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default);
        void TrackEvent(string clientId,
            string category,
            string eventName,
            string label = "None",
            int value = 0,
            string source = "Server",
            bool shouldClose = false);
        Task TrackEventAsync(string clientId,
            string category,
            string eventName,
            string label = "None",
            int value = 0,
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default);
        void TrackTransaction(string clientId,
            string transactionId,
            string transactionAffiliation,
            double transactionRevenue = 0.0f,
            double transactionTax = 0.0f,
            string source = "Server",
            bool shouldClose = false);
        Task TrackTransactionAsync(string clientId,
            string transactionId,
            string transactionAffiliation,
            double transactionRevenue = 0.0f,
            double transactionTax = 0.0f,
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default);
        void TrackItem(string clientId,
            string transactionId,
            string itemName,
            double itemPrice = 0.0f,
            int itemQuantity = 1,
            string itemCategory = "None",
            string source = "Server",
            bool shouldClose = false);
        Task TrackItemAsync(string clientId,
            string transactionId,
            string itemName,
            double itemPrice = 0.0f,
            int itemQuantity = 1,
            string itemCategory = "None",
            string source = "Server",
            bool shouldClose = false,
            CancellationToken cancellationToken = default);
    }
}
