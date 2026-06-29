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
    ///     A modified diesel truck "rolls coal," dumping black smoke on command as it passes. The driver paid extra for
    ///     the feature and times it for the people on the shoulder. Some supplies get fouled and the party chokes on it.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class RollingCoal : ItemDestroyer
    {
        /// <summary>Fired by the item destroyer event prefab before items are destroyed.</summary>
        /// <param name="destroyedItems">Items that were destroyed from the players inventory.</param>
        /// <returns>The <see cref="string" />.</returns>
        protected override string OnPostDestroyItems(IDictionary<Entities, int> destroyedItems)
        {
            // Change event text depending on if items were fouled or not.
            return destroyedItems.Count > 0
                ? TryKillPassengers("choked")
                : "just soot on the windshield.";
        }

        /// <summary>
        ///     Fired by the item destroyer event prefab after items are destroyed.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnPreDestroyItems()
        {
            var coalPrompt = new StringBuilder();
            coalPrompt.Clear();
            coalPrompt.AppendLine("A truck paid to dump black smoke on command");
            coalPrompt.Append("rolls coal over the shoulder, resulting in ");
            return coalPrompt.ToString();
        }
    }
}
