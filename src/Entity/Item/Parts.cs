// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Entity.Item
{
    /// <summary>
    ///     Defines a bunch of items that are used as parts in the vehicle.
    /// </summary>
    public static class Parts
    {
        /// <summary>
        ///     Base pump price of a five-gallon gas can before any location price curve. The daily-mileage formula in
        ///     <see cref="OregonTrailDotNet.Entity.Vehicle.Vehicle" /> is calibrated to this exact figure, so the gas cans that
        ///     actually sit in the vehicle inventory MUST be valued at this cost. The station price the player *pays* is scaled
        ///     by <see cref="FuelPricing" /> at the store layer only -- never bake the curve into inventory or the mileage math.
        /// </summary>
        public const float GasBaseCost = 25f;

        /// <summary>
        ///     Zero weight five-gallon gas cans that ride along with the SUV but are not actually a part of it, yet are still in the
        ///     list of inventory items that define the vehicle the player and his party is making the journey in. Use
        ///     <see cref="GasCans" /> with <see cref="FuelPricing.CurrentCost" /> when presenting/charging gas in a store so the
        ///     price curve applies; this default at <see cref="GasBaseCost" /> is what belongs in the vehicle inventory.
        /// </summary>
        public static SimItem Oxen => GasCans(GasBaseCost);

        /// <summary>
        ///     Builds a gas-can SimItem at an arbitrary per-can <paramref name="cost" />. The store uses this to charge the
        ///     location-scaled fuel price while the item added to inventory keeps its <see cref="GasBaseCost" /> value (the
        ///     mileage formula reads inventory cost, so it must stay fixed).
        /// </summary>
        /// <param name="cost">Per-can price to tag this gas can with.</param>
        public static SimItem GasCans(float cost) => new SimItem(Entities.Animal, "Gas Cans", "gas cans", "gas can", 20, cost, 0, 1, 0, 4);

        /// <summary>
        ///     Required to keep the SUV charged; if the alternator dies it must be replaced before the player can
        ///     continue their journey. Price: ~$300, a typical 2028 reman alternator part.
        /// </summary>
        public static SimItem Axle => new SimItem(Entities.Axle, "Alternator", "alternators", "alternator", 3, 300, 0, 1, 0, 2);

        /// <summary>
        ///     Required to keep the SUV running, if the transmission goes then the player will have to fix or replace it before
        ///     they can continue on the journey again. Price: $1,200 -- the single most expensive thing in the store on purpose;
        ///     a blown transmission with no spare is a budget catastrophe, just like real life.
        /// </summary>
        public static SimItem Tongue => new SimItem(Entities.Tongue, "Transmission", "transmissions", "transmission", 3, 1200, 0, 1, 0, 2);

        /// <summary>
        ///     Required to keep the SUV rolling down the highway, if any of the tires blow they must be replaced before the
        ///     journey can continue. Price: ~$200 for a mounted SUV tire in 2028.
        /// </summary>
        public static SimItem Wheel => new SimItem(Entities.Wheel, "Tire", "tires", "tire", 3, 200, 0, 1, 0, 2);
    }
}