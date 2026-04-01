using System;
using System.Collections.Generic;
using System.Linq;
using Llama.Core.Model.Elements;

namespace Llama.Core.PostProcessing
{
    /// <summary>
    /// Averages element integration-point stresses to per-node values for smooth visualization.
    /// </summary>
    public static class StressNodeAverager
    {
        /// <summary>
        /// Stress component indices within the <see cref="ElementStressResult.Components"/> list.
        /// Components layout: [intPt, sxx, syy, szz, sxy, sxz, syz] when Count >= 7,
        /// or [sxx, syy, szz, sxy, sxz, syz] when Count == 6.
        /// </summary>
        public enum StressComponent
        {
            Sxx = 0,
            Syy = 1,
            Szz = 2,
            Sxy = 3,
            Sxz = 4,
            Syz = 5,
            SvM = 6
        }

        /// <summary>
        /// Computes per-node averaged stress for a given component.
        /// Integration-point stresses are first averaged per element, then element values are
        /// averaged at nodes shared by multiple elements.
        /// </summary>
        /// <param name="stresses">Element stress results from the .dat parser.</param>
        /// <param name="elements">Model elements providing connectivity.</param>
        /// <param name="component">Which stress component to extract.</param>
        /// <returns>Dictionary mapping node ID to averaged stress value.</returns>
        public static Dictionary<int, double> ComputePerNodeStress(
            IReadOnlyList<ElementStressResult> stresses,
            IEnumerable<IElement> elements,
            StressComponent component)
        {
            if (stresses == null) throw new ArgumentNullException(nameof(stresses));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            // Step 1: Average integration points per element → single value per element.
            var elementStress = ComputePerElementStress(stresses, component);

            // Step 2: Build element connectivity map.
            var elementMap = new Dictionary<int, IElement>();
            foreach (var el in elements)
                elementMap[el.Id] = el;

            // Step 3: Accumulate at nodes and average.
            var nodeSum = new Dictionary<int, double>();
            var nodeCount = new Dictionary<int, int>();

            foreach (var kvp in elementStress)
            {
                if (!elementMap.TryGetValue(kvp.Key, out var element))
                    continue;

                var value = kvp.Value;

                // Only distribute to corner nodes (first 4 for tet, first 8 for hex).
                var cornerCount = GetCornerNodeCount(element.ElementType);
                var count = Math.Min(cornerCount, element.NodeIds.Count);

                for (var i = 0; i < count; i++)
                {
                    var nodeId = element.NodeIds[i];
                    if (nodeSum.ContainsKey(nodeId))
                    {
                        nodeSum[nodeId] += value;
                        nodeCount[nodeId]++;
                    }
                    else
                    {
                        nodeSum[nodeId] = value;
                        nodeCount[nodeId] = 1;
                    }
                }
            }

            var result = new Dictionary<int, double>(nodeSum.Count);
            foreach (var kvp in nodeSum)
                result[kvp.Key] = kvp.Value / nodeCount[kvp.Key];

            return result;
        }

        /// <summary>
        /// Computes min and max of the per-node stress values.
        /// </summary>
        public static (double Min, double Max) GetRange(IReadOnlyDictionary<int, double> nodeStress)
        {
            if (nodeStress == null || nodeStress.Count == 0)
                return (0.0, 0.0);

            var min = double.MaxValue;
            var max = double.MinValue;
            foreach (var v in nodeStress.Values)
            {
                if (v < min) min = v;
                if (v > max) max = v;
            }

            return (min, max);
        }

        private static Dictionary<int, double> ComputePerElementStress(
            IReadOnlyList<ElementStressResult> stresses,
            StressComponent component)
        {
            // Group by element ID, then average integration points.
            var groups = new Dictionary<int, (double Sum, int Count)>();

            foreach (var stress in stresses)
            {
                var value = ExtractComponent(stress.Components, component);
                if (double.IsNaN(value))
                    continue;

                if (groups.TryGetValue(stress.ElementId, out var acc))
                    groups[stress.ElementId] = (acc.Sum + value, acc.Count + 1);
                else
                    groups[stress.ElementId] = (value, 1);
            }

            var result = new Dictionary<int, double>(groups.Count);
            foreach (var kvp in groups)
                result[kvp.Key] = kvp.Value.Sum / kvp.Value.Count;

            return result;
        }

        private static double ExtractComponent(IReadOnlyList<double> components, StressComponent component)
        {
            // Determine offset: if count >= 7, first value is integration point index (skip it).
            var offset = components.Count >= 7 ? 1 : 0;
            var idx = (int)component;

            if (component == StressComponent.SvM)
            {
                // Compute von Mises from the 6 stress components.
                if (components.Count < offset + 6)
                    return double.NaN;

                var sxx = components[offset];
                var syy = components[offset + 1];
                var szz = components[offset + 2];
                var sxy = components[offset + 3];
                var sxz = components[offset + 4];
                var syz = components[offset + 5];

                var normal = 0.5 * (
                    (sxx - syy) * (sxx - syy) +
                    (syy - szz) * (syy - szz) +
                    (szz - sxx) * (szz - sxx));
                var shear = 3.0 * (sxy * sxy + sxz * sxz + syz * syz);
                return Math.Sqrt(Math.Max(0.0, normal + shear));
            }

            if (idx + offset >= components.Count)
                return double.NaN;

            return components[offset + idx];
        }

        private static int GetCornerNodeCount(CalculixElementType elementType)
        {
            switch (elementType)
            {
                case CalculixElementType.C3D4:
                case CalculixElementType.C3D10:
                    return 4;
                case CalculixElementType.C3D20R:
                    return 8;
                default:
                    return 4;
            }
        }
    }
}
