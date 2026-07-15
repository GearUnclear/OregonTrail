// The Asphalt Trail (2028 re-skin) -- SOLO_KILL (one satirical death).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: a slow-motion invasion everyone acknowledged and nobody stopped, left to breed for decades
    ///     because dealing with it was somebody else's line item. Grounded in the 2019 death of a Texas woman killed
    ///     by a sounder of feral hogs in a front yard (cause of death: "exsanguination due to feral hog assault") and
    ///     the ~1.5 million uncontrolled feral hogs spreading across the South and Midwest.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheSounder : ModernSoloDeath
    {
        /// <summary>Describes how this particular passenger met their end.</summary>
        protected override string OnDeath(Entity.Person.Person victim)
        {
            return "You pull off at a shuttered rural station before dawn and send " +
                   $"{victim.Name} across the lot for the restroom key. The forty-hog " +
                   "sounder that has claimed the dumpster does not negotiate; it simply " +
                   "arrives, all at once, at ankle height. They make it eleven feet from " +
                   "the car, which the county will later note is farther than most.";
        }
    }
}
