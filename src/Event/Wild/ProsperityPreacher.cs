// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A prosperity-gospel preacher needs a third private jet, because commercial flights, he explains, are full of
    ///     demons. You sow a seed and get nothing back; the larder is lighter for it.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class ProsperityPreacher : FoodDestroyer
    {
        /// <summary>
        ///     Fired by the food spoiler event prefab allowing implementations to explain the reason why the food went bad and or
        ///     was destroyed.
        /// </summary>
        /// <returns>Reason why the food was destroyed and or went bad.</returns>
        protected override string OnFoodSpoilReason()
        {
            return "A prosperity preacher needs a third jet, because commercial flights are full of demons. You sow a seed and get nothing back.";
        }
    }
}
