using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Location;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.Module.Creator;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Creator;

[ParentWindow(typeof(Travel))]
public sealed class CreatorDesk : Form<TravelInfo>
{
    private bool _left;
    public CreatorDesk(IWindow window) : base(window) { }
    public CreatorChannel Channel { get; private set; }
    public string Tab { get; private set; } = "studio";
    public override bool InputFillsBuffer => false;

    public override void OnFormPostCreate()
    {
        base.OnFormPostCreate();
        var game = GameSimulationApp.Instance;
        game.ActiveMenu = null;
        if (game.Vehicle.Status == VehicleStatus.Moving) game.Vehicle.Status = VehicleStatus.Stopped;
        Channel = new CreatorChannel(new Random(game.Random.Next()),
            () => game.Vehicle.Inventory[Entities.Cash].Quantity,
            delta =>
            {
                var cash = game.Vehicle.Inventory[Entities.Cash];
                if (delta < 0) cash.ReduceQuantity(-delta);
                else cash.AddQuantity(delta);
            }, UserData.CreatorCareer);
        Channel.ReceiveOrders(game.TotalTurns);
    }

    public bool Execute(string command, string input)
    {
        if (_left || Channel == null) return false;
        var game = GameSimulationApp.Instance;
        var day = game.TotalTurns;
        var spendsDay = false;
        bool accepted;
        if (command == "leave")
        {
            _left = true;
            ClearForm();
            return true;
        }
        if (command.StartsWith("tab."))
        {
            var tab = command[4..];
            if (!Channel.Career.Started || tab is not ("studio" or "shop" or "library")) return false;
            Tab = tab;
            return true;
        }
        if (command == "start") accepted = Channel.Start(input);
        else if (command == "film")
        {
            var location = game.Trail.CurrentLocation;
            CreatorCatalog.Locations.TryGetValue(location.Name, out var id);
            var arrived = location.Status == LocationStatus.Arrived;
            accepted = Channel.Film(id, arrived, arrived ? location.Name : $"On the road toward {game.Trail.NextLocation?.Name ?? "Seattle"}", day);
            spendsDay = accepted;
        }
        else if (command == "publish") accepted = Channel.Publish(day);
        else if (command == "discard") accepted = Channel.Discard();
        else if (command == "withdraw") accepted = Channel.Withdraw();
        else if (command == "wait-order") spendsDay = accepted = Channel.WaitForOrders();
        else if (command.StartsWith("buy.")) accepted = Channel.Buy(command[4..], day);
        else if (command.StartsWith("equip.")) accepted = Channel.Equip(command[6..]);
        else if (command.StartsWith("reedit.") && int.TryParse(command[7..], out var videoId))
            spendsDay = accepted = Channel.Reedit(videoId, day + 1);
        else return false;
        if (!accepted) return false;
        if (spendsDay) game.TakeTurn(false);
        Channel.ReceiveOrders(game.TotalTurns);
        if (spendsDay && game.Vehicle.PassengersDead)
            game.WindowManager.Add(typeof(GameOver.GameOver));
        return true;
    }

    public override string OnRenderForm() => "DEAD AIR / YouTube travel studio\n" +
        (Channel == null ? "Connecting..." : $"{Channel.Career.Name} | {Channel.Career.Subscribers:N0} subscribers");
    public override void OnInputBufferReturned(string input) { }
}
