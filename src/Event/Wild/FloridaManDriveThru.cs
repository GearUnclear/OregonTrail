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
    ///     A man at the drive-thru chucks a live alligator he found on the median through the window. Supplies get
    ///     scattered in the scramble and somebody at the wagon may catch a bite. The gator, a passer-by notes, is fine.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class FloridaManDriveThru : ItemDestroyer
    {
        /// <summary>Fired by the item destroyer event prefab before items are destroyed.</summary>
        /// <param name="destroyedItems">Items that were destroyed from the players inventory.</param>
        /// <returns>The <see cref="string" />.</returns>
        protected override string OnPostDestroyItems(IDictionary<Entities, int> destroyedItems)
        {
            // Change event text depending on if items were scattered or not.
            return destroyedItems.Count > 0
                ? TryKillPassengers("bitten")
                : "the gator was fine. Nobody hurt.";
        }

        /// <summary>
        ///     Fired by the item destroyer event prefab after items are destroyed.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnPreDestroyItems()
        {
            var floridaPrompt = new StringBuilder();
            floridaPrompt.Clear();
            floridaPrompt.AppendLine("A fella chucks a live gator he found on the");
            floridaPrompt.Append("median through the drive-thru window, resulting in ");
            return floridaPrompt.ToString();
        }
    }
}
