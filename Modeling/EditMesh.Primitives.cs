using System.Numerics;

namespace Modeling;

// The primitives, each added to what the mesh holds and selected, so the operation after it acts on
// it. Each is made with its lowest corner at the origin and then placed, by that corner or by the
// middle of its bounds, so every primitive is placed the same way whatever its shape.
public sealed partial class EditMesh
{
    /// <summary>
    /// Adds a box, eight vertices and six faces of four corners, as wide along X, tall along Y and
    /// deep along Z as <paramref name="size"/>.
    /// </summary>
    public void Box(Vector3 size, Vector3 at = default, bool centered = false)
    {
        Positive(size.X, nameof(size));
        Positive(size.Y, nameof(size));
        Positive(size.Z, nameof(size));

        // The floor's outline in Z and X, the order that makes it turn counterclockwise about Y.
        Prism([new(0, 0), new(size.Z, 0), new(size.Z, size.X), new(0, size.X)], Axis.Y, size.Y, at, centered);
        Did($"box {V(size)}{Placement(at, centered)}");
    }

    /// <summary>
    /// Adds a flat rectangle facing up, four vertices and one face whose four edges are open.
    /// </summary>
    public void Plane(float width, float depth, Vector3 at = default, bool centered = false)
    {
        Positive(width, nameof(width));
        Positive(depth, nameof(depth));

        var first = _positions.Count;
        AddVertex(new Vector3(0, 0, 0));
        AddVertex(new Vector3(0, 0, depth));
        AddVertex(new Vector3(width, 0, depth));
        AddVertex(new Vector3(width, 0, 0));
        AddFace([first, first + 1, first + 2, first + 3]);

        Placed(first, _faces.Count - 1, at, centered);
        Did($"plane {F(width)} {F(depth)}{Placement(at, centered)}");
    }

    /// <summary>Adds a cylinder standing along Y, its round side made of flat sides.</summary>
    /// <remarks>
    /// Its corners stand half a side round from the axes, so a cylinder of four sides is a box
    /// whose sides face along X and Z and one of eight has a flat side facing each way along them,
    /// where a corner on an axis would leave it standing on a point for the grid.
    /// </remarks>
    public void Cylinder(float radius, float height, int sides, Vector3 at = default, bool centered = false)
    {
        Positive(radius, nameof(radius));
        Positive(height, nameof(height));
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 3);

        Prism(Round(radius, sides), Axis.Y, height, at, centered);
        Did($"cylinder {F(radius)} {F(height)} {sides}{Placement(at, centered)}");
    }

    /// <summary>
    /// Adds a cone standing along Y, its point at the top over the middle of its base, whose
    /// corners stand half a side round from the axes.
    /// </summary>
    public void Cone(float radius, float height, int sides, Vector3 at = default, bool centered = false)
    {
        Positive(radius, nameof(radius));
        Positive(height, nameof(height));
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 3);

        var first = _positions.Count;
        var firstFace = _faces.Count;
        foreach (var point in Round(radius, sides)) AddVertex(Lift(point, Axis.Y, 0));
        var top = AddVertex(new Vector3(0, height, 0));

        // The base faces down, so its corners run the outline's way backwards, and each side runs
        // along the base's edge the other way from the base, as two faces sharing an edge do.
        AddFace(Enumerable.Range(0, sides).Select(i => first + sides - 1 - i).ToArray());
        for (int i = 0; i < sides; i++) AddFace([first + i, first + (i + 1) % sides, top]);

        Placed(first, firstFace, at, centered);
        Did($"cone {F(radius)} {F(height)} {sides}{Placement(at, centered)}");
    }

    /// <summary>
    /// Adds a wedge, a box cut through from one bottom edge to the opposite top edge, nothing tall
    /// at its lowest Z and its full height at its highest, so it rises as stairs do.
    /// </summary>
    public void Wedge(Vector3 size, Vector3 at = default, bool centered = false)
    {
        Positive(size.X, nameof(size));
        Positive(size.Y, nameof(size));
        Positive(size.Z, nameof(size));

        // The side's outline in Y and Z, which is the plane X is pushed out from.
        Prism([new(0, 0), new(0, size.Z), new(size.Y, size.Z)], Axis.X, size.X, at, centered);
        Did($"wedge {V(size)}{Placement(at, centered)}");
    }

    /// <summary>Adds a flight of stairs rising along Z, closed underneath.</summary>
    /// <remarks>
    /// The first step's front at the lowest Z and the top step's back at the highest, where a back
    /// face as tall as the flight closes it, and a floor under it, so the flight is a solid that
    /// stands on the ground and against a wall. Its two sides are faces of one outline each, a
    /// step's corner at a time. Four vertices more than four a step, and two faces more than four,
    /// a tread and a riser a step, the back, the floor and the two sides.
    /// </remarks>
    public void Stairs(int steps, float rise, float run, float width, Vector3 at = default, bool centered = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        Positive(rise, nameof(rise));
        Positive(run, nameof(run));
        Positive(width, nameof(width));

        // The side's outline in Y and Z, from the foot of the first riser up each riser and along
        // each tread, then down the back and along the floor to the start.
        var outline = new List<Vector2>(2 * steps + 2) { new(0, 0) };
        for (int step = 0; step < steps; step++)
        {
            outline.Add(new Vector2((step + 1) * rise, step * run));
            outline.Add(new Vector2((step + 1) * rise, (step + 1) * run));
        }
        outline.Add(new Vector2(0, steps * run));

        Prism([.. outline], Axis.X, width, at, centered);
        Did($"stairs {steps} {F(rise)} {F(run)} {F(width)}{Placement(at, centered)}");
    }

    /// <summary>
    /// Adds an arch, half a ring standing on its two feet, its opening along Z, twice the radius
    /// wide along X and the radius tall, its front and back one face each.
    /// </summary>
    public void Arch(float radius, float thickness, float depth, int sides, Vector3 at = default, bool centered = false)
    {
        Positive(radius, nameof(radius));
        Positive(thickness, nameof(thickness));
        Positive(depth, nameof(depth));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(thickness, radius);
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 2);

        // Over the top along the outside from the right foot to the left, then back along the
        // inside, which keeps the band on the outline's left, counterclockwise about Z.
        var inner = radius - thickness;
        var outline = new List<Vector2>(2 * sides + 2);
        for (int i = 0; i <= sides; i++) outline.Add(OnCircle(radius, MathF.PI * i / sides));
        for (int i = sides; i >= 0; i--) outline.Add(OnCircle(inner, MathF.PI * i / sides));

        Prism([.. outline], Axis.Z, depth, at, centered);
        Did($"arch {F(radius)} {F(thickness)} {F(depth)} {sides}{Placement(at, centered)}");
    }

    /// <summary>The three axes a prism is pushed out along.</summary>
    private enum Axis
    {
        X,
        Y,
        Z,
    }

    /// <summary>
    /// A solid made by pushing an outline out along an axis, its two ends the outline and a side
    /// face for each of its edges, which a box, a cylinder, a wedge, a flight of stairs and an arch
    /// each are, placed and selected.
    /// </summary>
    /// <param name="outline">
    /// The outline in the plane across the axis, in the two coordinates that turn counterclockwise
    /// about it, Y and Z about X, Z and X about Y and X and Y about Z. Its points are taken either
    /// way round, since the order that makes the ends face out is found from its area.
    /// </param>
    /// <param name="axis">The axis it is pushed out along.</param>
    /// <param name="length">How far.</param>
    /// <param name="at">
    /// Where its lowest corner goes, or its middle where <paramref name="centered"/> is set.
    /// </param>
    /// <param name="centered">
    /// Whether <paramref name="at"/> is its middle rather than its lowest corner.
    /// </param>
    private void Prism(Vector2[] outline, Axis axis, float length, Vector3 at, bool centered)
    {
        var (first, firstFace) = AddPrism(outline, axis, 0, length);
        Placed(first, firstFace, at, centered);
    }

    /// <summary>
    /// Adds the solid an outline makes pushed along an axis from one distance to another, where it
    /// stands, and says its first vertex and its first face.
    /// </summary>
    private (int FirstVertex, int FirstFace) AddPrism(Vector2[] outline, Axis axis, float from, float to)
    {
        if (SignedArea(outline) < 0) Array.Reverse(outline);

        var count = outline.Length;
        var first = _positions.Count;
        var firstFace = _faces.Count;
        foreach (var point in outline) AddVertex(Lift(point, axis, from));
        foreach (var point in outline) AddVertex(Lift(point, axis, to));

        // The near end faces back along the axis, so its corners run the outline backwards, the far
        // end runs it forwards, and each side runs up its edge, along the far end and back, which
        // turns it away from the outline's inside.
        AddFace(Enumerable.Range(0, count).Select(i => first + count - 1 - i).ToArray());
        AddFace(Enumerable.Range(0, count).Select(i => first + count + i).ToArray());
        for (int i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            AddFace([first + i, first + next, first + count + next, first + count + i]);
        }
        return (first, firstFace);
    }

    /// <summary>
    /// Moves what a primitive made, from its first vertex and its first face to the last, to where
    /// it was asked for, and selects it.
    /// </summary>
    private void Placed(int firstVertex, int firstFace, Vector3 at, bool centered)
    {
        var made = Enumerable.Range(firstVertex, _positions.Count - firstVertex).ToArray();
        var bounds = BoundsOf(made);
        var by = centered ? at - (bounds.Min + bounds.Max) * 0.5f : at - bounds.Min;
        foreach (var vertex in made) _positions[vertex] += by;

        _selectedVertices.Clear();
        _selectedFaces.Clear();
        _selectedVertices.UnionWith(made);
        _selectedFaces.UnionWith(Enumerable.Range(firstFace, _faces.Count - firstFace));
        Changed();
    }

    /// <summary>
    /// A point of an outline in the plane across an axis, at a distance along it.
    /// </summary>
    private static Vector3 Lift(Vector2 point, Axis axis, float along) => axis switch
    {
        Axis.X => new Vector3(along, point.X, point.Y),
        Axis.Y => new Vector3(point.Y, along, point.X),
        _ => new Vector3(point.X, point.Y, along),
    };

    /// <summary>
    /// A ring's corners about Y, in Z and X, the first half a side round from Z so the sides of an
    /// even count face along the axes.
    /// </summary>
    private static Vector2[] Round(float radius, int sides) =>
        Enumerable.Range(0, sides).Select(i => OnCircle(radius, MathF.Tau * (i + 0.5f) / sides)).ToArray();

    private static Vector2 OnCircle(float radius, float angle) => new(radius * MathF.Cos(angle), radius * MathF.Sin(angle));

    /// <summary>
    /// Twice the area an outline closes, positive where it runs counterclockwise.
    /// </summary>
    private static float SignedArea(Vector2[] outline)
    {
        var sum = 0f;
        for (int i = 0; i < outline.Length; i++)
        {
            var a = outline[i];
            var b = outline[(i + 1) % outline.Length];
            sum += a.X * b.Y - b.X * a.Y;
        }
        return sum;
    }

    /// <summary>
    /// Adds a tube along a chain of points, round about each with a radius of its own, as a branch
    /// or a pipe is, and closed at both ends.
    /// </summary>
    /// <param name="points">
    /// The points its middle runs through in order, two or more, no two that follow one another at
    /// one place.
    /// </param>
    /// <param name="radii">
    /// How far its round side is from its middle at each point, above zero, one for each point.
    /// </param>
    /// <param name="sides">How many sides its round side has, three or more.</param>
    /// <param name="wrap">
    /// How many times its texture coordinates carry a picture round it and how many meters one
    /// repeat runs along it, or null for once round and its first ring's circumference along.
    /// </param>
    /// <remarks>
    /// <para>
    /// A ring of <paramref name="sides"/> corners stands across the chain at each point, square to
    /// the way the chain runs there, midway between the ways in and out, and each ring is turned
    /// from the one before by the least that keeps it square, the rotation minimizing frame of Wang
    /// and others (2008), so the sides run along the chain with no twist however it bends. The
    /// sides are smoothed into one another, so the tube is round, and its two ends are flat.
    /// </para>
    /// <para>
    /// Its texture coordinates run once round it, and along it in lengths of its first ring's
    /// circumference, so a bark texture's texels are square where the tube starts and stretch along
    /// it only as far as it narrows. A tube that starts on another's middle with a radius under the
    /// other's there starts inside it, which closes the joint between them with no boolean, as a
    /// branch is joined to the stem it grows from.
    /// </para>
    /// <para>
    /// It stands where its points are, which are in the mesh's space, and its line in the recipe
    /// holds every point and radius, <c>tube SIDES X Y Z RADIUS X Y Z RADIUS</c> and on, with
    /// <c>wrap AROUND ALONG</c> after the sides where a wrap is given, and <c>radius</c> after it
    /// where
    /// the wrap follows the radius.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The sides are under three, a radius is not above zero, or a wrap's count or length is not.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// There are under two points, a radius is missing for a point, a point is not a place, or two
    /// points that follow one another are at one place.
    /// </exception>
    public void Tube(IReadOnlyList<Vector3> points, IReadOnlyList<float> radii, int sides, TubeWrap? wrap = null)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(radii);
        ArgumentOutOfRangeException.ThrowIfLessThan(sides, 3);
        if (points.Count < 2) throw new ArgumentException("A tube runs through two points or more.", nameof(points));
        if (radii.Count != points.Count) throw new ArgumentException($"A tube has a radius for each of its {points.Count} points, and {radii.Count} were given.", nameof(radii));
        if (wrap is { } given)
        {
            if (!(given.Around > 0) || !float.IsFinite(given.Around)) throw new ArgumentOutOfRangeException(nameof(wrap), given.Around, "A tube's picture goes round it a finite number of times above zero.");
            if (!(given.Along > 0) || !float.IsFinite(given.Along)) throw new ArgumentOutOfRangeException(nameof(wrap), given.Along, "A repeat of a tube's picture runs a finite length above zero along it.");
        }

        var count = points.Count;
        for (var i = 0; i < count; i++)
        {
            Positive(radii[i], nameof(radii));
            var point = points[i];
            if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(point.Z)) throw new ArgumentException($"A tube's point {i} is at {V(point)}, which is not a place.", nameof(points));
            if (i > 0 && point == points[i - 1]) throw new ArgumentException($"A tube's points {i - 1} and {i} are at one place, which gives the way it runs between them no direction.", nameof(points));
        }

        // The way the chain runs at each point, midway between its ways in and out, the one it has
        // where it turns straight back. No two points that follow one another are at one place, so
        // each way in and out has a length to be made one of.
        var ways = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            var into = i > 0 ? Vector3.Normalize(points[i] - points[i - 1]) : Vector3.Zero;
            var outOf = i < count - 1 ? Vector3.Normalize(points[i + 1] - points[i]) : Vector3.Zero;
            var both = into + outOf;
            ways[i] = both.LengthSquared() > 1e-8f ? Vector3.Normalize(both) : i > 0 ? into : outOf;
        }

        // Each ring's frame from the one before, by two reflections, the first in the plane square
        // to the chord between the points and the second in the one that brings the way in line.
        var across = Vector3.Normalize(Vector3.Cross(ways[0], MathF.Abs(ways[0].Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
        var first = _positions.Count;
        var firstFace = _faces.Count;
        var along = new float[count];
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                var chord = points[i] - points[i - 1];
                along[i] = along[i - 1] + chord.Length();
                var reflected = Reflect(across, chord);
                across = Vector3.Normalize(Reflect(reflected, ways[i] - Reflect(ways[i - 1], chord)));
            }

            var up = Vector3.Cross(ways[i], across);
            for (var side = 0; side < sides; side++)
            {
                var (sin, cos) = MathF.SinCos(MathF.Tau * side / sides);
                AddVertex(points[i] + (across * cos + up * sin) * radii[i]);
            }
        }

        // Each side runs round its near ring and back along its far one, which faces it out, as a
        // prism's sides do, and the ends run their rings backwards and forwards.
        var (around, round) = wrap is { } density ? (density.Around, density.Along) : (1f, MathF.Tau * radii[0]);
        // How far the coordinates run along the tube to each ring, a repeat every so many meters,
        // or,
        // where the wrap follows the radius, a repeat as much shorter as a segment's mean radius is
        // than the first ring's, so its texels keep the shape they have at the base.
        var follows = wrap is { FollowsRadius: true };
        var vs = new float[count];
        for (var i = 1; i < count; i++)
            vs[i] = vs[i - 1] + (along[i] - along[i - 1]) / (round * (follows ? (radii[i - 1] + radii[i]) / 2 / radii[0] : 1f));
        for (var i = 0; i < count - 1; i++)
        {
            var near = first + i * sides;
            var far = near + sides;
            for (var side = 0; side < sides; side++)
            {
                var next = (side + 1) % sides;
                var (u, uNext) = (around * side / sides, around * (side + 1) / sides);
                var (v, vFar) = (vs[i], vs[i + 1]);
                AddFace([near + side, near + next, far + next, far + side], smooth: 180f, uvs: [new(u, v), new(uNext, v), new(uNext, vFar), new(u, vFar)]);
            }
        }

        AddFace(Enumerable.Range(0, sides).Select(side => first + sides - 1 - side).ToArray());
        AddFace(Enumerable.Range(0, sides).Select(side => first + (count - 1) * sides + side).ToArray());

        _selectedVertices.Clear();
        _selectedFaces.Clear();
        _selectedVertices.UnionWith(Enumerable.Range(first, _positions.Count - first));
        _selectedFaces.UnionWith(Enumerable.Range(firstFace, _faces.Count - firstFace));
        Changed();

        var line = new System.Text.StringBuilder("tube ").Append(sides);
        if (wrap is { } named) line.Append(" wrap ").Append(F(named.Around)).Append(' ').Append(F(named.Along)).Append(named.FollowsRadius ? " radius" : "");
        for (var i = 0; i < count; i++) line.Append(' ').Append(V(points[i])).Append(' ').Append(F(radii[i]));
        Did(line.ToString());
    }

    /// <summary>
    /// A vector reflected in the plane square to another, which need not be of unit length.
    /// </summary>
    private static Vector3 Reflect(Vector3 vector, Vector3 normal)
    {
        var squared = normal.LengthSquared();
        return squared > 0f ? vector - normal * (2f * Vector3.Dot(normal, vector) / squared) : vector;
    }

    private static void Positive(float value, string name)
    {
        if (!(value > 0) || !float.IsFinite(value))
            throw new ArgumentOutOfRangeException(name, value, "A measure of a primitive is a finite number above zero.");
    }
}
