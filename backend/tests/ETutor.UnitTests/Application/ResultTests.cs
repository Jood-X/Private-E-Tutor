using ETutor.Api.Common;
using ETutor.Application.Common.Results;

namespace ETutor.UnitTests.Application;

public sealed class ResultTests
{
    [Fact]
    public void Success_ExposesValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_HidesValue()
    {
        Result<int> result = Error.NotFound("thing.not_found", "Not found.");

        Assert.False(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Equal("thing.not_found", result.Errors[0].Code);
    }

    [Fact]
    public void Failure_RequiresAtLeastOneError()
    {
        Assert.Throws<ArgumentException>(() => Result<int>.Failure([]));
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.BusinessRule, 422)]
    public void EveryErrorType_MapsToOneStatusCode(ErrorType type, int expectedStatus)
    {
        Assert.Equal(expectedStatus, ErrorStatusCodes.For(type));
    }

    [Fact]
    public void EveryErrorType_IsMapped()
    {
        foreach (var type in Enum.GetValues<ErrorType>())
        {
            _ = ErrorStatusCodes.For(type);
        }
    }
}
