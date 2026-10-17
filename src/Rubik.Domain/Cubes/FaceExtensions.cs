namespace Rubik.Domain.Cubes;

public static class FaceExtensions
{
    /// <summary>The one place that maps a face to its outward unit normal.</summary>
    public static Vector3 Normal(this Face face) => face switch
    {
        Face.Up => Vector3.UnitY,
        Face.Down => -Vector3.UnitY,
        Face.Left => -Vector3.UnitX,
        Face.Right => Vector3.UnitX,
        Face.Front => Vector3.UnitZ,
        Face.Back => -Vector3.UnitZ,
        _ => throw new ArgumentOutOfRangeException(nameof(face), face, null),
    };
}
