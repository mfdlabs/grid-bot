using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.TOTP
{
    internal class TOTPProvider : AbstractMFAProviderBase<TOTPConfig>, ITOTP
    {
        public TOTPProvider(Polymath polymath) : base(polymath)
        {
        }

        public override string Type => "totp";
    }
}