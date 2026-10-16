using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.Hunt
{
    [ParentWindow(typeof(Travel))]
    public sealed class HuntReady : InputForm<TravelInfo>
    {
        public HuntReady(IWindow window) : base(window) { }
        protected override DialogType DialogType => DialogType.YesNo;
        protected override string OnDialogPrompt() =>
            "Grab or press Space while the marker is in the striped zone. Eight trays, one attempt each. " +
            "Completing the sweep spends one trail day. Ready to start?";
        protected override void OnDialogResponse(DialogResponse response)
        {
            if (response == DialogResponse.Yes) SetForm(typeof(Hunting));
            else ClearForm();
        }
    }
}
