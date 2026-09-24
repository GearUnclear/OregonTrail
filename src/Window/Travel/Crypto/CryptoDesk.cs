using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.Module.Crypto;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Crypto;

[ParentWindow(typeof(Travel))]
public sealed class CryptoDesk : Form<TravelInfo>
{
    private bool _left;
    public CryptoDesk(IWindow window) : base(window) { }
    public CryptoExchange Exchange { get; private set; }
    public CryptoCareer Career => UserData.CryptoCareer;
    public override bool InputFillsBuffer => false;

    public override void OnFormPostCreate()
    {
        base.OnFormPostCreate();
        var game = GameSimulationApp.Instance;
        game.ActiveMenu = null;
        game.Vehicle.Status = VehicleStatus.Stopped;
        Exchange = new CryptoExchange(new Random(game.Random.Next()),
            () => game.Vehicle.Inventory[Entities.Cash].Quantity,
            delta =>
            {
                var cash = game.Vehicle.Inventory[Entities.Cash];
                if (delta < 0) cash.ReduceQuantity(-delta);
                else cash.AddQuantity(delta);
            }, Career);
    }

    public override void OnTick(bool systemTick, bool skipDay)
    {
        base.OnTick(systemTick, skipDay);
        if (!systemTick && !skipDay && !_left) Exchange?.Tick();
    }

    public bool Execute(string command, string input)
    {
        if (_left || Exchange == null) return false;
        if (command == "rename") return Exchange.Rename(input);
        if (command == "launch") return Exchange.Launch();
        if (command == "pull-out") return Exchange.PullOut();
        if (command.StartsWith("stake.") && int.TryParse(command[6..], out var stake)) return Exchange.SetStake(stake);
        if (command.StartsWith("narrative.")) return Exchange.SetNarrative(command[10..]);
        if (command.StartsWith("hype.")) return Exchange.StartCampaign(command[5..]);
        if (command != "leave" || Exchange.Phase is not ("lobby" or "result")) return false;
        _left = true;
        var usedDay = Exchange.Receipt != null;
        ClearForm();
        // Leaving a finished launch costs a day exactly once. Merely browsing the launch desk is free.
        if (usedDay) GameSimulationApp.Instance.TakeTurn(false);
        return true;
    }

    public override string OnRenderForm() => "RUG.RUN / Parking-lot exchange\n" +
        (Exchange == null ? "Connecting..." : $"{Exchange.Name} | {Exchange.Phase} | ${Exchange.Price:F4}");

    public override void OnInputBufferReturned(string input) { }
}
