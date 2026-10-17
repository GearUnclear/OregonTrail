using System.Reflection;
using OregonTrailDotNet;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.Event.Weather;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

/// <summary>
///     Issue #75: MLM leggings are trade goods only. The daily illness roll used to read the dollar value of the
///     leggings in the cargo, so a party with more crates got sick less often. The same seed must now give the same
///     health on every day whether the party carries no leggings or twenty crates, and a hoard of leggings must not
///     spare anyone in the grid-down ice storm (HailStorm), which used to skip its freezing roll at two crates a head.
/// </summary>
internal static class LeggingsExposureTests
{
    private static readonly FieldInfo Status =
        typeof(Person).GetField("_status", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Leggings exposure: " + message);
    }

    public static void Run()
    {
        foreach (var seed in new[] { 75, 7501, 1975 })
            foreach (var ration in new[] { RationLevel.Filling, RationLevel.BareBones })
            {
                var bare = Trajectory(seed, ration, 0);
                var stocked = Trajectory(seed, ration, 20);
                Check(bare.SequenceEqual(stocked),
                    $"seed {seed} on {ration} rations: 0 and 20 crates of leggings gave different health");

                // The roll must still bite, or the comparison above proves nothing.
                Check(bare.Any(health => health < 500), $"seed {seed} on {ration} rations: nobody ever got sick");
            }

        Check(IceStormFreezes(), "twenty crates of leggings kept every passenger alive through ten ice storms");

        Console.WriteLine("PASS leggings quantity never changes illness exposure or ice-storm deaths");
    }

    /// <summary>True if any passenger of a fresh, leggings-hoarding party freezes in one of ten ice storms.</summary>
    private static bool IceStormFreezes()
    {
        GameSimulationApp.Activate(null);
        GameSimulationApp.Create(75);
        try
        {
            var game = GameSimulationApp.Instance;
            game.Restart();
            var factory = new EventFactory();
            for (var storm = 0; storm < 10; storm++)
            {
                var vehicle = game.Vehicle;
                vehicle.ResetVehicle();
                for (var i = 0; i < 4; i++)
                    vehicle.AddPerson(new Person(Profession.Banker, "Traveler " + i, i == 0));
                vehicle.Inventory[Entities.Clothes].AddQuantity(20);
                vehicle.Status = VehicleStatus.Moving;
                factory.CreateInstance(typeof(HailStorm)).Execute(new RandomEventInfo { SourceEntity = vehicle });
                if (vehicle.Passengers.Any(person => person.HealthStatus == HealthStatus.Dead))
                    return true;
            }

            return false;
        }
        finally
        {
            GameSimulationApp.Instance?.Destroy();
            GameSimulationApp.Activate(null);
        }
    }

    /// <summary>Every passenger's hidden health at the end of each of 40 travel days.</summary>
    private static List<int> Trajectory(int seed, RationLevel ration, int crates)
    {
        GameSimulationApp.Activate(null);
        GameSimulationApp.Create(seed);
        try
        {
            var game = GameSimulationApp.Instance;
            game.Restart();
            var vehicle = game.Vehicle;
            for (var i = 0; i < 4; i++)
                vehicle.AddPerson(new Person(Profession.Banker, "Traveler " + i, i == 0));
            vehicle.Inventory[Entities.Food].AddQuantity(1000);
            vehicle.Inventory[Entities.Clothes].AddQuantity(crates);
            vehicle.ChangeRations(ration);
            vehicle.Status = VehicleStatus.Moving;
            Check(vehicle.Inventory[Entities.Clothes].Quantity == crates, $"party carries {crates} crates");

            var health = new List<int>();
            for (var day = 0; day < 40; day++)
                foreach (var person in vehicle.Passengers)
                {
                    person.OnTick(false, false);
                    health.Add((int) Status.GetValue(person)!);
                }

            return health;
        }
        finally
        {
            GameSimulationApp.Instance?.Destroy();
            GameSimulationApp.Activate(null);
        }
    }
}
