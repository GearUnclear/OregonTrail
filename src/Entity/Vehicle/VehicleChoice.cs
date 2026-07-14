// Created for the 2028 Asphalt Trail re-skin -- vehicle selection.

using WolfCurses.Utility;

namespace OregonTrailDotNet.Entity.Vehicle
{
    /// <summary>
    ///     The vehicle the party will be making the drive to Seattle in. Selected once at the start of the game and
    ///     locked in for its duration -- it determines travel speed, fuel efficiency, cargo capacity, and the maximum
    ///     number of people who can be crammed inside.
    /// </summary>
    public enum VehicleChoice
    {
        /// <summary>
        ///     The beige minivan. Numeric baseline for every other vehicle.
        /// </summary>
        [Description("Drive the beige minivan")] Minivan = 1,

        /// <summary>
        ///     The lifted pickup with a camper shell bolted to the bed.
        /// </summary>
        [Description("Drive the lifted pickup with a camper shell")] PickupCamper = 2,

        /// <summary>
        ///     The hybrid crossover SUV.
        /// </summary>
        [Description("Drive the hybrid crossover SUV")] HybridCrossover = 3,

        /// <summary>
        ///     The secondhand EV hatchback.
        /// </summary>
        [Description("Drive the secondhand EV hatchback")] ElectricHatchback = 4
    }
}
