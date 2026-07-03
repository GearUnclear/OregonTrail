// Created for the 2028 Asphalt Trail re-skin — vehicle selection.

namespace OregonTrailDotNet.Entity.Vehicle
{
    /// <summary>
    ///     Plain data holder describing the tuning numbers for one of the four vehicles the player can choose to make
    ///     the drive in. Instances are handed out by <see cref="VehicleModels" /> and never mutated afterward.
    /// </summary>
    public sealed class VehicleModel
    {
        /// <summary>Initializes a new instance of the <see cref="VehicleModel" /> class.</summary>
        /// <param name="choice">Which <see cref="VehicleChoice" /> this model represents.</param>
        /// <param name="name">Display name of the vehicle.</param>
        /// <param name="flavorText">Deadpan satirical description of the vehicle.</param>
        /// <param name="cost">Price subtracted from the party's starting cash.</param>
        /// <param name="speedMultiplier">Multiplier applied to daily mileage.</param>
        /// <param name="fuelEfficiencyMultiplier">Multiplier applied to the gas-derived portion of daily mileage.</param>
        /// <param name="cargoCapacity">Maximum pounds of cargo the vehicle can carry.</param>
        /// <param name="maxPartySize">Maximum number of passengers the vehicle can seat.</param>
        public VehicleModel(
            VehicleChoice choice,
            string name,
            string flavorText,
            float cost,
            double speedMultiplier,
            double fuelEfficiencyMultiplier,
            int cargoCapacity,
            int maxPartySize)
        {
            Choice = choice;
            Name = name;
            FlavorText = flavorText;
            Cost = cost;
            SpeedMultiplier = speedMultiplier;
            FuelEfficiencyMultiplier = fuelEfficiencyMultiplier;
            CargoCapacity = cargoCapacity;
            MaxPartySize = maxPartySize;
        }

        /// <summary>
        ///     Which <see cref="VehicleChoice" /> this model represents.
        /// </summary>
        public VehicleChoice Choice { get; }

        /// <summary>
        ///     Display name of the vehicle.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Deadpan satirical description of the vehicle.
        /// </summary>
        public string FlavorText { get; }

        /// <summary>
        ///     Price subtracted from the party's starting cash when this vehicle is chosen.
        /// </summary>
        public float Cost { get; }

        /// <summary>
        ///     Multiplier applied to daily mileage. 1.0 is the Minivan baseline.
        /// </summary>
        public double SpeedMultiplier { get; }

        /// <summary>
        ///     Multiplier applied to the gas-derived portion of daily mileage. 1.0 is the Minivan baseline.
        /// </summary>
        public double FuelEfficiencyMultiplier { get; }

        /// <summary>
        ///     Maximum pounds of cargo the vehicle can carry.
        /// </summary>
        public int CargoCapacity { get; }

        /// <summary>
        ///     Maximum number of passengers the vehicle can seat.
        /// </summary>
        public int MaxPartySize { get; }
    }
}
