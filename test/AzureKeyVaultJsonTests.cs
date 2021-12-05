//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2021 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using DotNetCore.Azure.Configuration.KvCertificates;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace DotNetCore.Azure.Configuration.KvCerfificates.Tests
{
    public class AzureKeyVaultJsonTests
    {

        /// <summary>
        /// (!!!) There're examples 
        /// </summary>
        string KeyVaultUrl = "https://mps-dev-microsevices.vault.azure.net/";
        string name = "dev-jwt-microservices";


        [Test]
        public void CertificateProviderJwtSmokeTest()
        {
            // Arrange
            var credential = new AzureCliCredential();
            var valtUri = new Uri(KeyVaultUrl);
            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = valtUri,
                Credential = credential,
                VaultCertificates = { name }
            };

            var source = new AzureKvCertificatesConfigurationSource(options);

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.Add(source);

            var configuration = configurationBuilder.Build();

            // Act
            var crt = configuration[AzureKvCertificatesConfigurationOptions.DefaultConfigurationSectionPrefix + ":" + name];
            var container = configuration.GetSection(AzureKvCertificatesConfigurationOptions.DefaultConfigurationSectionPrefix + ":" + name)
                .Get<KvCertificateConfigContainer>();

            // Assert
            Assert.NotNull(crt);
            Assert.NotNull(container);

        }

        [Test]
        public void CertificateJwtSingVerificationSmokeTest()
        {

            // Arrange

            var credential = new AzureCliCredential();
            var client = new CertificateClient(vaultUri: new Uri(KeyVaultUrl), credential);
            var secretClient = new SecretClient(vaultUri: new Uri(KeyVaultUrl), credential);


            // Act

            Pageable<CertificateProperties> certificateProperties = client.GetPropertiesOfCertificateVersions(name);
            CertificateProperties certificateProperty = certificateProperties.OrderByDescending(s => s.UpdatedOn).FirstOrDefault(w => w.Enabled.GetValueOrDefault());

            KeyVaultCertificateWithPolicy certificate = client.GetCertificate(name);
            var certName = certificate.Id.LocalPath.Split("/", StringSplitOptions.RemoveEmptyEntries)[1];

            Response<KeyVaultSecret> keyVaultSecretResp = secretClient.GetSecret(certName);
            KeyVaultSecret secret = keyVaultSecretResp.Value;

            string value = secret.Value;


            X509Certificate2 x509Cert = null;

            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidDataException($"Secret {certificate.SecretId} contains no value");
            }

            if (secret.Properties.ContentType is null || secret.Properties.ContentType == CertificateContentType.Pkcs12)
            {

                byte[] rawData = Convert.FromBase64String(value);
                x509Cert = new X509Certificate2(rawData);
            }
            else if (secret.Properties.ContentType == CertificateContentType.Pem)
            {
            }


            var token = CreateTestJwtTokenWithX509SigningCredentials(x509Cert);

            var clms = ValidateTokenWithX509SecurityToken(x509Cert, token);

            RSA RsaPb = x509Cert.GetRSAPublicKey();
            RSA RsaPr = x509Cert.GetRSAPrivateKey();

            string textToBeEncrypted = "Hello World";
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(textToBeEncrypted);
            byte[] encryptedBytes = RsaPb.Encrypt(bytesToBeEncrypted, RSAEncryptionPadding.OaepSHA256);

            byte[] decryptedBytes = RsaPr.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
            string result = Encoding.UTF8.GetString(decryptedBytes);


            Assert.NotNull(clms);
            Assert.AreEqual(textToBeEncrypted, result);

        }

        static string CreateTestJwtTokenWithX509SigningCredentials(X509Certificate2 signingCert)
        {
            var now = DateTime.UtcNow;
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                IssuedAt = now,
                Expires = now + TimeSpan.FromDays(1),
                Issuer = "TestJwtSmoke",
                Audience = "Audience-TestJwtSmoke",
                Subject = new ClaimsIdentity(new Claim[]
                        {
                        new Claim(ClaimTypes.Name, "RootMan"),
                        new Claim(ClaimTypes.Role, "Sales"),
                        new Claim(ClaimTypes.Role, "Admin"),
                        }),
                SigningCredentials = new X509SigningCredentials(signingCert)
            };

            JwtSecurityToken token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(token);
            return tokenString;
        }

        public static (ClaimsPrincipal, JwtSecurityToken) ValidateTokenWithX509SecurityToken(X509Certificate2 signingCert, string token)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);
            var key = new X509SecurityKey(signingCert);

            var validationParameters = new TokenValidationParameters()
            {

                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key
            };

            try
            {
                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                if (claimsPrincipal != null && validatedToken != null)
                {
                    return (claimsPrincipal, validatedToken as JwtSecurityToken);
                }
            }
            catch { }

            return (null, null);
        }



    }
}
