//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2021 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Azure.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace DotNetCore.Azure.Configuration.KvCertificates;


public delegate string KeyVaultSecretNameEncoder(string secretName);

/// <summary>
/// Options class used by the <see cref="AzureKvCertificatesConfigurationExtensions"/>.
/// </summary>

public class AzureKvCertificatesConfigurationOptions
{
    public const string DefaultConfigurationSectionPrefix = "certificates";

    /// <summary>
    /// Creates a new instance of <see cref="AzureKvCertificatesConfigurationOptions"/>.
    /// </summary>
    public AzureKvCertificatesConfigurationOptions()
    {
    }

    /// <summary>
    /// Gets or sets Azure KeyVault Uri
    /// </summary>
    public Uri VaultUri { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="TokenCredential"/> to use for client credentials.
    /// </summary>
    public TokenCredential Credential { get; set; }

    /// <summary>
    /// The secrets that should be pulled from the Key Vault Secrets.
    /// </summary>
    public List<string> VaultCertificates { get; set; } = new List<string>();

    /// <summary>
    /// The keys that should be pulled from the Key Vault Secrets and map to configuration values.
    /// </summary>
    public Dictionary<string, string> VaultCertificateMap { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Name Encoding Delegate for name conversion 
    /// Default implementation replaces '--' with ':' in the key name.
    /// </summary>
    public KeyVaultSecretNameEncoder KeyVaultCertificateNameEncoder { get; set; } = KeyVaultSecretNameEncoderDefault;

    /// <summary>
    /// Gets or sets a prefix for configuration section.
    /// </summary>
    public string ConfigurationSectionPrefix { get; set; } = DefaultConfigurationSectionPrefix;

    /// <summary>
    /// Gets or sets the timespan to wait between attempts at polling the Azure Key Vault for changes. <code>null</code> to disable reloading.
    /// </summary>
    public TimeSpan? ReloadInterval { get; set; }

    static string KeyVaultSecretNameEncoderDefault(string secretName)
    {
        return secretName.Replace("--", ConfigurationPath.KeyDelimiter);
    }

    /// <summary>
    /// Clients timeout connections - number of repeat times
    /// </summary>
    public int AzureErrorReloadTimes { get; set; } = 12;

    /// <summary>
    /// Clients timeout connections - delay before repeating 
    /// </summary>
    public int AzureErrorReloadDelay { get; set; } = 5;
}
