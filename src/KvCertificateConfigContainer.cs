//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2021 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Azure.Core;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetCore.Azure.Configuration.KvCertificates;

/// <summary>
/// Json based Kv Certificate Configuration
/// container that will be stored in the 
/// </summary>

[TypeConverter(typeof(KvCertificateConfigContainerConverter))]
public class KvCertificateConfigContainer
{
    [JsonIgnore]
    private X509Certificate2 _x509Certificate2;

    [JsonIgnore]
    public X509Certificate2 X509Certificate2
    {
        get
        {
            if (_x509Certificate2 == null)
            {
                try
                {
                    if (Pkcs12Base64 != null && PemCertBase64 == null)
                    {
                        byte[] rawData = Convert.FromBase64String(Pkcs12Base64);
                        _x509Certificate2 = X509CertificateLoader.LoadPkcs12(rawData, null);
                    }
                    else if (Pkcs12Base64 != null && PemCertBase64 != null)
                    {
                        byte[] cer = Convert.FromBase64String(PemCertBase64);
                        var certificate = PemReader.LoadCertificate(Pkcs12Base64.AsSpan(), cer, allowCertificateOnly: true);
                        // SSL NetCore PEM support ssl - stream 
                        byte[] pkcs12 = certificate.Export(X509ContentType.Pfx);
                        _x509Certificate2 = X509CertificateLoader.LoadPkcs12(pkcs12, null);
                    }
                }
                catch { /*Ignore*/ }
            }
            return _x509Certificate2;
        }
    }

    public string Pkcs12Base64 { get; set; }
    public string PemCertBase64 { get; set; }
}

public class KvCertificateConfigContainerConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        string str = (string)value;
        object obj = null;
        try
        {
            obj = JsonSerializer.Deserialize(str, typeof(KvCertificateConfigContainer));
        }
        catch {/* ignore */ }
        return obj;

    }
}



