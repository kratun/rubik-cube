namespace Rubik.Domain.Cubes;

/// <summary>An integer 3D vector, used for both cubie positions and sticker normals.</summary>
public readonly record struct Vector3(int X, int Y, int Z)
{
    public static Vector3 Zero { get; } = new(0, 0, 0);

    public static Vector3 UnitX { get; } = new(1, 0, 0);

    public static Vector3 UnitY { get; } = new(0, 1, 0);

    public static Vector3 UnitZ { get; } = new(0, 0, 1);

    public static Vector3 operator -(Vector3 vector) => new(-vector.X, -vector.Y, -vector.Z);

    public static int Dot(Vector3 left, Vector3 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;
}
