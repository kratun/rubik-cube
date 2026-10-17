namespace Rubik.Domain.Cubes;

/// <summary>A quarter turn of one face. Parsed strictly from standard notation by <see cref="TryParse"/>.</summary>
public readonly record struct Move(Face Face, Direction Direction)
{
    private const char CounterClockwiseSuffix = '\'';

    private const int FaceLetterIndex = 0;

    private const int SuffixIndex = 1;

    private const int PlainTokenLength = 1;

    private const int PrimedTokenLength = 2;

    private static readonly IReadOnlyDictionary<char, Face> FacesByLetter = new Dictionary<char, Face>
    {
        ['F'] = Face.Front,
        ['R'] = Face.Right,
        ['U'] = Face.Up,
        ['B'] = Face.Back,
        ['L'] = Face.Left,
        ['D'] = Face.Down,
    };

    /// <summary>Formats the move as standard notation; the inverse of <see cref="TryParse"/>.</summary>
    public string ToNotation()
    {
        var face = Face;
        var letter = FacesByLetter.Single(entry => entry.Value == face).Key;
        return Direction == Direction.CounterClockwise ? $"{letter}{CounterClockwiseSuffix}" : letter.ToString();
    }

    /// <summary>
    /// Parses one token: a face letter (<c>F R U B L D</c>, upper case) optionally followed by a single <c>'</c>.
    /// Returns false for anything else; never throws. On failure <paramref name="move"/> is the default value.
    /// </summary>
    public static bool TryParse(string? token, out Move move)
    {
        move = default;

        if (string.IsNullOrEmpty(token) || !FacesByLetter.TryGetValue(token[FaceLetterIndex], out var face))
        {
            return false;
        }

        if (token.Length == PlainTokenLength)
        {
            move = new Move(face, Direction.Clockwise);
            return true;
        }

        if (token.Length == PrimedTokenLength && token[SuffixIndex] == CounterClockwiseSuffix)
        {
            move = new Move(face, Direction.CounterClockwise);
            return true;
        }

        return false;
    }
}
