using System.Collections;
using System.Numerics;

namespace Modeling;

// An edit mesh's points, read and written as floats through the indexer, the one door every
// operation takes, so what the points are kept in behind it changes in one place.
internal sealed class Points : IReadOnlyList<Vector3>
{
    private readonly List<Vector3> _points = [];

    public int Count => _points.Count;

    public Vector3 this[int index]
    {
        get => _points[index];
        set => _points[index] = value;
    }

    public void Add(Vector3 point) => _points.Add(point);

    public void AddRange(IEnumerable<Vector3> points) => _points.AddRange(points);

    public void Clear() => _points.Clear();

    public IEnumerator<Vector3> GetEnumerator() => _points.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
