// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Entity.Item
{
    /// <summary>
    ///     Defines a bunch of predefined fair-food and door-buster items that the player can grab for snacks during a food
    ///     sweep. Weights are the real portion the tray represents, and they are what the sweep is balanced on: only the
    ///     Brisket is heavy enough to threaten the hundred pound haul limit on its own.
    /// </summary>
    public static class Animals
    {
        /// <summary>
        ///     Gets the deep-fried butter tray. A full catering pan, and the second best thing on the table.
        /// </summary>
        public static SimItem Bear => new(Entities.Food, "Deep-Fried Butter Tray", "pounds", "pound", 2000, 0, 20);

        /// <summary>
        ///     You must use *all* the brisket... (the Buc-ee's brisket haul, biggest grab of the day). This is the only
        ///     tray heavy enough on its own to put a dent in the hundred pound haul limit, which is why the crowd wants
        ///     it as badly as you do -- see <see cref="Window.Travel.Hunt.HuntManager.CONTESTEDWEIGHT" />.
        /// </summary>
        public static SimItem Buffalo => new(Entities.Food, "Brisket", "pounds", "pound", 2000, 0,
            GameSimulationApp.Instance.Random.Next(40, 70));

        /// <summary>
        ///     Gets the 72oz steak (the Big Texan challenge platter). Seventy two ounces is four and a half pounds no
        ///     matter how big the sign out front is; the rest of the weight is the sides.
        /// </summary>
        public static SimItem Caribou => new(Entities.Food, "72oz Steak", "pounds", "pound", 2000, 0,
            GameSimulationApp.Instance.Random.Next(5, 9));

        /// <summary>
        ///     Gets the corn dog bucket.
        /// </summary>
        public static SimItem Deer => new(Entities.Food, "Corn Dog Bucket", "pounds", "pound", 2000, 0, 8);

        /// <summary>
        ///     Gets the funnel cake.
        /// </summary>
        public static SimItem Duck => new(Entities.Food, "Funnel Cake", "pounds", "pound", 2000, 0, 2);

        /// <summary>
        ///     Gets the turkey leg.
        /// </summary>
        public static SimItem Goose => new(Entities.Food, "Turkey Leg", "pounds", "pound", 2000, 0, 2);

        /// <summary>
        ///     Gets the nacho boat.
        /// </summary>
        public static SimItem Rabbit => new(Entities.Food, "Nacho Boat", "pounds", "pound", 2000, 0, 3);

        /// <summary>
        ///     Gets the bag of pork rinds. You will fight a stranger for this and win one (1) pound of pork rinds.
        /// </summary>
        public static SimItem Squirrel => new(Entities.Food, "Pork Rinds", "pounds", "pound", 2000, 0, 1);
    }
}