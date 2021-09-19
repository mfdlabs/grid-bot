using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.Duo
{
    internal class DuoProvider : AbstractMFAProviderBase<DuoConfig>, IDuo
    {
        public DuoProvider(Polymath polymath) : base(polymath)
        {
        }

        public override string Type => "duo";
    }
}