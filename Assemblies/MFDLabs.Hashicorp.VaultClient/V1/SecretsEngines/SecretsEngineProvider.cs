using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.ActiveDirectory;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.AliCloud;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.AWS;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Azure;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Consul;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Cubbyhole;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Database;
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
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.TOTP;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Transit;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Terraform;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines
{
    internal class SecretsEngineProvider : ISecretsEngine
    {
        public SecretsEngineProvider(Polymath polymath)
        {
            Enterprise = new EnterpriseProvider(polymath);

            ActiveDirectory = new ActiveDirectorySecretsEngineProvider(polymath);
            AliCloud = new AliCloudSecretsEngineProvider(polymath);
            AWS = new AWSSecretsEngineProvider(polymath);
            Azure = new AzureSecretsEngineProvider(polymath);
            Consul = new ConsulSecretsEngineProvider(polymath);
            Cubbyhole = new CubbyholeSecretsEngineProvider(polymath);
            Database = new DatabaseSecretsEngineProvider(polymath);
            GoogleCloud = new GoogleCloudSecretsEngineProvider(polymath);
            GoogleCloudKMS = new GoogleCloudKMSSecretsEngineProvider(polymath);
            Identity = new IdentitySecretsEngineProvider(polymath);
            KeyValue = new KeyValueSecretsEngineProvider(polymath);
            MongoDBAtlas = new MongoDBAtlasSecretsEngineProvider(polymath);
            Nomad = new NomadSecretsEngineProvider(polymath);
            OpenLDAP = new OpenLDAPSecretsEngineProvider(polymath);
            PKI = new PKISecretsEngineProvider(polymath);
            RabbitMQ = new RabbitMQSecretsEngineProvider(polymath);
            SSH = new SSHSecretsEngineProvider(polymath);
            Terraform = new TerraformSecretsEngineProvider(polymath);
            TOTP = new TOTPSecretsEngineProvider(polymath);
            Transit = new TransitSecretsEngineProvider(polymath);
        }

        public IEnterprise Enterprise { get; }

        public IActiveDirectorySecretsEngine ActiveDirectory { get; }

        public IAliCloudSecretsEngine AliCloud { get; }

        public IAWSSecretsEngine AWS { get; }

        public IAzureSecretsEngine Azure { get; }

        public IConsulSecretsEngine Consul { get; }

        public ICubbyholeSecretsEngine Cubbyhole { get; }

        public IDatabaseSecretsEngine Database { get; }

        public IGoogleCloudSecretsEngine GoogleCloud { get; }

        public IGoogleCloudKMSSecretsEngine GoogleCloudKMS { get; }

        public IKeyValueSecretsEngine KeyValue { get; }

        public IIdentitySecretsEngine Identity { get; }

        public IMongoDBAtlasSecretsEngine MongoDBAtlas { get; }

        public INomadSecretsEngine Nomad { get; }

        public IOpenLDAPSecretsEngine OpenLDAP { get; }

        public IPKISecretsEngine PKI { get; }

        public IRabbitMQSecretsEngine RabbitMQ { get; }

        public ISSHSecretsEngine SSH { get; }

        public ITerraformSecretsEngine Terraform { get; }

        public ITOTPSecretsEngine TOTP { get; }

        public ITransitSecretsEngine Transit { get; }
    }
}