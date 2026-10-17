using Rubik.Domain.Cubes;

namespace Rubik.Domain.Tests.Cubes;

public class Vector3Tests
{
    private const int Scalar = 3;

    public static TheoryData<Vector3, Vector3, Vector3> ClockwiseQuarterTurns => new()
    {
        // Seen from the tip of the axis looking at the origin, clockwise.
        { new Vector3(0, 1, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 0) },
        { new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, -1, 0) },
        { new Vector3(0, 0, 1), new Vector3(1, 0, 0), new Vector3(0, 1, 0) },
        { new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0) },
        { new Vector3(1, 1, 1), new Vector3(0, 1, 0), new Vector3(-1, 1, 1) },
    };

    [Theory]
    [MemberData(nameof(ClockwiseQuarterTurns))]
    public void RotateClockwiseAbout_UnitAxis_TurnsQuarterClockwise(Vector3 vector, Vector3 axis, Vector3 expected)
    {
        Assert.Equal(expected, vector.RotateClockwiseAbout(axis));
    }

    [Fact]
    public void Cross_UnitX_UnitY_IsUnitZ()
    {
        Assert.Equal(Vector3.UnitZ, Vector3.Cross(Vector3.UnitX, Vector3.UnitY));
    }

    [Fact]
    public void Operators_AddAndScale_WorkPerComponent()
    {
        var sum = new Vector3(1, 2, 3) + new Vector3(4, 5, 6);
        var scaled = new Vector3(1, 2, 3) * Scalar;

        Assert.Equal(new Vector3(5, 7, 9), sum);
        Assert.Equal(new Vector3(3, 6, 9), scaled);
    }
}
