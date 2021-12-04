//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2021 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.


using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using DotNetCore.Azure.Configuration.KvCertificates;

namespace DotNetCore.Azure.Configuration.KvCerfificates.Tests
{
    public class ReloadControlKeyVaultProvider : AzureKvCertificatesConfigurationProvider
    {
        private TaskCompletionSource<object> _releaseTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource<object> _engageTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public ReloadControlKeyVaultProvider(CertificateClient certificateClient,
            SecretClient secretClient,
            AzureKvCertificatesConfigurationOptions options) : base(certificateClient, secretClient, options)
        {
        }

        protected override async Task WaitForReload()
        {
            _engageTaskCompletionSource.SetResult(null);
            await _releaseTaskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(60));
        }

        public async Task Wait()
        {
            await _engageTaskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(60));
        }

        public void Release()
        {
            if (!_engageTaskCompletionSource.Task.IsCompleted)
            {
                throw new InvalidOperationException("Provider is not waiting for reload");
            }

            var releaseTaskCompletionSource = _releaseTaskCompletionSource;
            _releaseTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            _engageTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            releaseTaskCompletionSource.SetResult(null);
        }
    }
}
