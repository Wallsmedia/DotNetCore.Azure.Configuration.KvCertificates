# DotNetCore Azure Configuration Key Vault Certificates

The AspNetCore.Azure.Configuration.KvCertificate based on idea [DotNetCore.Azure.Configuration.KvSecrets](https://www.nuget.org/packages/DotNetCore.Azure.Configuration.KvSecrets)
which package allows storing configuration values using Azure Key Vault Certificates.

## Features

- Allows to load certificates by list and map them into new names.
- Allows to load  certificates into the configuration section.

## Getting started

### Install the package

Install the package with [DotNetCore.Azure.Configuration.KvCertificates](https://www.nuget.org/packages/DotNetCore.Azure.Configuration.KvCertificates):

**Version 10.0.x** : **supports only **Microsoft.AspNetCore.App** 10.0


```Powershell
    dotnet add package DotNetCore.Azure.Configuration.KvCertificates
```

### Prerequisites

You need an [Azure subscription][azure_sub] 


## Examples

To load initialize configuration from Azure Key Vault secrets call the `AddAzureKeyVault` on `ConfigurationBuilder`:

**Program.cs**

``` CSharp 
      var builder = WebApplication.CreateBuilder(args);
      builder.AddKeyVaultConfigurationProvider();      
```

**StartupExt.cs**

Used DotNetCore Configuration Templates to inject secrets into Microservice configuration.
(Add to project nuget package DotNetCore.Configuration.Formatter.)

``` CSharp 
  public static void AddKeyVaultConfigurationProvider(this WebApplicationBuilder builder)
    {

        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions()
            {
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeInteractiveBrowserCredential = true
            });

        var optionsCert = builder.Configuration
                           .GetTypeNameFormatted<AzureKvCertificatesConfigurationOptions>();

        // Adds Azure Key Valt configuration provider.
        builder.Configuration.AddAzureKeyVaultCertificates(credential, optionsCert);
    }
```

**appsettings.json**

```JSON

 "AzureKvCertificatesConfigurationOptions": {
    "ConfigurationSectionPrefix": "certificates",
    "VaultUri": "https://mps-Development-microsevices.vault.azure.net/",
    "VaultCertificates": [
      "Development-jwt-microservices"
    ]
  }
  
  ```

The [Azure Identity library][identity] provides easy Azure Active Directory support for authentication.

Read more about [configuration in ASP.NET Core][aspnetcore_configuration_doc].


## Example with DotNetCore Configuration Formatter


Use [DotNetCore Configuration Formatter](https://github.com/Wallsmedia/DotNetCore.Configuration.Formatter) 
to inject secrets into Microservice configuration.

Add to project nuget package [DotNetCore.Azure.Configuration.KvSecrets](https://www.nuget.org/packages/DotNetCore.Azure.Configuration.KvSecrets).

Add to project nuget package [DotNetCore.Configuration.Formatter](https://www.nuget.org/packages/DotNetCore.Configuration.Formatter/).



##### Environment Variables set to :

```
DOTNET_RUNNING_IN_CONTAINER=true
ASPNETCORE_ENVIRONMENT=Development
...
host_environmet=datacenter
```


##### Microservice has the ApplicationConfiguration.cs

``` CSharp

public class ApplicationConfiguration 
{
     public bool IsDocker {get; set;}
     public string RunLocation {get; set;}
     public string AppEnvironment {get; set;}
     public string BusConnection {get; set;}
     public string DbUser {get; set;}
     public string DbPassword {get; set;}
}
```

##### Microservice has the following appsettings.json:

``` JSON 
{
"AzureKvConfigurationOptions": {
  "ConfigurationSectionPrefix": "secret",
  "VaultUri": "https://secrets128654s235.vault.azure.net/",
  "VaultSecrets": [ 
    "service-bus-Development-connection",
    "sql-Development-password",
    "sql-Development-user",
    "service-bus-Production-connection",
    "sql-Production-password",
    "sql-Production-user" ]
    },

 "AzureKvCertificatesConfigurationOptions": {
    "ConfigurationSectionPrefix": "certificates",
    "VaultUri": "https://mps-Development-microsevices.vault.azure.net/",
    "VaultCertificates": [
      "Development-jwt-microservices"
    ]
  }

  ApplicationConfiguration:{
     "IsDocker": "{DOTNET_RUNNING_IN_CONTAINER??false}",
     "RunLocation":"{host_environmet??local}",
     "AppEnvironment":"{ENVIRONMENT}",
     "BusConnection":"{secret:service-bus-{ENVIRONMENT}-connection}",
     "DbPassword":"{secret:sql-{ENVIRONMENT}-password}",
     "DbUser":"{secret:sql-{ENVIRONMENT}-user}",
     "JwtCertificate": "{certificates:{ENVIRONMENT}-jwt-microservices}"
  }
}
```

##### Microservice the StartupExt.cs


``` CSharp

     var applicationConfig = Configuration.UseFormater()
     .GetSection(nameof(ApplicationConfiguration))
     .Get<ApplicationConfiguration>();
  ```
 

##### Microservice has the ApplicationConfiguration.cs

``` CSharp

public class ApplicationConfiguration 
{
     public bool IsDocker {get; set;}
     public string RunLocation {get; set;}
     public string AppEnvironment {get; set;}
     public string BusConnection {get; set;}
     public string DbUser {get; set;}
     public string DbPassword {get; set;}
     public KvCertificateConfigContainer JwtCertificate { get; set; }
}
```


**Program.cs**

``` CSharp 
   var builder = WebApplication.CreateBuilder(args);
      builder.AddKeyVaultConfigurationProvider();      
 ```

**StartupExt.cs**

``` CSharp
public static void AddKeyVaultConfigurationProvider(this WebApplicationBuilder builder)
    {

        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions()
            {
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeInteractiveBrowserCredential = true
            });

        var optionsCert = builder.Configuration
                           .GetTypeNameFormatted<AzureKvCertificatesConfigurationOptions>();

        // Adds Azure Key Valt configuration provider.
        builder.Configuration.AddAzureKeyVaultCertificates(credential, optionsCert);

        var optionsSecrets = builder.Configuration
                           .GetTypeNameFormatted<AzureKvConfigurationOptions>()

           
           // Adds Azure Key Valt configuration provider.
            builder.Configuration.AddAzureKeyVault(credential, options);
    }


```


<!-- LINKS -->
[azure_cli]: https://docs.microsoft.com/cli/azure
[azure_sub]: https://azure.microsoft.com/free/
[identity]: https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/identity/Azure.Identity/README.md
[aspnetcore_configuration_doc]: https://docs.microsoft.com/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1
[error_codes]: https://docs.microsoft.com/rest/api/storageservices/blob-service-error-codes
