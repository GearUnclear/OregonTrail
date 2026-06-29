// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A porch pirate / repo agent who comes in the middle of the night and takes things from the vehicle inventory.
    ///     The repo agent will do whatever it takes to recover the collateral, so there is a chance some of your party
    ///     members may get shot.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class Thief : ItemDestroyer
    {
        /// <summary>Fired by the item destroyer event prefab before items are destroyed.</summary>
        /// <param name="destroyedItems">Items that were destroyed from the players inventory.</param>
        /// <returns>The <see cref="string" />.</returns>
        protected override string OnPostDestroyItems(IDictionary<Entities, int> destroyedItems)
        {
            // Ammo spent confronting the repo agent is randomly generated.
            GameSimulationApp.Instance.Vehicle.Inventory[Entities.Ammo].ReduceQuantity(
                GameSimulationApp.Instance.Random.Next(1, 5));

            // Change event text depending on if items were taken or not.
            return destroyedItems.Count > 0
                ? TryKillPassengers("shot")
                : "no loss of items.";
        }

        /// <summary>
        ///     Fired by the item destroyer event prefab after items are destroyed.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnPreDestroyItems()
        {
            var theifPrompt = new StringBuilder();
            theifPrompt.Clear();
            theifPrompt.AppendLine("A porch pirate works the");
            theifPrompt.Append("block at night, resulting in ");
            return theifPrompt.ToString();
        }
    }
}