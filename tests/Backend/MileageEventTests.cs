using System.Reflection;
using OregonTrailDotNet;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.Event.River;
using OregonTrailDotNet.Event.Weather;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

/// <summary>
///     Issue #73: four events ported from the 1978 BASIC multiplied WolfCurses' Random.Next(), an integer in [0, 60), as
///     if it were BASIC's 0-1 RND. Hail set the day's mileage to 5-595 and fog to 10-305; the river events could add
///     over a thousand miles. Each must now take exactly the BASIC loss off the day's mileage.
/// </summary>
internal static class MileageEventTests
{
    private static readonly PropertyInfo Mileage = typeof(Vehicle).GetProperty(nameof(Vehicle.Mileage))!;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Mileage events: " + message);
    }

    public static void Run()
    {
        GameSimulationApp.Activate(null);
        GameSimulationApp.Create(73);
        try
        {
            GameSimulationApp.Instance.Restart();
            var factory = new EventFactory();
            foreach (var (type, least, most) in new[]
            {
                (typeof(HeavyFog), 10, 15),      // TOTAL_MILES=TOTAL_MILES-10-5*RND(0)
                (typeof(HailStorm), 5, 15),      // TOTAL_MILES=TOTAL_MILES-5-RND(0)*10
                (typeof(VehicleWashOut), 20, 40), // TOTAL_MILES=TOTAL_MILES-20-20*RND(0)
                (typeof(VehicleFloods), 20, 40)
            })
            {
                var seen = new HashSet<int>();
                for (var trial = 0; trial < 2000; trial++)
                    seen.Add(Loss(factory, type, 500, VehicleStatus.Moving));
                Check(seen.Min() == least && seen.Max() == most && seen.Count == most - least + 1,
                    $"{type.Name} takes {least}-{most} miles off the day, saw {seen.Min()}-{seen.Max()} ({seen.Count} values)");

                // The loss is clamped at zero and never adds distance, and a parked vehicle keeps its mileage.
                Check(Loss(factory, type, 3, VehicleStatus.Moving) == 3, $"{type.Name} stops at zero miles");
                Check(Loss(factory, type, 500, VehicleStatus.Stopped) == 0, $"{type.Name} leaves a parked vehicle alone");
            }
        }
        finally
        {
            GameSimulationApp.Instance?.Destroy();
            GameSimulationApp.Activate(null);
        }

        Console.WriteLine("PASS fog, hail, and river events take the 1978 mileage losses");
    }

    private static int Loss(EventFactory factory, Type type, int mileage, VehicleStatus status)
    {
        var vehicle = GameSimulationApp.Instance.Vehicle;
        vehicle.Status = status;
        Mileage.SetValue(vehicle, mileage);
        factory.CreateInstance(type).Execute(new RandomEventInfo { SourceEntity = vehicle });
        return mileage - vehicle.Mileage;
    }
}
