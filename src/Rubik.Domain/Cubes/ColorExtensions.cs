namespace Rubik.Domain.Cubes;

public static class ColorExtensions
{
    /// <summary>The one place that maps a colour to its letter in the exploded view text.</summary>
    public static char Letter(this Color color) =>
        color switch
        {
            Color.White => 'W',
            Color.Yellow => 'Y',
            Color.Green => 'G',
            Color.Blue => 'B',
            Color.Red => 'R',
            Color.Orange => 'O',
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null),
        };
}
