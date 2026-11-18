# 3DEngine.Modeling cheatsheet

The module's public types, `EditMesh`, shaped by its operations or by a recipe's lines, `EditMeshBuild`,
which builds it into the triangles an engine draws, and `IModelFiles`, the files a mesh reads and
writes, with the types they take and give. The README's section Binding it in your engine says how an
engine takes them.

Every public type of the library with its public members, each a line of its declaration and the first sentence
of its summary. The list is written by `Cheatsheet.Write` from the built library, and the text around it by hand.

<!-- written by Cheatsheet.Write from the built library -->
```csharp
class EditMesh                                           // A polygon mesh shaped by primitives and by operations on its selection, its vertices, its faces, its material slots, its selection and the lines of its recipe
float DefaultEpsilon = 1E-05f;                           // How near a point is to a plane to lie in it where the mesh says no other, a hundredth of a millimeter
float OpeningMargin = 0.01f;                             // How far beyond a wall's faces a window or a door is cut, a centimeter, so its sides lie in no plane of the wall's
float FinestGrid = 0.0625f;                              // The finest grid step, a sixteenth of a meter
float CoarsestGrid = 64f;                                // The coarsest grid step, sixty-four meters
string DefaultSlot = "default";                          // The name of the first material slot, which every new face takes
string Extension = ".model";                             // What a recipe's file name ends in
string Header = "model 1";                               // The line a recipe's file starts with, its form and the form's version
float DefaultSmoothAngle = 30f;                          // The angle a face is smoothed across when none is named, thirty degrees, so a cylinder of twelve sides or more is drawn round and a box's edges stay sharp
EditMesh(IModelFiles? files = null);                     // Makes an empty mesh, which reads and writes through the files given
float BooleanEpsilon { get; }                            // How near a point is to a plane to lie in it, for the booleans, and ten times it how near two points are to be one
IModelFiles Files { get; }                               // What the mesh reads and writes, its recipe's file and the files its lines name, which every mesh it makes in a join or a boolean shares
int VertexCount { get; }                                 // How many vertices the mesh has, a vertex's number running from zero to one less
int FaceCount { get; }                                   // How many faces the mesh has, a face's number running from zero to one less
int EdgeCount { get; }                                   // How many edges the faces have between them, an edge two faces share counted once
IReadOnlyList<string> Slots { get; }                     // The names of the material slots, in their order, DefaultSlot first until it is renamed
int Version { get; }                                     // A count that moves on every change to what the mesh builds into, and not on a change of the selection
float GridStep { get; }                                  // The grid step Snap rounds to, one meter until Grid sets another
int OpenEdges { get; }                                   // How many edges are not shared by two faces running along them in opposite directions, which is none for a closed mesh whose faces all face out or all face in
bool IsClosed { get; }                                   // Whether the mesh has faces and every edge is shared by two faces running along it in opposite directions, so it holds a volume and a collider of it has an inside
float Volume { get; }                                    // The space a closed mesh holds, in cubic meters, negative where its faces face in
Bounds Bounds { get; }                                   // The smallest box holding every vertex, or two zeros for a mesh with none
IReadOnlyList<string> Recipe { get; }                    // The operations that made the mesh, a line each, the oldest first
IReadOnlyList<string> Modifiers { get; }                 // The modifiers in the order they apply, each as its words after modifier in the recipe
IReadOnlyCollection<string> Undone { get; }              // The lines taken back and not done again, the next to be done again first
bool CanUndo { get; }                                    // Whether there is an operation to undo
bool CanRedo { get; }                                    // Whether there is an undone operation to do again
string RecipeText { get; }                               // The recipe as the text of its file, its header first
int SelectedVertexCount { get; }                         // How many vertices are selected, the corners of the selected faces among them
int SelectedFaceCount { get; }                           // How many faces are selected
int SelectedEdgeCount { get; }                           // How many edges are selected, which is those whose two ends are
void Bevel(float width, int segments = 1);               // Cuts each selected edge back by a width into a band of faces, round across in a number of segments, and closes each corner with one face
void Epsilon(float distance);                            // Sets how near a point is to a plane to lie in it, for the booleans, above zero and up to a centimeter
void Union(string pathOrSolid, Vector3 at = default);    // Joins another solid into this one where they overlap, a model's recipe file moved by an offset or a solid primitive's line
void Subtract(string pathOrSolid, Vector3 at = default);  // Takes another solid away from this one, a model's recipe file moved by an offset or a solid primitive's line
void Intersect(string pathOrSolid, Vector3 at = default);  // Keeps what this solid and another both hold, a model's recipe file moved by an offset or a solid primitive's line
void Cut(string primitive);                              // Takes a solid primitive, made where its line places it, away from this mesh, the faces it left selected
void Window(float width, float height, float along, float sill);  // Cuts a window through the wall whose face was selected first, measured on every selected face in its plane, as deep as the wall
void Door(float width, float height, float along);       // Cuts a door through the wall whose face was selected first, from its bottom, as deep as the wall
void Room(Vector3 size, float thickness, Vector3 at = default, bool centered = false);  // Adds a room, a box with its inside hollowed out by a thickness of wall, floor and ceiling, and selects it
void Ramp(float rise, float run, float width, Vector3 at = default, bool centered = false);  // Adds a ramp rising along Z, a wedge given in the words of a flight of stairs, and selects it
Vector3 Position(int vertex);                            // A vertex's place, in the mesh's space, in meters
ReadOnlySpan<int> Corners(int face);                     // A face's vertices, in order round it, counterclockwise seen from the side it faces
string SlotOf(int face);                                 // The name of a face's material slot
int SlotIndexOf(int face);                               // The number of a face's material slot in Slots, which the build gives the face's triangles to
float SmoothOf(int face);                                // The angle in degrees up to which a face is smoothed into those beside it, or zero where it is drawn flat
Vector3 Area(int face);                                  // A face's normal at twice its area, Newell's sum, which a smoothed corner's normal is weighed by
Vector3 Normal(int face);                                // The way a face faces, a length of one, or zero for a face with no area
Vector3 Center(int face);                                // The middle of a face, the mean of its corners
void Clear();                                            // Takes away every vertex, face and slot but the first, and the selection with them
void LoopCut(float fraction = 0.5f);                     // Cuts round the ring of four-cornered faces each selected edge crosses, through each face from edge to opposite edge at a fraction along them
void Subdivide(int cuts = 1);                            // Cuts each selected face into a grid, each of its edges into one more part than the count of cuts, and selects the grid's faces
void Knife(Vector3 normal, Vector3 point = default);     // Cuts the selected faces along a plane through a point, each face the plane crosses split in two along it
void Import(string path);                                // Adds the meshes of a model file, read by ReadModel, as faces of this mesh, and selects them
string Export(string path, bool embedBuffer = false, Func<string, Vector4>? prototypeColor = null);  // Writes the mesh with its modifiers applied as a glTF 2.0 file through the mesh's files, a primitive and a material for each slot a face is in, and answers the file's full path
void Bridge();                                           // Joins the loops round two regions of selected faces, taking the faces away and filling between the loops with a band of four-cornered faces
void Fill();                                             // Closes each hole whose edges all have both ends selected with one face, which is selected after
void Dissolve();                                         // Joins each region of selected faces into one face round it, taking away the edges and the vertices inside it
void DissolveEdges();                                    // Takes away each selected edge that two faces run along, joining the faces on either side into one
EditMesh Modified();                                     // The mesh with every modifier applied in order, this mesh itself where there is none, made again only after a change
void ModifierMirror(string axes, Vector3 pivot = default);  // Adds a modifier that mirrors the mesh across the planes through a pivot square to each axis named, x, y or z, one after another
void ModifierArray(int count, Vector3 offset);           // Adds a modifier that repeats the mesh a number of times in all, each copy an offset further on
void ModifierArrayCurve(ReadOnlySpan<Vector3> positions);  // Adds a modifier that repeats the mesh at each of a curve's positions, moved by the position's offset from the first, which is the mesh itself
void ModifierBevel(float width, int segments = 1, float angle = 0f);  // Adds a modifier that bevels every edge two faces run along, or those whose faces meet at more than an angle in degrees
void ModifierSubdivision(int level);                     // Adds a modifier that subdivides the mesh by Catmull and Clark's rule to a level, each level making every face four-cornered and rounding the shape
void ModifierSolidify(float thickness);                  // Adds a modifier that gives the mesh a thickness, a second surface out along its normals, or in where the thickness is below zero, and a rim along its open edges
void ModifierBoolean(string kind, string pathOrSolid, Vector3 at = default);  // Adds a modifier that joins a solid to the mesh, takes it away or keeps where the two overlap, as Union, Subtract and Intersect do, each time the mesh is built
void ModifierTriangulate();                              // Adds a modifier that cuts every face of more than three corners into triangles
void ModifierMove(int from, int to);                     // Moves a modifier from one place in the stack to another, the ones between moving up or down one
void ModifierRemove(int index);                          // Takes a modifier out of the stack
void ModifierApply(int index);                           // Does a modifier to the mesh itself, as though it were first in the stack, and takes it out, the selection left as nothing
void Box(Vector3 size, Vector3 at = default, bool centered = false);  // Adds a box, eight vertices and six faces of four corners, as wide along X, tall along Y and deep along Z as size
void Plane(float width, float depth, Vector3 at = default, bool centered = false);  // Adds a flat rectangle facing up, four vertices and one face whose four edges are open
void Cylinder(float radius, float height, int sides, Vector3 at = default, bool centered = false);  // Adds a cylinder standing along Y, its round side made of flat sides
void Cone(float radius, float height, int sides, Vector3 at = default, bool centered = false);  // Adds a cone standing along Y, its point at the top over the middle of its base, whose corners stand half a side round from the axes
void Wedge(Vector3 size, Vector3 at = default, bool centered = false);  // Adds a wedge, a box cut through from one bottom edge to the opposite top edge, nothing tall at its lowest Z and its full height at its highest, so it rises as stairs do
void Stairs(int steps, float rise, float run, float width, Vector3 at = default, bool centered = false);  // Adds a flight of stairs rising along Z, closed underneath
void Arch(float radius, float thickness, float depth, int sides, Vector3 at = default, bool centered = false);  // Adds an arch, half a ring standing on its two feet, its opening along Z, twice the radius wide along X and the radius tall, its front and back one face each
void Tube(IReadOnlyList<Vector3> points, IReadOnlyList<float> radii, int sides, TubeWrap? wrap = null);  // Adds a tube along a chain of points, round about each with a radius of its own, as a branch or a pipe is, and closed at both ends
bool Undo();                                             // Takes back the last operation, by making the mesh again from the lines before it, and says whether there was one
bool Redo();                                             // Does again the operation last taken back, and says whether there was one, an operation done after an undo forgetting what was taken back
void Run(string line);                                   // Runs one line of a recipe on the mesh, as though its operation were called, a comment after # left out
EditMesh FromRecipe(string text, string name = "the recipe", IModelFiles? files = null);  // Makes a mesh from a recipe's text, its lines run in order from an empty mesh, blank lines and comments skipped
EditMesh Load(string path, IModelFiles? files = null);   // Makes a mesh from a recipe's file, read through the files given, which starts with Header
string Save(string path);                                // Writes the recipe to a file through the mesh's files and answers the full path it was written to
bool IsVertexSelected(int vertex);                       // Whether a vertex is selected
bool IsFaceSelected(int face);                           // Whether a face is selected
void SelectAll();                                        // Selects every vertex and face, select all in the recipe
void SelectNone();                                       // Selects nothing, select none in the recipe
void SelectInvert();                                     // Selects what was not selected, and leaves what was
void SelectFaces(ReadOnlySpan<int> faces, SelectionMode mode = SelectionMode.Replace);  // Selects faces by their indices
void SelectEdges(ReadOnlySpan<(int A, int B)> edges, SelectionMode mode = SelectionMode.Replace);  // Selects edges by their two ends, which selects the ends
void SelectFacing(Vector3 direction, SelectionMode mode = SelectionMode.Replace);  // Selects the faces that face nearest one way along an axis, such as up
void SelectInside(Vector3 corner, Vector3 opposite, SelectionMode mode = SelectionMode.Replace);  // Selects the vertices inside a box, the faces all of whose corners are inside it, and the edges between
void SelectSlot(string slot, SelectionMode mode = SelectionMode.Replace);  // Selects the faces in a material slot, a name no slot has selecting none
SlotLook LookAt(int slot);                               // What a slot is drawn with, by its index in Slots, nothing given where it is the prototype material of its name
SlotLook LookOf(string slot);                            // What a slot is drawn with, by its name, nothing given where it is the prototype material of its name
void SlotAdd(string slot);                               // Adds a slot no face is in yet, drawn with the prototype material of its name
void SlotRename(string slot, string name);               // Gives a slot another name, its faces and its look kept
void SlotRemove(string slot);                            // Takes a slot away, its faces given the first slot, the slots after it moving down by one
void SlotColor(string slot, Vector4 color);              // Gives a slot a color, as sRGB numbers from zero to one, which tints its texture, or its grid where it has none, alpha below one drawing it blended
void SlotMap(string slot, MaterialMap map, string? path);  // Gives a slot a map by its file, or takes it away with null, the base color's texture taking the grid's place
void SlotEmissive(string slot, Vector3 color);           // Gives a slot an emissive color, in linear numbers that may pass one for a bright light
void SlotMetallic(string slot, float metallic);          // Sets how metal a slot's faces are, from zero to one
void SlotRoughness(string slot, float roughness);        // Sets how rough a slot's faces are, from zero, a mirror, to one
int? SelectedTextureWidth();                             // The width in pixels of the texture of the first selected face's slot, read from its file through the mesh's files, or null where it has none or the file cannot be read
void Mirror(Vector3 axis, Vector3 pivot = default);      // Copies the selected faces mirrored across the plane through a point square to an axis, the vertices on the plane shared by both, and selects the copy
void Spin(Vector3 axis, float degrees, int steps, Vector3 center = default);  // Sweeps the selected faces, or where none is selected the selected edges, round an axis through a point by an angle in steps, a band of faces each step
void Smooth(float factor = 0.5f, int repeat = 1);        // Moves each selected vertex toward the mean of the vertices it shares an edge with by a factor, all from where they stood
void Extrude(float distance);                            // Pushes the selected faces out along their normals, the sides between them and what they left filled
void Extrude(Vector3 by);                                // Pushes the selected faces out by one vector, every corner of the region moved by it, as a wall is pushed up from a floor's edge
void Inset(float distance);                              // Makes each selected face a smaller face inside it, with a band of faces round it
void Delete(bool fill = false);                          // Takes away the selected faces, or where none is selected the selected vertices with every face using them
void Merge(float distance);                              // Joins the selected vertices nearer one another than a distance into one, where the first of them is
void Flip();                                             // Turns the selected faces to face the other way, their corners run backwards
void Triangulate();                                      // Cuts each selected face of more than three corners into triangles, as the build cuts it
void Duplicate();                                        // Copies the selected faces where they are, with vertices of their own, and selects the copy, which moves without the first
void Join(string path);                                  // Adds another model's mesh, made from its recipe's file, to this one, and selects it
void Move(Vector3 by);                                   // Moves the selected vertices by a distance along each axis, in meters
void Rotate(Vector3 axis, float degrees, Vector3? pivot = null);  // Turns the selected vertices about an axis, counterclockwise looking back along it
void Scale(Vector3 factor, Vector3? pivot = null);       // Scales the selected vertices toward or away from a point, the middle of the box holding them where no pivot is given
void Grid(float step);                                   // Sets the grid step Snap rounds to, a meter or a power of two of it from a sixteenth to sixty-four
void Snap();                                             // Puts each selected vertex on the nearest point of the grid, a coordinate halfway between two steps going to the one further from zero
void Material(string slot);                              // Puts the selected faces in a material slot, which is made where the mesh has none of that name
void Shade(bool smooth, float degrees = 30f);            // Draws the selected faces smoothed into one another up to an angle between two faces, so a cylinder's sides are round and its ends stay flat, or flat
Vector2[] UvsOf(int face);                               // The texture coordinates of a face's corners, in their order, its own or the world-aligned default
bool HasOwnUvs(int face);                                // Whether a face has texture coordinates of its own, given by a uv operation, rather than the world-aligned default
void UvPlanar(Vector3 facing);                           // Gives the selected faces coordinates by a planar projection along a direction, in meters
void UvBox();                                            // Gives the selected faces the world-aligned default as coordinates of their own, which then follow their corners
void UvCylinder(Vector3 axis, Vector3? center = null);   // Gives the selected faces coordinates by a cylindrical projection about an axis
void UvSphere(Vector3? center = null);                   // Gives the selected faces coordinates by a spherical projection about a point
void UvMove(float u, float v);                           // Moves the selected faces' coordinates, a whole number leaving a repeating texture where it was
void UvScale(float u, float v, bool each = false);       // Scales the selected faces' coordinates about their middle, two showing the picture twice as often, as a whole or each face about its own middle
void UvRotate(float degrees, bool each = false);         // Turns the selected faces' coordinates about their middle, from the first coordinate toward the second, as a whole or each face about its own middle
void UvDensity(float pixelsPerMeter, int texturePixels);  // Scales the selected faces' coordinates about their middle so a texture of a width shows so many of its pixels a meter
void UvReset();                                          // Puts the selected faces back on the world-aligned default

class EditMeshBuild                                      // Builds an edit mesh into the vertices and triangles of one mesh for each material slot
(MeshVertex[] Vertices, uint[] Indices)[] Slots(EditMesh mesh);  // The vertices and triangles of each material slot, in the order of the mesh's slots

record struct MeshVertex                                 // A corner of a built mesh's triangle, its place, its normal and its texture coordinates
MeshVertex(Vector3 Position, Vector3 Normal, Vector2 Uv);
Vector3 Position { get; init; }                          // The place, in the mesh's space, in meters
Vector3 Normal { get; init; }                            // The way the surface faces at the corner, a length of one
Vector2 Uv { get; init; }                                // The texture coordinates

record struct Bounds                                     // A box along the axes, its lowest corner and its highest, in meters
Bounds(Vector3 Min, Vector3 Max);
Vector3 Min { get; init; }                               // The corner with the lowest X, Y and Z
Vector3 Max { get; init; }                               // The corner with the highest X, Y and Z

record SlotLook                                          // What a material slot of an edit mesh is drawn with, a color, a texture and the maps of a glTF material, each null until the recipe gives it
SlotLook();                                              // Makes a look with nothing given, the prototype material of the slot's name
Vector4? Color { get; init; }                            // The color, as sRGB numbers from zero to one as a color picker shows them, with its alpha, or null
string? Texture { get; init; }                           // The picture the faces show, the material's base color texture, or null
string? NormalMap { get; init; }                         // The normal map, or null
string? OcclusionMap { get; init; }                      // The ambient occlusion map, or null
string? MetallicRoughnessMap { get; init; }              // The map of metalness in its blue channel and roughness in its green, as glTF lays it out, or null
Vector3? Emissive { get; init; }                         // The emissive color, in linear numbers that may pass one, or null
string? EmissiveMap { get; init; }                       // The emissive map, or null
float? Metallic { get; init; }                           // How metal the faces are, from zero to one, or null
float? Roughness { get; init; }                          // How rough the faces are, from zero to one, or null
bool IsPrototype { get; }                                // Whether nothing is given, so the slot is drawn with the prototype material of its name
string Words();                                          // What is given, in the recipe's words, as color 1 0 0 1, texture bricks.png, or nothing for a prototype

enum MaterialMap                                         // A map a material slot's look names by its file, each with the word its recipe line takes
Albedo = 0,                                              // The base color's texture, which takes the grid's place, texture in a recipe
Normal = 1,                                              // The normal map, in tangent space, normal-map in a recipe
Occlusion = 2,                                           // The ambient occlusion map, occlusion-map in a recipe
MetallicRoughness = 3,                                   // The map of metalness in its blue channel and roughness in its green, as glTF lays it out, metallic-roughness-map in a recipe
Emission = 4,                                            // The emissive map, emissive-map in a recipe

interface IModelFiles                                    // The files an EditMesh reads and writes, its recipe's own file, the recipes its join lines name, the model files its import lines name, the textures its slots name and the glTF file it is exported as, found by a path as the program that holds the mesh finds its files
string ReadText(string path);                            // Reads a text file whole, a recipe's
string WriteText(string path, string text);              // Writes a text file whole, so a program reading it as it is written reads the old file or the new one whole, and answers the full path it was written to
string WriteBytes(string path, byte[] bytes);            // Writes a file of bytes whole, as WriteText writes text, a glTF file's and its buffer's, and answers the full path it was written to
IReadOnlyList<ModelPart> ReadModel(string path);         // Reads a model file's meshes, each placed as the file's nodes place it
int? ImageWidth(string path);                            // The width in pixels of an image file, or null where there is no such file or its header cannot be read

record ModelPart                                         // One mesh of a model file, its triangles placed as the file's nodes place it, its material's name and, where the file holds them, its corners' normals and texture coordinates and its material's look
ModelPart(IReadOnlyList<Vector3> Positions, IReadOnlyList<int> Indices, string? Material, IReadOnlyList<Vector3>? Normals = null, IReadOnlyList<Vector2>? Uvs = null, MaterialDescription? Look = null);
IReadOnlyList<Vector3> Positions { get; init; }          // The vertices' places, in meters, in the file's space
IReadOnlyList<int> Indices { get; init; }                // Three vertices a triangle, counterclockwise seen from the side it faces
string? Material { get; init; }                          // The name of the mesh's material as the file gives it, or null where it has none
IReadOnlyList<Vector3>? Normals { get; init; }           // Each vertex's normal in the file's space, a length of one, one for each place, or null
IReadOnlyList<Vector2>? Uvs { get; init; }               // Each vertex's texture coordinates, the second running down the picture, one for each place, or null
MaterialDescription? Look { get; init; }                 // The material's look, its color linear and its pictures' paths as the files find them, or null

class ModelFolder                                        // The files of a folder, a relative path read below it and a rooted one as it is, for the tests and for a program with no engine
ModelFolder(string? root = null);
ModelFolder WorkingDirectory { get; }                    // The working directory's files, which a mesh made with no files of its own reads
string Full(string path);                                // The full path a path names, below the folder where it is relative
string ReadText(string path);                            // Reads a text file below the folder whole
string WriteText(string path, string text);              // Writes a text file below the folder whole, its folders made, and answers its full path
string WriteBytes(string path, byte[] bytes);            // Writes a file of bytes below the folder whole, its folders made, and answers its full path
IReadOnlyList<ModelPart> ReadModel(string path);         // Reads a glTF file below the folder into a part for each of its materials
int? ImageWidth(string path);                            // The width in pixels of a PNG or a KTX2 below the folder, from its header, or null for any other file or none

enum SelectionMode                                       // What a selection of an EditMesh does with what was selected before it
Replace = 0,                                             // What it picks is all that is selected after it
Add = 1,                                                 // What it picks is selected as well as what was
Remove = 2,                                              // What it picks is taken out of the selection, and the rest stays as it was

record struct TubeWrap                                   // How a tube's texture coordinates carry a picture, how many times round it and how many meters of the tube one repeat runs along it
TubeWrap(float Around, float Along, bool FollowsRadius = false);
float Around { get; init; }                              // How many times the picture goes round the tube, above zero
float Along { get; init; }                               // How many meters along the tube one repeat of the picture runs at its first ring, above zero
bool FollowsRadius { get; init; }                        // Whether a repeat along grows shorter as the tube narrows, in step with its radius

class ModelCommands                                      // The model commands, one for each operation of an edit mesh's recipe and those that make, name, read, write, show and forget a model, run on the models of a ModelStore
ModelCommands(ModelStore store, IModelScene scene, Func<string, Vector4>? prototypeColor = null);  // Makes the commands over a store's models and an engine's world
IReadOnlyList<ModelCommand> All { get; }                 // Every command, those of the store first and then one for each operation of the recipe
CommandAnswer Run(string line);                          // Runs a line, its command's name and then its words, and answers what the command answers

record ModelCommand                                      // One model command, its name, what it does, the words it takes and what runs it
ModelCommand(string Name, string Summary, string Usage, Func<string, CommandAnswer> Run);
string Name { get; init; }                               // The command's name, as model.box, the line's first word
string Summary { get; init; }                            // What the command does, a sentence a console's help shows
string Usage { get; init; }                              // The words the command takes, its name first, in the grammar ModelCommands gives
Func<string, CommandAnswer> Run { get; init; }           // Runs the command on the words of a line after its name

class ModelStore                                         // The models the commands reach, each by a name of one word, and the files every mesh it makes reads and writes
ModelStore(IModelFiles? files = null);
IModelFiles Files { get; }                               // What the meshes the store makes read and write
IReadOnlyList<(string Name, EditMesh Mesh)> All { get; }  // The models by name, in the order they were named
bool IsName(string? name);                               // Whether a word is a model's name, one word with no space in it
EditMesh Make();                                         // Makes an empty mesh that reads and writes through the store's files
void Name(string name, EditMesh mesh);                   // Gives a mesh a name, in place of the mesh that had it
EditMesh? Find(string name);                             // The mesh of a name, or null where no model has it
bool Forget(string name);                                // Forgets a name, and says whether a model had it

record CommandAnswer                                     // What a model command answers, the text a script reads and why it was refused where it was
CommandAnswer(string Text, CommandFailure? Failure = null);
string Text { get; init; }                               // The answer, a model's counts after an operation, or why the line was refused
CommandFailure? Failure { get; init; }                   // Why the line was refused, or null where it was done
bool Done { get; }                                       // Whether the line was done
string? Code { get; }                                    // The failure as a code of capitals and underscores, as NO_SUCH_MODEL, which a console that names its failures so takes as it is, or null where the line was done

enum CommandFailure                                      // Why a model command refused its line
BadArgument = 0,                                         // A word of the line is not what the command takes, or the operation refused it
NoSuchCommand = 1,                                       // The line names no command of ModelCommands
NoSuchModel = 2,                                         // No model has the name, which an operation that makes no model needs
NothingToUndo = 3,                                       // The model has no operation to take back
NothingToRedo = 4,                                       // The model has no operation taken back to do again
NoSuchFile = 5,                                          // There is no file at the path a load names
BadFile = 6,                                             // The file a load names is not a model's recipe, or a line of it fails
NotWritten = 7,                                          // A file could not be written

interface IModelScene                                    // What the model commands ask of an engine's world, to draw a model and to take away what draws it
void Show(string name, EditMesh mesh, Vector3 at);       // Draws a model at a point in the world, in meters, or moves it there where it is drawn already
void Forget(string name);                                // Takes away what draws a model, where anything does
```
