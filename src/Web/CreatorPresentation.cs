using OregonTrailDotNet.Module.Creator;
using OregonTrailDotNet.Window.Travel.Creator;

namespace OregonTrailDotNet.Web;

public sealed record CreatorDeskDto(string Tab, bool Started, string Name, CreatorStatsDto Stats,
    CreatorPartnerDto Partner, CreatorKitDto Kit, CreatorDraftDto Draft, IReadOnlyList<CreatorGearDto> Gear,
    IReadOnlyList<CreatorOrderDto> Orders, IReadOnlyList<CreatorVideo> Videos, IReadOnlyList<CreatorNotice> Notices,
    int ProductionCost, string FilmingLocation);
public sealed record CreatorStatsDto(int Subscribers, long TotalViews, long SubscriberViews, long DiscoveryViews,
    int Uploads, int Reuploads, int DaysWorked, int GearSpent, int ProductionSpent, decimal Revenue, int PaidOut,
    decimal Unpaid, decimal Net);
public sealed record CreatorPartnerDto(bool Monetized, int SubscriberThreshold, int ViewThreshold, int PayoutMinimum);
public sealed record CreatorKitDto(string CameraId, string Camera, string Mount, bool Hdmi, int Ceiling);
public sealed record CreatorDraftDto(string Title, string Premise, string Location, int FilmedDay, int Ceiling, string Camera);
public sealed record CreatorGearDto(string Id, string Category, string Name, int Price, int Ceiling,
    string Specs, string Pitch, string Mount, bool Hdmi, bool Owned, bool Compatible, bool Active,
    string BuyBlock, string BuyActionId, string EquipActionId);
public sealed record CreatorOrderDto(string GearId, string Name, int Price, int DaysRemaining);

internal static partial class PresentationCatalog
{
    private static CreatorDeskDto BuildCreator(CreatorDesk desk, GameSimulationApp game)
    {
        var channel = desk?.Channel;
        if (channel == null) return null;
        var c = channel.Career;
        var d = c.Draft;
        return new CreatorDeskDto(desk.Tab, c.Started, c.Name,
            new CreatorStatsDto(c.Subscribers, c.TotalViews, c.SubscriberViews, c.DiscoveryViews, c.Uploads,
                c.Reuploads, c.DaysWorked, c.GearSpent, c.ProductionSpent, c.Revenue, c.PaidOut, c.Unpaid, c.Net),
            new CreatorPartnerDto(c.Monetized, CreatorChannel.SubscriberThreshold, CreatorChannel.ViewThreshold,
                CreatorChannel.PayoutMinimum),
            new CreatorKitDto(channel.Camera.Id, channel.Camera.Name, channel.Camera.Mount, channel.Camera.Hdmi, channel.Ceiling),
            d == null ? null : new CreatorDraftDto(d.Idea.Title, d.Idea.Premise, d.Location, d.FilmedDay, d.Ceiling, d.Camera),
            CreatorCatalog.Gear.Prepend(CreatorCatalog.Phone).Select(g => new CreatorGearDto(g.Id, g.Category,
                g.Name, g.Price, g.Ceiling, g.Specs, g.Pitch, g.Mount, g.Hdmi, c.Owned.Contains(g.Id),
                channel.Compatible(g), g.Category == "camera" ? c.ActiveCameraId == g.Id :
                    c.Owned.Contains(g.Id) && channel.Compatible(g) && !CreatorCatalog.Gear.Any(other =>
                        other.Category == g.Category && c.Owned.Contains(other.Id) && channel.Compatible(other) && other.Ceiling > g.Ceiling),
                channel.BuyBlock(g), $"creator.buy.{g.Id}", g.Category == "camera" ? $"creator.equip.{g.Id}" : null)).ToArray(),
            c.Orders.Select(o => new CreatorOrderDto(o.GearId, CreatorCatalog.Gear.Single(g => g.Id == o.GearId).Name,
                o.Price, Math.Max(0, o.DeliveryDay - game.TotalTurns))).ToArray(),
            c.Videos.ToArray(), c.Notices.ToArray(), CreatorChannel.ProductionCost,
            game.Trail.CurrentLocation.Status == Entity.Location.LocationStatus.Arrived
                ? game.Trail.CurrentLocation.Name : $"On the road toward {game.Trail.NextLocation?.Name ?? "Seattle"}");
    }

    private static List<GameActionDto> BuildCreatorActions(CreatorDesk desk,
        IDictionary<string, PresentationActionBinding> bindings)
    {
        var actions = new List<GameActionDto>();
        var channel = desk?.Channel;
        if (channel == null) return actions;
        var c = channel.Career;
        void Add(string command, string label, bool enabled = true, string kind = "select", InputSpecDto input = null,
            bool selected = false)
        {
            var id = $"creator.{command}";
            actions.Add(new GameActionDto(id, label, kind, enabled, selected));
            if (enabled) bindings[id] = new PresentationActionBinding(PresentationActionKind.Creator, command, Input: input);
        }
        if (!c.Started)
            Add("start", "Start your channel", kind: "primary", input: new InputSpecDto("text", "Channel name",
                "Miles From Solvent", true, null, null, 32, "creator.start"));
        else
        {
            foreach (var (tab, label) in new[] { ("studio", "Studio"), ("shop", "V&H equipment"), ("library", "Video library") })
                Add($"tab.{tab}", label, kind: "tab", selected: desk.Tab == tab);
            Add("film", $"Film whatever happens · 1 day + ${CreatorChannel.ProductionCost}", channel.CanFilm, "primary");
            if (c.Draft != null)
            {
                Add("publish", "Publish this video", kind: "primary");
                Add("discard", "Discard footage · no refund", kind: "secondary");
            }
            Add("withdraw", "Transfer ad earnings to road cash", channel.CanWithdraw, "secondary");
            if (c.Orders.Count > 0) Add("wait-order", "Wait for delivery · 1 day", kind: "secondary");
            foreach (var gear in CreatorCatalog.Gear) Add($"buy.{gear.Id}", $"Order · ${gear.Price}", channel.BuyBlock(gear) == null);
            foreach (var gear in CreatorCatalog.Gear.Prepend(CreatorCatalog.Phone).Where(g => g.Category == "camera" && c.Owned.Contains(g.Id)))
                Add($"equip.{gear.Id}", "Use this camera", gear.Id != c.ActiveCameraId, selected: gear.Id == c.ActiveCameraId);
            foreach (var video in c.Videos.Where(v => v.Demonetized && !v.Reedited))
                Add($"reedit.{video.Id}", "Re-edit & reupload · 1 day / half the views", kind: "secondary");
        }
        Add("leave", "Back to the road", kind: "secondary");
        return actions;
    }
}
