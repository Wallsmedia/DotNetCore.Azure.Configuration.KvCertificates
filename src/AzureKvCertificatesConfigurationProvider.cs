//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2021 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.Azure.Configuration.KvCertificates;

/// <summary>
/// An AzureKeyVault based <see cref="ConfigurationProvider"/>.
/// </summary>
public class AzureKvCertificatesConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly TimeSpan? _reloadInterval;
    private Task _pollingTask;

    private readonly CertificateClient _certificateClient;
    private readonly SecretClient _secretClient;

    private List<string> _uploadKeyList;
    private Dictionary<string, string> _uploadAndMapKeys;
    private KeyVaultSecretNameEncoder _keyVaultSecretNameEncoder;
    private string _configurationSectionPrefix;
    private Dictionary<string, LoadedCertificate> _loadedCertificates;

    private bool _fullLoad;
    private readonly CancellationTokenSource _cancellationToken;
    private readonly AzureKvCertificatesConfigurationOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="AzureKvCertificatesConfigurationProvider"/>.
    /// </summary>
    /// <param name="certificateClient">The client <see cref="CertificateClient"/></param>
    /// <param name="secretClient">The client <see cref="SecretClient"/></param>
    /// <param name="options">The <see cref="AzureKvCertificatesConfigurationOptions"/> to use for configuration options.</param>
    public AzureKvCertificatesConfigurationProvider(CertificateClient certificateClient,
        SecretClient secretClient,
        AzureKvCertificatesConfigurationOptions options)
    {
        ArgumentValidation.AssertNotNull(certificateClient, nameof(certificateClient));
        ArgumentValidation.AssertNotNull(secretClient, nameof(secretClient));
        ArgumentValidation.AssertNotNull(options, nameof(options));
        ArgumentValidation.AssertNotNull(options.VaultUri, nameof(options.VaultUri));
        ArgumentValidation.AssertNotNull(options.KeyVaultCertificateNameEncoder, nameof(options.KeyVaultCertificateNameEncoder));

        _reloadInterval = options.ReloadInterval;

        _certificateClient = certificateClient;
        _secretClient = secretClient;

        _uploadKeyList = options.VaultCertificates != null ? new List<string>(options.VaultCertificates) : new List<string>();
        _uploadAndMapKeys = options.VaultCertificateMap != null ? new Dictionary<string, string>(options.VaultCertificateMap) : new Dictionary<string, string>();
        _configurationSectionPrefix = options.ConfigurationSectionPrefix;
        _keyVaultSecretNameEncoder = options.KeyVaultCertificateNameEncoder;

        _fullLoad = _uploadKeyList.Count == 0 && _uploadAndMapKeys.Count == 0;
        _cancellationToken = new CancellationTokenSource();
        _options = options;
    }

    /// <summary>
    /// Creates a new instance of <see cref="AzureKvCertificatesConfigurationProvider"/>.
    /// </summary>
    /// <param name="options">The <see cref="AzureKvCertificatesConfigurationOptions"/> to use for configuration options.</param>

    public AzureKvCertificatesConfigurationProvider(AzureKvCertificatesConfigurationOptions options)
    {
        ArgumentValidation.AssertNotNull(options, nameof(options));
        ArgumentValidation.AssertNotNull(options.Credential, nameof(options.Credential));
        ArgumentValidation.AssertNotNull(options.VaultUri, nameof(options.VaultUri));
        ArgumentValidation.AssertNotNull(options.KeyVaultCertificateNameEncoder, nameof(options.KeyVaultCertificateNameEncoder));

        _reloadInterval = options.ReloadInterval;

        _certificateClient = new CertificateClient(options.VaultUri, options.Credential);
        _secretClient = new SecretClient(options.VaultUri, options.Credential);

        _uploadKeyList = options.VaultCertificates != null ? new List<string>(options.VaultCertificates) : new List<string>();
        _uploadAndMapKeys = options.VaultCertificateMap != null ? new Dictionary<string, string>(options.VaultCertificateMap) : new Dictionary<string, string>();
        _configurationSectionPrefix = options.ConfigurationSectionPrefix;
        _keyVaultSecretNameEncoder = options.KeyVaultCertificateNameEncoder;

        _fullLoad = _uploadKeyList.Count == 0 && _uploadAndMapKeys.Count == 0;
        _cancellationToken = new CancellationTokenSource();
        _options = options;
    }

    #region IConfigurationProvider
    /// <summary>
    /// Load secrets into this provider.
    /// </summary>
    public override void Load()
    {
        bool loaded = false;
        int tolerance = _options.AzureErrorReloadTimes;
        do
        {

            try
            {
                LoadAsync().GetAwaiter().GetResult();
                loaded = true;

            }
            catch (Exception /*ex*/)
            {
                Thread.Sleep(TimeSpan.FromSeconds(_options.AzureErrorReloadDelay));
                loaded = false;
                if (tolerance-- <= 0)
                {
                    throw;
                }
            }

        } while (!loaded);
    }

    #endregion

    private async Task PollForSecretChangesAsync()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            await WaitForReload().ConfigureAwait(false);
            try
            {
                await LoadAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }

    protected virtual Task WaitForReload()
    {
        // WaitForReload is only called when the _reloadInterval has a value.
        return Task.Delay(_reloadInterval.Value, _cancellationToken.Token);
    }

    private async Task LoadAsync()
    {
        using var certificateLoader = new ParallelCertificateLoader(_certificateClient, _secretClient);
        var newLoadedCertificates = new Dictionary<string, LoadedCertificate>();
        var oldLoadedCertificates = Interlocked.Exchange(ref _loadedCertificates, null);
        if (_fullLoad)
        {
            AsyncPageable<CertificateProperties> secretPages = _certificateClient.GetPropertiesOfCertificatesAsync();
            await foreach (var certificate in secretPages.ConfigureAwait(false))
            {
                if (certificate.Enabled != true)
                {
                    continue;
                }
                VerifyCertificateToLoad(certificateLoader, newLoadedCertificates, oldLoadedCertificates, certificate);
            }
        }
        else
        {
            foreach (var key in _uploadKeyList)
            {
                AsyncPageable<CertificateProperties> certificateProperties = _certificateClient.GetPropertiesOfCertificateVersionsAsync(key);
                var certificateList = await certificateProperties.ToListAsync();
                var certificate = certificateList.OrderByDescending(s => s.UpdatedOn).FirstOrDefault(w => w.Enabled.GetValueOrDefault());
                if (certificate != null)
                {
                    VerifyCertificateToLoad(certificateLoader, newLoadedCertificates, oldLoadedCertificates, certificate);
                }
            }

            foreach (var keyValue in _uploadAndMapKeys)
            {
                AsyncPageable<CertificateProperties> certificateProperties = _certificateClient.GetPropertiesOfCertificateVersionsAsync(keyValue.Key);
                var certificateList = await certificateProperties.ToListAsync();
                var certificate = certificateList.OrderByDescending(s => s.UpdatedOn).FirstOrDefault(w => w.Enabled.GetValueOrDefault());
                if (certificate != null)
                {
                    VerifyCertificateToLoad(certificateLoader, newLoadedCertificates, oldLoadedCertificates, certificate);
                }
            }
        }

        var loadedCertificates = await certificateLoader.WaitForAllSecrets().ConfigureAwait(false);

        foreach (var (keyVaultCertificateWithPolicy, keyVaultSecret) in loadedCertificates)
        {
            if (keyVaultSecret == null)
            {
                continue;
            }
            string configName = keyVaultSecret.Name;

            if (!_fullLoad)
            {
                if (_uploadAndMapKeys.Keys.Contains(configName))
                {
                    configName = _uploadAndMapKeys[configName];
                }
            }

            if (!string.IsNullOrWhiteSpace(_configurationSectionPrefix))
            {
                configName = _configurationSectionPrefix + ConfigurationPath.KeyDelimiter + configName;
            }

            newLoadedCertificates.Add(keyVaultSecret.Name,
                 new LoadedCertificate
                 {
                     Key = _keyVaultSecretNameEncoder(configName),
                     KeyVaultCertificateWithPolicy = keyVaultCertificateWithPolicy,
                     KeyVaultSecret = keyVaultSecret,
                     Updated = keyVaultSecret.Properties.UpdatedOn
                 });
        }

        _loadedCertificates = newLoadedCertificates;

        // Reload is needed if we are loading secrets that were not loaded before or
        // secret that was loaded previously is not available anymore
        if (loadedCertificates.Any() || oldLoadedCertificates?.Any() == true)
        {
            SetData(_loadedCertificates, fireToken: oldLoadedCertificates != null);
        }

        // schedule a polling task only if none exists and a valid delay is specified
        if (_pollingTask == null && _reloadInterval != null)
        {
            _pollingTask = PollForSecretChangesAsync();
        }
    }

    private static void VerifyCertificateToLoad(ParallelCertificateLoader certificateLoader,
        Dictionary<string, LoadedCertificate> newLoadedCertificates,
        Dictionary<string, LoadedCertificate> oldLoadedCertificates,
        CertificateProperties certificate)
    {
        var certificateName = certificate.Name;
        if (certificate.Enabled == true)
        {
            if (oldLoadedCertificates != null
                && oldLoadedCertificates.TryGetValue(certificateName, out var existingCertificate)
                && existingCertificate.IsUpToDate(certificate.UpdatedOn))
            {
                oldLoadedCertificates.Remove(certificateName);
                newLoadedCertificates.Add(certificateName, existingCertificate);
            }
            else
            {
                certificateLoader.AddCertificateToLoad(certificate.Name);
            }
        }
        else
        {
            if (oldLoadedCertificates != null
                && oldLoadedCertificates.TryGetValue(certificateName, out var existingCertificate))
            {
                oldLoadedCertificates.Remove(certificateName);
            }
        }
    }

    private void SetData(Dictionary<string, LoadedCertificate> loadedCertificates, bool fireToken)
    {
        var data = new Dictionary<string, string>(loadedCertificates.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var certificateItem in loadedCertificates)
        {
            var certificate = certificateItem.Value.KeyVaultCertificateWithPolicy;
            var secret = certificateItem.Value.KeyVaultSecret;
            string json = "";
            var kvCertificateConfigContainer = new KvCertificateConfigContainer();
            if (secret.Properties.ContentType is null || secret.Properties.ContentType == CertificateContentType.Pkcs12)
            {
                kvCertificateConfigContainer.Pkcs12Base64 = new string(secret.Value.ToCharArray());
                json = JsonSerializer.Serialize(kvCertificateConfigContainer);
                data.Add(certificateItem.Value.Key, json);
            }
            else if (secret.Properties.ContentType == CertificateContentType.Pem)
            {
                kvCertificateConfigContainer.Pkcs12Base64 = new string(secret.Value.ToCharArray());
                kvCertificateConfigContainer.PemCertBase64 = new string(Convert.ToBase64String(certificate.Cer).ToCharArray());
                json = JsonSerializer.Serialize(kvCertificateConfigContainer);
                data.Add(certificateItem.Value.Key, json);
            }
        }

        Data = data;
        if (fireToken)
        {
            OnReload();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _cancellationToken.Cancel();
        _cancellationToken.Dispose();
    }

    private class LoadedCertificate
    {
        public string Key { get; set; }
        public KeyVaultCertificateWithPolicy KeyVaultCertificateWithPolicy { get; set; }
        public KeyVaultSecret KeyVaultSecret { get; set; }
        public DateTimeOffset? Updated { get; set; }

        public bool IsUpToDate(DateTimeOffset? updated)
        {
            if (updated.HasValue != Updated.HasValue)
            {
                return false;
            }

            return updated.GetValueOrDefault() == Updated.GetValueOrDefault();
        }
    }
}
