using System.Numerics;
using Xunit;

namespace Modeling.Tests;

/// <summary>
/// An edit mesh built into the triangles an engine draws, a mesh of each slot with each face's own
/// corners, normals and texture coordinates in meters, smoothing within an angle, and concave faces
/// cut inside their outlines.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EditMeshBuildTests
{
    private const float Close = 1e-4f;

    private static EditMesh New() => new();

    private static (MeshVertex[] Vertices, uint[] Indices)[] Built(EditMesh mesh) => EditMeshBuild.Slots(mesh);

    [Fact]
    public void A_Box_Builds_Into_One_Mesh_Of_Twenty_Four_Corners_Each_Face_With_Its_Own_Normal()
    {
        var mesh = New();
        mesh.Box(new Vector3(2, 3, 4));
        var (vertices, indices) = Built(mesh).Single();
        Assert.Equal((24, 36), (vertices.Length, indices.Length));

        // Each corner of the top faces up and is laid out by X and Z, in meters, so the top's
        // texture spans two by four.
        var top = vertices.Where(vertex => vertex.Normal == Vector3.UnitY).ToArray();
        Assert.Equal(4, top.Count());
        Assert.All(top, vertex => Assert.True(vertex.Position.Y == 3));
        Assert.Equal(2, (top.Max(vertex => vertex.Uv.X) - top.Min(vertex => vertex.Uv.X)), Close);
        Assert.Equal(4, (top.Max(vertex => vertex.Uv.Y) - top.Min(vertex => vertex.Uv.Y)), Close);

        // A wall's second coordinate runs down the world, so a texture stands upright on it.
        var front = vertices.Where(vertex => vertex.Normal == Vector3.UnitZ).ToArray();
        Assert.Equal(3, (front.MinBy(vertex => vertex.Position.Y).Uv.Y - front.MaxBy(vertex => vertex.Position.Y).Uv.Y), Close);

        AllTrianglesFaceTheirNormals(vertices, indices);
    }

    [Fact]
    public void Faces_In_Slots_Build_Into_A_Mesh_Each_In_The_Order_Of_The_Slots()
    {
        var mesh = New();
        mesh.Box(new Vector3(4, 3, 4));
        mesh.SelectFacing(Vector3.UnitY);
        mesh.Material("ceiling");
        mesh.SelectAll();
        mesh.Material("wall");
        var built = Built(mesh);

        Assert.Equal(new[] { "default", "ceiling", "wall" }, mesh.Slots.ToArray());
        Assert.Equal(3, built.Count());
        // A slot no face is in is one triangle with no area.
        Assert.Equal(new[] { 0u, 1u, 2u }, built[0].Indices);
        Assert.Single(built[0].Vertices.Select(vertex => vertex.Position).Distinct());
        Assert.Equal((24, 36), (built[2].Vertices.Length, built[2].Indices.Length));

        mesh.SelectFacing(-Vector3.UnitY);
        mesh.Material("floor");
        built = Built(mesh);
        Assert.Equal((20, 4), (built[2].Vertices.Length, built[3].Vertices.Length));
    }

    [Fact]
    public void A_Cylinder_Smoothed_Past_The_Angle_Between_Its_Sides_Is_Round_And_Its_Ends_Stay_Flat()
    {
        var mesh = New();
        mesh.Cylinder(1, 2, 8, Vector3.Zero, centered: true);
        mesh.SelectAll();
        mesh.Shade(true, 50);

        foreach (var vertex in Built(mesh)[0].Vertices)
        {
            if (MathF.Abs(vertex.Normal.Y) > 0.5f)
            {
                // An end, ninety degrees from the sides, keeps its own normal.
                Assert.Equal(1, MathF.Abs(vertex.Normal.Y), Close);
                continue;
            }
            // A side's corner faces straight out from the axis, as a round side's would.
            var outward = Vector3.Normalize(vertex.Position with { Y = 0 });
            Assert.Equal(0, vertex.Normal.Y, Close);
            Assert.True(Vector3.Dot(vertex.Normal, outward) > 0.9999f, $"{vertex.Normal} at {vertex.Position}");
        }

        // Smoothed at thirty degrees, less than the forty-five between sides, it stays faceted.
        var faceted = New();
        faceted.Cylinder(1, 2, 8);
        faceted.SelectAll();
        faceted.Shade(true);
        Assert.Equal(10, Built(faceted)[0].Vertices.Select(vertex => vertex.Normal).Distinct().Count());
    }

    [Fact]
    public void A_Flights_Concave_Sides_Are_Cut_Inside_Their_Outlines()
    {
        var mesh = New();
        mesh.Stairs(6, 0.2f, 0.3f, 1.5f);
        var (vertices, indices) = Built(mesh)[0];

        // Each side of 14 corners is 12 triangles, and the 14 faces of four corners 2 each.
        Assert.Equal((2 * 12 + 14 * 2) * 3, indices.Length);
        AllTrianglesFaceTheirNormals(vertices, indices);
        Assert.Equal(MeshInfo.Of(mesh).Volume, Volume(vertices.Select(vertex => vertex.Position).ToArray(), indices), Close);
    }

    // Each triangle's winding gives a normal along its corners' normals.
    private static void AllTrianglesFaceTheirNormals(MeshVertex[] vertices, uint[] indices)
    {
        for (int t = 0; t < indices.Length; t += 3)
        {
            var (a, b, c) = (vertices[indices[t]].Position, vertices[indices[t + 1]].Position, vertices[indices[t + 2]].Position);
            Assert.True(Vector3.Dot(Vector3.Cross(b - a, c - a), vertices[indices[t]].Normal) > 0, $"the triangle at {t / 3} winds the way it faces");
        }
    }

    // The volume triangles close, by the solid each makes with the origin.
    private static float Volume(Vector3[] positions, uint[] indices)
    {
        double sum = 0;
        for (int t = 0; t < indices.Length; t += 3)
            sum += Vector3.Dot(positions[indices[t]], Vector3.Cross(positions[indices[t + 1]], positions[indices[t + 2]]));
        return (float)(sum / 6.0);
    }
}
