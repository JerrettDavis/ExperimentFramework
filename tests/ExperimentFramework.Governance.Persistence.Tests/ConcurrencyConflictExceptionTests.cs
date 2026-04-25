using FluentAssertions;

namespace ExperimentFramework.Governance.Persistence.Tests;

public sealed class ConcurrencyConflictExceptionTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var ex = new ConcurrencyConflictException("my-exp", "etag-old", "etag-new");

        ex.ExperimentName.Should().Be("my-exp");
        ex.ExpectedETag.Should().Be("etag-old");
        ex.ActualETag.Should().Be("etag-new");
        ex.Message.Should().Contain("my-exp");
        ex.Message.Should().Contain("etag-old");
        ex.Message.Should().Contain("etag-new");
    }

    [Fact]
    public void Constructor_WithNullActualETag_SetsUnknown()
    {
        var ex = new ConcurrencyConflictException("exp-1", "expected-tag", null);

        ex.ExperimentName.Should().Be("exp-1");
        ex.ExpectedETag.Should().Be("expected-tag");
        ex.ActualETag.Should().BeNull();
        ex.Message.Should().Contain("unknown");
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ConcurrencyConflictException("exp-2", "etag-a", "etag-b", inner);

        ex.ExperimentName.Should().Be("exp-2");
        ex.ExpectedETag.Should().Be("etag-a");
        ex.ActualETag.Should().Be("etag-b");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Constructor_WithInnerException_AndNullActualETag_Succeeds()
    {
        var inner = new Exception("cause");
        var ex = new ConcurrencyConflictException("exp-3", "old-tag", null, inner);

        ex.ActualETag.Should().BeNull();
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Contain("unknown");
    }

    [Fact]
    public void IsException_OfBaseType()
    {
        var ex = new ConcurrencyConflictException("e", "a", "b");

        ex.Should().BeAssignableTo<Exception>();
    }
}
