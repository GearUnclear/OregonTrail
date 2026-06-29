// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Entity.Item
{
    /// <summary>
    ///     Defines a bunch of predefined fair-food and door-buster items that can be grabbed for snacks using boxes of ammo by
    ///     the player during a food sweep.
    /// </summary>
    public static class Animals
    {
        /// <summary>
        ///     Gets the deep-fried butter tray.
        /// </summary>
        public static SimItem Bear => new(Entities.Food, "Deep-Fried Butter Tray", "pounds", "pound", 2000, 0);

        /// <summary>
        ///     You must use *all* the brisket... (the Buc-ee's brisket haul, biggest grab of the day).
        /// </summary>
        public static SimItem Buffalo => new(Entities.Food, "Brisket", "pounds", "pound", 2000, 0,
            GameSimulationApp.Instance.Random.Next(350, 500));

        /// <summary>
        ///     Gets the 72oz steak (the Big Texan challenge platter).
        /// </summary>
        public static SimItem Caribou => new(Entities.Food, "72oz Steak", "pounds", "pound", 2000, 0,
            GameSimulationApp.Instance.Random.Next(300, 350));

        /// <summary>
        ///     Gets the corn dog bucket.
        /// </summary>
        public static SimItem Deer => new(Entities.Food, "Corn Dog Bucket", "pounds", "pound", 2000, 0, 50);

        /// <summary>
        ///     Gets the funnel cake.
        /// </summary>
        public static SimItem Duck => new(Entities.Food, "Funnel Cake", "pounds", "pound", 2000, 0);

        /// <summary>
        ///     Gets the turkey leg.
        /// </summary>
        public static SimItem Goose => new(Entities.Food, "Turkey Leg", "pounds", "pound", 2000, 0, 2);

        /// <summary>
        ///     Gets the nacho boat.
        /// </summary>
        public static SimItem Rabbit => new(Entities.Food, "Nacho Boat", "pounds", "pound", 2000, 0, 2);

        /// <summary>
        ///     Gets the bag of pork rinds.
        /// </summary>
        public static SimItem Squirrel => new(Entities.Food, "Pork Rinds", "pounds", "pound", 2000, 0);
    }
}