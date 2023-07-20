public static class SquareDistanceTransform
{
    public static List<double> ReferenceTransform(List<double> f)
    {
        var result = new List<double>(f.Count);

        for (var p = 0; p < f.Count; ++p)
        {
            result.Add(Enumerable.Range(0, f.Count).Select(q => (p - q) * (p - q) + f[q]).Min());
        }

        return result;
    }

    public static List<double> ReferenceTransform(List<bool> indicatrice)
    {
        var sites = Enumerable.Range(0, indicatrice.Count).Where(i => indicatrice[i]).ToList();

        if (!sites.Any())
        {
            return indicatrice.Select(v => 0.0).ToList();
        }

        var result = new List<double>(indicatrice.Count);

        for (var p = 0; p < indicatrice.Count; ++p)
        {
            result.Add(sites.Select(q => Math.Pow(p - q, 2)).Min());
        }

        return result;
    }

    public static List<double> Transform(List<bool> indicatrice)
    {
        var sites = Enumerable.Range(0, indicatrice.Count).Where(i => indicatrice[i]).ToList();
        var n = sites.Count;

        if (n == 0)
        {
            return indicatrice.Select(v => 0.0).ToList();
        }

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
            var s = Intersection(sites[q], 0.0, sites[v[k]], 0.0);
            while (s <= z[k])
            {
                k -= 1;
                s = Intersection(sites[q], 0.0, sites[v[k]], 0.0);
            }

            k += 1;
            v[k] = q;
            z[k] = s;
            z[k + 1] = double.PositiveInfinity;
        }

        var result = new List<double>(indicatrice.Count);
        k = 0;
        for (var q = 0; q < indicatrice.Count; ++q)
        {
            while (z[k + 1] < q)
            {
                k += 1;
            }

            result.Add(Math.Pow(q - sites[v[k]], 2));
        }

        return result;

        static double Intersection(int q, double fq, int vk, double fvk)
        {
            return ((fq + Math.Pow(q, 2)) - (fvk + Math.Pow(vk, 2))) / (2.0 * q - 2.0 * vk);
        }
    }

    public static List<double> Transform(List<double> f)
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
            var s = Intersection(q, f[q], v[k], f[v[k]]);
            while (s <= z[k])
            {
                k -= 1;
                s = Intersection(q, f[q], v[k], f[v[k]]);
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

            result.Add(Math.Pow(q - v[k], 2) + f[v[k]]);
        }

        return result;

        static double Intersection(int q, double fq, int vk, double fvk)
        {
            return ((fq + Math.Pow(q, 2)) - (fvk + Math.Pow(vk, 2))) / (2.0 * q - 2.0 * vk);
        }
    }
}
