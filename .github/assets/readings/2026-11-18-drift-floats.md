Where an edit mesh holds a float between two cuts, read with `grep -c _positions Modeling/EditMesh*.cs`
and `grep -n "(float)" Modeling/EditMesh*.cs`.

| What | Where | Count |
|---|---|---|
| The store of every point, `List<Vector3>` | `EditMesh.cs:63` | 1 |
| Its uses | 14 partial files, the modifiers 22, the topology 18, the mesh 14, the booleans 13, the sweeps 10, the coordinates 9, the bevel 8, the primitives 7, the cuts 7, the transforms 5, the selection 4, the exchange 2, the recipe and the joins 1 each | 155 |
| The partition's points made floats again | `EditMesh.Booleans.cs:220`, once at each cut's end | 1 |
| Sums worked in doubles and stored as floats | `EditMesh.cs:307`, `EditMesh.Topology.cs:447` | 2 |
| Public members giving a point | `Position`, `Center`, `Bounds` | 3 |
| A face's own coordinates and their fit, in floats | `Face.Uvs`, `UvMap` | 2 |

The partition and the joining, `Csg` and `FaceMerge`, work in doubles already, so a cut loses its
precision only where its points are stored back into the one list of floats and read from it by
the next cut. The steps that follow from it, each leaving the suite green, are the 155 uses put
behind two accessors with nothing else changed, the list made one of doubles behind them with the
public members still giving floats, the cut storing the partition's points as they are, and the
skipped wall of 200 m turned on where it then passes.
