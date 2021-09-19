using System;
using MFDLabs.Hashicorp.VaultClient.Core;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AliCloud;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AppRole;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AWS;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Azure;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Cert;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.CloudFoundry;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Custom;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.GitHub;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.GoogleCloud;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.JWT;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Kerberos;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Kubernetes;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.LDAP;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Okta;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.RADIUS;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Token;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.UserPass;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods
{
    internal static class AuthProviderFactory
    {
        public static IAuthMethodLoginProvider CreateAuthenticationProvider(IAuthMethodInfo authInfo, Polymath polymath)
        {
            if (authInfo.AuthMethodType == AuthMethodType.AliCloud)
            {
                return new AliCloudAuthMethodLoginProvider(authInfo as AliCloudAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.AppRole)
            {
                return new AppRoleAuthMethodLoginProvider(authInfo as AppRoleAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.AWS)
            {
                return new AWSAuthMethodLoginProvider(authInfo as AbstractAWSAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.Azure)
            {
                return new AzureAuthMethodLoginProvider(authInfo as AzureAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.GitHub)
            {
                return new GitHubAuthMethodLoginProvider(authInfo as GitHubAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.GoogleCloud)
            {
                return new GoogleCloudAuthMethodLoginProvider(authInfo as GoogleCloudAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.JWT)
            {
                return new JWTAuthMethodLoginProvider(authInfo as JWTAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.Kubernetes)
            {
                return new KubernetesAuthMethodLoginProvider(authInfo as KubernetesAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.LDAP)
            {
                return new LDAPAuthMethodLoginProvider(authInfo as LDAPAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.Kerberos)
            {
                return new KerberosAuthMethodLoginProvider(authInfo as KerberosAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.Okta)
            {
                return new OktaAuthMethodLoginProvider(authInfo as OktaAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.RADIUS)
            {
                return new RADIUSAuthMethodLoginProvider(authInfo as RADIUSAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.Cert)
            {
                // we have attached the certificates to request elsewhere.
                return new CertAuthMethodLoginProvider(authInfo as CertAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.Token)
            {
                return new TokenAuthMethodLoginProvider(authInfo as TokenAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.UserPass)
            {
                return new UserPassAuthMethodLoginProvider(authInfo as UserPassAuthMethodInfo, polymath);
            }

            if (authInfo.AuthMethodType == AuthMethodType.CloudFoundry)
            {
                return new CloudFoundryAuthMethodLoginProvider(authInfo as CloudFoundryAuthMethodInfo, polymath);
            }

            var customAuthMethodInfo = authInfo as CustomAuthMethodInfo;

            if (customAuthMethodInfo != null)
            {
                return new CustomAuthMethodLoginProvider(customAuthMethodInfo, polymath);
            }

            throw new NotSupportedException("The requested authentication backend type is not supported: " + authInfo.AuthMethodType);
        }
    }
}