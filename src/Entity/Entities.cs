// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using WolfCurses.Utility;

namespace OregonTrailDotNet.Entity
{
    /// <summary>
    ///     Defines all the possible types of items, used for sorting and quickly being able to determine type when iterating
    ///     over them in a list.
    /// </summary>
    public enum Entities
    {
        /// <summary>
        ///     Represents how much monies the player decided to spend on five-gallon gas cans when purchasing initial items for the
        ///     journey down the interstate. The purpose for this is to offer up a clear distinction between a part of the SUV and
        ///     something that is keeping it moving. In a manner of speaking the gas is the fuel in the vehicle; if it drains
        ///     completely the SUV will be considered disabled and can no longer roll down the road.
        /// </summary>
        [Description("Gas cans          @AMT@")] Animal = 1,

        /// <summary>
        ///     Snacks from food sweeps or stores. Represented in pounds. Can typically take back only 250 pounds to the SUV from a
        ///     grab. Consumed by the party members at the end of each day of the simulation. Depending on ration level the amount
        ///     of snacks in pounds eaten each day can vary.
        /// </summary>
        [Description("Snacks            @AMT@")] Food = 2,

        /// <summary>
        ///     MLM leggings are hoarded by the party in unsellable crates; the only barter the sovereign-citizen river guide will
        ///     accept. They double as the clothing slot, so running out still raises the risk of exposure on cold nights.
        /// </summary>
        [Description("MLM leggings      @AMT@")] Clothes = 3,

        /// <summary>
        ///     Boxes of ammo bought off the shelf next to the flour, no license or check required; can also be traded with other
        ///     travelers on the road.
        /// </summary>
        [Description("Boxes of ammo     @AMT@")] Ammo = 4,

        /// <summary>
        ///     Tire on the SUV that must be kept track of, if it blows the user will have to use a spare to replace it.
        /// </summary>
        [Description("Spare tires       @AMT@")] Wheel = 5,

        /// <summary>
        ///     Alternator that keeps the SUV charged, if this part dies it must be replaced or the total possible mileage for the
        ///     current block the simulation is running is lost.
        /// </summary>
        [Description("Alternators       @AMT@")] Axle = 6,

        /// <summary>
        ///     Transmission for the SUV which delivers power to the wheels.
        /// </summary>
        [Description("Transmissions     @AMT@")] Tongue = 7,

        /// <summary>
        ///     Defines the vessel in which the party members, their inventory, monies, hopes and dreams, and everything else
        ///     resides. The purpose of this enum value is so we can treat the entity properly and give it a type.
        /// </summary>
        Vehicle = 8,

        /// <summary>
        ///     Represents a given occupant in the vehicle, this is used mostly to separate the player entities from vehicle and
        ///     ensure the game never confuses them for being items.
        /// </summary>
        Person = 9,

        /// <summary>
        ///     Represents paper currency which can be exchanged for goods at store. The game makes no attempt at money delineation
        ///     outside of quantity of single dollars.
        /// </summary>
        Cash = 10,

        /// <summary>
        ///     Location on the trail the player can visit with their vehicle and purchase things, or a river crossing, or a toll
        ///     road, etc.
        /// </summary>
        Location = 11
    }
}