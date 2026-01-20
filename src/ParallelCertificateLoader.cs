//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2021 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.Azure.Configuration.KvCertificates;

public class ParallelCertificateLoader : IDisposable
{
    private const int ParallelismLevel = 32;
    private readonly CertificateClient _client;
    private readonly SecretClient _secretClient;
    private readonly SemaphoreSlim _semaphore;
    private readonly List<Task<(KeyVaultCertificateWithPolicy,KeyVaultSecret)>> _tasks;

    public ParallelCertificateLoader(CertificateClient client,SecretClient secretClient)
    {
        _client = client;
        _secretClient = secretClient;
        _semaphore = new SemaphoreSlim(ParallelismLevel, ParallelismLevel);
        _tasks = new List<Task<(KeyVaultCertificateWithPolicy,KeyVaultSecret)>>();
    }

    public void AddCertificateToLoad(string name)
    {
        _tasks.Add(GetCertificateAsync(name));
    }

    private async Task<(KeyVaultCertificateWithPolicy, KeyVaultSecret)> GetCertificateAsync(string name)
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            var keyVaultCertificateWithPolicy = await _client.GetCertificateAsync(name).ConfigureAwait(false);
            var certIdArr = keyVaultCertificateWithPolicy.Value.Id.LocalPath.Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (certIdArr.Length >=2)
            {
                var certName = certIdArr[1];
                var keyVaultSecret = await _secretClient.GetSecretAsync(certName).ConfigureAwait(false);
                return (keyVaultCertificateWithPolicy.Value, keyVaultSecret.Value);
            }
            else
            {
                return (keyVaultCertificateWithPolicy.Value, null);
            }
        }

        finally
        {
            _semaphore.Release();
        }
    }

    public Task<(KeyVaultCertificateWithPolicy, KeyVaultSecret)[]> WaitForAllSecrets()
    {
        return Task.WhenAll(_tasks);
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}