//   \\      /\  /\\
//  o \\ \  //\\// \\
//  |  \//\//       \\
// Copyright (c) i-Wallsmedia 2022 Alex & Artem Paskhin All rights reserved.

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using DotNetCore.Azure.Configuration.KvCerfificates.Tests.Helpers;
using DotNetCore.Azure.Configuration.KvCertificates;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;

namespace DotNetCore.Azure.Configuration.KvCerfificates.Tests
{
    public partial class AzureKeyVaultConfigurationTests
    {
        private static TimeSpan NoReloadDelay { get; } = TimeSpan.FromMilliseconds(20);


        [Test]
        public void LoadsAllSecretsFromVaultIntoSection()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value1", true, null),
                    ("Certificate2", "Value2", true, null));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = "secrets"
            };

            // Act
            using (var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options))
            {
                provider.Load();

                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "secrets", "secrets" }, childKeys);
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("secrets:Certificate1"));
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value2\",\"PemCertBase64\":null}", provider.Get("secrets:Certificate2"));
            }
        }

        [Test]
        public void LoadsAllSecretsFromVaultIntoEncodeSection()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();


            MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("secrets--Certificate1", "Value1", true, null),
                    ("secrets--Certificate2", "Value2", true, null));


            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null
            };

            // Act
            using (var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options))
            {
                provider.Load();

                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "secrets", "secrets" }, childKeys);
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("secrets:Certificate1"));
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value2\",\"PemCertBase64\":null}", provider.Get("secrets:Certificate2"));
            }
        }

        private KeyVaultCertificateWithPolicy CreateCertificate(string name, string value, bool? enabled = true, DateTimeOffset? updated = null)
        {
            var id = new Uri("http://azure.keyvault/" + name);

            var CertificateProperties = CertificateModelFactory.CertificateProperties(id, name: name, updatedOn: updated);
            CertificateProperties.Enabled = enabled;

            return CertificateModelFactory.KeyVaultCertificateWithPolicy(CertificateProperties);
        }

        [Test]
        public void DoesNotLoadFilteredItems()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value1", true, null),
                    ("Certificate2", "Value2", true, null));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultCertificates = new List<string> { "Certificate1" },
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null
            };

            // Act
            using (var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "Certificate1" }, childKeys);
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));
            }
        }

        [Test]
        public void DoesNotLoadFilteredAndRemapItems()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value1", true, null),
                    ("Certificate2", "Value2", true, null));


            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultCertificateMap = new Dictionary<string, string> { ["Certificate1"] = "CertificateMap" },
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null

            };

            // Act
            using (var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "CertificateMap" }, childKeys);
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("CertificateMap"));
            }
        }

        [Test]
        public void DoesNotLoadDisabledItems()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();


            MockSetupUtils.SetClientMocks(client,
                secretClient,
                null,
                ("Certificate1", "Value1", true, null),
                ("Certificate2", "Value2", false, null),
                ("Certificate3", "Value3", null, null));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null
            };

            // Act
            using (var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.AreEqual(new[] { "Certificate1" }, childKeys);
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));
                Assert.Throws<InvalidOperationException>(() => provider.Get("Certificate2"));
                Assert.Throws<InvalidOperationException>(() => provider.Get("Certificate3"));
            }
        }

        [Test]
        public void SupportsReload()
        {
            var updated = DateTime.Now;

            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                secretClient,
                null,
                ("Certificate1", "Value1", enabled: true, updated: updated));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null
            };

            // Act
            using (var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options))
            {
                provider.Load();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));

                MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value2", enabled: true, updated: updated.AddSeconds(1)));

                provider.Load();
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value2\",\"PemCertBase64\":null}", provider.Get("Certificate1"));
            }
        }

        [Test]
        public async Task SupportsAutoReload()
        {
            var updated = DateTime.Now;
            int numOfTokensFired = 0;

            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                secretClient,
                null,
                ("Certificate1", "Value1", enabled: true, updated: updated));


            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null,
                ReloadInterval = NoReloadDelay
            };
            // Act & Assert
            using (var provider = new ReloadControlKeyVaultProvider(client.Object, secretClient.Object, options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));

                await provider.Wait();

                MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value2", enabled: true, updated: updated.AddSeconds(1)));

                provider.Release();

                await provider.Wait();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value2\",\"PemCertBase64\":null}", provider.Get("Certificate1"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public async Task DoesntReloadUnchanged()
        {
            var updated = DateTime.Now;
            int numOfTokensFired = 0;

            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                secretClient,
                null,
                ("Certificate1", "Value1", enabled: true, updated: updated));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(client.Object, secretClient.Object, options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));

                await provider.Wait();

                provider.Release();

                await provider.Wait();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));
                Assert.AreEqual(0, numOfTokensFired);
            }
        }

        [Test]
        public async Task SupportsReloadOnRemove()
        {
            int numOfTokensFired = 0;

            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();


            MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value1", true, null),
                    ("Certificate2", "Value2", true, null));


            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(client.Object, secretClient.Object, options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));

                await provider.Wait();

                MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate2", "Value2", false, null));

                provider.Release();


                await provider.Wait();

                Assert.Throws<InvalidOperationException>(() => provider.Get("Certificate2"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public async Task SupportsReloadOnEnabledChange()
        {
            int numOfTokensFired = 0;

            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value1", true, null),
                    ("Certificate2", "Value2", true, null));


            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(client.Object, secretClient.Object, options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value2\",\"PemCertBase64\":null}", provider.Get("Certificate2"));

                await provider.Wait();

                MockSetupUtils.SetClientMocks(client,
                    secretClient,
                    null,
                    ("Certificate1", "Value2", true, null),
                    ("Certificate2", "Value2", false, null));

                provider.Release();

                await provider.Wait();

                Assert.Throws<InvalidOperationException>(() => provider.Get("Certificate2"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public async Task SupportsReloadOnAdd()
        {
            int numOfTokensFired = 0;

            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            MockSetupUtils.SetClientMocks(client,
                secretClient,
                null,
                ("Certificate1", "Value1", true, null));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null,
                ReloadInterval = NoReloadDelay
            };

            // Act
            using (var provider = new ReloadControlKeyVaultProvider(client.Object, secretClient.Object, options))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () =>
                    {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));

                await provider.Wait();

                MockSetupUtils.SetClientMocks(client,
                        secretClient,
                        null,
                        ("Certificate1", "Value1", true, null),
                        ("Certificate2", "Value2", true, null));

                provider.Release();

                await provider.Wait();

                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value2\",\"PemCertBase64\":null}", provider.Get("Certificate2"));
                Assert.AreEqual(1, numOfTokensFired);
            }
        }

        [Test]
        public void ReplaceDoubleMinusInKeyName()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();


            MockSetupUtils.SetClientMocks(client,
                secretClient,
                null,
                ("Section--Certificate1", "Value1", true, null));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null
            };

            // Act
            using (var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options))
            {
                provider.Load();

                // Assert
                Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Section:Certificate1"));
            }
        }

        [Test]
        public async Task LoadsSecretsInParallel()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var expectedCount = 2;
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();



            MockSetupUtils.SetClientMocks(client,
                secretClient,
                async (id) =>
                {
                    if (Interlocked.Decrement(ref expectedCount) == 0)
                    {
                        tcs.SetResult(null);
                    }

                    await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                },
                ("Certificate1", "Value1", true, null),
                ("Certificate2", "Value2", true, null));

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null
            };

            // Act
            var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options);

            provider.Load();

            await tcs.Task;

            // Assert
            Assert.AreEqual("{\"Pkcs12Base64\":\"Value1\",\"PemCertBase64\":null}", provider.Get("Certificate1"));
            Assert.AreEqual("{\"Pkcs12Base64\":\"Value2\",\"PemCertBase64\":null}", provider.Get("Certificate2"));
        }

        [Test]
        public void LimitsMaxParallelism()
        {
            var expectedCount = 10;
            var currentParallel = 0;
            var maxParallel = 0;
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            // Create 10 pages of 10 secrets
            List<(string name, string rawdata, bool? enabled, DateTimeOffset? updated)> pages =
                Enumerable.Range(0, 10).Select<int, (string name, string rawdata, bool? enabled, DateTimeOffset? updated)>(a => ("Certificate" + a.ToString(),a.ToString(), true, null)).ToList();


            MockSetupUtils.SetClientMocks(client,
                secretClient,
                async (id) =>
                {
                    var i = Interlocked.Increment(ref currentParallel);

                    maxParallel = Math.Max(i, maxParallel);

                    await Task.Delay(30);
                    Interlocked.Decrement(ref currentParallel);
                },
                pages
            );

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null
            };

            // Act
            var provider = new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options);

            provider.Load();

            // Assert
            for (int i = 0; i < expectedCount; i++)
            {
                Assert.AreEqual($"{{\"Pkcs12Base64\":\"{i}\",\"PemCertBase64\":null}}", provider.Get("Certificate" + i));
            }

            Assert.LessOrEqual(maxParallel, 32);
        }

        [Test]
        public void ConstructorThrowsForNullManager()
        {
            Assert.Throws<ArgumentNullException>(() => new AzureKvCertificatesConfigurationProvider(null, null, null));
        }

        [Test]
        public void ConstructorThrowsForNullValueOfClient()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();
            var options = new AzureKvCertificatesConfigurationOptions
            {
            };

            Assert.Throws<ArgumentNullException>(() => new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options));
        }

        [Test]
        public void ConstructorThrowsForNullValueOfKeyVaultCertificateWithPolicyNameEncoder()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = new Uri("http://azure.com/keyvault/"),
                ConfigurationSectionPrefix = null,
                KeyVaultCerficateNameEncoder = null
            };

            Assert.Throws<ArgumentNullException>(() => new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options));
        }

        [Test]
        public void ConstructorThrowsForNullValueOfUploadAndMapKeys()
        {
            var client = new Mock<CertificateClient>();
            var secretClient = new Mock<SecretClient>();

            var options = new AzureKvCertificatesConfigurationOptions
            {
                VaultUri = null,
                ConfigurationSectionPrefix = null,
                VaultCertificates = null
            };

            Assert.Throws<ArgumentNullException>(() => new AzureKvCertificatesConfigurationProvider(client.Object, secretClient.Object, options));
        }

    }
}
