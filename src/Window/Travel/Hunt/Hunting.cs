using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Hunt
{
    [ParentWindow(typeof(Travel))]
    public sealed class Hunting : Form<TravelInfo>
    {
        public Hunting(IWindow window) : base(window) { }
        public HuntManager Sweep => UserData.Hunt;
        public override bool InputFillsBuffer => false;
        public override bool AllowInput => false;
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();
            UserData.GenerateHunt();
        }
        public override void OnTick(bool systemTick, bool skipDay)
        {
            base.OnTick(systemTick, skipDay);
            Sweep.OnTick(systemTick, skipDay);
            if (Sweep.ShouldEndHunt) SetForm(typeof(HuntingResult));
        }
        public override void OnInputBufferReturned(string input) { }
        public override string OnRenderForm() => Sweep.HuntInfo;
    }
}
