//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2022 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.


using System;
using Azure.Core;
using Microsoft.Extensions.Configuration;


#pragma warning disable AZC0001 // Extension methods have to be in the correct namespace to appear in intellisense.
namespace DotNetCore.Azure.Configuration.KvCertificates
#pragma warning restore
{


    /// <summary>
    /// Extension methods for registering <see cref="AzureKvCertificatesConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class AzureKvCertificatesConfigurationExtensions
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vaultUri">The Azure Key Vault uri.</param>
        /// <param name="credential">The credential to to use for authentication.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVaultCertificates(
            this IConfigurationBuilder configurationBuilder,
            Uri vaultUri,
            TokenCredential credential)
        {
            return configurationBuilder.AddAzureKeyVaultCertificates(vaultUri, credential, new AzureKvCertificatesConfigurationOptions());
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vaultUri">Azure Key Vault uri.</param>
        /// <param name="credential">The credential to to use for authentication.</param>
        /// <param name="options">The <see cref="AzureKvCertificatesConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVaultCertificates(
            this IConfigurationBuilder configurationBuilder,
            Uri vaultUri,
            TokenCredential credential,
            AzureKvCertificatesConfigurationOptions options)
        {
            options = options ?? new AzureKvCertificatesConfigurationOptions();
            options.VaultUri = vaultUri;
            options.Credential = credential;
            return configurationBuilder.AddAzureKeyVaultCertificates(options);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="credential">The credential to to use for authentication.</param>
        /// <param name="options">The <see cref="AzureKvCertificatesConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVaultCertificates(
            this IConfigurationBuilder configurationBuilder,
            TokenCredential credential,
            AzureKvCertificatesConfigurationOptions options)
        {
            options = options ?? new AzureKvCertificatesConfigurationOptions();
            options.Credential = credential;
            return configurationBuilder.AddAzureKeyVaultCertificates(options);
        }


        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="options">The <see cref="AzureKvCertificatesConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        internal static IConfigurationBuilder AddAzureKeyVaultCertificates(this IConfigurationBuilder configurationBuilder, AzureKvCertificatesConfigurationOptions options)
        {
            ArgumentValidation.AssertNotNull(configurationBuilder, nameof(configurationBuilder));
            ArgumentValidation.AssertNotNull(options, nameof(options));
            ArgumentValidation.AssertNotNull(options.VaultUri, $"{nameof(options)}.{nameof(options.VaultUri)}");
            ArgumentValidation.AssertNotNull(options.Credential, $"{nameof(options)}.{nameof(options.Credential)}");
            ArgumentValidation.AssertNotNull(options.KeyVaultCerficateNameEncoder, $"{nameof(options)}.{nameof(options.KeyVaultCerficateNameEncoder)}");
            configurationBuilder.Add(new AzureKvCertificatesConfigurationSource(options));

            return configurationBuilder;
        }
    }
}
