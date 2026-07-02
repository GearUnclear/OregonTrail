// Created for the 2028 Asphalt Trail re-skin — dynamic fuel pricing.

using System;

namespace OregonTrailDotNet.Entity.Item
{
    /// <summary>
    ///     Computes the location-scaled price of gas cans. Gas is normal at the start of the journey, cheapest in the middle,
    ///     and most expensive at the end — turning "buy fuel" into a timing decision. This affects the price the player PAYS at
    ///     the store only; the gas cans that land in the vehicle inventory keep <see cref="Parts.GasBaseCost" /> so the daily
    ///     mileage formula (calibrated to that figure) is untouched.
    /// </summary>
    public static class FuelPricing
    {
        /// <summary>
        ///     Price multiplier at the very start of the trip and again at the mid-point trough. Start is baseline (1.0), the
        ///     middle dips to <see cref="LowMult" />.
        /// </summary>
        private const float LowMult = 0.7f;

        /// <summary>
        ///     Price multiplier at the very end of the trip — the most expensive fuel of the whole journey.
        /// </summary>
        private const float HighMult = 2.0f;

        /// <summary>
        ///     Fraction of the journey completed, measured by settlement index so the price steps once per stop and is
        ///     deterministic. 0.0 at the first location, 1.0 at the last.
        /// </summary>
        private static float Progress
        {
            get
            {
                var trail = GameSimulationApp.Instance?.Trail;
                if (trail == null)
                    return 0f;

                var lastIndex = trail.Locations.Count - 1;
                if (lastIndex <= 0)
                    return 0f;

                var p = trail.LocationIndex / (float) lastIndex;
                return p < 0f ? 0f : (p > 1f ? 1f : p);
            }
        }

        /// <summary>
        ///     Piecewise price multiplier: 1.0x at the start, falling to <see cref="LowMult" /> at the mid-point, then rising to
        ///     <see cref="HighMult" /> at the end.
        /// </summary>
        private static float Multiplier()
        {
            var p = Progress;
            if (p <= 0.5f)
                return 1.0f + (LowMult - 1.0f) * (p / 0.5f);

            return LowMult + (HighMult - LowMult) * ((p - 0.5f) / 0.5f);
        }

        /// <summary>
        ///     Current per-can price of gas at the player's location, base cost scaled by the journey price curve.
        /// </summary>
        public static float CurrentCost()
        {
            return (float) Math.Round(Parts.GasBaseCost * Multiplier(), 2);
        }

        /// <summary>
        ///     Short human tag describing how the local fuel price compares to baseline, for store UI.
        /// </summary>
        public static string Trend()
        {
            var m = Multiplier();
            if (m <= 0.9f)
                return "(cheap here)";
            if (m >= 1.25f)
                return "(pricey here)";
            return "(about normal)";
        }
    }
}
