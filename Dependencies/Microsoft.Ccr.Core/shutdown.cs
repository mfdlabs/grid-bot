namespace Microsoft.Ccr.Core
{
    public class Shutdown
    {
        public SuccessFailurePort ResultPort
        {
            get
            {
                return this._resultPort;
            }
            set
            {
                this._resultPort = value;
            }
        }

        private SuccessFailurePort _resultPort = new SuccessFailurePort();
    }
}
