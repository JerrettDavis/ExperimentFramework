using FluentAssertions;

namespace ExperimentFramework.Governance.Persistence.Tests;

public sealed class PersistenceResultTests
{
    [Fact]
    public void Ok_ReturnsSuccessfulResult_WithEntityAndETag()
    {
        var result = PersistenceResult<string>.Ok("my-entity", "etag-1");

        result.Success.Should().BeTrue();
        result.Entity.Should().Be("my-entity");
        result.NewETag.Should().Be("etag-1");
        result.ConflictDetected.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Conflict_ReturnsConflictResult_WithDefaultMessage()
    {
        var result = PersistenceResult<string>.Conflict();

        result.Success.Should().BeFalse();
        result.ConflictDetected.Should().BeTrue();
        result.Entity.Should().BeNull();
        result.NewETag.Should().BeNull();
        result.ErrorMessage.Should().Be("Concurrency conflict detected");
    }

    [Fact]
    public void Conflict_ReturnsConflictResult_WithCustomMessage()
    {
        var result = PersistenceResult<string>.Conflict("custom conflict message");

        result.Success.Should().BeFalse();
        result.ConflictDetected.Should().BeTrue();
        result.ErrorMessage.Should().Be("custom conflict message");
    }

    [Fact]
    public void Failure_ReturnsFailureResult_WithMessage()
    {
        var result = PersistenceResult<string>.Failure("something went wrong");

        result.Success.Should().BeFalse();
        result.ConflictDetected.Should().BeFalse();
        result.ErrorMessage.Should().Be("something went wrong");
        result.Entity.Should().BeNull();
        result.NewETag.Should().BeNull();
    }

    [Fact]
    public void Ok_WithComplexEntity_PreservesEntity()
    {
        var entity = new { Name = "test", Value = 42 };
        var result = PersistenceResult<object>.Ok(entity, "etag-abc");

        result.Success.Should().BeTrue();
        result.Entity.Should().BeSameAs(entity);
        result.NewETag.Should().Be("etag-abc");
    }

    [Fact]
    public void Conflict_WithEmptyMessage_ReturnsResult()
    {
        var result = PersistenceResult<int>.Conflict(string.Empty);

        result.Success.Should().BeFalse();
        result.ConflictDetected.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }
}
