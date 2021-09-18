namespace Microsoft.Ccr.Core
{
    public class SuccessResult
    {
        public SuccessResult()
        {
        }

        public SuccessResult(int status)
        {
            this.m_Status = status;
        }

        public SuccessResult(string status)
        {
            this.m_StrStatus = status;
        }

        public static SuccessResult Instance
        {
            get
            {
                return SuccessResult._instance;
            }
        }

        public string StatusMessage
        {
            get
            {
                return this.m_StrStatus;
            }
        }

        public int Status
        {
            get
            {
                return this.m_Status;
            }
        }

        private static SuccessResult _instance = new SuccessResult();

        private readonly int m_Status;

        private readonly string m_StrStatus;
    }
}
