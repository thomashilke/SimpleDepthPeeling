using System.Data.Common;

public class Rgb
{
    public Rgb(double v) : this (v, v, v)
    { }

    public Rgb(double r, double g, double b)
    {
        R = r;
        G = g;
        B = b;
    }

    public double R { get; }

    public double G { get; }

    public double B { get; }


    public static implicit operator Rgb(double v) => new Rgb(v);

    public static Rgb operator +(Rgb c1, Rgb c2) => new Rgb(c1.R + c2.R, c1.G + c2.G, c1.B + c2.B);

    public static Rgb operator -(Rgb c1, Rgb c2) => new Rgb(c1.R - c2.R, c1.G - c2.G, c1.B - c2.B);

    public static Rgb operator *(Rgb c1, Rgb c2) => new Rgb(c1.R * c2.R, c1.G * c2.G, c1.B * c2.B);

    public static bool operator ==(Rgb c1, Rgb c2) => c1.R == c2.R && c1.G == c2.G && c1.B == c2.B;

    public static bool operator !=(Rgb c1, Rgb c2) => !(c1 == c2);
}

public class Color
{
    public Color(Rgb rgb, double a)
    {
        Rgb = rgb;
        A = a;
    }

    public Color(double r, double g, double b, double a)
    {
        Rgb = new Rgb(r, g, b);
        A = a;
    }

    public static readonly Color OpaqueBlack = new Color(0.0, 0.0, 0.0, 1.0);

    public Rgb Rgb { get; }

    public double R  => Rgb.R;
    public double G => Rgb.G;
    public double B => Rgb.B;
    public double A { get; }

    public static implicit operator Color(double v) => new Color(new Rgb(v), v);

    public static Color operator +(Color c1, Color c2) => new Color(c1.Rgb + c2.Rgb, c1.A + c2.A);

    public static Color operator -(Color c1, Color c2) => new Color(c1.Rgb - c2.Rgb, c1.A - c2.A);

    public static Color operator *(Color c1, Color c2) => new Color(c1.Rgb * c2.Rgb, c1.A * c2.A);

    public static bool operator ==(Color c1, Color c2) => c1.R == c2.R && c1.G == c2.G && c1.B == c2.B && c1.A == c2.A;

    public static bool operator !=(Color c1, Color c2) => !(c1 == c2);
}
