using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.Duo;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.Okta;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.PingID;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.TOTP;

namespace MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA
{
    /// <summary>
    /// MFA provider.
    /// </summary>
    internal class MFAProvider : IMFA
    {
        public MFAProvider(Polymath polymath)
        { 
            Duo = new DuoProvider(polymath);
            Okta = new OktaProvider(polymath);
            PingID = new PingIDProvider(polymath);
            TOTP = new TOTPProvider(polymath);
        }

        public IDuo Duo { get; }

        public IOkta Okta { get; }

        public IPingID PingID { get; }

        public ITOTP TOTP { get; }
    }
}