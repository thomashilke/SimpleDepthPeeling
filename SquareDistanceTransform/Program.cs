var samples = Enumerable.Range(0, 10).Select(i => Math.Sin(i)).ToList();

var sdt = SquaredDistanceTransform(samples);
var refe = ReferenceSquaredDistanceTransform(samples);

Console.WriteLine(string.Join(", ", sdt.Select(v => v.ToString())));

Console.WriteLine(string.Join(", ", refe.Select(v => v.ToString())));

List<double> ReferenceSquaredDistanceTransform(List<double> f)
{
    var result = new List<double>(f.Count);

    for (var p = 0; p < f.Count; ++p)
    {
        result.Add(Enumerable.Range(0, f.Count).Select(q => (p - q) * (p - q) + f[q]).Min());
    }

    return result;
}


List<double> SquaredDistanceTransformOfIndicatrice(List<bool> f)
{
    var indicatrice = Enumerable.Range(0, f.Count).Where(i => f[i]).ToList();
    var n = indicatrice.Count;

    var v = new int[n];
    Array.Fill(v, 0);
    var z = new double[n + 1];
    Array.Fill(z, 0.0);

    var k = 0;
    v[0] = 0;
    z[0] = double.NegativeInfinity;
    z[1] = double.PositiveInfinity;

    for (var q = 1; q < n; ++q)
    {

    }
}

List<double> SquaredDistanceTransform(List<double> f)
{
    var n = f.Count;

    var v = new int[n];
    Array.Fill(v, 0);
    var z = new double[n + 1];
    Array.Fill(z, 0.0);

    var k = 0;
    v[0] = 0;
    z[0] = double.NegativeInfinity;
    z[1] = double.PositiveInfinity;

    for (var q = 1; q < n; ++q)
    {
        var s = ((f[q] + q * q) - (f[v[k]] + v[k] * v[k])) / (2.0 * q - 2.0 * v[k]);
        while (s <= z[k])
        {
            k -= 1;
            s = ((f[q] + q * q) - (f[v[k]] + v[k] * v[k])) / (2.0 * q - 2.0 * v[k]);
        }

        k += 1;

        v[k] = q;
        z[k] = s;
        z[k+1] = double.PositiveInfinity;
    }

    var result = new List<double>(n);
    k = 0;
    for (var q = 0; q < n; ++q)
    {
        while (z[k + 1] < q)
        {
            k += 1;
        }

        result.Add((q - v[k])*(q - v[k]) + f[v[k]]);
    }

    return result;

    static double Intersection(int q, double fq, int vk, double fvk)
    {
        return ((fq + q * q) - (fvk + vk * vk)) / (2.0 * q - 2.0 * vk);
    }
}
