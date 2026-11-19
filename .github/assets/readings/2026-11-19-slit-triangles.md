How the wall of 200 m's front is cut into triangles as the 25th window's cut meets it, measured by
cutting its front with the recipe's `triangulate`, which follows the rule the cut's partition takes
its triangles by, each triangle's height over its longest side.

| What | Measure |
|---|---|
| Front faces and their corners | 9, of 120, 23, 20, 19, 8, 5, 3, 3 and 3 |
| Triangles ear clipping cuts them into | 186 |
| The thinnest | 1.5e-8 m across a side of 178.6 m |
| Thinner than 1e-4 m, the join's distance | 31 |
| Thinner than 1e-3 m | 39 |
| Thinner than 1e-2 m | 73 |

Ear clipping cuts a face for a cut in `EditMesh.Booleans.cs` where a mesh's faces become the
partition's polygons, and for the build, the triangulation and a solid's own faces elsewhere. The
face of 120 corners reaches round the windows through slits of no width, so its thinnest triangles
lie along a slit, near no area, and the partition skips a triangle by its area alone, 1e-12, which
they pass. Of the two mends the bound is the smaller: the partition's polygons leave out a triangle
thinner than the boolean's epsilon, one test at one line, where convex pieces round each window
would change how the join builds every face a cut makes and the counts the tests pin.
