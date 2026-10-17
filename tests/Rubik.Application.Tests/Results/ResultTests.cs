using Rubik.Application.Cubes;
using Rubik.Application.Results;

namespace Rubik.Application.Tests.Results;

public class ResultTests
{
    private const string SomeValue = "value";

    [Fact]
    public void Success_Always_HasTheValueAndNoError()
    {
        var result = Result<string>.Success(SomeValue);

        Assert.True(result.IsSuccess);
        Assert.Equal(SomeValue, result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_Always_HasTheErrorAndNoValue()
    {
        var error = new CubeNotFound(Guid.NewGuid());

        var result = Result<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
