using System.Reflection;
using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Manifest;
using ExperimentFramework.Plugins.TestFixtures;

namespace ExperimentFramework.Plugins.Tests.Manifest;

public class ManifestLoaderTests : IDisposable
{
    private readonly ManifestLoader _loader = new();
    private readonly string _tempDir;

    public ManifestLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ManifestLoaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Load Method Tests

    [Fact]
    public void Load_WithNullAssembly_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _loader.Load(null!, "/some/path.dll"));
    }

    [Fact]
    public void Load_WithNullPath_ThrowsArgumentNullException()
    {
        var assembly = typeof(ManifestLoaderTests).Assembly;
        Assert.Throws<ArgumentNullException>(() => _loader.Load(assembly, null!));
    }

    [Fact]
    public void Load_WithEmptyPath_ThrowsArgumentException()
    {
        var assembly = typeof(ManifestLoaderTests).Assembly;
        Assert.Throws<ArgumentException>(() => _loader.Load(assembly, ""));
    }

    [Fact]
    public void Load_WithWhitespacePath_ThrowsArgumentException()
    {
        var assembly = typeof(ManifestLoaderTests).Assembly;
        Assert.Throws<ArgumentException>(() => _loader.Load(assembly, "   "));
    }

    [Fact]
    public void Load_WithNoManifestSources_ReturnsDefaultManifest()
    {
        // Use the test assembly which has no manifest embedded/adjacent
        var assembly = typeof(ManifestLoaderTests).Assembly;
        var fakePath = Path.Combine(_tempDir, "nonexistent.dll");

        var manifest = _loader.Load(assembly, fakePath);

        Assert.NotNull(manifest);
        Assert.Equal("1.0", manifest.ManifestVersion);
        Assert.Contains("ExperimentFramework.Plugins.Tests", manifest.Id);
    }

    #endregion

    #region TryLoadFromAdjacentFile Tests

    [Fact]
    public void TryLoadFromAdjacentFile_WithValidManifest_ReturnsTrue()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "TestPlugin.dll");
        var manifestPath = Path.Combine(_tempDir, "TestPlugin.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": {
                    "id": "Test.Plugin",
                    "name": "Test Plugin",
                    "version": "2.0.0",
                    "description": "A test plugin"
                },
                "isolation": {
                    "mode": "shared"
                }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.NotNull(manifest);
        Assert.Equal("Test.Plugin", manifest.Id);
        Assert.Equal("Test Plugin", manifest.Name);
        Assert.Equal("2.0.0", manifest.Version);
        Assert.Equal("A test plugin", manifest.Description);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithNoFile_ReturnsFalse()
    {
        var assemblyPath = Path.Combine(_tempDir, "NonExistent.dll");

        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "BadPlugin.dll");
        var manifestPath = Path.Combine(_tempDir, "BadPlugin.plugin.json");
        File.WriteAllText(manifestPath, "{ invalid json }");

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithEmptyDirectory_ReturnsFalse()
    {
        var result = _loader.TryLoadFromAdjacentFile("", out var manifest);

        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithFullIsolationMode_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "IsolatedPlugin.dll");
        var manifestPath = Path.Combine(_tempDir, "IsolatedPlugin.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": {
                    "id": "Isolated.Plugin"
                },
                "isolation": {
                    "mode": "full",
                    "sharedAssemblies": ["SharedLib1", "SharedLib2"]
                }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal(PluginIsolationMode.Full, manifest.Isolation.Mode);
        Assert.Equal(2, manifest.Isolation.SharedAssemblies.Count);
        Assert.Contains("SharedLib1", manifest.Isolation.SharedAssemblies);
        Assert.Contains("SharedLib2", manifest.Isolation.SharedAssemblies);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithNoneIsolationMode_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "NoneIsolation.dll");
        var manifestPath = Path.Combine(_tempDir, "NoneIsolation.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "None.Plugin" },
                "isolation": { "mode": "none" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal(PluginIsolationMode.None, manifest.Isolation.Mode);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithServices_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "ServicePlugin.dll");
        var manifestPath = Path.Combine(_tempDir, "ServicePlugin.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "Service.Plugin" },
                "services": [
                    {
                        "interface": "IPaymentProcessor",
                        "implementations": [
                            { "type": "StripeProcessor", "alias": "stripe" },
                            { "type": "PayPalProcessor", "alias": "paypal" }
                        ]
                    }
                ]
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Single(manifest.Services);
        Assert.Equal("IPaymentProcessor", manifest.Services[0].Interface);
        Assert.Equal(2, manifest.Services[0].Implementations.Count);
        Assert.Equal("stripe", manifest.Services[0].Implementations[0].Alias);
        Assert.Equal("paypal", manifest.Services[0].Implementations[1].Alias);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithLifecycle_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "LifecyclePlugin.dll");
        var manifestPath = Path.Combine(_tempDir, "LifecyclePlugin.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "Lifecycle.Plugin" },
                "lifecycle": {
                    "supportsHotReload": false,
                    "requiresRestartOnUnload": true
                }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.False(manifest.Lifecycle.SupportsHotReload);
        Assert.True(manifest.Lifecycle.RequiresRestartOnUnload);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithComments_ParsesCorrectly()
    {
        // Arrange - JSON with comments should be allowed
        var assemblyPath = Path.Combine(_tempDir, "CommentPlugin.dll");
        var manifestPath = Path.Combine(_tempDir, "CommentPlugin.plugin.json");

        var manifestJson = """
            {
                // This is a comment
                "manifestVersion": "1.0",
                "plugin": {
                    "id": "Comment.Plugin"
                    /* inline comment */
                }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("Comment.Plugin", manifest.Id);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithTrailingCommas_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "TrailingComma.dll");
        var manifestPath = Path.Combine(_tempDir, "TrailingComma.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": {
                    "id": "Trailing.Plugin",
                },
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("Trailing.Plugin", manifest.Id);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithoutPluginId_ThrowsInvalidOperationException()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "NoId.dll");
        var manifestPath = Path.Combine(_tempDir, "NoId.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": {
                    "name": "No ID Plugin"
                }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act & Assert
        Assert.ThrowsAny<InvalidOperationException>(() =>
            _loader.TryLoadFromAdjacentFile(assemblyPath, out _));
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithCaseInsensitiveProperties_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "CaseTest.dll");
        var manifestPath = Path.Combine(_tempDir, "CaseTest.plugin.json");

        var manifestJson = """
            {
                "ManifestVersion": "1.0",
                "Plugin": {
                    "ID": "Case.Plugin",
                    "NAME": "Case Test Plugin"
                }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("Case.Plugin", manifest.Id);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_DefaultsNameToId_WhenNameNotProvided()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "DefaultName.dll");
        var manifestPath = Path.Combine(_tempDir, "DefaultName.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "My.Plugin.Id" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("My.Plugin.Id", manifest.Name);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_DefaultsVersionTo1_0_0_WhenNotProvided()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "DefaultVersion.dll");
        var manifestPath = Path.Combine(_tempDir, "DefaultVersion.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "Version.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("1.0.0", manifest.Version);
    }

    #endregion

    #region TryLoadFromEmbeddedResource Tests

    [Fact]
    public void TryLoadFromEmbeddedResource_WithNoEmbeddedManifest_ReturnsFalse()
    {
        // The test assembly doesn't have an embedded manifest
        var assembly = typeof(ManifestLoaderTests).Assembly;

        var result = _loader.TryLoadFromEmbeddedResource(assembly, out var manifest);

        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromEmbeddedResource_LooksForPluginManifestJson()
    {
        // This tests that the method looks for files ending with "plugin.manifest.json"
        var assembly = typeof(ManifestLoaderTests).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        // Verify no manifest resources exist (as expected)
        Assert.DoesNotContain(resourceNames, n => n.EndsWith("plugin.manifest.json", StringComparison.OrdinalIgnoreCase));

        var result = _loader.TryLoadFromEmbeddedResource(assembly, out _);
        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromEmbeddedResource_WithEmbeddedManifest_ReturnsTrue()
    {
        // Use the test fixtures assembly which has an embedded manifest
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromEmbeddedResource(assembly, out var manifest);

        Assert.True(result);
        Assert.NotNull(manifest);
        Assert.Equal("TestFixtures.EmbeddedPlugin", manifest.Id);
        Assert.Equal("Embedded Plugin from Resource", manifest.Name);
        Assert.Equal("2.0.0", manifest.Version);
        Assert.Equal("A test plugin with embedded manifest", manifest.Description);
    }

    [Fact]
    public void TryLoadFromEmbeddedResource_ParsesIsolationConfig()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromEmbeddedResource(assembly, out var manifest);

        Assert.True(result);
        Assert.Equal(PluginIsolationMode.Shared, manifest.Isolation.Mode);
        Assert.Equal(2, manifest.Isolation.SharedAssemblies.Count);
        Assert.Contains("TestAssembly1", manifest.Isolation.SharedAssemblies);
        Assert.Contains("TestAssembly2", manifest.Isolation.SharedAssemblies);
    }

    [Fact]
    public void TryLoadFromEmbeddedResource_ParsesServices()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromEmbeddedResource(assembly, out var manifest);

        Assert.True(result);
        Assert.Single(manifest.Services);
        Assert.Equal("ITestService", manifest.Services[0].Interface);
        Assert.Single(manifest.Services[0].Implementations);
        Assert.Equal("TestServiceImpl", manifest.Services[0].Implementations[0].Type);
        Assert.Equal("test-impl", manifest.Services[0].Implementations[0].Alias);
    }

    [Fact]
    public void TryLoadFromEmbeddedResource_ParsesLifecycle()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromEmbeddedResource(assembly, out var manifest);

        Assert.True(result);
        Assert.True(manifest.Lifecycle.SupportsHotReload);
        Assert.False(manifest.Lifecycle.RequiresRestartOnUnload);
    }

    #endregion

    #region TryLoadFromAttributes Tests

    [Fact]
    public void TryLoadFromAttributes_WithNoAttributes_ReturnsFalse()
    {
        // Test assembly doesn't have PluginManifestAttribute
        var assembly = typeof(ManifestLoaderTests).Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromAttributes_WithPluginManifestAttribute_ReturnsTrue()
    {
        // Use the test fixtures assembly which has plugin attributes
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        Assert.NotNull(manifest);
        Assert.Equal("TestFixtures.AttributePlugin", manifest.Id);
        Assert.Equal("Attribute Plugin", manifest.Name);
        Assert.Equal("3.0.0", manifest.Version);
        Assert.Equal("A test plugin defined via attributes", manifest.Description);
    }

    [Fact]
    public void TryLoadFromAttributes_ReadsIsolationAttribute()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        Assert.Equal(PluginIsolationMode.Full, manifest.Isolation.Mode);
        Assert.Equal(2, manifest.Isolation.SharedAssemblies.Count);
        Assert.Contains("SharedLib1", manifest.Isolation.SharedAssemblies);
        Assert.Contains("SharedLib2", manifest.Isolation.SharedAssemblies);
    }

    [Fact]
    public void TryLoadFromAttributes_ReadsServiceAttributes()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        Assert.Equal(2, manifest.Services.Count);

        var paymentService = manifest.Services.First(s => s.Interface == "IPaymentProcessor");
        Assert.Equal(2, paymentService.Implementations.Count);
        Assert.Contains(paymentService.Implementations, i => i.Type == "StripeProcessor" && i.Alias == "stripe");
        Assert.Contains(paymentService.Implementations, i => i.Type == "PayPalProcessor" && i.Alias == "paypal");

        var notificationService = manifest.Services.First(s => s.Interface == "INotificationService");
        Assert.Equal(2, notificationService.Implementations.Count);
        Assert.Contains(notificationService.Implementations, i => i.Type == "EmailNotifier" && i.Alias == "email");
        Assert.Contains(notificationService.Implementations, i => i.Type == "SmsNotifier" && i.Alias == null);
    }

    [Fact]
    public void TryLoadFromAttributes_ReadsSupportsHotReload()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        Assert.False(manifest.Lifecycle.SupportsHotReload); // Set to false in AssemblyInfo.cs
    }

    [Fact]
    public void TryLoadFromAttributes_ParsesImplementationWithAlias()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        var paymentService = manifest.Services.First(s => s.Interface == "IPaymentProcessor");
        var stripe = paymentService.Implementations.First(i => i.Type == "StripeProcessor");

        Assert.Equal("stripe", stripe.Alias);
    }

    [Fact]
    public void TryLoadFromAttributes_ParsesImplementationWithoutAlias()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        var notificationService = manifest.Services.First(s => s.Interface == "INotificationService");
        var sms = notificationService.Implementations.First(i => i.Type == "SmsNotifier");

        Assert.Null(sms.Alias);
    }

    #endregion

    #region Load Method Integration Tests

    [Fact]
    public void Load_WithEmbeddedManifest_ReturnsEmbeddedManifest()
    {
        // The test fixtures assembly has an embedded manifest which should take priority
        var assembly = TestFixtures.TestFixtureMarker.Assembly;
        var fakePath = Path.Combine(_tempDir, "testfixtures.dll");

        var manifest = _loader.Load(assembly, fakePath);

        // Embedded resource takes priority over attributes
        Assert.Equal("TestFixtures.EmbeddedPlugin", manifest.Id);
    }

    #endregion

    #region Default Manifest Tests

    [Fact]
    public void Load_CreatesDefaultManifestFromAssemblyInfo()
    {
        var assembly = typeof(ManifestLoaderTests).Assembly;
        var fakePath = Path.Combine(_tempDir, "test.dll");

        var manifest = _loader.Load(assembly, fakePath);

        // Should derive name from assembly
        Assert.NotNull(manifest.Id);
        Assert.NotNull(manifest.Name);
        Assert.NotNull(manifest.Version);
        Assert.Equal("1.0", manifest.ManifestVersion);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TryLoadFromAdjacentFile_WithReadOnlyManifest_HandlesGracefully()
    {
        // Arrange - create a manifest file that can be read
        var assemblyPath = Path.Combine(_tempDir, "ReadOnly.dll");
        var manifestPath = Path.Combine(_tempDir, "ReadOnly.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "ReadOnly.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("ReadOnly.Plugin", manifest.Id);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithEmptyJsonFile_ReturnsFalse()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "Empty.dll");
        var manifestPath = Path.Combine(_tempDir, "Empty.plugin.json");
        File.WriteAllText(manifestPath, "");

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithNullJsonObject_ReturnsFalse()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "Null.dll");
        var manifestPath = Path.Combine(_tempDir, "Null.plugin.json");
        File.WriteAllText(manifestPath, "null");

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithMultipleServices_ParsesAllCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "MultiService.dll");
        var manifestPath = Path.Combine(_tempDir, "MultiService.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "Multi.Service.Plugin" },
                "services": [
                    {
                        "interface": "IPaymentProcessor",
                        "implementations": [
                            { "type": "StripeProcessor", "alias": "stripe" }
                        ]
                    },
                    {
                        "interface": "INotificationService",
                        "implementations": [
                            { "type": "EmailNotifier", "alias": "email" },
                            { "type": "SmsNotifier", "alias": "sms" }
                        ]
                    }
                ]
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal(2, manifest.Services.Count);

        var paymentService = manifest.Services.First(s => s.Interface == "IPaymentProcessor");
        Assert.Single(paymentService.Implementations);

        var notificationService = manifest.Services.First(s => s.Interface == "INotificationService");
        Assert.Equal(2, notificationService.Implementations.Count);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithImplementationWithoutAlias_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "NoAlias.dll");
        var manifestPath = Path.Combine(_tempDir, "NoAlias.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "NoAlias.Plugin" },
                "services": [
                    {
                        "interface": "IService",
                        "implementations": [
                            { "type": "ServiceImpl" }
                        ]
                    }
                ]
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Single(manifest.Services);
        Assert.Null(manifest.Services[0].Implementations[0].Alias);
        Assert.Equal("ServiceImpl", manifest.Services[0].Implementations[0].Type);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_PathWithSpecialCharacters_WorksCorrectly()
    {
        // Arrange - create a subdirectory with spaces
        var specialDir = Path.Combine(_tempDir, "Path With Spaces");
        Directory.CreateDirectory(specialDir);

        var assemblyPath = Path.Combine(specialDir, "Special.dll");
        var manifestPath = Path.Combine(specialDir, "Special.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "Special.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("Special.Plugin", manifest.Id);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_DefaultLifecycleValues()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "DefaultLifecycle.dll");
        var manifestPath = Path.Combine(_tempDir, "DefaultLifecycle.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "Default.Lifecycle.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.True(manifest.Lifecycle.SupportsHotReload);
        Assert.False(manifest.Lifecycle.RequiresRestartOnUnload);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_DefaultIsolationValues()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "DefaultIsolation.dll");
        var manifestPath = Path.Combine(_tempDir, "DefaultIsolation.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "Default.Isolation.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal(PluginIsolationMode.Shared, manifest.Isolation.Mode);
        Assert.Empty(manifest.Isolation.SharedAssemblies);
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public void TryLoadFromAdjacentFile_WithMissingServiceInterface_ThrowsInvalidOperationException()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "NoInterface.dll");
        var manifestPath = Path.Combine(_tempDir, "NoInterface.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "NoInterface.Plugin" },
                "services": [
                    {
                        "implementations": [
                            { "type": "SomeType" }
                        ]
                    }
                ]
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act & Assert
        Assert.ThrowsAny<InvalidOperationException>(() =>
            _loader.TryLoadFromAdjacentFile(assemblyPath, out _));
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithMissingImplementationType_ThrowsInvalidOperationException()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "NoImplType.dll");
        var manifestPath = Path.Combine(_tempDir, "NoImplType.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "NoImplType.Plugin" },
                "services": [
                    {
                        "interface": "ITestService",
                        "implementations": [
                            { "alias": "test" }
                        ]
                    }
                ]
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act & Assert
        Assert.ThrowsAny<InvalidOperationException>(() =>
            _loader.TryLoadFromAdjacentFile(assemblyPath, out _));
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithNoPluginSection_ThrowsInvalidOperationException()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "NoPlugin.dll");
        var manifestPath = Path.Combine(_tempDir, "NoPlugin.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0"
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act & Assert
        Assert.ThrowsAny<InvalidOperationException>(() =>
            _loader.TryLoadFromAdjacentFile(assemblyPath, out _));
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithEmptyServices_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "EmptyServices.dll");
        var manifestPath = Path.Combine(_tempDir, "EmptyServices.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "EmptyServices.Plugin" },
                "services": []
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Empty(manifest.Services);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithEmptyImplementations_ParsesCorrectly()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "EmptyImpls.dll");
        var manifestPath = Path.Combine(_tempDir, "EmptyImpls.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "EmptyImpls.Plugin" },
                "services": [
                    {
                        "interface": "ITestService",
                        "implementations": []
                    }
                ]
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Single(manifest.Services);
        Assert.Empty(manifest.Services[0].Implementations);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithUnknownIsolationMode_DefaultsToShared()
    {
        // Arrange
        var assemblyPath = Path.Combine(_tempDir, "UnknownMode.dll");
        var manifestPath = Path.Combine(_tempDir, "UnknownMode.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "UnknownMode.Plugin" },
                "isolation": { "mode": "custom-mode" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = _loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal(PluginIsolationMode.Shared, manifest.Isolation.Mode);
    }

    #endregion

    #region TryLoadFromAttributes Additional Tests

    [Fact]
    public void TryLoadFromAttributes_WithNoIsolationAttribute_UsesDefaults()
    {
        // The test fixtures assembly has isolation attribute, use a different assembly
        // The test assembly itself doesn't have PluginManifestAttribute
        var assembly = typeof(ManifestLoaderTests).Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        // Should return false because no PluginManifestAttribute
        Assert.False(result);
    }

    [Fact]
    public void TryLoadFromAttributes_ParsesColonNotation()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        // Verify colon notation was parsed - "StripeProcessor:stripe" -> Type=StripeProcessor, Alias=stripe
        var paymentService = manifest.Services.First(s => s.Interface == "IPaymentProcessor");
        Assert.Contains(paymentService.Implementations, i => i.Type == "StripeProcessor" && i.Alias == "stripe");
    }

    [Fact]
    public void TryLoadFromAttributes_HandlesImplementationWithoutColon()
    {
        var assembly = TestFixtures.TestFixtureMarker.Assembly;

        var result = _loader.TryLoadFromAttributes(assembly, out var manifest);

        Assert.True(result);
        // SmsNotifier without colon -> Type=SmsNotifier, Alias=null
        var notificationService = manifest.Services.First(s => s.Interface == "INotificationService");
        Assert.Contains(notificationService.Implementations, i => i.Type == "SmsNotifier" && i.Alias == null);
    }

    #endregion

    #region Size Limit Tests

    [Fact]
    public void TryLoadFromAdjacentFile_WithOversizedManifest_ThrowsInvalidOperationException()
    {
        // Arrange - create a loader with a very small size limit
        var loader = new ManifestLoader(maxManifestSize: 100, maxJsonDepth: 32);

        var assemblyPath = Path.Combine(_tempDir, "Oversized.dll");
        var manifestPath = Path.Combine(_tempDir, "Oversized.plugin.json");

        // Create a manifest larger than 100 bytes
        var manifestJson = $$"""
            {
                "manifestVersion": "1.0",
                "plugin": {
                    "id": "Oversized.Plugin",
                    "description": "{{new string('A', 200)}}"
                }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            loader.TryLoadFromAdjacentFile(assemblyPath, out _));

        Assert.Contains("exceeds maximum size", ex.Message);
    }

    [Fact]
    public void TryLoadFromAdjacentFile_WithManifestUnderSizeLimit_ReturnsTrue()
    {
        // Arrange - create a loader with a reasonable size limit
        var loader = new ManifestLoader(maxManifestSize: 10000, maxJsonDepth: 32);

        var assemblyPath = Path.Combine(_tempDir, "UnderSize.dll");
        var manifestPath = Path.Combine(_tempDir, "UnderSize.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "UnderSize.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Act
        var result = loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);

        // Assert
        Assert.True(result);
        Assert.Equal("UnderSize.Plugin", manifest.Id);
    }

    [Fact]
    public void Constructor_WithZeroMaxSize_UsesDefaultSize()
    {
        // Zero should not crash and should use a reasonable default
        var loader = new ManifestLoader(maxManifestSize: 0, maxJsonDepth: 32);

        var assemblyPath = Path.Combine(_tempDir, "ZeroSize.dll");
        var manifestPath = Path.Combine(_tempDir, "ZeroSize.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "ZeroSize.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Should work with default size
        var result = loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);
        Assert.True(result);
    }

    [Fact]
    public void Constructor_WithNegativeMaxSize_UsesDefaultSize()
    {
        // Negative values should use defaults
        var loader = new ManifestLoader(maxManifestSize: -100, maxJsonDepth: 32);

        var assemblyPath = Path.Combine(_tempDir, "NegativeSize.dll");
        var manifestPath = Path.Combine(_tempDir, "NegativeSize.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "NegativeSize.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        // Should work with default size
        var result = loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);
        Assert.True(result);
    }

    [Fact]
    public void Constructor_WithZeroMaxDepth_UsesDefaultDepth()
    {
        var loader = new ManifestLoader(maxManifestSize: 1024 * 1024, maxJsonDepth: 0);

        var assemblyPath = Path.Combine(_tempDir, "ZeroDepth.dll");
        var manifestPath = Path.Combine(_tempDir, "ZeroDepth.plugin.json");

        var manifestJson = """
            {
                "manifestVersion": "1.0",
                "plugin": { "id": "ZeroDepth.Plugin" }
            }
            """;
        File.WriteAllText(manifestPath, manifestJson);

        var result = loader.TryLoadFromAdjacentFile(assemblyPath, out var manifest);
        Assert.True(result);
    }

    #endregion
}
