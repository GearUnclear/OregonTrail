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
        public static SimItem Clothing => new SimItem(Entities.Clothes, "MLM Leggings", "crates", "crate", 50, 10, 1, 1, 0, 2);

        /// <summary>
        ///     Boxes of ammo used in the food-sweep minigame so the players can grab snacks by clearing out the fair food.
        /// </summary>
        public static SimItem Bullets => new SimItem(Entities.Ammo, "Ammunition", "boxes", "box", 99, 2, 0, 20, 0, 1, 50);

        /// <summary>
        ///     Serves as a generic reference item that represents a given amount of snacks. This could be from any food sweep or
        ///     known fair-food resource marked as such.
        /// </summary>
        public static SimItem Food => new SimItem(Entities.Food, "Snacks", "pounds", "pound", 2000, 0.20f, 1, 1, 0, 1, 25);

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