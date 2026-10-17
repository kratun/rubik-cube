using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Rubik.Api.Errors;
using Rubik.Application.Cubes;
using Rubik.Application.Results;

namespace Rubik.Api.Tests.Errors;

public class ResultMapperTests
{
    private const string TraceId = "trace-1";
    private const string SomeValue = "value";
    private const string BadToken = "f";
    private const string NullWord = "null";

    private readonly DefaultHttpContext context = new() { TraceIdentifier = TraceId };
    private readonly ResultMapper mapper = new(new ErrorReporter(new FakeLogger<ErrorReporter>()));

    private sealed record UnmappedError : ApplicationError;

    [Fact]
    public void ToActionResult_Success_ReturnsOkWithTheValue()
    {
        var actionResult = mapper.ToActionResult(Result<string>.Success(SomeValue), context);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(SomeValue, ok.Value);
    }

    [Fact]
    public void ToActionResult_CubeNotFound_Returns404WithErrorDetails()
    {
        var cubeId = Guid.NewGuid();

        var actionResult = mapper.ToActionResult(Result<string>.Failure(new CubeNotFound(cubeId)), context);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        var details = Assert.IsType<ErrorDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, details.Status);
        Assert.Equal(TraceId, details.TraceId);
        Assert.Contains(cubeId.ToString(), details.Detail);
    }

    [Fact]
    public void ToActionResult_InvalidMove_Returns400NamingTheToken()
    {
        var actionResult = mapper.ToActionResult(Result<string>.Failure(new InvalidMove(BadToken)), context);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var details = Assert.IsType<ErrorDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Equal(TraceId, details.TraceId);
        Assert.Contains($"\"{BadToken}\"", details.Detail);
    }

    [Fact]
    public void ToActionResult_InvalidMoveWithNullToken_Returns400WithADetailThatSaysNull()
    {
        var actionResult = mapper.ToActionResult(Result<string>.Failure(new InvalidMove(Token: null)), context);

        var details = Assert.IsType<ErrorDetails>(Assert.IsType<ObjectResult>(actionResult).Value);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Contains(NullWord, details.Detail);
    }

    [Fact]
    public void ToActionResult_RevisionConflict_Returns409WithErrorDetails()
    {
        var actionResult = mapper.ToActionResult(Result<string>.Failure(new RevisionConflict(Guid.NewGuid())), context);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var details = Assert.IsType<ErrorDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status409Conflict, details.Status);
        Assert.Equal(TraceId, details.TraceId);
    }

    [Fact]
    public void ToActionResult_UnmappedError_Throws()
    {
        var result = Result<string>.Failure(new UnmappedError());

        Assert.Throws<InvalidOperationException>(() => mapper.ToActionResult(result, context));
    }

    [Fact]
    public void ToBadRequest_Always_Returns400WithErrorDetails()
    {
        var actionResult = mapper.ToBadRequest(context);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var details = Assert.IsType<ErrorDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, details.Status);
        Assert.Equal(TraceId, details.TraceId);
    }
}
