//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2022 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Moq;

namespace DotNetCore.Azure.Configuration.KvCerfificates.Tests.Helpers
{
    public static class MockSetupUtils
    {
        public static void SetClientMocks(
            Mock<CertificateClient> mockCrtClient,
            Mock<SecretClient> mockSecClient,
            Func<string, Task> getSecretCallback,
            params (string name, string rawdata, bool? enabled, DateTimeOffset? updated)[] pagesTestData)
        {
            SetClientMocks(mockCrtClient, mockSecClient, getSecretCallback, pagesTestData.ToList());
        }

        public static void SetClientMocks(
            Mock<CertificateClient> mockCrtClient,
            Mock<SecretClient> mockSecClient,
            Func<string, Task> getSecretCallback,
            List<(string name, string rawdata, bool? enabled, DateTimeOffset? updated)> pagesTestData)

        {

            mockCrtClient.Reset();
            mockSecClient.Reset();  

            getSecretCallback ??= (_ => Task.CompletedTask);

            List<CertificateProperties> certificateProperties = new();
            List<KeyVaultSecret> keyVaultSecrets = new();
            List<KeyVaultCertificateWithPolicy> keyVaultCertificateWithPolicies = new();

            foreach (var (name, value, enabled, updated) in pagesTestData)
            {
                keyVaultCertificateWithPolicies.Add(CreateCertificate(name, enabled, updated));
                keyVaultSecrets.Add(CreateSecret(name, value, enabled, updated));
                certificateProperties.Add(CreateCertificateProperties(name, value, enabled, updated));
            }

            mockCrtClient.Setup(m => m.GetPropertiesOfCertificatesAsync(false, default))
                .Returns((bool name, CancellationToken token) =>
               new MockAsyncPageable<CertificateProperties>(certificateProperties));

            foreach (var certificateProp in certificateProperties)
            {

                mockCrtClient.Setup(client => client.GetPropertiesOfCertificateVersionsAsync(certificateProp.Name, default))
                .Returns((string name, CancellationToken token) =>
                   new MockAsyncPageable<CertificateProperties>(certificateProperties));
            }


            foreach (var keyVaultSecret in keyVaultSecrets)
            {

                mockSecClient.Setup(client => client.GetSecretAsync(keyVaultSecret.Name, null, default))
               .Returns(async (string name,string version, CancellationToken token) =>
               {
                   return Response.FromValue(keyVaultSecret, Mock.Of<Response>());
               });
            }

            foreach (var keyVaultCertificateWithPolicy in keyVaultCertificateWithPolicies)
            {

                mockCrtClient.Setup(client => client.GetCertificateAsync(keyVaultCertificateWithPolicy.Name, default))
               .Returns( async (string name, CancellationToken token) =>
               {
                   await getSecretCallback(name);
                   return Response.FromValue(keyVaultCertificateWithPolicy, Mock.Of<Response>());
               });
            }

        }

        public static CertificateProperties CreateCertificateProperties(string name, string value, bool? enabled = true, DateTimeOffset? updated = null)
        {
            var id = new Uri("http://azure.com/keyvault/" + name);
            var  certificateProperties  = CertificateModelFactory.CertificateProperties(id, name: name, updatedOn: updated);
            certificateProperties.Enabled = enabled;
            return certificateProperties;

        }
        public static KeyVaultSecret CreateSecret(string name, string value, bool? enabled = true, DateTimeOffset? updated = null)
        {
            var id = new Uri("http://azure.com/keyvault/" + name);

            var secretProperties = SecretModelFactory.SecretProperties(id, name: name, updatedOn: updated);
            secretProperties.Enabled = enabled;

            return SecretModelFactory.KeyVaultSecret(secretProperties, value);
        }

        public static KeyVaultCertificateWithPolicy CreateCertificate(string name,
         bool? enabled = true,
         DateTimeOffset? updated = null)
        {
            var id = new Uri("http://azure.com/keyvault/" + name);

            var certificateProperties = CertificateModelFactory.CertificateProperties(
                id,
                name: name,
                updatedOn: updated,
                version: "0.1.0",
                x509thumbprint: new byte[] { 1, 2, 3, 34, 0 });

            updated = updated ?? DateTimeOffset.UtcNow;

            certificateProperties.Enabled = enabled;

            var certificatePolicy = new CertificatePolicy(WellKnownIssuerNames.Self, "CN=Azure SDK")
            {
                ReuseKey = true,
                CertificateTransparency = true,
                Exportable = false,
                ContentType = CertificateContentType.Pem,
                KeySize = 3072
            };

            return CertificateModelFactory.KeyVaultCertificateWithPolicy
                (certificateProperties,
                new Uri("http://azure.com/keyvault/" + name),
                new Uri("http://azure.com/keyvault/" + name),
                new byte[] { 1, 2, 3, 34, 0 },
                certificatePolicy);
        }
    }
}
