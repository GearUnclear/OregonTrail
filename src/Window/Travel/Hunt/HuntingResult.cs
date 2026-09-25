using OregonTrailDotNet.Entity;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.Hunt
{
    [ParentWindow(typeof(Travel))]
    public sealed class HuntingResult : InputForm<TravelInfo>
    {
        private int _food;
        private bool _collected;
        public HuntingResult(IWindow window) : base(window) { }
        public override void OnFormPostCreate()
        {
            _food = UserData.Hunt.KillWeight;
            base.OnFormPostCreate();
            GameSimulationApp.Instance.TakeTurn(false);
        }
        protected override string OnDialogPrompt() =>
            $"Food sweep complete. {_food} lb packed. One trail day spent.";
        protected override void OnDialogResponse(DialogResponse response)
        {
            if (_collected) return;
            _collected = true;
            GameSimulationApp.Instance.Vehicle.Inventory[Entities.Food].AddQuantity(_food);
            UserData.DestroyHunt();
            ClearForm();
        }
    }
}
