//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2025 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace DotNetCore.Azure.Configuration.KvCertificates.Tests;

public static class ConfigurationProviderExtensions
{
    public static string Get(this IConfigurationProvider provider, string key)
    {
        string value;

        if (!provider.TryGet(key, out value))
        {
            throw new InvalidOperationException("Key not found");
        }

        return value;
    }
}