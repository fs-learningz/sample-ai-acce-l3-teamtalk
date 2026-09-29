using ErrorOr;
using Microsoft.AspNetCore.Http;
using TeamTalk.Api.Common;

namespace TeamTalk.Api.Tests.Common;

public class ErrorTypeExtensionsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    public void TypeToCode_ForMappedErrorType_ReturnsMatchingStatusCode(ErrorType type, int expected)
    {
        // act
        var code = type.TypeToCode();

        // assert
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Unexpected)]
    public void TypeToCode_ForUnmappedErrorType_ReturnsInternalServerError(ErrorType type)
    {
        // act
        var code = type.TypeToCode();

        // assert
        Assert.Equal(StatusCodes.Status500InternalServerError, code);
    }

    [Fact]
    public void TypeToCode_ForUndefinedErrorTypeValue_ReturnsInternalServerError()
    {
        // arrange
        var undefined = (ErrorType)999;

        // act
        var code = undefined.TypeToCode();

        // assert
        Assert.Equal(StatusCodes.Status500InternalServerError, code);
    }

    [Fact]
    public void TypeToCode_ForEveryDefinedErrorType_ReturnsAValidHttpErrorStatus()
    {
        // arrange
        var types = Enum.GetValues<ErrorType>();

        // act
        var codes = types.Select(t => t.TypeToCode()).ToArray();

        // assert
        Assert.All(codes, code => Assert.InRange(code, 400, 599));
    }
}
