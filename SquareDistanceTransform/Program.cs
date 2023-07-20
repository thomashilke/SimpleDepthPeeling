using static SquareDistanceTransform;

var samples = Enumerable.Range(0, 10).Select(i => Math.Sin(i)).ToList();

var sdt = Transform(samples);
var refe = ReferenceTransform(samples);

Console.WriteLine(string.Join(", ", sdt.Select(v => v.ToString())));

Console.WriteLine(string.Join(", ", refe.Select(v => v.ToString())));

var indicatrice = new[] { false, true, true, true, false }.ToList();

Console.WriteLine(string.Join(", ", ReferenceTransform(indicatrice).Select(v => v.ToString())));

Console.WriteLine(string.Join(", ", Transform(indicatrice).Select(v => v.ToString())));


var ind = Enumerable.Repeat<bool>(false, 300).Concat(Enumerable.Repeat<bool>(true, 200)).Concat(Enumerable.Repeat<bool>(false, 300));
var dst = Transform(ind.ToList());
Console.WriteLine();
