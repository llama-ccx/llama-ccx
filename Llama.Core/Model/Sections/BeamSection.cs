using Llama.Core.Materials;

namespace Llama.Core.Model.Sections
{
    /// <summary>
    /// Beam section assignment with generic properties.
    /// </summary>
    public sealed class BeamSection : SectionBase
    {
        public BeamSectionProperties Properties { get; }

        public BeamSection(string elementSetName, MaterialBase material, BeamSectionProperties properties)
            : base(elementSetName, material)
        {
            Properties = properties;
        }
    }
}
