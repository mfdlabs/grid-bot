using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.KeyManagement;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.KMIP;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.Transform;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise
{
    /// <summary>
    /// Enterprise Secrets Engines
    /// </summary>
    internal class EnterpriseProvider : IEnterprise
    {
        public EnterpriseProvider(Polymath polymath)
        {
            KeyManagement = new KeyManagementSecretsEngineProvider(polymath);
            KMIP = new KMIPSecretsEngineProvider(polymath);
            Transform = new TransformSecretsEngineProvider(polymath);
        }

        public IKeyManagementSecretsEngine KeyManagement { get; }
        public IKMIPSecretsEngine KMIP { get; }
        public ITransformSecretsEngine Transform { get; }
    }
}