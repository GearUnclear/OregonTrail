using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Item;
using OregonTrailDotNet.Entity.Vehicle;

internal static class TradeCapacityTests
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    public static void Run()
    {
        var vehicle = new Vehicle();
        var gas = vehicle.Inventory[Entities.Animal];
        gas.AddQuantity(19);
        Check(vehicle.CanReceiveItem(new SimItem(gas, 1)), "exact fit should be allowed");
        Check(!vehicle.CanReceiveItem(new SimItem(gas, 3)), "overflow must be refused");
        Check(gas.Quantity == 19, "validation must not mutate stock");
        gas.AddQuantity(1);
        Check(!vehicle.CanReceiveItem(new SimItem(gas, 1)), "full stock must refuse reward");
        Check(!vehicle.CanReceiveItem(null), "missing reward must be refused");
        Console.WriteLine("PASS TradeCapacityTests");
    }
}
