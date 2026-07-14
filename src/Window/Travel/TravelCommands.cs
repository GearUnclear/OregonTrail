// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using WolfCurses.Utility;

namespace OregonTrailDotNet.Window.Travel
{
    /// <summary>
    ///     All of the commands associated with the traveling game Windows which is one of the primary simulation game modes
    ///     since it is at the bottom of the game modes stack and all others will be stacking on top of it.
    /// </summary>
    public enum TravelCommands
    {
        /// <summary>
        ///     If the simulation is paused and the player is on the traveling screen this will restart it back into whatever pace
        ///     they had it set to previously.
        /// </summary>
        [Description("Keep driving")] ContinueOnTrail = 1,

        /// <summary>
        ///     "Status" tells you the medical conditions of everyone in your party, as well as your current inventory and
        ///     Occupation.
        /// </summary>
        [Description("Check supplies")] CheckSupplies = 2,

        /// <summary>
        ///     "Map" shows you your current progress across the country, as well as some major landmarks, and your beat-up
        ///     SUV crawling to nowhere in particular.
        /// </summary>
        [Description("Look at map")] LookAtMap = 3,

        /// <summary>
        ///     There are three settings for Pace
        /// </summary>
        [Description("Change pace")] ChangePace = 4,

        /// <summary>
        ///     "Rations" is where you can set how much your party eats
        /// </summary>
        [Description("Change food rations")] ChangeFoodRations = 5,

        /// <summary>
        ///     Resting often improves/restores the health of a sick party member.  Resting is helpful, but if you do it too much,
        ///     you'll find yourself traveling through tough winter weather in the end of the game.
        /// </summary>
        [Description("Pull over to rest")] StopToRest = 6,

        /// <summary>
        ///     "Trade" is a very useful feature.  You can often get items you need for cheap. Simply enter the item you wish to
        ///     trade for, and the number of them, and someone will offer you a trade.  If you don't like the trade, you can
        ///     "Haggle" with them in an attempt to get a better deal.  The more haggling you do with a person, the more their
        ///     prices will slowly be driven up.  If you haggle too high, simply exit the trade screen, continue on the trail (and
        ///     distance, as long as you've moved), then attempt to trade again.
        /// </summary>
        [Description("Attempt to trade")] AttemptToTrade = 7,

        /// <summary>
        ///     Some locations along the road offer up the chance to grab food off the fair midway or the door-buster trays,
        ///     other situations require only grabbing and don't reward with snacks only getting yourself out of the crush.
        /// </summary>
        [Description("Grab some food")] HuntForFood = 8,

        /// <summary>
        ///     Using "Talk," you can talk to fellow travelers to further the story or for advice.  If you don't know what to do,
        ///     talk to someone or consult the Guide.
        /// </summary>
        [Description("Talk to people")] TalkToPeople = 9,

        /// <summary>
        ///     You can only buy items at Buc-ee's and big-box stops along the road.  If you're at one, pick "Buy" to see what is
        ///     in stock.  Prices increase the farther along the road you go.
        /// </summary>
        [Description("Buy supplies")] BuySupplies = 10,

        /// <summary>
        ///     Pick up a day of DoorDash delivery gigs in town for some quick cash -- at the cost of your own fuel and tires.
        /// </summary>
        [Description("Drive for DoorDash")] DriveForDoorDash = 11
    }
}