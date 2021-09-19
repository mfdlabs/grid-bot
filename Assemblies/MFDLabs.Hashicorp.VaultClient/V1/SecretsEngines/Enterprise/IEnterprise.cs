using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.KeyManagement;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.KMIP;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.Transform;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise
{
    /// <summary>
    /// Enterprise Secrets Engines
    /// </summary>
    public interface IEnterprise
    {
        /// <summary>
        /// The KeyManagement Secrets Engine.
        /// </summary>
        IKeyManagementSecretsEngine KeyManagement { get; }

        /// <summary>
        /// The KMIP Secrets Engine.
        /// </summary>
        IKMIPSecretsEngine KMIP { get; }

        /// <summary>
        /// The Transform Secrets Engine.
        /// </summary>
        ITransformSecretsEngine Transform { get; }
    }
}