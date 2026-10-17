using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Vehicle;

internal static class EventReportTests
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Event report: " + message);
    }

    public static void Run()
    {
        // Loss and gain reports describe inventory by the 2028 item units, never by legacy Entities identifiers.
        var inventory = new Vehicle().Inventory;
        Check(inventory[Entities.Animal].DescribeQuantity(6) == "6 gas cans", "gas cans use their plural unit");
        Check(inventory[Entities.Animal].DescribeQuantity(1) == "1 gas can", "one gas can uses its singular unit");
        Check(inventory[Entities.Wheel].DescribeQuantity(1) == "1 tire", "wheels are reported as tires");
        Check(inventory[Entities.Axle].DescribeQuantity(2) == "2 alternators", "axles are reported as alternators");
        Check(inventory[Entities.Tongue].DescribeQuantity(1) == "1 transmission", "tongues are reported as transmissions");
        Check(inventory[Entities.Food].DescribeQuantity(1200) == "1,200 pounds", "food keeps thousands separators");
        Check(inventory[Entities.Cash].DescribeQuantity(3604) == "$3,604 cash", "cash is reported as money");
        Console.WriteLine("PASS event reports use player-facing inventory units");
    }
}
