using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Llama.Core.Model;
using Llama.Core.PostProcessing;
using Llama.Gh.Widgets;
using Rhino.Geometry;

namespace Llama.Gh.Components
{
    public class VisResultsComponent : GH_SwitcherComponent
    {
        private MenuDropDown _ddStressComponent;

        // ── Result cache ──
        private string _cacheKey;
        private StructuralModel _cachedModel;
        private IReadOnlyList<NodalVectorResult> _cachedDisplacements;
        private IReadOnlyList<ElementStressResult> _cachedStresses;
        private IReadOnlyList<NodalReactionResult> _cachedReactions;
        private Dictionary<int, double> _cachedDispMagnitudes;
        private double _cachedDispMin;
        private double _cachedDispMax;

        public VisResultsComponent()
            : base("VisResults", "VisDat",
                "Visualise deformed geometry, stress contours, and reaction force arrows from CalculiX results.",
                "Llama", "Application")
        {
            Message = Name + "\nLlama";
        }

        protected override string DefaultEvaluationUnit => "VisResults";

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void RegisterEvaluationUnits(EvaluationUnitManager mngr)
        {
            var unit = new EvaluationUnit("VisResults", "VisResults",
                "Visualise deformed mesh with stress contours and reaction force arrows.");

            // ── Main inputs (visible on component body) ──
            // [0] Model
            unit.RegisterInputParam(new Param_GenericObject(), "Model", "M",
                "StructuralModel with a solved .dat file.", GH_ParamAccess.item);
            // [1] Set Name
            unit.RegisterInputParam(new Param_String { Optional = true }, "Set Name", "S",
                "Optional CalculiX set name to filter results.", GH_ParamAccess.item);

            // ── Deformed Geometry inputs ──
            // [2] Displacement Scale
            unit.RegisterInputParam(new Param_Number(), "Displacement Scale", "Sc",
                "Scale factor for displacements (1.0 = true scale).", GH_ParamAccess.item);
            ((Param_Number)unit.Inputs[2].Parameter).SetPersistentData(new GH_Number(1.0));
            // [3] Deformed Colors
            unit.RegisterInputParam(new Param_Colour { Optional = true }, "Deformed Colors", "DC",
                "Gradient colors for deformed mesh. Default: blue→cyan→green→yellow→red.",
                GH_ParamAccess.list);

            // ── Stress Component inputs ──
            // [4] Stress Colors
            unit.RegisterInputParam(new Param_Colour { Optional = true }, "Stress Colors", "SC",
                "Gradient colors for stress contours. Default: blue→cyan→green→yellow→red.",
                GH_ParamAccess.list);

            // ── Reaction Forces inputs ──
            // [5] Force Scale
            unit.RegisterInputParam(new Param_Number(), "Force Scale", "Fs",
                "Scale factor for reaction force arrows.", GH_ParamAccess.item);
            ((Param_Number)unit.Inputs[5].Parameter).SetPersistentData(new GH_Number(1.0));

            // ── Outputs ──
            // [0] Model (pass-through)
            unit.RegisterOutputParam(new Param_GenericObject(), "Model", "M",
                "Pass-through of the input StructuralModel.");
            // [1] Deformed Mesh
            unit.RegisterOutputParam(new Param_Mesh(), "Deformed Mesh", "DM",
                "Deformed mesh with vertex colors.");
            // [2] Stress Mesh
            unit.RegisterOutputParam(new Param_Mesh(), "Stress Mesh", "SM",
                "Deformed mesh colored by the selected stress component.");
            // [3] Stress Min
            unit.RegisterOutputParam(new Param_Number(), "Stress Min", "Smin",
                "Minimum stress value for the selected component.");
            // [4] Stress Max
            unit.RegisterOutputParam(new Param_Number(), "Stress Max", "Smax",
                "Maximum stress value for the selected component.");
            // [5] Force Lines
            unit.RegisterOutputParam(new Param_Line(), "Force Lines", "FL",
                "Reaction force arrows as lines from support nodes.");
            // [6] Force Colors
            unit.RegisterOutputParam(new Param_Colour(), "Force Colors", "FC",
                "Colors for reaction force lines mapped by magnitude.");

            // ── Deformed Geometry menu ──
            var meshMenu = new GH_ExtendableMenu(0, "menu_mesh") { Name = "Deformed Geometry" };
            meshMenu.RegisterInputPlug(unit.Inputs[2]);  // Displacement Scale
            meshMenu.RegisterInputPlug(unit.Inputs[3]);  // Deformed Colors
            meshMenu.RegisterOutputPlug(unit.Outputs[1]); // Deformed Mesh
            meshMenu.Expand();
            unit.AddMenu(meshMenu);

            // ── Stress Component menu ──
            var stressMenu = new GH_ExtendableMenu(1, "menu_stress") { Name = "Stress Component" };
            var stressPanel = new MenuPanel(0, "panel_stress");
            _ddStressComponent = new MenuDropDown(0, "stress_comp", "Component");
            _ddStressComponent.AddItem("SvM", "Von Mises");
            _ddStressComponent.AddItem("Sxx", "Sxx");
            _ddStressComponent.AddItem("Syy", "Syy");
            _ddStressComponent.AddItem("Szz", "Szz");
            _ddStressComponent.AddItem("Sxy", "Sxy");
            _ddStressComponent.AddItem("Sxz", "Sxz");
            _ddStressComponent.AddItem("Syz", "Syz");
            _ddStressComponent.Value = 0; // default = Von Mises
            _ddStressComponent.ValueChanged += (s, e) => ExpireSolution(true);
            stressPanel.AddControl(_ddStressComponent);
            stressMenu.AddControl(stressPanel);
            stressMenu.RegisterInputPlug(unit.Inputs[4]);  // Stress Colors
            stressMenu.RegisterOutputPlug(unit.Outputs[2]); // Stress Mesh
            stressMenu.RegisterOutputPlug(unit.Outputs[3]); // Stress Min
            stressMenu.RegisterOutputPlug(unit.Outputs[4]); // Stress Max
            stressMenu.Expand();
            unit.AddMenu(stressMenu);

            // ── Reaction Forces menu ──
            var forceMenu = new GH_ExtendableMenu(2, "menu_forces") { Name = "Reaction Forces" };
            forceMenu.RegisterInputPlug(unit.Inputs[5]);  // Force Scale
            forceMenu.RegisterOutputPlug(unit.Outputs[5]); // Force Lines
            forceMenu.RegisterOutputPlug(unit.Outputs[6]); // Force Colors
            forceMenu.Expand();
            unit.AddMenu(forceMenu);

            mngr.RegisterUnit(unit);
        }

        protected override void SolveInstance(IGH_DataAccess DA, EvaluationUnit unit)
        {
            // ── Main inputs ──
            object modelObj = null;
            if (!DA.GetData(0, ref modelObj))
                return;

            string setName = null;
            string setNameInput = null;
            DA.GetData(1, ref setNameInput);
            if (!string.IsNullOrWhiteSpace(setNameInput))
                setName = setNameInput.Trim();

            // ── Deformed Geometry inputs ──
            double dispScale = 1.0;
            DA.GetData(2, ref dispScale);

            var defColors = new List<Color>();
            if (Params.Input.Count > 3 && Params.Input[3].SourceCount > 0)
                DA.GetDataList(3, defColors);

            // ── Stress Component inputs ──
            var stressColors = new List<Color>();
            if (Params.Input.Count > 4 && Params.Input[4].SourceCount > 0)
                DA.GetDataList(4, stressColors);

            // ── Reaction Forces inputs ──
            double forceScale = 1.0;
            DA.GetData(5, ref forceScale);

            // ── Unwrap model ──
            if (!TryUnwrapStructuralModel(modelObj, out var model))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input must be a StructuralModel.");
                return;
            }

            // Output [0]: pass-through model
            DA.SetData(0, model);

            var datPath = ResolveDatPath(model);
            if (string.IsNullOrWhiteSpace(datPath) || !File.Exists(datPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"DAT file not found at '{datPath}'.");
                return;
            }

            // ── Cache key: dat path + set name ──
            var key = datPath + "|" + (setName ?? "");
            if (key != _cacheKey)
            {
                IReadOnlyList<CalculixDatTable> tables;
                try
                {
                    tables = CalculixDatParser.ParseFile(datPath);
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to parse DAT: {ex.Message}");
                    return;
                }

                CalculixDatExtractors.TryGetNodalDisplacements(tables, out var displacements, setName);
                CalculixDatExtractors.TryGetElementStress(tables, out var stresses, setName);
                CalculixDatExtractors.TryGetNodalReactions(tables, out var reactions, setName);

                // Pre-compute displacement magnitudes (model-dependent, scale-independent)
                Dictionary<int, double> dispMag = null;
                double dMin = 0.0, dMax = 0.0;
                if (displacements != null && displacements.Count > 0)
                {
                    dispMag = new Dictionary<int, double>(displacements.Count);
                    dMin = double.MaxValue;
                    dMax = double.MinValue;
                    foreach (var d in displacements)
                    {
                        var mag = Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
                        dispMag[d.NodeId] = mag;
                        if (mag < dMin) dMin = mag;
                        if (mag > dMax) dMax = mag;
                    }
                }

                _cachedModel = model;
                _cachedDisplacements = displacements;
                _cachedStresses = stresses;
                _cachedReactions = reactions;
                _cachedDispMagnitudes = dispMag;
                _cachedDispMin = dMin;
                _cachedDispMax = dMax;
                _cacheKey = key;
            }

            // ── Build deformed mesh data (scale-dependent, cheap) ──
            var meshData = DeformedMeshBuilder.Build(model, _cachedDisplacements, dispScale);

            // ── Gradients ──
            var defGradient = defColors.Count >= 2 ? defColors : DefaultDeformedGradient();
            var stressGradient = stressColors.Count >= 2 ? stressColors : DefaultStressGradient();

            // ── Displacement magnitudes for deformed mesh coloring ──
            // Output [1]: Deformed Mesh (colored by displacement magnitude)
            var deformedMesh = BuildRhinoMesh(meshData, _cachedDispMagnitudes, _cachedDispMin, _cachedDispMax, defGradient);
            DA.SetData(1, deformedMesh);

            // ── Stress computation ──
            var stressComponent = MapDropdownToStressComponent(_ddStressComponent?.Value ?? 0);

            Dictionary<int, double> nodeStress = null;
            double stressMin = 0.0, stressMax = 0.0;

            if (_cachedStresses != null && _cachedStresses.Count > 0)
            {
                nodeStress = StressNodeAverager.ComputePerNodeStress(_cachedStresses, model.Elements, stressComponent);
                var range = StressNodeAverager.GetRange(nodeStress);
                stressMin = range.Min;
                stressMax = range.Max;
            }

            // Output [2]: Stress Mesh (colored by stress)
            var stressMesh = BuildRhinoMesh(meshData, nodeStress, stressMin, stressMax, stressGradient);
            DA.SetData(2, stressMesh);

            // Output [3],[4]: Stress Min/Max
            DA.SetData(3, stressMin);
            DA.SetData(4, stressMax);

            // ── Reaction force lines ──
            if (_cachedReactions != null && _cachedReactions.Count > 0)
            {
                var nodeMap = model.Nodes.ToDictionary(n => n.Id);
                var forceLines = new List<Line>();
                var forceColors = new List<Color>();

                double fMin = double.MaxValue, fMax = double.MinValue;
                var magnitudes = new List<double>(_cachedReactions.Count);
                foreach (var r in _cachedReactions)
                {
                    var mag = Math.Sqrt(r.Fx * r.Fx + r.Fy * r.Fy + r.Fz * r.Fz);
                    magnitudes.Add(mag);
                    if (mag < fMin) fMin = mag;
                    if (mag > fMax) fMax = mag;
                }

                for (var i = 0; i < _cachedReactions.Count; i++)
                {
                    var r = _cachedReactions[i];
                    Point3d origin;
                    if (meshData.DeformedPositions.TryGetValue(r.NodeId, out var pos))
                        origin = new Point3d(pos.X, pos.Y, pos.Z);
                    else if (nodeMap.TryGetValue(r.NodeId, out var node))
                        origin = new Point3d(node.X, node.Y, node.Z);
                    else
                        continue;

                    var tip = origin + new Vector3d(r.Fx * forceScale, r.Fy * forceScale, r.Fz * forceScale);
                    forceLines.Add(new Line(origin, tip));
                    forceColors.Add(InterpolateGradient(stressGradient, magnitudes[i], fMin, fMax));
                }

                DA.SetDataList(5, forceLines);
                DA.SetDataList(6, forceColors);
            }
            else
            {
                DA.SetDataList(5, new Line[0]);
                DA.SetDataList(6, new Color[0]);
            }
        }

        private static Mesh BuildRhinoMesh(
            DeformedMeshData meshData,
            IReadOnlyDictionary<int, double> nodeStress,
            double stressMin,
            double stressMax,
            IReadOnlyList<Color> gradient)
        {
            var mesh = new Mesh();

            // Map node IDs to mesh vertex indices.
            var nodeToVertex = new Dictionary<int, int>();

            int GetOrAddVertex(int nodeId)
            {
                if (nodeToVertex.TryGetValue(nodeId, out var idx))
                    return idx;

                if (!meshData.DeformedPositions.TryGetValue(nodeId, out var pos))
                    pos = (0, 0, 0);

                idx = mesh.Vertices.Count;
                mesh.Vertices.Add(pos.X, pos.Y, pos.Z);
                nodeToVertex[nodeId] = idx;
                return idx;
            }

            foreach (var face in meshData.BoundaryFaces)
            {
                if (face.NodeIds.Count == 3)
                {
                    var a = GetOrAddVertex(face.NodeIds[0]);
                    var b = GetOrAddVertex(face.NodeIds[1]);
                    var c = GetOrAddVertex(face.NodeIds[2]);
                    mesh.Faces.AddFace(a, b, c);
                }
                else if (face.NodeIds.Count == 4)
                {
                    var a = GetOrAddVertex(face.NodeIds[0]);
                    var b = GetOrAddVertex(face.NodeIds[1]);
                    var c = GetOrAddVertex(face.NodeIds[2]);
                    var d = GetOrAddVertex(face.NodeIds[3]);
                    mesh.Faces.AddFace(a, b, c, d);
                }
            }

            // Apply vertex colors from stress.
            if (nodeStress != null && nodeStress.Count > 0)
            {
                var colors = new Color[mesh.Vertices.Count];
                foreach (var kvp in nodeToVertex)
                {
                    var nodeId = kvp.Key;
                    var vertexIdx = kvp.Value;
                    if (nodeStress.TryGetValue(nodeId, out var value))
                        colors[vertexIdx] = InterpolateGradient(gradient, value, stressMin, stressMax);
                    else
                        colors[vertexIdx] = Color.Gray;
                }

                mesh.VertexColors.SetColors(colors);
            }

            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }

        private static Color InterpolateGradient(IReadOnlyList<Color> gradient, double value, double min, double max)
        {
            if (gradient.Count == 0)
                return Color.Gray;
            if (gradient.Count == 1)
                return gradient[0];

            var range = max - min;
            double t;
            if (range < 1e-15)
                t = 0.5;
            else
                t = Math.Max(0.0, Math.Min(1.0, (value - min) / range));

            var scaledT = t * (gradient.Count - 1);
            var idx = (int)Math.Floor(scaledT);
            if (idx >= gradient.Count - 1)
                return gradient[gradient.Count - 1];

            var frac = scaledT - idx;
            var c0 = gradient[idx];
            var c1 = gradient[idx + 1];
            return Color.FromArgb(
                (int)(c0.R + frac * (c1.R - c0.R)),
                (int)(c0.G + frac * (c1.G - c0.G)),
                (int)(c0.B + frac * (c1.B - c0.B)));
        }

        private static List<Color> DefaultDeformedGradient()
        {
            return new List<Color>
            {
                Color.FromArgb(10, 0, 0),
                Color.FromArgb(77, 0, 0),
                Color.FromArgb(144, 0, 0),
                Color.FromArgb(211, 0, 0),
                Color.FromArgb(255, 23, 0),
                Color.FromArgb(255, 90, 0),
                Color.FromArgb(255, 157, 0),
                Color.FromArgb(255, 224, 0),
                Color.FromArgb(255, 255, 54),
                Color.FromArgb(255, 255, 154)
            };
        }

        private static List<Color> DefaultStressGradient()
        {
            return new List<Color>
            {
                Color.FromArgb(48, 18, 59),
                Color.FromArgb(68, 90, 205),
                Color.FromArgb(62, 155, 254),
                Color.FromArgb(24, 214, 203),
                Color.FromArgb(70, 247, 131),
                Color.FromArgb(162, 252, 60),
                Color.FromArgb(225, 220, 55),
                Color.FromArgb(253, 165, 49),
                Color.FromArgb(239, 90, 17),
                Color.FromArgb(196, 37, 2)
            };
        }

        private static StressNodeAverager.StressComponent MapDropdownToStressComponent(int index)
        {
            switch (index)
            {
                case 0: return StressNodeAverager.StressComponent.SvM;
                case 1: return StressNodeAverager.StressComponent.Sxx;
                case 2: return StressNodeAverager.StressComponent.Syy;
                case 3: return StressNodeAverager.StressComponent.Szz;
                case 4: return StressNodeAverager.StressComponent.Sxy;
                case 5: return StressNodeAverager.StressComponent.Sxz;
                case 6: return StressNodeAverager.StressComponent.Syz;
                default: return StressNodeAverager.StressComponent.SvM;
            }
        }

        private static bool TryUnwrapStructuralModel(object input, out StructuralModel model)
        {
            model = input as StructuralModel;
            if (model != null)
                return true;

            if (input is IGH_Goo goo)
            {
                var scriptValue = goo.ScriptVariable();
                model = scriptValue as StructuralModel;
                if (model != null)
                    return true;
            }

            var valueProp = input?.GetType().GetProperty("Value");
            if (valueProp != null && valueProp.GetIndexParameters().Length == 0)
            {
                try
                {
                    var value = valueProp.GetValue(input);
                    model = value as StructuralModel;
                    if (model != null)
                        return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        private static string ResolveDatPath(StructuralModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Path))
                return string.Empty;

            var extension = Path.GetExtension(model.Path);
            if (string.Equals(extension, ".dat", StringComparison.OrdinalIgnoreCase))
                return model.Path;

            if (string.Equals(extension, ".inp", StringComparison.OrdinalIgnoreCase))
                return Path.ChangeExtension(model.Path, ".dat");

            return model.Path + ".dat";
        }

        protected override System.Drawing.Bitmap Icon => Llama.Gh.Properties.Resources.Llama_24x24;
        public override Guid ComponentGuid => new Guid("a2c3e8f1-7b4d-4a9e-b6c2-1d5f8e3a7b90");
    }
}
