// The Asphalt Trail (2028 re-skin) -- SOLO_MAIM (one badly hurt + injured).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: mobility as a venture-funded impulse -- unlock a 30 mph vehicle with a QR code, no helmet, no
    ///     brakes you have tested, terms of service accepted with a thumb. Grounded in the documented surge of e-
    ///     scooter head injuries and fatalities since dockless share programs scaled, most riders helmetless.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheRentalScooter : ModernSoloMaim
    {
        /// <summary>Describes how this particular passenger was maimed.</summary>
        protected override string OnMaim(Entity.Person.Person victim)
        {
            return $"At the charging stop {victim.Name} unlocks a shared e-scooter with a QR\n" +
                   "code to grab dinner two blocks up -- no helmet, because who rents a\n" +
                   "helmet. A pothole the app did not mention stops the front wheel and\n" +
                   "not the rider. The curb wins the introduction to the skull, and\n" +
                   "something in how they talk afterward does not come all the way back.";
        }
    }
}
