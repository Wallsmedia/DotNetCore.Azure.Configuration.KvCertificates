# Azure Key Vault Certificates configuration provider for Microsoft.Extensions.Configuration

The AspNetCore.Azure.Configuration.KvCertificate based on idea [DotNetCore.Azure.Configuration.KvSecrets](https://www.nuget.org/packages/DotNetCore.Azure.Configuration.KvSecrets)
which package allows storing configuration values using Azure Key Vault Certificates.

## Features

- Allows to load certifcates by list and map them into new names.
- Allows to load  certifcates into the configuration section.

## Getting started

### Install the package

Install the package with [DotNetCore.Azure.Configuration.KvCertificates](https://www.nuget.org/packages/DotNetCore.Azure.Configuration.KvCertificates):

**Version 6.x.x** : **supports only **Microsoft.AspNetCore.App** 6.0-***


```Powershell
    dotnet add package DotNetCore.Azure.Configuration.KvCertificates
```

### Prerequisites

You need an [Azure subscription][azure_sub] 


## Examples

To load initialize configuration from Azure Key Vault secrets call the `AddAzureKeyVault` on `ConfigurationBuilder`:

**Program.cs**

```C# 
    public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration(Startup.AddKvCertificatesConfigurations);
                    webBuilder.UseStartup<Startup>();
                });
```

**Startup.cs**

```C# 
        public static void AddKvCertificatesConfigurations(WebHostBuilderContext hostingContext, IConfigurationBuilder configurationBuilder)
        {
            var configBuilder = new ConfigurationBuilder().AddInMemoryCollection();
            IHostEnvironment env = hostingContext.HostingEnvironment;
            configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
            configBuilder.AddEnvironmentVariables();

            var config = configBuilder.Build();

            string KeyVaultUrl = config[nameof(KeyVaultUrl)];
            List<string> VaultCertificates = config.GetSection(nameof(VaultCertificates)).Get<List<string>>();
            string ConfigurationSectionPrefix = config[nameof(ConfigurationSectionPrefix)];

            var credential = new AzureCliCredential();
            //var credential = new DefaultAzureCredential();
            var client = new CertificateClient(vaultUri: new Uri(KeyVaultUrl), credential);
            var options = new AzureKvCertificatesConfigurationOptions()
            {
                ConfigurationSectionPrefix = ConfigurationSectionPrefix,
                VaultCertificates = VaultCertificates
            };

            configurationBuilder.AddAzureKeyVaultCertificates(client, options);
        }
```

**appsettings.json**

```JSON

  "ConfigurationSectionPrefix": "secret",
  "KeyVaultUrl": "https://secrets128654s235.vault.azure.net/",
  "VaultCertificates": [ "FuseEval--Certificate8", "CertificateLoadIn", "RealCertificateVault" ]

```

The [Azure Identity library][identity] provides easy Azure Active Directory support for authentication.

## Next steps

Read more about [configuration in ASP.NET Core][aspnetcore_configuration_doc].

## Contributing

This project welcomes contributions and suggestions.  Most contributions require
you to agree to a Contributor License Agreement (CLA) declaring that you have
the right to, and actually do, grant us the rights to use your contribution. For
details, visit [cla.microsoft.com][cla].

This project has adopted the [Microsoft Open Source Code of Conduct][coc].
For more information see the [Code of Conduct FAQ][coc_faq]
or contact [opencode@microsoft.com][coc_contact] with any
additional questions or comments.

![Impressions](https://azure-sdk-impressions.azurewebsites.net/api/impressions/azure-sdk-for-net%2Fsdk%2Fextensions%2FAzure.Extensions.AspNetCore.Configuration.Secrets%2FREADME.png)

<!-- LINKS -->
[azure_cli]: https://docs.microsoft.com/cli/azure
[azure_sub]: https://azure.microsoft.com/free/
[identity]: https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/identity/Azure.Identity/README.md
[aspnetcore_configuration_doc]: https://docs.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1
[error_codes]: https://docs.microsoft.com/rest/api/storageservices/blob-service-error-codes
