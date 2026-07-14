// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using WolfCurses.Utility;

namespace OregonTrailDotNet.Window.Travel.RiverCrossing
{
    /// <summary>
    ///     Determines what kind of river crossing the player would like to perform the time comes to dice roll the probability
    ///     of failure and what will happen.
    /// </summary>
    public enum RiverCrossChoice
    {
        /// <summary>
        ///     Default choice when crossing the river, not shown in the menu but is set to this value by default until user
        ///     changes it to something.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Drives straight into the flooded Interstate without any special precautions, if it is greater than three feet of
        ///     floodwater the SUV will be submerged and highly damaged.
        /// </summary>
        [Description("GUN IT THROUGH THE HIGH WATER (free & fast - deep water can total the car)")] Ford = 1,

        /// <summary>
        ///     Attempts to seal the SUV's doors and take the washed-out shoulder detour to the other side, there is a much higher
        ///     chance for bad things to happen.
        /// </summary>
        [Description("SEAL THE DOORS, TAKE THE WASHED-OUT SHOULDER DETOUR (free, slow, can soak supplies)")] Float = 2,

        /// <summary>
        ///     Prompts to pay for a National Guard high-water convoy that will haul the SUV across the flooded stretch without the
        ///     danger of the family trying it themselves.
        /// </summary>
        [Description("PAY FOR THE NATIONAL GUARD HIGH-WATER CONVOY (costs cash, safe)")] Ferry = 3,

        /// <summary>
        ///     Prompts to pay in crates of MLM leggings (the clothing slot) for a sovereign-citizen local who will guide the SUV
        ///     across, his price changes and goes up the more food you hauled in.
        /// </summary>
        [Description("HIRE A SOVEREIGN-CITIZEN LOCAL (costs leggings, safe)")] Indian = 4,

        /// <summary>
        ///     Waits for a day still ticking events but waiting to see if the floodwater recedes and FEMA reopens the road.
        /// </summary>
        [Description("WAIT FOR THE WATER TO RECEDE / FEMA TO REOPEN (costs a day, the water drops)")] WaitForWeather = 5,

        /// <summary>
        ///     Attached a state on top of the river crossing Windows to explain what the different options mean and how they work.
        /// </summary>
        [Description("ASK A PASSER-BY WHY THE INTERSTATE IS IN THE RIVER")] GetMoreInformation = 6
    }
}