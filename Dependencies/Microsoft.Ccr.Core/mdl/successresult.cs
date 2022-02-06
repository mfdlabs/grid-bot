namespace Microsoft.Ccr.Core
{
    public class SuccessResult
    {
        public SuccessResult() { }
        public SuccessResult(int status) => m_Status = status;
        public SuccessResult(string status) => m_StrStatus = status;

        public static SuccessResult Instance => _instance;
        public string StatusMessage => m_StrStatus;
        public int Status => m_Status;

        private static readonly SuccessResult _instance = new();
        private readonly int m_Status;
        private readonly string m_StrStatus;
    }
}
