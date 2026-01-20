//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2025 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DotNetCore.Azure.Configuration.KvCertificates.Tests;


public class AzureKeyVaultJsonTests
{

    /// <summary>
    /// (!!!) There're examples 
    /// </summary>
    string KeyVaultUrl = "https://mps-dev-microsevices.vault.azure.net/";
    string name_pfx = "dev-jwt-microservices-pfx";
    string name_pem = "dev-jwt-microservices-pem";


    [Test]
    public void CertificateProviderJwtSmokeTestPfx()
    {
        // Arrange
        var credential = new AzureCliCredential();
        var valtUri = new Uri(KeyVaultUrl);
        var options = new AzureKvCertificatesConfigurationOptions
        {
            VaultUri = valtUri,
            Credential = credential,
            VaultCertificates = { name_pfx }
        };

        var source = new AzureKvCertificatesConfigurationSource(options);

        ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.Add(source);

        var configuration = configurationBuilder.Build();

        // Act
        var crt = configuration[AzureKvCertificatesConfigurationOptions.DefaultConfigurationSectionPrefix + ":" + name_pfx];
        var container = configuration.GetSection(AzureKvCertificatesConfigurationOptions.DefaultConfigurationSectionPrefix + ":" + name_pfx)
            .Get<KvCertificateConfigContainer>();

        // Assert
        ClassicAssert.NotNull(crt);
        ClassicAssert.NotNull(container);

        var x509Cert = container.X509Certificate2;

        var token = CreateTestJwtTokenWithX509SigningCredentials(x509Cert);

        var clms = ValidateTokenWithX509SecurityToken(x509Cert, token);

        RSA RsaPb = x509Cert.GetRSAPublicKey();
        RSA RsaPr = x509Cert.GetRSAPrivateKey();

        string textToBeEncrypted = "Hello World";
        byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(textToBeEncrypted);
        byte[] encryptedBytes = RsaPb.Encrypt(bytesToBeEncrypted, RSAEncryptionPadding.OaepSHA256);

        byte[] decryptedBytes = RsaPr.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
        string result = Encoding.UTF8.GetString(decryptedBytes);


        ClassicAssert.NotNull(clms);
        ClassicAssert.AreEqual(textToBeEncrypted, result);

    }

    [Test]
    public void CertificateProviderJwtSmokeTestPem()
    {
        // Arrange
        var credential = new AzureCliCredential();
        var valtUri = new Uri(KeyVaultUrl);
        var options = new AzureKvCertificatesConfigurationOptions
        {
            VaultUri = valtUri,
            Credential = credential,
            VaultCertificates = { name_pem }
        };

        var source = new AzureKvCertificatesConfigurationSource(options);
        ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.Add(source);

        var configuration = configurationBuilder.Build();

        // Act
        var crt = configuration[AzureKvCertificatesConfigurationOptions.DefaultConfigurationSectionPrefix + ":" + name_pem];
        var container = configuration.GetSection(AzureKvCertificatesConfigurationOptions.DefaultConfigurationSectionPrefix + ":" + name_pem)
            .Get<KvCertificateConfigContainer>();

        // Assert
        ClassicAssert.NotNull(crt);
        ClassicAssert.NotNull(container);

        var x509Cert = container.X509Certificate2;

        var token = CreateTestJwtTokenWithX509SigningCredentials(x509Cert);

        var clms = ValidateTokenWithX509SecurityToken(x509Cert, token);

        RSA RsaPb = x509Cert.GetRSAPublicKey();
        RSA RsaPr = x509Cert.GetRSAPrivateKey();

        string textToBeEncrypted = "Hello World";
        byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(textToBeEncrypted);
        byte[] encryptedBytes = RsaPb.Encrypt(bytesToBeEncrypted, RSAEncryptionPadding.OaepSHA256);

        byte[] decryptedBytes = RsaPr.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
        string result = Encoding.UTF8.GetString(decryptedBytes);


        ClassicAssert.NotNull(clms);
        ClassicAssert.AreEqual(textToBeEncrypted, result);
    }

    [Test]
    public void CertificateJwtSingVerificationSmokeTestPfx()
    {

        // Arrange

        var credential = new AzureCliCredential();
        var client = new CertificateClient(vaultUri: new Uri(KeyVaultUrl), credential);
        var secretClient = new SecretClient(vaultUri: new Uri(KeyVaultUrl), credential);


        // Act

        Pageable<CertificateProperties> certificateProperties = client.GetPropertiesOfCertificateVersions(name_pfx);
        CertificateProperties certificateProperty = certificateProperties.OrderByDescending(s => s.UpdatedOn).FirstOrDefault(w => w.Enabled.GetValueOrDefault());

        KeyVaultCertificateWithPolicy certificate = client.GetCertificate(name_pfx);
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
            x509Cert = X509CertificateLoader.LoadPkcs12(rawData, null);
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


        ClassicAssert.NotNull(clms);
        ClassicAssert.AreEqual(textToBeEncrypted, result);

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


