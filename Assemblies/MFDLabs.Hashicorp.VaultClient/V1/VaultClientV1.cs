using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines;
using MFDLabs.Hashicorp.VaultClient.V1.SystemBackend;

namespace MFDLabs.Hashicorp.VaultClient.V1
{
    internal class VaultClientV1 : IVaultClientV1
    {
        public VaultClientV1(Polymath polymath)
        { 
            System = new SystemBackendProvider(polymath);
            Auth = new AuthMethodProvider(polymath);
            Secrets = new SecretsEngineProvider(polymath);
        }

        public ISecretsEngine Secrets { get; }

        public IAuthMethod Auth { get; }

        public ISystemBackend System { get; }
    }
}
