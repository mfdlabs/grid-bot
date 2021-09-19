using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.KeyValue.V1;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.KeyValue.V2;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.KeyValue
{
    /// <summary>
    /// The KeyValue Secrets Engine.
    /// </summary>
    public interface IKeyValueSecretsEngine
    {
        /// <summary>
        /// The V1 version of the KeyValue secrets engine.
        /// </summary>
        IKeyValueSecretsEngineV1 V1 { get; }

        /// <summary>
        /// The V2 version of the KeyValue secrets engine.
        /// </summary>
        IKeyValueSecretsEngineV2 V2 { get; }
    }
}