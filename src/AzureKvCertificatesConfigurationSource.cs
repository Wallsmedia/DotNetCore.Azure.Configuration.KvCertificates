//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2022 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace DotNetCore.Azure.Configuration.KvCertificates
{
    /// <summary>
    /// Represents Azure Key Vault secrets as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class AzureKvCertificatesConfigurationSource : IConfigurationSource
    {
        private readonly AzureKvCertificatesConfigurationOptions _options;

        public AzureKvCertificatesConfigurationSource(AzureKvCertificatesConfigurationOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new AzureKvCertificatesConfigurationProvider(_options);
        }
    }
}



