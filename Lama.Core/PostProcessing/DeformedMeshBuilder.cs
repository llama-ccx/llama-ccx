using System;
using System.Collections.Generic;
using System.Linq;
using Lama.Core.Model;
using Lama.Core.Model.Elements;

namespace Lama.Core.PostProcessing
{
    /// <summary>
    /// Holds boundary face data extracted from a structural model with deformed node positions.
    /// </summary>
    public sealed class DeformedMeshData
    {
        /// <summary>Deformed node positions keyed by node ID.</summary>
        public IReadOnlyDictionary<int, (double X, double Y, double Z)> DeformedPositions { get; }

        /// <summary>Boundary faces. Each face contains ordered corner node IDs and the owning element ID.</summary>
        public IReadOnlyList<BoundaryFace> BoundaryFaces { get; }

        public DeformedMeshData(
            IReadOnlyDictionary<int, (double X, double Y, double Z)> deformedPositions,
            IReadOnlyList<BoundaryFace> boundaryFaces)
        {
            DeformedPositions = deformedPositions ?? throw new ArgumentNullException(nameof(deformedPositions));
            BoundaryFaces = boundaryFaces ?? throw new ArgumentNullException(nameof(boundaryFaces));
        }
    }

    /// <summary>
    /// A single boundary face of an element, defined by its corner node IDs.
    /// </summary>
    public sealed class BoundaryFace
    {
        /// <summary>Corner node IDs in winding order (3 for tri, 4 for quad).</summary>
        public IReadOnlyList<int> NodeIds { get; }

        /// <summary>The element that owns this face.</summary>
        public int ElementId { get; }

        public BoundaryFace(IReadOnlyList<int> nodeIds, int elementId)
        {
            NodeIds = nodeIds ?? throw new ArgumentNullException(nameof(nodeIds));
            ElementId = elementId;
        }
    }

    /// <summary>
    /// Builds deformed node positions and extracts boundary faces from a structural model.
    /// </summary>
    public static class DeformedMeshBuilder
    {
        // C3D10: 4 triangular faces defined by corner node indices (0-3 of the 10-node connectivity).
        private static readonly int[][] Tetra10FaceCorners =
        {
            new[] { 0, 1, 2 },
            new[] { 0, 3, 1 },
            new[] { 1, 3, 2 },
            new[] { 0, 2, 3 }
        };

        // C3D20R: 6 quad faces defined by corner node indices (0-7 of the 20-node connectivity).
        private static readonly int[][] Hexa20FaceCorners =
        {
            new[] { 0, 1, 2, 3 },
            new[] { 4, 7, 6, 5 },
            new[] { 0, 4, 5, 1 },
            new[] { 2, 6, 7, 3 },
            new[] { 0, 3, 7, 4 },
            new[] { 1, 5, 6, 2 }
        };

        /// <summary>
        /// Computes deformed node positions and extracts the external (boundary) faces of the model.
        /// </summary>
        /// <param name="model">The structural model.</param>
        /// <param name="displacements">Nodal displacement results.</param>
        /// <param name="scale">Displacement scale factor.</param>
        public static DeformedMeshData Build(
            StructuralModel model,
            IReadOnlyList<NodalVectorResult> displacements,
            double scale)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            // Build deformed positions: start with originals, then overlay displacements.
            var deformed = new Dictionary<int, (double X, double Y, double Z)>(model.Nodes.Count);
            foreach (var node in model.Nodes)
                deformed[node.Id] = (node.X, node.Y, node.Z);

            if (displacements != null)
            {
                foreach (var d in displacements)
                {
                    if (!deformed.TryGetValue(d.NodeId, out var pos))
                        continue;
                    deformed[d.NodeId] = (pos.X + scale * d.X, pos.Y + scale * d.Y, pos.Z + scale * d.Z);
                }
            }

            // Count face occurrences to find boundary faces.
            var faceCounts = new Dictionary<FaceKey, (BoundaryFace Face, int Count)>();

            foreach (var element in model.Elements)
            {
                var faceCorners = GetFaceCornerTable(element.ElementType);
                if (faceCorners == null)
                    continue;

                foreach (var cornerIndices in faceCorners)
                {
                    var nodeIds = new int[cornerIndices.Length];
                    for (var i = 0; i < cornerIndices.Length; i++)
                        nodeIds[i] = element.NodeIds[cornerIndices[i]];

                    var key = FaceKey.Create(nodeIds);
                    if (faceCounts.TryGetValue(key, out var existing))
                    {
                        faceCounts[key] = (existing.Face, existing.Count + 1);
                    }
                    else
                    {
                        faceCounts[key] = (new BoundaryFace(nodeIds, element.Id), 1);
                    }
                }
            }

            var boundaryFaces = faceCounts.Values
                .Where(kv => kv.Count == 1)
                .Select(kv => kv.Face)
                .ToList();

            return new DeformedMeshData(deformed, boundaryFaces);
        }

        private static int[][] GetFaceCornerTable(CalculixElementType elementType)
        {
            switch (elementType)
            {
                case CalculixElementType.C3D4:
                case CalculixElementType.C3D10:
                    return Tetra10FaceCorners;
                case CalculixElementType.C3D20R:
                    return Hexa20FaceCorners;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Hash key for a face, based on sorted corner node IDs.
        /// </summary>
        private readonly struct FaceKey : IEquatable<FaceKey>
        {
            private readonly int _a;
            private readonly int _b;
            private readonly int _c;
            private readonly int _d; // 0 for triangular faces

            private FaceKey(int a, int b, int c, int d)
            {
                _a = a;
                _b = b;
                _c = c;
                _d = d;
            }

            public static FaceKey Create(int[] nodeIds)
            {
                var sorted = (int[])nodeIds.Clone();
                Array.Sort(sorted);
                return sorted.Length >= 4
                    ? new FaceKey(sorted[0], sorted[1], sorted[2], sorted[3])
                    : new FaceKey(sorted[0], sorted[1], sorted[2], 0);
            }

            public bool Equals(FaceKey other) =>
                _a == other._a && _b == other._b && _c == other._c && _d == other._d;

            public override bool Equals(object obj) => obj is FaceKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = _a;
                    hash = (hash * 397) ^ _b;
                    hash = (hash * 397) ^ _c;
                    hash = (hash * 397) ^ _d;
                    return hash;
                }
            }
        }
    }
}
