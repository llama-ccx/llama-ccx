using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Llama.Core.Model;
using Llama.Core.Model.Loads;

namespace Llama.Gh.Components
{
    public class NodalLoadComponent : GH_Component
    {
        public NodalLoadComponent()
            : base("NodalLoad", "Load", "Create nodal loads from a force and/or moment vector.", "Llama", "Model")
        {
            Message = Name + "\nLlama";
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "Target point to match to a model node.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Force", "F", "Force vector (Fx, Fy, Fz).", GH_ParamAccess.item);
            pManager.AddVectorParameter("Moment", "M", "Moment vector (Mx, My, Mz).", GH_ParamAccess.item, Vector3d.Zero);
            pManager[pManager.ParamCount - 1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Nodal Loads", "L", "List of NodalLoad objects (one per non-zero DOF).", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var point = Point3d.Unset;
            var force = Vector3d.Zero;
            var moment = Vector3d.Zero;

            if (!DA.GetData(0, ref point))
                return;
            DA.GetData(1, ref force);
            DA.GetData(2, ref moment);

            var loads = new List<NodalLoad>();

            if (force.X != 0.0)
                loads.Add(new NodalLoad(point.X, point.Y, point.Z, StructuralDof.Ux, force.X));
            if (force.Y != 0.0)
                loads.Add(new NodalLoad(point.X, point.Y, point.Z, StructuralDof.Uy, force.Y));
            if (force.Z != 0.0)
                loads.Add(new NodalLoad(point.X, point.Y, point.Z, StructuralDof.Uz, force.Z));

            if (moment.X != 0.0)
                loads.Add(new NodalLoad(point.X, point.Y, point.Z, StructuralDof.Rx, moment.X));
            if (moment.Y != 0.0)
                loads.Add(new NodalLoad(point.X, point.Y, point.Z, StructuralDof.Ry, moment.Y));
            if (moment.Z != 0.0)
                loads.Add(new NodalLoad(point.X, point.Y, point.Z, StructuralDof.Rz, moment.Z));

            if (loads.Count == 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Both Force and Moment are zero.");

            DA.SetDataList(0, loads);
        }

        protected override System.Drawing.Bitmap Icon => Llama.Gh.Properties.Resources.Llama_24x24;
        public override Guid ComponentGuid => new Guid("7360dc75-f79f-4417-ab5f-4ec8e5e20750");
    }
}
