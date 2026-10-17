using OregonTrailDotNet.Entity.Item;
using OregonTrailDotNet.Entity.Vehicle;

namespace OregonTrailDotNet.Window.Travel.Trade
{
    /// <summary>
    ///     Trade copy shared by the console trade form and the browser presentation, so a wording change happens in one
    ///     place.
    /// </summary>
    public static class TradeCopy
    {
        /// <summary>Shown when no trader is around at this stop.</summary>
        public const string NoTraders = "Nobody's working their downline here right now.";

        /// <summary>
        ///     The trader's pitch for one offer, as unwrapped prose. The console word-wraps it; the browser shows it as is.
        /// </summary>
        public static string Pitch(SimItem wanted, SimItem offered)
        {
            var wantedName = wanted.Name.ToLowerInvariant();
            var offeredName = offered.Name.ToLowerInvariant();
            return $"A hun from the parking-lot MLM wants {wanted.Quantity:N0} {wantedName}. " +
                   $"She'll swap you {offered.Quantity:N0} {offeredName}, hon. " +
                   "Ground floor, only goes up from here.";
        }

        /// <summary>Asked when the family can pay for the offer and has room for the reward.</summary>
        public const string AcceptPrompt = "Deal?";

        /// <summary>Shown when the family has enough of the wanted item but the reward will not fit.</summary>
        public const string NoRoom = "You don't have room for all of it, so there's no deal. Nothing changed hands.";

        /// <summary>Shown when the offer can't be taken for any other reason.</summary>
        public const string NoSupplies = "You don't have the supplies for this trade.";

        /// <summary>Shown when the family holds less of the wanted item than the trader asks for.</summary>
        public static string Shortfall(int held, SimItem wanted)
        {
            var name = wanted.Name.ToLowerInvariant();
            return $"You only have {held:N0} {name}, so you can't cover this trade.";
        }

        /// <summary>
        ///     Explains why an offer can't be accepted. A payment shortfall is reported first, then a capacity problem, then
        ///     the generic refusal.
        /// </summary>
        public static string Refusal(Vehicle vehicle, SimItem wanted, SimItem offered)
        {
            var held = vehicle?.Inventory != null && vehicle.Inventory.TryGetValue(wanted.Category, out var stock)
                ? stock.Quantity
                : 0;
            if (held < wanted.Quantity)
                return Shortfall(held, wanted);

            return vehicle?.CanReceiveItem(offered) == true ? NoSupplies : NoRoom;
        }
    }
}
