using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Item;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.Window.Travel.Trade;

internal static class TradeCopyTests
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    public static void Run()
    {
        // The console trade form and the browser presentation both read these strings from TradeCopy.
        Check(TradeCopy.AcceptPrompt == "Deal?", "accept prompt wording");

        var vehicle = new Vehicle();
        var ammo = vehicle.Inventory[Entities.Ammo];
        var gas = vehicle.Inventory[Entities.Animal];
        var food = vehicle.Inventory[Entities.Food];
        ammo.AddQuantity(2);
        gas.AddQuantity(gas.MaxQuantity - gas.Quantity);
        var ammoName = ammo.Name.ToLowerInvariant();

        // Short on payment and no room for the reward: the shortfall is reported first.
        var shortfall = TradeCopy.Refusal(vehicle, new SimItem(ammo, 5), new SimItem(gas, 1));
        Check(shortfall == $"You only have 2 {ammoName}, so you can't cover this trade.",
            $"shortfall wording: {shortfall}");

        // Enough to pay, but the reward does not fit.
        var noRoom = TradeCopy.Refusal(vehicle, new SimItem(ammo, 2), new SimItem(gas, 1));
        Check(noRoom == "You don't have room for all of it, so there's no deal. Nothing changed hands.",
            $"capacity wording: {noRoom}");

        // Enough to pay and room for the reward, yet the trade is still refused.
        var generic = TradeCopy.Refusal(vehicle, new SimItem(ammo, 2), new SimItem(food, 1));
        Check(generic == "You don't have the supplies for this trade.", $"generic wording: {generic}");

        // The pitch is one unwrapped sentence pair; each surface applies its own wrapping.
        var pitch = TradeCopy.Pitch(new SimItem(food, 1250), new SimItem(ammo, 3));
        Check(pitch == $"A hun from the parking-lot MLM wants 1,250 {food.Name.ToLowerInvariant()}. " +
                       $"She'll swap you 3 {ammoName}, hon. Ground floor, only goes up from here.",
            $"pitch wording: {pitch}");
        Check(!pitch.Contains('\n') && !pitch.Contains('—'), "pitch must be unwrapped plain punctuation");
        Check(TradeCopy.NoTraders == "Nobody's working their downline here right now.", "no-trader wording");

        // Quantities keep the thousands separator both surfaces used before.
        food.AddQuantity(1500);
        var large = TradeCopy.Refusal(vehicle, new SimItem(food, 1800), new SimItem(ammo, 1));
        Check(large == $"You only have 1,500 {food.Name.ToLowerInvariant()}, so you can't cover this trade.",
            $"shortfall uses N0: {large}");

        Check(ammo.Quantity == 2 && gas.Quantity == gas.MaxQuantity && food.Quantity == 1500,
            "refusal copy must not mutate stock");
        Console.WriteLine("PASS TradeCopyTests");
    }
}
