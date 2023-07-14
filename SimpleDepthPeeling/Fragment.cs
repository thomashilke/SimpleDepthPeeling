public class Fragment
{
    public Fragment(Color color, double depth)
    {
        Color = color;
        Depth = depth;
    }
    public static Fragment Random(Random randomSource)
    {
        return new Fragment(
            new Color(
                clamp(randomSource.NextDouble(), 0.0, 1.0),
                clamp(randomSource.NextDouble(), 0.0, 1.0),
                clamp(randomSource.NextDouble(), 0.0, 1.0),
                clamp(randomSource.NextDouble(), 0.0, 1.0)),
            clamp(randomSource.NextDouble(), 0.0, 1.0));

        double clamp(double x, double lowerBound, double upperBound) => Math.Min(upperBound, Math.Max(x, lowerBound));
    }
    public Color Color { get; }
    public double Depth { get; }
}

