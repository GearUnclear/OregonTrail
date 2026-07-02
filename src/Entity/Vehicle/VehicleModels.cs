// Created for the 2028 Asphalt Trail re-skin — vehicle selection.

using System;

namespace OregonTrailDotNet.Entity.Vehicle
{
    /// <summary>
    ///     Static catalog of the four vehicles a player may choose to make the drive to Seattle in. The Minivan is the
    ///     numeric baseline (SpeedMultiplier 1.0, FuelEfficiencyMultiplier 1.0) — picking it reproduces the pre-choice
    ///     default behavior exactly.
    /// </summary>
    public static class VehicleModels
    {
        /// <summary>
        ///     The default vehicle used whenever nothing else has been selected yet. The Minivan.
        /// </summary>
        public static VehicleModel Default => Get(VehicleChoice.Minivan);

        /// <summary>
        ///     Looks up the tuning numbers for a given <see cref="VehicleChoice" />.
        /// </summary>
        /// <param name="choice">Which vehicle the player picked.</param>
        /// <returns>The <see cref="VehicleModel" /> describing that vehicle.</returns>
        public static VehicleModel Get(VehicleChoice choice)
        {
            switch (choice)
            {
                case VehicleChoice.Minivan:
                    return new VehicleModel(
                        VehicleChoice.Minivan,
                        "Beige Minivan",
                        "Balanced, boring, and reliable. The family hauler nobody dreams about but everybody survives in.",
                        1200f,
                        1.0d,
                        1.0d,
                        300,
                        4);
                case VehicleChoice.PickupCamper:
                    return new VehicleModel(
                        VehicleChoice.PickupCamper,
                        "Lifted Pickup with Camper Shell",
                        "Hauls everything you own and then some, but it is slow and thirsty at the pump the whole way there.",
                        6500f,
                        0.9d,
                        0.7d,
                        500,
                        4);
                case VehicleChoice.HybridCrossover:
                    return new VehicleModel(
                        VehicleChoice.HybridCrossover,
                        "Hybrid Crossover SUV",
                        "Quick and fuel-sipping, but the trunk and the back seat are both smaller than the brochure photos suggested.",
                        4500f,
                        1.1d,
                        1.4d,
                        200,
                        3);
                case VehicleChoice.ElectricHatchback:
                    return new VehicleModel(
                        VehicleChoice.ElectricHatchback,
                        "Secondhand EV Hatchback",
                        "Fastest and most fuel-efficient of the four thanks to instant torque, but it has the least room for people " +
                        "or cargo, and still keeps a gas generator riding along in back for when the 2028 EV charging network " +
                        "between Florida and Seattle fails to materialize.",
                        5000f,
                        1.2d,
                        1.6d,
                        150,
                        3);
                default:
                    throw new ArgumentOutOfRangeException(nameof(choice), choice, null);
            }
        }
    }
}
