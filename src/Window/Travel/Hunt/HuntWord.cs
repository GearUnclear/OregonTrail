// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Window.Travel.Hunt
{
    /// <summary>
    ///     Defines all the grab words that are used to determine how quickly the player responded while sweeping the food
    ///     tables. Used to determine if they snagged the tray before another shopper did.
    /// </summary>
    public enum HuntWord
    {
        None = 0,
        // ReSharper disable once UnusedMember.Global
        Grab = 1,
        // ReSharper disable once UnusedMember.Global
        Swipe = 2,
        // ReSharper disable once UnusedMember.Global
        Snag = 3,
        // ReSharper disable once UnusedMember.Global
        Stack = 4
    }
}