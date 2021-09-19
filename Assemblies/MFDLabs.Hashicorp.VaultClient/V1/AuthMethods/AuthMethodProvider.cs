using System;
using System.Threading.Tasks;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AliCloud;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AppRole;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AWS;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Azure;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Cert;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.CloudFoundry;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.GitHub;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Kerberos;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Kubernetes;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.LDAP;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.OCI;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Okta;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.RADIUS;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Token;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.UserPass;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods
{
    internal class AuthMethodProvider : IAuthMethod
    {
        private readonly Polymath _polymath;

        public AuthMethodProvider(Polymath polymath)
        {
            _polymath = polymath;

            LDAP = new LDAPAuthMethodProvider(_polymath);
            Token = new TokenAuthMethodProvider(_polymath);
        }

        public IAliCloudAuthMethod AliCloud => throw new NotImplementedException();

        public IAppRoleAuthMethod AppRole => throw new NotImplementedException();

        public IAWSAuthMethod AWS => throw new NotImplementedException();

        public IAzureAuthMethod Azure => throw new NotImplementedException();

        public ICloudFoundryAuthMethod CloudFoundry => throw new NotImplementedException();

        public IGitHubAuthMethod GitHub => throw new NotImplementedException();

        public IGitHubAuthMethod GoogleCloud => throw new NotImplementedException();

        public IKubernetesAuthMethod Kubernetes => throw new NotImplementedException();

        public ILDAPAuthMethod LDAP { get; }

        public IKerberosAuthMethod Kerberos => throw new NotImplementedException();

        public IOCIAuthMethod OCI => throw new NotImplementedException();

        public IOktaAuthMethod Okta => throw new NotImplementedException();

        public IRADIUSAuthMethod RADIUS => throw new NotImplementedException();

        public ICertAuthMethod Cert => throw new NotImplementedException();

        public ITokenAuthMethod Token { get; }

        public IUserPassAuthMethod UserPass => throw new NotImplementedException();

        public void ResetVaultToken()
        {
            _polymath.SetVaultTokenDelegate();
        }

        public async Task PerformImmediateLogin()
        {
            await _polymath.PerformImmediateLogin();
        }
    }
}