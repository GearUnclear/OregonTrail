using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Entity.Vehicle;

internal static class FoodTests
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Food: " + message);
    }

    static Vehicle Pantry(int pounds, int people = 4)
    {
        var vehicle = new Vehicle();
        vehicle.Inventory[Entities.Food].AddQuantity(pounds);
        for (var i = 0; i < people; i++)
            vehicle.AddPerson(new Person(Profession.Banker, "Traveler " + i, i == 0));
        return vehicle;
    }

    public static void Run()
    {
        foreach (var (ration, dailyPounds) in new[]
        {
            (RationLevel.Filling, 8m), (RationLevel.Meager, 6m), (RationLevel.BareBones, 4m)
        })
        {
            var vehicle = Pantry(100);
            vehicle.ChangeRations(ration);
            Check(vehicle.FoodPerDay == dailyPounds, "four-person supply forecast matches ration choice");
            for (var day = 0; day < 3; day++)
            {
                for (var meal = 0; meal < 4; meal++) Check(vehicle.TryConsumeMeal(), "stocked meal succeeds");
                Check(vehicle.FoodPoundsRemaining == 100 - (day + 1) * dailyPounds,
                    "multiple days consume the advertised amount without rounding per person");
            }
            vehicle.Passengers[3].Kill();
            Check(vehicle.FoodPerDay == dailyPounds * .75m, "forecast excludes dead passengers");
        }

        var oddParty = Pantry(12, 3);
        oddParty.ChangeRations(RationLevel.Meager);
        for (var i = 0; i < 3; i++) oddParty.TryConsumeMeal();
        Check(oddParty.FoodPerDay == 4.5m && oddParty.FoodPoundsRemaining == 7.5m,
            "odd-size parties keep half-pound portions");
        Check(oddParty.CargoWeight == 8, "opened food still occupies cargo space");
        for (var i = 0; i < 3; i++) oddParty.TryConsumeMeal();
        Check(oddParty.FoodPoundsRemaining == 3m && oddParty.CargoWeight == 3,
            "two odd-party days conserve nine pounds of food");

        var switching = Pantry(10);
        switching.ChangeRations(RationLevel.Meager);
        switching.TryConsumeMeal();
        switching.ChangeRations(RationLevel.Filling);
        switching.TryConsumeMeal();
        switching.ChangeRations(RationLevel.BareBones);
        switching.TryConsumeMeal();
        Check(switching.FoodPoundsRemaining == 5.5m, "changing rations neither discards nor creates half portions");
        switching.ResetVehicle();
        Check(switching.FoodPoundsRemaining == 0m && switching.FoodPerDay == 0m && !switching.TryConsumeMeal(),
            "restarting clears fractional food and the empty party forecast");

        var lastMeal = Pantry(2, 1);
        lastMeal.ChangeRations(RationLevel.Meager);
        Check(lastMeal.TryConsumeMeal() && lastMeal.FoodPoundsRemaining == .5m,
            "half pound remains after opening the last food");
        Check(lastMeal.TryConsumeMeal() && lastMeal.FoodPoundsRemaining == 0m,
            "the last partial meal is available even when whole-pound inventory is empty");
        Check(!lastMeal.TryConsumeMeal() && lastMeal.Inventory[Entities.Food].Quantity == 0,
            "empty pantry causes hunger without negative inventory or free meals");
        Console.WriteLine("PASS food rations, party forecasts, fractional meals, cargo, ration changes, restart, and empty pantry");
    }
}
