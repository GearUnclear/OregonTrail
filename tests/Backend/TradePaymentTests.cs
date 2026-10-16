using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Item;
using OregonTrailDotNet.Entity.Vehicle;

internal static class TradePaymentTests
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    public static void Run()
    {
        var vehicle = new Vehicle();
        var ammo = vehicle.Inventory[Entities.Ammo];
        ammo.AddQuantity(1);
        Check(!vehicle.ContainsItem(new SimItem(ammo, 5)), "one ammo cannot pay five");
        Check(vehicle.ContainsItem(new SimItem(ammo, 1)), "exact payment should be allowed");
        ammo.AddQuantity(4);
        Check(vehicle.ContainsItem(new SimItem(ammo, 5)), "five ammo can pay five");
        ammo.ReduceQuantity(5);
        Check(!vehicle.ContainsItem(new SimItem(ammo, 1)), "zero cannot pay one");
        Check(!vehicle.ContainsItem(null), "missing request must be refused");
        Console.WriteLine("PASS TradePaymentTests");
    }
}
