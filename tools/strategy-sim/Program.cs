using System.Reflection;
using System.Text.Json;
using OregonTrailDotNet;
using OregonTrailDotNet.Web;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Window.Travel;

var options = args.Select((v, i) => (v, i)).Where(x => x.v.StartsWith("--") && x.i + 1 < args.Length)
    .ToDictionary(x => x.v[2..], x => args[x.i + 1]);
int N(string k, int d) => options.TryGetValue(k, out var v) ? int.Parse(v) : d;
var policy = new Policy(N("profession", 1), N("vehicle", 1), N("gas", 20), N("clothes", 4), N("food", 296),
    N("tires", 2), N("alternators", 1), N("transmissions", 1), N("pace", 1), N("ration", 1),
    N("pack", 2), N("bucees", 2), N("caravan", 1), N("arm", 3), N("checkpoint", 2),
    N("crossing", 0), N("month", 1), N("restock", 1));
var trials = N("trials", 1);
for (var i = 0; i < trials; i++)
{
    var result = Runner.Run(policy, N("seed", 1) + i, N("trace", 0) != 0);
    Console.WriteLine(JsonSerializer.Serialize(result));
}

record Policy(int Profession, int Vehicle, int Gas, int Clothes, int Food, int Tires, int Alternators,
    int Transmissions, int Pace, int Ration, int Pack, int Bucees, int Caravan, int Arm, int Checkpoint,
    int Crossing, int Month, int Restock);
record Result(int Seed, string Outcome, int Days, int Living, int Score, string Location, string Error);

static class Runner
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly FieldInfo LastTick = typeof(WolfCurses.SimulationApp).GetField("_lastTickTime", Flags)!;
    static void Pulse(GameSimulationApp game, bool system)
    {
        // Replace elapsed wall time with explicit fixed ticks; preserve the production game-day cadence.
        LastTick.SetValue(game, DateTime.Now);
        game.OnTick(system);
    }
    static void Input(GameSimulationApp game, string value)
    {
        game.InputManager.ClearBuffer();
        foreach (var c in value) game.InputManager.AddCharToInputBuffer(c);
        game.InputManager.SendInputBufferAsCommand();
    }
    public static Result Run(Policy p, int seed, bool trace)
    {
        GameSimulationApp.Activate(null);
        GameSimulationApp.Create();
        var game = GameSimulationApp.Instance;
        game.Random.GetType().GetField("_random", Flags)!.SetValue(game.Random, new Random(seed));
        var stocked = new HashSet<int>();
        var last = "";
        GameSnapshotDto state = null;
        var names = 0;
        var inventoryAtCheckout = new Dictionary<string, int>();
        var shop = false;
        try
        {
            Pulse(game, false);
            for (var step = 0; step < 20000; step++)
            {
                Pulse(game, true);
                var frame = PresentationCatalog.Build(game, step);
                state = frame.Snapshot;
                var form = frame.CurrentForm;
                var name = form?.GetType().Name ?? state.Screen.Id.Split('-').Last();
                if (trace && (name != last || name == "RiverCross"))
                    Console.Error.WriteLine($"{step} {name} {state.Hud?.Date} ${state.Hud?.Balance} {state.Hud?.LocationName} {state.Hud?.Health} food {state.Inventory.FirstOrDefault(x=>x.ItemId=="snacks")?.Quantity} | " +
                        string.Join("; ", state.Screen.Actions.Where(a => a.Enabled).Select(a => a.Label)));
                last = name;
                Result Done(string outcome, string error = null) => new(seed, outcome, (game.Time.Date.Year - 2028) * 360 + ((int)game.Time.Date.Month - (p.Month + 2)) * 30 + game.Time.Date.Day - 1, state.Hud?.LivingPartyCount ?? 0,
                    state.Score?.FinalPoints ?? 0, state.Hud?.LocationName, error);
                if (state.Score != null) return Done("win");
                if (state.Party.Count > 0 && state.Hud.LivingPartyCount == 0) return Done("death");
                if (name == "UnableToContinue") return Done("stranded");
                if (game.TotalTurns > 500) return Done("timeout");

                var actions = state.Screen.Actions.Where(a => a.Enabled).ToList();
                GameActionDto action = null;
                string value = null;
                var index = name switch
                {
                    "ProfessionSelector" => p.Profession - 1,
                    "VehicleSelector" => p.Vehicle - 1,
                    "SelectStartingMonthState" => p.Month - 1,
                    "PackTheCarDecision" => p.Pack - 1,
                    "BuceesHaulDecision" => p.Bucees - 1,
                    "CaravanDecision" => p.Caravan - 1,
                    "ArmYourselfDecision" => p.Arm - 1,
                    "CheckpointDecision" => p.Checkpoint == 2 && game.Choices.GetDecision("caravan") != "join" ? 0 : p.Checkpoint - 1,
                    "ChangePace" => p.Pace - 1,
                    "ChangeRations" => p.Ration - 1,
                    "LocationFork" => 0,
                    _ => -1
                };
                if (index >= 0)
                {
                    action = actions.ElementAtOrDefault(index);
                    if (actions.Count == 1 && actions[0].Label == "Submit")
                    { action = actions[0]; value = (index + 1).ToString(); }
                }
                else if (name == "InputPlayerNames") { action = actions.FirstOrDefault(); value = "Traveler" + ++names; }
                else if (name is "ContinueOnTrail" or "Resting" or "CrossingTick")
                {
                    bool finished = name == "CrossingTick" && (bool)form.GetType().GetProperty("AllowInput", Flags)!.GetValue(form);
                    if (name == "Resting") finished = ((TravelInfo)form.GetType().GetProperty("UserData", Flags)!.GetValue(form)).DaysToRest <= 0;
                    if (finished && actions.Count > 0) action = actions.First();
                    else { Pulse(game, false); continue; }
                }
                else if (state.Store != null)
                {
                    if (!shop)
                    {
                        inventoryAtCheckout = state.Inventory.ToDictionary(x => x.ItemId, x => x.Quantity);
                        shop = true;
                    }
                    var targets = new (string, int)[] { ("gas", p.Gas), ("leggings", p.Clothes),
                        ("snacks", Math.Min(p.Food, state.Hud.CargoCapacity - p.Clothes)),
                        ("tire", p.Tires), ("alternator", p.Alternators), ("transmission", p.Transmissions) };
                    foreach (var (id, target) in targets)
                    {
                        var row = state.Store.Rows.Single(x => x.ItemId == id);
                        var wanted = Math.Max(0, target - inventoryAtCheckout[id]);
                        if (row.Quantity >= wanted) continue;
                        var funds = state.Store.Balance - state.Store.PendingTotal;
                        var add = Math.Min(wanted - row.Quantity, (int)(funds / row.UnitPrice));
                        if (id is "snacks" or "leggings") add = Math.Min(add, state.Store.CargoCapacity - state.Store.CargoWeight);
                        if (add <= 0) continue;
                        action = actions.FirstOrDefault(x => x.ActionId == row.SetActionId);
                        if (action == null) continue;
                        value = (row.Quantity + add).ToString();
                        break;
                    }
                    if (action == null)
                    {
                        action = actions.SingleOrDefault(x => x.ActionId == "store.checkout");
                        stocked.Add(state.Progress.CurrentStopIndex);
                        shop = false;
                    }
                }
                else if (name == "RiverCross")
                {
                    var data = (TravelInfo)form.GetType().GetProperty("UserData", Flags)!.GetValue(form);
                    var river = data.River;
                    if (trace) Console.Error.WriteLine($"Depth {river.RiverDepth}, guide {river.IndianCost}, ferry {river.FerryCost}");
                    string contains;
                    if (p.Crossing == 1) contains = "GUN IT";
                    else if (p.Crossing == 2) contains = "SEAL THE";
                    else if (p.Crossing != 3 && river.FerryCost > 0 && state.Hud.Balance > (decimal)river.FerryCost) contains = "NATIONAL GUARD";
                    else if (river.IndianCost > 0 && state.Inventory.Single(x=>x.ItemId=="leggings").Quantity >= river.IndianCost) contains = "HIRE A";
                    else if (river.RiverDepth > 5) contains = "WAIT FOR";
                    else contains = "SEAL THE";
                    action = actions.FirstOrDefault(x => x.Label.Contains(contains, StringComparison.OrdinalIgnoreCase));
                }
                else if (name == "travel" || name == "TravelMenu")
                {
                    // A form created while an event closes can be published before its first render.
                    if (actions.Count == 1 && actions[0].Label == "Submit") continue;
                    if (state.Hud.Pace != ((OregonTrailDotNet.Entity.Vehicle.TravelPace)p.Pace).ToString())
                        action = actions.FirstOrDefault(x=>x.Label == "Change pace");
                    else if (state.Hud.Rations.Replace(" ", "") != ((OregonTrailDotNet.Entity.Person.RationLevel)p.Ration).ToString())
                        action = actions.FirstOrDefault(x=>x.Label == "Change food rations");
                    else if (p.Restock != 0 && !stocked.Contains(state.Progress.CurrentStopIndex))
                        action = actions.FirstOrDefault(x => x.Label == "Buy supplies");
                    action ??= actions.FirstOrDefault(x => x.Label == "Keep driving" || x.Label == "Get back on the road");
                }
                else action = actions.FirstOrDefault();
                if (action == null)
                {
                    if (actions.Count > 0) return Done("policy-error", "No policy for " + name);
                    Pulse(game, false);
                    continue;
                }
                if (trace) Console.Error.WriteLine(" -> " + action.Label + " " + value);
                var binding = frame.Bindings[action.ActionId];
                switch (binding.Kind)
                {
                    case PresentationActionKind.StoreSet:
                        form.GetType().GetMethod("SetQuantity", Flags)!.Invoke(form, new object[] { binding.StoreItem.Value, int.Parse(value) });
                        break;
                    case PresentationActionKind.SkipIntro:
                    case PresentationActionKind.PrepareDeparture:
                        Input(game, "");
                        break;
                    case PresentationActionKind.Select:
                        var menu = game.ActiveMenu;
                        while (menu.SelectedIndex != binding.MenuIndex) menu.MoveDown();
                        Input(game, value ?? binding.LegacyValue ?? "");
                        break;
                    default:
                        Input(game, value ?? binding.LegacyValue ?? "");
                        break;
                }
            }
            return new(seed, "timeout", game.TotalTurns, state?.Hud?.LivingPartyCount ?? 0, 0, state?.Hud?.LocationName, last);
        }
        catch (Exception ex)
        {
            return new(seed, "error", game.TotalTurns, state?.Hud?.LivingPartyCount ?? 0, 0, state?.Hud?.LocationName, last + ": " + ex);
        }
        finally { game.Destroy(); GameSimulationApp.Activate(null); }
    }
}
