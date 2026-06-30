// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Window.Travel.Hunt
{
    /// <summary>
    ///     Defines all the grab words that are used to determine how quickly the player responded while sweeping the food
    ///     tables. Used to determine if they snagged the tray before another shopper did. The selector in
    ///     <see cref="HuntManager" /> rolls across every value via Enum.GetValues, so adding members here (kept contiguous
    ///     from None=0) simply widens the variety of words the player has to type, which keeps the food sweep fresh.
    /// </summary>
    public enum HuntWord
    {
        None = 0,
        Grab = 1,
        Swipe = 2,
        Snag = 3,
        Stack = 4,
        Nab = 5,
        Yoink = 6,
        Clutch = 7,
        Scoop = 8,
        Bag = 9,
        Hustle = 10,
        Hoard = 11,
        Pile = 12
    }
}
