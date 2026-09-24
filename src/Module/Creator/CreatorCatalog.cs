using System.Text.Json;

namespace OregonTrailDotNet.Module.Creator;

public sealed record VideoIdea(string Id, string Title, string Premise, string ConceptKey, int Potential,
    string[] Locations, int AuthorBatch);

public sealed record CreatorGear(string Id, string Category, string Name, int Price, int Ceiling,
    string Specs, string Pitch, string Mount = null, bool Hdmi = false);

public static class CreatorCatalog
{
    private static readonly Lazy<IReadOnlyList<VideoIdea>> IdeasSource = new(() =>
    {
        using var stream = typeof(CreatorCatalog).Assembly.GetManifestResourceStream(
            "OregonTrailDotNet.Module.Creator.video-ideas.json")
            ?? throw new InvalidOperationException("The travel-video idea catalog is missing.");
        return JsonSerializer.Deserialize<VideoIdea[]>(stream, new JsonSerializerOptions
            { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("The idea catalog is empty.");
    });
    public static IReadOnlyList<VideoIdea> Ideas => IdeasSource.Value;

    public static readonly IReadOnlyDictionary<string, string> Locations = new Dictionary<string, string>
    {
        ["Cape Coral, FL"] = "cape-coral",
        ["I-40 Pigeon River Gorge Washout"] = "pigeon-gorge",
        ["I-10 Francine Flood Crossing"] = "francine-flood",
        ["Buc-ee's, Sevierville TN"] = "bucees",
        ["Touchdown Jesus, Monroe OH"] = "touchdown-jesus",
        ["Wall Drug, SD"] = "wall-drug",
        ["Carhenge, Alliance NE"] = "carhenge",
        ["The I-44 Texas Detour"] = "texas-detour",
        ["Big Texan Steak Ranch"] = "big-texan",
        ["Cadillac Ranch"] = "cadillac-ranch",
        ["Great Salt Lake Causeway Flood Crossing"] = "salt-lake",
        ["Iowa State Fair Butter Cow"] = "iowa-fair",
        ["Open-Carry Walmart, Springfield MO"] = "springfield",
        ["Columbia/Snake 'Sovereign Citizen' Crossing"] = "sovereign-crossing",
        ["Portland, OR"] = "portland",
        ["The Cascades: I-90 vs Highway 1 Fork"] = "cascades",
        ["Tacoma 'No Kings' Rally Town"] = "tacoma",
        ["The Gorge Fork"] = "gorge-fork",
        ["Columbia I-5 Bridge"] = "columbia-bridge",
        ["I-405 Express Toll Lanes"] = "i405",
        ["Seattle, WA"] = "seattle"
    };

    public static readonly CreatorGear Phone = new("phone", "camera", "The phone you already own", 0, 50000,
        "Cracked screen · fixed lens · built-in storage", "A camera. In this economy.");

    public static readonly IReadOnlyList<CreatorGear> Gear = new CreatorGear[]
    {
        new("pocket-used", "camera", "ReCertified Pocket 1080", 129, 65000, "Fixed 28mm · 1080p · mystery stain", "Tested to power on. Further questions cost extra."),
        new("action", "camera", "GoBroke Adventure 4K", 249, 95000, "Fixed ultrawide · splash resistant", "Make every parking lot look like an extreme sport."),
        new("compact", "camera", "VlogStar Fixed-Lens Deluxe", 499, 150000, "24–70mm equivalent · face tracking", "Tracks your face while you explain the purchase."),
        new("mirrorless", "camera", "Almost Alpha E10 Creator Kit", 749, 240000, "VH-E mount · 16–50mm kit lens · clean HDMI", "Interchangeable lenses. Non-interchangeable regrets.", "VH-E", true),
        new("full-frame", "camera", "DebtFrame E7 IV Body + Kit", 1899, 650000, "VH-E mount · full frame · kit lens · clean HDMI", "Full-frame coverage of your empty checking account.", "VH-E", true),
        new("cinema", "camera", "CineMortgage C6 Starter Kit", 3499, 1200000, "VH-C mount · 6K RAW · kit lens · clean HDMI", "The word cinema appears seven times on the box.", "VH-C", true),
        new("light-clip", "light", "InfluenceClip USB Ring", 19, 2000, "3-inch ring · cool white · USB", "A halo for someone financing gas with ad revenue."),
        new("light-pocket", "light", "Pocket RGB Emotional Support", 69, 6000, "2500–9000K · 12 dramatic presets", "Police-light mode does not confer police powers."),
        new("light-panel", "light", "Bi-Color Parking Lot Panel", 159, 14000, "30W panel · 95 CRI · soft case", "Your cereal deserves flattering key light."),
        new("light-tube", "light", "RGB Tube of Professionalism", 299, 22000, "2-foot tube · internal battery", "A $299 stick that glows. Victor calls it essential."),
        new("light-cob", "light", "Daylight COB 200D + Softbox", 599, 45000, "200W · Bowens mount · mains power", "Nearly as bright as the sun, less portable."),
        new("light-cinema", "light", "Sun Replacement 600 Pro", 1499, 85000, "600W daylight · rolling hard case", "Requires power, space, and an explanation to your family."),
        new("sd-basic", "storage", "64GB V30 Reasonable Card", 14, 1500, "64GB · U3/V30 · full-size SD", "The adapter is the part you will lose."),
        new("sd-v60", "storage", "256GB V60 Ambition Card", 59, 6000, "256GB · UHS-II · V60", "Enough room for 83 takes of hello guys."),
        new("sd-v90", "storage", "512GB V90 Future-Proofish", 179, 16000, "512GB · V90 · gold label", "Future-proof until Tuesday's press release."),
        new("ssd", "storage", "4TB Rugged Backup Brick", 329, 28000, "4TB SSD · rubber bumper · USB-C", "Back up the backup of the thing nobody watched."),
        new("battery-usb", "power", "10,000mAh Almost Enough", 25, 2500, "USB-C PD · pocket bank", "One more take before the dashboard battery warning."),
        new("battery-kit", "power", "Two Batteries and a Prayer", 79, 8000, "Universal regulated camera power kit", "Includes charger. Includes hope. No viewers included."),
        new("battery-vmount", "power", "99Wh V-Mount Status Symbol", 219, 20000, "USB-C / D-Tap · mounting plate", "Exactly the battery your battery needed."),
        new("battery-station", "power", "1kWh Portable Wall Outlet", 799, 45000, "1000Wh · AC inverter · heavy", "The road trip now has an electrical department."),
        new("monitor-basic", "monitor", "5-inch Focus Anxiety Display", 169, 12000, "HDMI · focus peaking · sun hood", "See your mistakes on a second screen.", Hdmi: true),
        new("monitor-bright", "monitor", "7-inch Daylight Doubt Monitor", 499, 30000, "2000 nits · waveform · false color", "Now you can expose correctly for an empty audience.", Hdmi: true),
        new("monitor-recorder", "monitor", "ProRes Receipt Recorder", 999, 65000, "5-inch HDR · external recording", "Records every detail except a business model.", Hdmi: true),
        new("lens-prime", "lens", "VH-E Nifty Fifty f/1.8", 149, 15000, "50mm f/1.8 · VH-E mount", "Blur the background. The rent remains in focus.", "VH-E"),
        new("lens-wide", "lens", "VH-E Tiny Van 11mm f/1.8", 499, 38000, "11mm f/1.8 · VH-E mount", "Makes 40 square feet look like 43.", "VH-E"),
        new("lens-zoom", "lens", "VH-E Content Master 24–70", 1699, 95000, "24–70mm f/2.8 · VH-E mount", "Victor says this is the last lens you'll ever need. Again.", "VH-E"),
        new("lens-cinema", "lens", "VH-C Anamorphic Life Choice", 2499, 150000, "35mm T2 · 1.6× squeeze · VH-C mount", "Widescreen disappointment, oval bokeh.", "VH-C"),
        new("nd-clip", "filter", "Universal Clip ND8", 18, 1500, "3 stops · universal phone/lens clamp", "Sunglasses for the camera. Not a personality."),
        new("nd-variable", "filter", "Variable ND 2–5 Stops", 89, 9000, "Universal adapter · variable density", "Rotate until the bill disappears into shadow."),
        new("nd-premium", "filter", "Signature Brass ND Collection", 249, 23000, "ND8 / ND64 / ND1000 · adapters", "Same sun. Premium invoice."),
        new("mic-wired", "audio", "Lapel Mic With Long Cable", 29, 3500, "TRRS/USB-C · foam windscreen", "Now the family argument has excellent intelligibility."),
        new("mic-wireless", "audio", "Dual Wireless Overshare Kit", 249, 19000, "2 transmitters · safety track", "The safety track cannot save you from what was said."),
        new("tripod", "support", "Tabletop Tripod of Resolve", 24, 2000, "Mini ball head · phone clamp", "Three legs. One questionable career move."),
        new("gimbal", "support", "Three-Axis Financial Instability", 299, 25000, "Mirrorless/phone stabilizer · case", "Stabilizes footage, not income.")
    };

    public static bool Eligible(VideoIdea idea, string locationId, bool arrived) =>
        idea.Locations.Length == 0 || arrived && idea.Locations.Contains(locationId);
}
