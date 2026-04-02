using System;

namespace Llama.Core.Units
{
    /// <summary>
    /// Global unit state for the Llama session.
    /// Users pick Length, Force, and Mass; all other units are derived.
    /// </summary>
    public static class UnitSystem
    {
        public static LengthUnit Length { get; set; } = LengthUnit.m;
        public static ForceUnit Force { get; set; } = ForceUnit.N;
        public static MassUnit Mass { get; set; } = MassUnit.kg;
        public static TimeUnit Time { get; set; } = TimeUnit.s;
        public static AngleUnit Angle { get; set; } = AngleUnit.rad;
        public static TemperatureUnit Temperature { get; set; } = TemperatureUnit.K;

        /// <summary>Raised after any unit selection changes via <see cref="Apply"/>.</summary>
        public static event Action UnitsChanged;

        /// <summary>
        /// Bulk-update all primary selections and raise <see cref="UnitsChanged"/> once.
        /// </summary>
        public static void Apply(LengthUnit length, ForceUnit force, MassUnit mass,
                                  TemperatureUnit temperature = TemperatureUnit.K,
                                  AngleUnit angle = AngleUnit.rad)
        {
            Length = length;
            Force = force;
            Mass = mass;
            Temperature = temperature;
            Angle = angle;
            UnitsChanged?.Invoke();
        }
    }
}
