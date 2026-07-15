// The Asphalt Trail (2028 re-skin) -- SUPPLY_DRAIN (cash).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: manufacturing a spectacle of your fertility with a pyrotechnic device in a drought. Grounded in
    ///     the 2020 El Dorado Fire -- a gender-reveal smoke bomb that burned 22,000 acres, killed a firefighter, and
    ///     led to felony charges and restitution -- and the broader run of reveal-party wildfires, explosions, and
    ///     wrongful-death suits.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class BlueSmokeRedSky : ModernSupplyDrain
    {
        /// <summary>Which inventory category this drain empties.</summary>
        protected override Entities DrainCategory => Entities.Cash;

        /// <summary>Inclusive lower bound of the amount drained.</summary>
        protected override int MinAmount => 80;

        /// <summary>Exclusive upper bound of the amount drained.</summary>
        protected override int MaxAmount => 280;

        /// <summary>Describes the indignity, given how much was actually taken.</summary>
        protected override string OnDrain(int amountLost)
        {
            return "A cousin's roadside gender-reveal insists on the blue smoke cannon\n" +
                   "despite the burn ban and the wind, and the dry grass at the\n" +
                   "trailhead takes it personally. The fire is small, the citations are\n" +
                   "not, and as the nearest adults with a vehicle registration you are\n" +
                   $"named in the paperwork. ${amountLost} goes to the county for\n" +
                   "restitution. It's a boy.";
        }
    }
}
