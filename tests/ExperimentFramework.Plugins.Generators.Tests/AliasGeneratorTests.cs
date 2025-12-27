using ExperimentFramework.Plugins.Generators.CodeGen;
using Xunit;

namespace ExperimentFramework.Plugins.Generators.Tests;

/// <summary>
/// Tests for the AliasGenerator class.
/// </summary>
public class AliasGeneratorTests
{
    [Theory]
    [InlineData("StripeV2Processor", "stripe-v2")]
    [InlineData("AdyenPaymentHandler", "adyen-payment")]
    [InlineData("MollieServiceImpl", "mollie")]
    [InlineData("StripeProcessor", "stripe")]
    [InlineData("MyHandler", "my")]
    [InlineData("SimpleService", "simple")]
    [InlineData("DataProvider", "data")]
    [InlineData("PaymentImplementation", "payment")]
    public void GenerateAlias_StripsCommonSuffixes(string className, string expected)
    {
        // Act
        var result = AliasGenerator.GenerateAlias(className);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("MyClass", "my-class")]
    [InlineData("SimpleTest", "simple-test")]
    [InlineData("HTTPHandler", "http")]  // HTTP is preserved, Handler stripped
    [InlineData("XMLParser", "xml-parser")]
    public void GenerateAlias_ConvertsToKebabCase(string className, string expected)
    {
        // Act
        var result = AliasGenerator.GenerateAlias(className);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("V2Processor", "v2")]
    [InlineData("ProcessorV2", "processor-v2")]
    [InlineData("StripeV2", "stripe-v2")]
    [InlineData("V2V3Handler", "v2v3")]  // Sequential version numbers stay together
    public void GenerateAlias_HandlesVersionNumbers(string className, string expected)
    {
        // Act
        var result = AliasGenerator.GenerateAlias(className);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("A", "a")]
    [InlineData("AB", "ab")]
    [InlineData("abc", "abc")]
    public void GenerateAlias_HandlesEdgeCases(string className, string expected)
    {
        // Act
        var result = AliasGenerator.GenerateAlias(className);

        // Assert
        Assert.Equal(expected, result);
    }
}
