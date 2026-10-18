namespace Rubik.Domain.Cubes;

/// <summary>An integer 3D vector, used for both cubie positions and sticker normals.</summary>
public readonly record struct Vector3(int X, int Y, int Z)
{
    public static Vector3 Zero { get; } = new(0, 0, 0);

    public static Vector3 UnitX { get; } = new(1, 0, 0);

    public static Vector3 UnitY { get; } = new(0, 1, 0);

    public static Vector3 UnitZ { get; } = new(0, 0, 1);

    public static Vector3 operator -(Vector3 vector) => new(-vector.X, -vector.Y, -vector.Z);

    public static Vector3 operator +(Vector3 left, Vector3 right) =>
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3 operator *(Vector3 vector, int scalar) =>
        new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    public static int Dot(Vector3 left, Vector3 right) => left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    public static Vector3 Cross(Vector3 left, Vector3 right) =>
        new(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X
        );

    /// <summary>
    /// Quarter turn about a unit axis, clockwise when seen from the tip of the axis looking at the origin.
    /// For an axis along a coordinate direction the result is exact in integers.
    /// </summary>
    public Vector3 RotateClockwiseAbout(Vector3 axis) => Cross(this, axis) + axis * Dot(axis, this);
}
