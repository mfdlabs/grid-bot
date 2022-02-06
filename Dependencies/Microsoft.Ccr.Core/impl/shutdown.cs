namespace Microsoft.Ccr.Core
{
    public class Shutdown
    {
        public SuccessFailurePort ResultPort
        {
            get => _resultPort;
            set => _resultPort = value;
        }

        private SuccessFailurePort _resultPort = new();
    }
}
