using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SystemBackend.MFA.Okta
{
    internal class OktaProvider : AbstractMFAProviderBase<OktaConfig>, IOkta
    {
        public OktaProvider(Polymath polymath) : base(polymath)
        {
        }

        public override string Type => "okta";
    }
}