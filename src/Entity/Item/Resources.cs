// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Entity.Item
{
    /// <summary>
    ///     Defines items which are used by the vehicle party members, typically consuming them everyday.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        ///     Crates of MLM leggings the party hauls around; they double as the clothing that keeps party members warm when it is
        ///     cold outside from the climate simulation, and as the only barter the river guide accepts.
        /// </summary>
        // Price: ~$75 a crate — a case of fast-fashion athleisure leggings at 2028 retail.
        public static SimItem Clothing => new SimItem(Entities.Clothes, "MLM Leggings", "crates", "crate", 50, 75, 1, 1, 0, 2);

        /// <summary>
        ///     Boxes of ammo used in the food-sweep minigame so the players can grab snacks by clearing out the fair food.
        /// </summary>
        // Price: ~$30 a box — a box of range ammo at 2028 prices (buy as few as 1 box).
        public static SimItem Bullets => new SimItem(Entities.Ammo, "Ammunition", "boxes", "box", 99, 30, 0, 1, 0, 1, 50);

        /// <summary>
        ///     Serves as a generic reference item that represents a given amount of snacks. This could be from any food sweep or
        ///     known fair-food resource marked as such.
        /// </summary>
        // Price: $1.50 a pound — bulk gas-station/road-trip snack food at 2028 prices.
        public static SimItem Food => new SimItem(Entities.Food, "Snacks", "pounds", "pound", 2000, 1.50f, 1, 1, 0, 1, 25);

        /// <summary>
        ///     Represents the SUV entity, this is not used as the actual vehicle the people travel in but rather a reference to a
        ///     vehicle in the collection of simulation entities.
        /// </summary>
        public static SimItem Vehicle => new SimItem(Entities.Vehicle, "SUV", "SUVs", "SUV", 2000, 50, 500, 1, 0, 50);

        /// <summary>
        ///     Represents a person entity, this is not used as actual person but rather a reference to a person object in the
        ///     collection of vehicle entities.
        /// </summary>
        public static SimItem Person => new SimItem(Entities.Person, "Person", "people", "person", 2000, 0, 1, 1, 0, 800);

        /// <summary>
        ///     Represents monies the player can spend, rather than just binding some integer to a property it makes more sense to
        ///     tabulate and treat it like an item like anything else in the simulation.
        /// </summary>
        public static SimItem Cash => new SimItem(Entities.Cash, "Cash", "dollars", "dollar", int.MaxValue, 1, 0, 1, 0, 1, 5);
    }
}