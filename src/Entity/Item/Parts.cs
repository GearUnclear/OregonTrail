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
        ///     Zero weight five-gallon gas cans that ride along with the SUV but are not actually a part of it, yet are still in the
        ///     list of inventory items that define the vehicle the player and his party is making the journey in.
        /// </summary>
        public static SimItem Oxen => new SimItem(Entities.Animal, "Gas Cans", "gas cans", "gas can", 20, 20, 0, 1, 0, 4);

        /// <summary>
        ///     Required to keep the SUV charged; if the alternator dies it must be replaced before the player can
        ///     continue their journey.
        /// </summary>
        public static SimItem Axle => new SimItem(Entities.Axle, "Alternator", "alternators", "alternator", 3, 10, 0, 1, 0, 2);

        /// <summary>
        ///     Required to keep the SUV running, if the transmission goes then the player will have to fix or replace it before
        ///     they can continue on the journey again.
        /// </summary>
        public static SimItem Tongue => new SimItem(Entities.Tongue, "Transmission", "transmissions", "transmission", 3, 10, 0, 1, 0, 2);

        /// <summary>
        ///     Required to keep the SUV rolling down the highway, if any of the tires blow they must be replaced before the
        ///     journey can continue.
        /// </summary>
        public static SimItem Wheel => new SimItem(Entities.Wheel, "Tire", "tires", "tire", 3, 10, 0, 1, 0, 2);
    }
}