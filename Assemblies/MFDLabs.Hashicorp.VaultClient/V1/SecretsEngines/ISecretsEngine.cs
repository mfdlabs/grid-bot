using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.ActiveDirectory;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.AliCloud;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.AWS;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Azure;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Consul;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Cubbyhole;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Database;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.GoogleCloud;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.GoogleCloudKMS;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Identity;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.KeyValue;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.MongoDBAtlas;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Nomad;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.OpenLDAP;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.PKI;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.RabbitMQ;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.SSH;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Terraform;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.TOTP;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Transit;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines
{
    /// <summary>
    /// The secrets engine interface.
    /// </summary>
    public interface ISecretsEngine
    {
        /// <summary>
        /// The Enterprise Secrets Engines.
        /// </summary>
        IEnterprise Enterprise { get; }

        /// <summary>
        /// The ActiveDirectory Secrets Engine.
        /// </summary>
        IActiveDirectorySecretsEngine ActiveDirectory { get; }

        /// <summary>
        /// The AliCloud Secrets Engine.
        /// </summary>
        IAliCloudSecretsEngine AliCloud { get; }

        /// <summary>
        /// The AWS Secrets Engine.
        /// </summary>
        IAWSSecretsEngine AWS { get; }

        /// <summary>
        /// The Azure Secrets Engine.
        /// </summary>
        IAzureSecretsEngine Azure { get; }

        /// <summary>
        /// The Consul Secrets Engine.
        /// </summary>
        IConsulSecretsEngine Consul { get; }

        /// <summary>
        /// The Cubbyhole Secrets Engine.
        /// </summary>
        ICubbyholeSecretsEngine Cubbyhole { get; }

        /// <summary>
        /// The Database Secrets Engine.
        /// </summary>
        IDatabaseSecretsEngine Database { get; }

        /// <summary>
        /// The GoogleCloud Secrets Engine.
        /// </summary>
        IGoogleCloudSecretsEngine GoogleCloud { get; }

        /// <summary>
        /// The GoogleCloud KMS Secrets Engine.
        /// </summary>
        IGoogleCloudKMSSecretsEngine GoogleCloudKMS { get; }

        /// <summary>
        /// The Identity Secrets Engine.
        /// </summary>
        IIdentitySecretsEngine Identity { get; }

        /// <summary>
        /// The KeyValue Secrets Engine.
        /// </summary>
        IKeyValueSecretsEngine KeyValue { get; }

        /// <summary>
        /// The MongoDBAtlas Secrets Engine.
        /// </summary>
        IMongoDBAtlasSecretsEngine MongoDBAtlas { get; }

        /// <summary>
        /// The Nomad Secrets Engine.
        /// </summary>
        INomadSecretsEngine Nomad { get; }

        /// <summary>
        /// The OpenLDAP Secrets Engine.
        /// </summary>
        IOpenLDAPSecretsEngine OpenLDAP { get; }

        /// <summary>
        /// The PKI Secrets Engine.
        /// </summary>
        IPKISecretsEngine PKI { get; }

        /// <summary>
        /// The RabbitMQ Secrets Engine.
        /// </summary>
        IRabbitMQSecretsEngine RabbitMQ { get; }

        /// <summary>
        /// The SSH Secrets Engine.
        /// </summary>
        ISSHSecretsEngine SSH { get; }

        /// <summary>
        /// The Terraform Secrets Engine.
        /// </summary>
        ITerraformSecretsEngine Terraform { get; }

        /// <summary>
        /// The TOTP Secrets Engine.
        /// </summary>
        ITOTPSecretsEngine TOTP { get; }

        /// <summary>
        /// The Transit Secrets Engine.
        /// </summary>
        ITransitSecretsEngine Transit { get; }
    }
}
