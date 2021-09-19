﻿
using System;
using System.Security.Cryptography.X509Certificates;
using MFDLabs.Hashicorp.VaultClient.Core;

namespace MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Cert
{
    /// <summary>
    /// Represents the login information for the Cert Authentication backend.
    /// </summary>
    public class CertAuthMethodInfo : AbstractAuthMethodInfo
    {
        /// <summary>
        /// Gets the type of the authentication backend.
        /// </summary>
        /// <value>
        /// The type of the authentication backend.
        /// </value>
        public override AuthMethodType AuthMethodType => AuthMethodType.Cert;

        /// <summary>
        /// Gets the mount point.
        /// Presence or absence of leading or trailing slashes don't matter.
        /// </summary>
        /// <value>
        /// The mount point.
        /// </value>
        public string MountPoint { get; }

        /// <summary>
        /// Gets the client certificate.
        /// </summary>
        /// <value>
        /// The client certificate.
        /// </value>
        public X509Certificate2 ClientCertificate { get; }

        /// <summary>
        /// Optionally, you may specify a single certificate role to authenticate against.
        /// </summary>
        public string RoleName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertAuthMethodInfo" /> class.
        /// </summary>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="roleName">A single certificate role to authenticate against.</param>
        public CertAuthMethodInfo(X509Certificate2 clientCertificate, string roleName = null) : this(AuthMethodType.Cert.Type, clientCertificate, roleName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertAuthMethodInfo"/> class.
        /// </summary>
        /// <param name="mountPoint">The mount point.</param>
        /// <param name="clientCertificate">The client certificate.</param>
        /// <param name="roleName">A single certificate role to authenticate against.</param>
        public CertAuthMethodInfo(string mountPoint, X509Certificate2 clientCertificate, string roleName = null)
        {
            Checker.NotNull(mountPoint, "mountPoint");
            Checker.NotNull(clientCertificate, "clientCertificate");

            if (!clientCertificate.HasPrivateKey)
            {
                throw new ArgumentException("Certificate does not contain a private key.");
            }

            MountPoint = mountPoint;
            ClientCertificate = clientCertificate;
            RoleName = roleName;
        }
    }
}