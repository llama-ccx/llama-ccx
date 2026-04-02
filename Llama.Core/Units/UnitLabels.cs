namespace Llama.Core.Units
{
    /// <summary>
    /// Provides display labels for primary and derived unit quantities
    /// based on the current <see cref="UnitSystem"/> selections.
    /// </summary>
    public static class UnitLabels
    {
        // ── Primary ──────────────────────────────────────────────

        public static string LengthLabel => UnitSystem.Length.ToString();
        public static string ForceLabel => UnitSystem.Force.ToString();
        public static string MassLabel => UnitSystem.Mass.ToString();
        public static string TimeLabel => UnitSystem.Time.ToString();
        public static string AngleLabel => UnitSystem.Angle.ToString();
        public static string TemperatureLabel => UnitSystem.Temperature.ToString();

        // ── Derived ──────────────────────────────────────────────

        /// <summary>Force / Length²  (e.g. kN/m²)</summary>
        public static string StressLabel => $"{ForceLabel}/{LengthLabel}\u00B2";

        /// <summary>Mass / Length³  (e.g. kg/m³)</summary>
        public static string DensityLabel => $"{MassLabel}/{LengthLabel}\u00B3";

        /// <summary>Force × Length  (e.g. kN·m)</summary>
        public static string MomentLabel => $"{ForceLabel}\u00B7{LengthLabel}";

        /// <summary>Force / Length  (e.g. N/m)</summary>
        public static string SpringStiffnessLabel => $"{ForceLabel}/{LengthLabel}";

        /// <summary>Length / Time²  (e.g. m/s²)</summary>
        public static string AccelerationLabel => $"{LengthLabel}/{TimeLabel}\u00B2";

        /// <summary>Length²  (e.g. m²)</summary>
        public static string AreaLabel => $"{LengthLabel}\u00B2";

        /// <summary>Length⁴  (e.g. m⁴)</summary>
        public static string MomentOfInertiaLabel => $"{LengthLabel}\u2074";

        // ── Convenience bracketed versions for component descriptions ─

        public static string Length_  => $"[{LengthLabel}]";
        public static string Force_   => $"[{ForceLabel}]";
        public static string Mass_    => $"[{MassLabel}]";
        public static string Stress_  => $"[{StressLabel}]";
        public static string Density_ => $"[{DensityLabel}]";
        public static string Moment_  => $"[{MomentLabel}]";
        public static string SpringK_ => $"[{SpringStiffnessLabel}]";
        public static string Accel_   => $"[{AccelerationLabel}]";
        public static string Area_    => $"[{AreaLabel}]";
        public static string Inertia_ => $"[{MomentOfInertiaLabel}]";
        public static string Angle_   => $"[{AngleLabel}]";
        public static string Temp_    => $"[{TemperatureLabel}]";
    }
}
