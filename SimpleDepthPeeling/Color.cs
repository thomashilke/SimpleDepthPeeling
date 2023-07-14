using System.Data.Common;

public class Color
{
    public Color(double r, double g, double b, double a)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static readonly Color Black = new Color(0.0, 0.0, 0.0, 1.0);

    public double R { get; }
    public double G { get; }
    public double B { get; }
    public double A { get; }

    public static implicit operator Color(double v) => new Color(v, v, v, v);

    public static Color operator +(Color c1, Color c2) => new Color(c1.R + c2.R, c1.G + c2.G, c1.B + c2.B, c1.A + c2.A);

    public static Color operator -(Color c1, Color c2) => new Color(c1.R - c2.R, c1.G - c2.G, c1.B - c2.B, c1.A - c2.A);

    public static Color operator *(Color c1, Color c2) => new Color(c1.R * c2.R, c1.G * c2.G, c1.B * c2.B, c1.A * c2.A);

    public static bool operator ==(Color c1, Color c2) => c1.R == c2.R && c1.G == c2.G && c1.B == c2.B && c1.A == c2.A;

    public static bool operator !=(Color c1, Color c2) => !(c1 == c2);
}
