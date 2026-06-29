// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A streamer pushing a stroller 3,500 miles across the country eating only Subway flags you down. The crowd that
    ///     follows him built him a solar stroller, and he shares whatever he is carrying with the party.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class SubwaySeanWalker : ItemCreator
    {
        /// <summary>Fired by the event prefab after the event has executed.</summary>
        /// <param name="createdItems"></param>
        /// <returns>The <see cref="string" />.</returns>
        protected override string OnPostCreateItems(IDictionary<Entities, int> createdItems)
        {
            return createdItems.Count > 0 ? $"and he shares:{Environment.NewLine}" : "but he is out of sandwiches today";
        }

        /// <summary>
        ///     Fired by the event prefab before the event has executed.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnPreCreateItems()
        {
            var eventText = new StringBuilder();
            eventText.AppendLine("A streamer pushing a stroller 3,500 miles on Subway alone");
            eventText.AppendLine("waves you over to his new solar stroller,");
            return eventText.ToString();
        }
    }
}
