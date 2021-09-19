using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.Duo;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.Okta;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.PingID;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.TOTP;

namespace MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA
{
    /// <summary>
    /// The MFA interface.
    /// </summary>
    public interface IMFA
    {
        IDuo Duo { get; }

        IOkta Okta { get; }

        IPingID PingID { get; }

        ITOTP TOTP { get; }
    }
}