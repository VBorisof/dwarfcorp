using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    /// <summary>
    /// Represents a collection of voxels with a surface mesh. Efficiently culls away
    /// invisible voxels, and properly constructs ramps.
    /// </summary>
    public class VoxelListPrimitive : GeometricPrimitive, IDisposable
    {
        protected readonly bool[] drawFace = new bool[6];
        protected bool isRebuilding = false;
        private readonly Mutex rebuildMutex = new Mutex();
        private static bool StaticsInitialized = false;

        protected void InitializeStatics()
        {
            if(!StaticsInitialized)
            {
                FaceDeltas[(int)BoxFace.Back] = new Vector3(0, 0, 1);
                FaceDeltas[(int)BoxFace.Front] = new Vector3(0, 0, -1);
                FaceDeltas[(int)BoxFace.Left] = new Vector3(-1, 0, 0);
                FaceDeltas[(int)BoxFace.Right] = new Vector3(1, 0, 0);
                FaceDeltas[(int)BoxFace.Top] = new Vector3(0, 1, 0);
                FaceDeltas[(int)BoxFace.Bottom] = new Vector3(0, -1, 0);

                VertexNeighbors2D[(int)VoxelVertex.FrontTopLeft] = new List<Vector3>()
                {
                    new Vector3(-1, 0, 0),
                    new Vector3(-1, 0, 1),
                    new Vector3(0, 0, 1)
                };
                VertexNeighbors2D[(int)VoxelVertex.FrontTopRight] = new List<Vector3>()
                {
                    new Vector3(0, 0, 1),
                    new Vector3(1, 0, 1),
                    new Vector3(1, 0, 0)
                };
                VertexNeighbors2D[(int)VoxelVertex.BackTopLeft] = new List<Vector3>()
                {
                    new Vector3(-1, 0, 0),
                    new Vector3(-1, 0, -1),
                    new Vector3(0, 0, -1)
                };
                VertexNeighbors2D[(int)VoxelVertex.BackTopRight] = new List<Vector3>()
                {
                    new Vector3(0, 0, -1),
                    new Vector3(1, 0, -1),
                    new Vector3(1, 0, 0)
                };

                VoxelChunk.CreateFaceDrawMap();
                StaticsInitialized = true;
            }
        }

        public VoxelListPrimitive() :
            base()
        {
            InitializeStatics();
        }


        public static bool IsTopVertex(VoxelVertex v)
        {
            return v == VoxelVertex.BackTopLeft || v == VoxelVertex.FrontTopLeft || v == VoxelVertex.FrontTopRight || v == VoxelVertex.BackTopRight;
        }

        public void InitializeFromChunk(VoxelChunk chunk)
        {
            if (chunk == null)
            {
                return;
            }

            rebuildMutex.WaitOne();

            if (isRebuilding)
            {
                rebuildMutex.ReleaseMutex();
                return;
            }

            isRebuilding = true;
            rebuildMutex.ReleaseMutex();
            int[] ambientValues = new int[4];
            int maxIndex = 0;
            int maxVertex = 0;

            VoxelHandle v = chunk.MakeVoxel(0, 0, 0);
            VoxelHandle voxelOnFace = chunk.MakeVoxel(0, 0, 0);
            VoxelHandle worldVoxel = new VoxelHandle();
            VoxelHandle[] manhattanNeighbors = new VoxelHandle[4];

            BoxPrimitive bedrockModel = VoxelLibrary.GetPrimitive("Bedrock");
            List<VoxelHandle> lightingScratchSpace = new List<VoxelHandle>(8);
            int totalFaces = 6;
            
            if (Vertices == null)
            {
                Vertices = new ExtendedVertex[1024];
            }

            if (Indexes == null)
            {
                Indexes = new ushort[512];
            }

            for (int y = 0; y < Math.Min(chunk.Manager.ChunkData.MaxViewingLevel + 1, chunk.SizeY); y++)
            {
                for(int x = 0; x < chunk.SizeX; x++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        v.GridPosition = new Vector3(x, y, z); 

                        if((v.IsExplored && v.IsEmpty) || !v.IsVisible) continue;

                        BoxPrimitive primitive = VoxelLibrary.GetPrimitive(v.Type);

                        if (v.IsExplored && primitive == null) continue;

                        if (!v.IsExplored)
                        {
                            primitive = bedrockModel;
                        }

                        Color tint = v.Type.Tint;

                        BoxPrimitive.BoxTextureCoords uvs = primitive.UVs;

                        if (v.Type.HasTransitionTextures && v.IsExplored)
                        {
                            uvs = ComputeTransitionTexture(v, manhattanNeighbors);
                        }

                        for(int i = 0; i < totalFaces; i++)
                        {
                            BoxFace face = (BoxFace) i;
                            Vector3 delta = FaceDeltas[(int)face];

                            // Pull the current neighbor voxel based on the face it would be touching.

                            if (chunk.IsCellValid(x + (int)delta.X, y + (int)delta.Y, z + (int)delta.Z))
                            {
                                voxelOnFace.GridPosition = v.GridPosition + delta;
                                drawFace[(int)face] = IsFaceVisible(v, voxelOnFace, face);
                            }
                            else
                            {
                                bool success = chunk.Manager.ChunkData.GetNonNullVoxelAtWorldLocation(
                                    v.GridPosition + delta + chunk.Origin, ref worldVoxel);

                                drawFace[(int)face] = !success || IsFaceVisible(v, worldVoxel, face);
                            }

                            if (drawFace[(int)face])
                            {
                                // Set up vertex data

                                int faceIndex = 0;
                                int faceCount = 0;
                                int vertexIndex = 0;
                                int vertexCount = 0;

                                primitive.GetFace(face, uvs, out faceIndex, out faceCount, out vertexIndex, out vertexCount);
                                int indexOffset = maxVertex;

                                // Add vertex data to visible voxel faces

                                for (int vertOffset = 0; vertOffset < vertexCount; vertOffset++)
                                {
                                    ExtendedVertex vert = primitive.Vertices[vertOffset + vertexIndex];
                                    VoxelVertex bestKey = primitive.Deltas[vertOffset + vertexIndex];

                                    VoxelChunk.VertexColorInfo colorInfo = new VoxelChunk.VertexColorInfo();
                                    VoxelChunk.CalculateVertexLight(v, bestKey, chunk.Manager, lightingScratchSpace, ref colorInfo);
                                    ambientValues[vertOffset] = colorInfo.AmbientColor;
                                    Vector3 offset = Vector3.Zero;
                                    Vector2 texOffset = Vector2.Zero;

                                    if (v.Type.CanRamp && VoxelChunk.ShouldRamp(bestKey, v.RampType))
                                    {
                                        offset = new Vector3(0, -v.Type.RampSize, 0);
                                    }

                                    if (maxVertex >= Vertices.Length)
                                    {
                                        ExtendedVertex[] newVertices = new ExtendedVertex[Vertices.Length * 2];
                                        Vertices.CopyTo(newVertices, 0);
                                        Vertices = newVertices;
                                    }

                                    Vertices[maxVertex] = new ExtendedVertex(
                                        vert.Position + v.Position + offset +
                                            VertexNoise.GetNoiseVectorFromRepeatingTexture(vert.Position + v.Position),
                                        new Color(colorInfo.SunColor, colorInfo.AmbientColor, colorInfo.DynamicColor),
                                        tint,
                                        uvs.Uvs[vertOffset + vertexIndex] + texOffset,
                                        uvs.Bounds[faceIndex / 6]);

                                    maxVertex++;
                                }

                                bool flippedQuad = ambientValues[0] + ambientValues[2] >
                                                   ambientValues[1] + ambientValues[3];

                                for (int idx = faceIndex; idx < faceCount + faceIndex; idx++)
                                {
                                    if (maxIndex >= Indexes.Length)
                                    {
                                        ushort[] indexes = new ushort[Indexes.Length * 2];
                                        Indexes.CopyTo(indexes, 0);
                                        Indexes = indexes;
                                    }

                                    ushort vertexOffset = flippedQuad ? primitive.FlippedIndexes[idx] : primitive.Indexes[idx];
                                    ushort vertexOffset0 = flippedQuad ? primitive.FlippedIndexes[faceIndex] : primitive.Indexes[faceIndex];
                                    Indexes[maxIndex] =
                                        (ushort)((int)indexOffset + (int)((int)vertexOffset - (int)vertexOffset0));
                                    maxIndex++;
                                }
                            }
                        }
                        // End looping faces
                    }
                }
            }

            MaxIndex = maxIndex;
            MaxVertex = maxVertex;
            GenerateLightmap(chunk.Manager.ChunkData.Tilemap.Bounds);
            isRebuilding = false;

            //chunk.PrimitiveMutex.WaitOne();
            chunk.NewPrimitive = this;
            chunk.NewPrimitiveReceived = true;
            //chunk.PrimitiveMutex.ReleaseMutex();
        }

        private BoxPrimitive.BoxTextureCoords ComputeTransitionTexture(VoxelHandle V, VoxelHandle[] manhattanNeighbors)
        {
            if (!V.Type.HasTransitionTextures && V.Primitive != null)
            {
                return V.Primitive.UVs;
            }
            else if (V.Primitive == null)
            {
                return null;
            }
            else
            {
                return V.Type.TransitionTextures[V.Chunk.ComputeTransitionValue(
                    V.Type.Transitions, (int)V.GridPosition.X, (int)V.GridPosition.Y, (int)V.GridPosition.Z, manhattanNeighbors)];
            }
        }

        public void Dispose()
        {
            rebuildMutex.Dispose();
        }
    }

}