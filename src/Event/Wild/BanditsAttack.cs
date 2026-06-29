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
    ///     Deals with a random event that involves a Highway Patrol civil-asset-forfeiture stop. The K-9 "alerts," the cash
    ///     is declared the suspect, and the money is seized without charges. If you resist, the encounter can turn fatal.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class BanditsAttack : ItemDestroyer
    {
        /// <summary>Fired by the item destroyer event prefab before items are destroyed.</summary>
        /// <param name="destroyedItems">Items that were destroyed from the players inventory.</param>
        /// <returns>The <see cref="string" />.</returns>
        protected override string OnPostDestroyItems(IDictionary<Entities, int> destroyedItems)
        {
            // Ammo spent resisting the stop is randomly generated.
            GameSimulationApp.Instance.Vehicle.Inventory[Entities.Ammo].ReduceQuantity(
                GameSimulationApp.Instance.Random.Next(3, 15));

            // Change event text depending on if items were seized or not.
            return destroyedItems.Count > 0
                ? TryKillPassengers("shot")
                : "no cash seized. They ran your plates, found nothing, and waved you on.";
        }

        /// <summary>
        ///     Fired by the item destroyer event prefab after items are destroyed.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnPreDestroyItems()
        {
            var firePrompt = new StringBuilder();
            firePrompt.Clear();
            firePrompt.AppendLine("Highway Patrol stop. The K-9 'alerts.'");
            firePrompt.Append("The money was the suspect, resulting in ");
            return firePrompt.ToString();
        }
    }
}