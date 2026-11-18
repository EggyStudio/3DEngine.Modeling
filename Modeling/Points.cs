using System.Collections;
using System.Numerics;

namespace Modeling;

// An edit mesh's points, kept in doubles and read and written as floats through the indexer, the
// one door every operation takes. A cut reads and writes them as they are through Exact and
// AddExact, so a point the partition worked out in doubles reaches the next cut as it was and not
// rounded to a float between.
internal sealed class Points : IReadOnlyList<Vector3>
{
    private readonly List<Csg.Point> _points = [];

    public int Count => _points.Count;

    public Vector3 this[int index]
    {
        get => Float(_points[index]);
        set => _points[index] = new Csg.Point(value.X, value.Y, value.Z);
    }

    // A point as it is kept, in doubles.
    public Csg.Point Exact(int index) => _points[index];

    public void Add(Vector3 point) => _points.Add(new Csg.Point(point.X, point.Y, point.Z));

    public void AddExact(Csg.Point point) => _points.Add(point);

    public void AddRange(IEnumerable<Vector3> points)
    {
        foreach (var point in points) Add(point);
    }

    public void Clear() => _points.Clear();

    public IEnumerator<Vector3> GetEnumerator() => _points.Select(Float).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static Vector3 Float(Csg.Point point) => new((float)point.X, (float)point.Y, (float)point.Z);
}
