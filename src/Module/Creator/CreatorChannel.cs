using System.Text.RegularExpressions;

namespace OregonTrailDotNet.Module.Creator;

public sealed record CreatorOrder(string GearId, int OrderedDay, int DeliveryDay, int Price);
public sealed record CreatorDraft(VideoIdea Idea, string Location, int FilmedDay, int Ceiling, string Camera);
public sealed record CreatorNotice(int Id, string Text);
public sealed record CreatorVideo(int Id, string Title, string Premise, string Location, int FilmedDay,
    int PublishedDay, int Views, int SubscriberViews, int DiscoveryViews, int SubscribersGained, int SubscribersLost,
    int OriginalViews, int Ceiling, string Camera, bool Demonetized, string Incident, bool Reedited,
    decimal Revenue, decimal Rpm, string Feedback, bool Viral = false, bool Licensed = false, int LicensePaid = 0);
/// <summary>
/// Server-only Viral Hog offer. The ad rate and the ad money the video would earn stay here until the player
/// answers, so declining is a real gamble. Only <see cref="Offer"/> is shown before the decision.
/// </summary>
internal sealed record CreatorLicenseOffer(int VideoId, int Offer, decimal Revenue, decimal Rpm);

/// <summary>Entire career belongs to one journey. Neither idea potential nor recommendation score leaves the server.</summary>
public sealed class CreatorCareer
{
    public bool Started { get; internal set; }
    public string Name { get; internal set; } = "Miles From Solvent";
    public int Subscribers { get; internal set; }
    public long TotalViews { get; internal set; }
    public long SubscriberViews { get; internal set; }
    public long DiscoveryViews { get; internal set; }
    public int Uploads { get; internal set; }
    public int Reuploads { get; internal set; }
    public int DaysWorked { get; internal set; }
    public int GearSpent { get; internal set; }
    public int ProductionSpent { get; internal set; }
    public decimal Revenue { get; internal set; }
    public int PaidOut { get; internal set; }
    /// <summary>Viral Hog lump sums, paid straight to road cash. Channel income, but never ad revenue.</summary>
    public int LicenseIncome { get; internal set; }
    public int ViralHits { get; internal set; }
    public bool Monetized { get; internal set; }
    public string ActiveCameraId { get; internal set; } = "phone";
    public CreatorDraft Draft { get; internal set; }
    public List<CreatorVideo> Videos { get; } = new();
    public List<CreatorOrder> Orders { get; } = new();
    public HashSet<string> Owned { get; } = new() { "phone" };
    public List<CreatorNotice> Notices { get; } = new();
    internal HashSet<string> FilmedIdeas { get; } = new();
    internal CreatorLicenseOffer License { get; set; }
    public bool LicensePending => License != null;
    internal double AlgorithmScore { get; set; } = 1;
    internal Random Random { get; set; }
    internal int NoticeId { get; set; }
    public decimal Unpaid => Revenue - PaidOut;
    public decimal Net => Revenue + LicenseIncome - GearSpent - ProductionSpent;
}

/// <summary>
/// Turn-based production. Actions commit once here; the travel adapter applies their trail-day cost once.
/// Gear is absent from the demand, subscriber-conversion, and recommendation formulas. It is only a final clamp.
/// </summary>
public sealed class CreatorChannel
{
    public const int ProductionCost = 8;
    public const int SubscriberThreshold = 500;
    public const int ViewThreshold = 10000;
    public const int PayoutMinimum = 10;
    public const double DemonetizationChance = .15;
    internal const double BreakoutChance = .03;
    /// <summary>Hidden potential at which an idea can convert subscribers and break out.</summary>
    internal const int GoodIdeaPotential = 80;
    /// <summary>Share of filming days that land a good idea while both kinds remain eligible.</summary>
    internal const double GoodIdeaChance = .32;
    /// <summary>
    /// Rare jackpot, rolled once per original upload of a good idea only. Separate from breakout: a breakout is a
    /// strong algorithm push that the kit ceiling still caps; a viral video escapes the ceiling and draws Viral Hog.
    /// </summary>
    internal const double ViralChance = .005;
    internal const int ViralMinimumViews = 200_000;
    internal const int ViralViewSpread = 800_000;
    public const int RpmMinimum = 3;
    public const int RpmMaximum = 7;
    /// <summary>Viral Hog pays this per 1,000 views, between the lowest and average ad rate, rounded to $25.</summary>
    public const decimal LicenseRatePerThousand = 4m;
    private readonly Func<int> _balance;
    private readonly Action<int> _wallet;
    private readonly IReadOnlyList<VideoIdea> _ideas;
    private Random Random => Career.Random;
    public CreatorCareer Career { get; }
    public CreatorGear Camera => Career.ActiveCameraId == "phone" ? CreatorCatalog.Phone :
        CreatorCatalog.Gear.Single(g => g.Id == Career.ActiveCameraId);
    public int Ceiling => Camera.Ceiling + CreatorCatalog.Gear
        .Where(g => g.Category != "camera" && Career.Owned.Contains(g.Id) && Compatible(g))
        .GroupBy(g => g.Category).Sum(group => group.Max(g => g.Ceiling));
    public bool CanFilm => Career.Started && !Career.LicensePending && Career.Draft == null && _balance() >= ProductionCost;
    public bool CanWithdraw => !Career.LicensePending && Career.Unpaid >= PayoutMinimum;

    // These are opaque platform allegations, not claims that invented events occurred in the footage.
    // Keep six entries and the same random draw so this copy change does not alter career outcomes.
    private static readonly string[] Incidents =
    {
        "The automated review alleged background profanity. It supplied no timestamp, and nobody remembers swearing.",
        "The automated review alleged background nudity. The appeal form offers no way to ask where.",
        "A copyright bot claimed an unidentified audio fragment. Its report does not say whether the clip contains music.",
        "The advertiser bot alleged an offensive gesture. It declined to identify a frame, a person, or a gesture.",
        "The review system alleged adult-service advertising. It provided neither a frame nor the supposed text.",
        "The review bot marked the clip unsuitable for advertisers. Its explanation is an explanation-shaped blank space."
    };

    public CreatorChannel(Random random, Func<int> balance, Action<int> wallet, CreatorCareer career,
        IReadOnlyList<VideoIdea> ideas = null)
    {
        Career = career;
        Career.Random ??= random;
        _balance = balance;
        _wallet = wallet;
        _ideas = ideas ?? CreatorCatalog.Ideas;
    }

    public bool Start(string name)
    {
        name = name?.Trim();
        if (Career.Started || string.IsNullOrEmpty(name) || name.Length > 32 ||
            !Regex.IsMatch(name, @"^[\p{L}\p{N} '&.!-]+$") || !name.Any(char.IsLetterOrDigit)) return false;
        Career.Started = true;
        Career.Name = name;
        Notice("Channel created. Your phone is ready. The family has not agreed to be recurring characters.");
        return true;
    }

    public bool Compatible(CreatorGear gear) =>
        (gear.Category != "lens" || Camera.Mount != null && gear.Mount == Camera.Mount) &&
        (gear.Category != "monitor" || Camera.Hdmi);

    public string BuyBlock(CreatorGear gear)
    {
        if (!Career.Started) return "Create a channel first";
        if (Career.LicensePending) return "Answer Viral Hog first";
        if (Career.Owned.Contains(gear.Id)) return "Owned";
        if (Career.Orders.Any(o => o.GearId == gear.Id)) return "In transit";
        if (gear.Category == "lens" && !Compatible(gear)) return $"Requires an active {gear.Mount} camera";
        if (gear.Category == "monitor" && !Compatible(gear)) return "Requires a camera with clean HDMI";
        return _balance() < gear.Price ? "Not enough road cash" : null;
    }

    public bool Buy(string gearId, int day)
    {
        var gear = CreatorCatalog.Gear.FirstOrDefault(g => g.Id == gearId);
        if (gear == null || BuyBlock(gear) != null) return false;
        _wallet(-gear.Price);
        Career.GearSpent += gear.Price;
        Career.Orders.Add(new CreatorOrder(gear.Id, day, day + 1, gear.Price));
        Notice($"V&H charged ${gear.Price} for {gear.Name}. Locker delivery after one trail day. Shipping: improbably free.");
        return true;
    }

    public void ReceiveOrders(int day)
    {
        foreach (var order in Career.Orders.Where(o => o.DeliveryDay <= day).ToArray())
        {
            Career.Owned.Add(order.GearId);
            Career.Orders.Remove(order);
            var gear = CreatorCatalog.Gear.Single(g => g.Id == order.GearId);
            Notice($"Delivered: {gear.Name}. " + (gear.Category == "camera"
                ? "It is in the camera bag. Switch to it there." : "Fitted automatically, if it works with your camera."));
        }
    }

    public bool Equip(string id)
    {
        if (!Career.Started || Career.LicensePending || id == Career.ActiveCameraId || !Career.Owned.Contains(id) ||
            id != "phone" && !CreatorCatalog.Gear.Any(g => g.Id == id && g.Category == "camera")) return false;
        Career.ActiveCameraId = id;
        Notice($"In use: {Camera.Name}. Footage you already have keeps the camera it was shot on.");
        return true;
    }

    public bool Film(string locationId, bool arrived, string locationName, int day)
    {
        if (!CanFilm) return false;
        var eligible = _ideas.Where(i => CreatorCatalog.Eligible(i, locationId, arrived)).ToArray();
        if (eligible.Length == 0) return false;
        // Good and bad ideas are drawn as separate pools so the good share holds after the scarce good ideas are used.
        var good = eligible.Where(i => i.Potential >= GoodIdeaPotential).ToArray();
        var bad = eligible.Where(i => i.Potential < GoodIdeaPotential).ToArray();
        var pool = good.Length == 0 ? bad : bad.Length == 0 ? good : Random.NextDouble() < GoodIdeaChance ? good : bad;
        var fresh = pool.Where(i => !Career.FilmedIdeas.Contains(i.Id)).ToArray();
        if (fresh.Length == 0) fresh = pool;
        var idea = fresh[Random.Next(fresh.Length)];
        _wallet(-ProductionCost);
        Career.ProductionSpent += ProductionCost;
        Career.DaysWorked++;
        Career.FilmedIdeas.Add(idea.Id);
        Career.Draft = new CreatorDraft(idea, locationName, day, Ceiling, Camera.Name);
        Notice($"One day filming “{idea.Title}”. ${ProductionCost} for data, cloud storage, and the creator tax on existing.");
        return true;
    }

    public bool Discard()
    {
        if (Career.Draft == null) return false;
        Notice($"Deleted “{Career.Draft.Idea.Title}”. The day and ${ProductionCost} are gone too.");
        Career.Draft = null;
        return true;
    }

    public bool Publish(int day)
    {
        var draft = Career.Draft;
        if (draft == null) return false;
        var potential = draft.Idea.Potential;
        // A dull topic has a hard demand ceiling, even for an established channel with expensive equipment.
        var topicLimit = potential < 36 ? 360 : potential < GoodIdeaPotential ? 2400 : 2_000_000;
        var breakout = Random.NextDouble() < BreakoutChance;
        // Bad ideas never draw this roll, so their RNG stream and outcomes are unchanged.
        var viral = potential >= GoodIdeaPotential && Random.NextDouble() < ViralChance;
        var interest = Random.NextDouble();
        var discoveryDemand = potential < 36 ? 12 + interest * 160 : potential < GoodIdeaPotential ? 70 + interest * 950 :
            breakout ? 65000 + Math.Pow(interest, 2) * 650000 : 600 + interest * 5500;
        var regularDemand = (int)(Career.Subscribers * (.04 + Random.NextDouble() * .46));
        // Viral strangers ignore the recommender, the topic limit, and the kit ceiling: a phone can hit the jackpot.
        var outsiderDemand = viral ? ViralMinimumViews + (int)(Math.Pow(interest, 2) * ViralViewSpread) :
            (int)(discoveryDemand * Career.AlgorithmScore * (.65 + potential / 140.0));
        var views = viral ? regularDemand + outsiderDemand :
            Math.Min(Math.Min(topicLimit, draft.Ceiling), Math.Max(0, regularDemand + outsiderDemand));
        var regular = Math.Min(views, regularDemand);
        var discovery = views - regular;
        var conversionRoll = Random.NextDouble();
        var conversion = Random.NextDouble();
        // Good ideas arrive often enough to tempt; rare breakouts and modest conversion keep 12- and 24-upload careers mostly a loss.
        var gained = conversionRoll < .28 ? 0 : (int)(discovery *
            (potential >= GoodIdeaPotential ? .005 + conversion * .014 : .0005 + conversion * .006));
        var lost = Math.Min(Career.Subscribers, (int)(Career.Subscribers * Random.NextDouble() *
            (potential < 36 ? .045 : .012)));
        // Subscriber response and discovery response each move the hidden recommender in both directions.
        var subscriberResponse = Career.Subscribers == 0 ? (gained > 0 ? .035 : -.035) :
            Math.Clamp((gained - lost) / (double)Math.Max(20, Career.Subscribers), -.12, .12);
        var discoveryResponse = Math.Clamp(Math.Log((discovery + 40) / 500.0) * .08 +
            (Random.NextDouble() - .5) * .22, -.2, .2);
        Career.AlgorithmScore = Math.Clamp(Career.AlgorithmScore * Math.Exp(subscriberResponse + discoveryResponse), .35, 2.2);
        var demonetized = Random.NextDouble() < DemonetizationChance;
        // Draw all incident/RPM randomness even for an unrestricted video: equipment cannot change the RNG stream.
        var incident = Incidents[Random.Next(Incidents.Length)];
        var rpm = Math.Round((decimal)(RpmMinimum + Random.NextDouble() * (RpmMaximum - RpmMinimum)), 2);
        Career.Subscribers = Math.Clamp(Career.Subscribers + gained - lost, 0, 10_000_000);
        Career.Uploads++;
        Career.TotalViews += views;
        Career.SubscriberViews += regular;
        Career.DiscoveryViews += discovery;
        CheckMonetization();
        var revenue = Career.Monetized && !demonetized ? Math.Round(views / 1000m * rpm, 2) : 0;
        // A viral video's ad money is held, unseen, until the player answers Viral Hog.
        if (viral)
        {
            Career.ViralHits++;
            Career.License = new CreatorLicenseOffer(Career.Uploads, LicenseOffer(views), revenue, rpm);
        }
        else Career.Revenue += revenue;
        var feedback = viral ? "It went viral. Several hundred thousand strangers now have opinions about your family's worst afternoon." :
            views >= 10000 ? "Discovery picked it up. The family is suddenly referred to as the team." :
            potential < 36 ? "The people who clicked mostly wanted to know when it would end." :
            views < 500 ? "Uploaded successfully. The internet continued with its day." :
            "A few strangers stayed. Victor recommends a better lens.";
        var video = new CreatorVideo(Career.Uploads, draft.Idea.Title, draft.Idea.Premise, draft.Location,
            draft.FilmedDay, day, views, regular, discovery, gained, lost, views, draft.Ceiling, draft.Camera,
            demonetized, demonetized ? incident : null, false, viral ? 0 : revenue, viral ? 0 : rpm, feedback, viral);
        Career.Videos.Insert(0, video);
        if (Career.Videos.Count > 60) Career.Videos.RemoveAt(60);
        Career.Draft = null;
        Notice($"Published “{video.Title}”: {views:N0} views; {gained:N0} new subscribers, {lost:N0} left." +
            (demonetized ? " Limited ads: read the review flag in your video library." : ""));
        if (viral)
            Notice($"Viral Hog is calling about “{video.Title}”. They want to buy it for ${Career.License.Offer:N0}, today.");
        return true;
    }

    /// <summary>Lump sum for a viral video's rights, from its views only. Ad status and RPM never change it.</summary>
    internal static int LicenseOffer(int views) =>
        (int)(Math.Round(views / 1000m * LicenseRatePerThousand / 25m, MidpointRounding.AwayFromZero) * 25);

    /// <summary>Sell the viral video. Pays road cash once and forfeits its ad money and its one re-edit.</summary>
    public bool AcceptLicense()
    {
        var offer = Career.License;
        if (offer == null) return false;
        Career.License = null;
        var index = Career.Videos.FindIndex(v => v.Id == offer.VideoId);
        Career.LicenseIncome += offer.Offer;
        _wallet(offer.Offer);
        if (index >= 0)
        {
            var video = Career.Videos[index];
            Career.Videos[index] = video with { Licensed = true, LicensePaid = offer.Offer, Revenue = 0, Rpm = offer.Rpm,
                Feedback = offer.Revenue > 0
                    ? $"Viral Hog owns it now, and your face in it. At this video's ad rate you would have earned ${offer.Revenue:N2}. They expect to make far more."
                    : video.Demonetized && Career.Monetized
                        ? "Viral Hog owns it now, and your face in it. Ad review had limited it, and the re-edit that might have paid is theirs too. They expect to make a great deal."
                        : "Viral Hog owns it now, and your face in it. It would have earned you nothing in ads. They expect to make a great deal." };
        }
        Notice($"Viral Hog paid ${offer.Offer:N0} into road cash. The clip, its ad money, and any re-edit are theirs forever, in every format yet invented.");
        return true;
    }

    /// <summary>Keep the viral video. The held ad money is released under the normal rules, exactly once.</summary>
    public bool DeclineLicense()
    {
        var offer = Career.License;
        if (offer == null) return false;
        Career.License = null;
        Career.Revenue += offer.Revenue;
        var index = Career.Videos.FindIndex(v => v.Id == offer.VideoId);
        if (index >= 0)
            Career.Videos[index] = Career.Videos[index] with { Revenue = offer.Revenue, Rpm = offer.Rpm };
        Notice(offer.Revenue > 0
            ? $"You kept it. Ads paid ${offer.Revenue:N2} at ${offer.Rpm:N2} per 1,000 views. Viral Hog said it would be in touch, in a tone."
            : "You kept it. It earned nothing in ads. Viral Hog thanked you for your time and hung up.");
        return true;
    }

    public bool Reedit(int id, int day)
    {
        if (Career.LicensePending) return false;
        var index = Career.Videos.FindIndex(v => v.Id == id && v.Demonetized && !v.Reedited && !v.Licensed);
        if (index < 0) return false;
        var video = Career.Videos[index];
        var views = video.OriginalViews / 2;
        var regular = video.SubscriberViews / 2;
        var discovery = views - regular;
        Career.TotalViews += views;
        Career.SubscriberViews += regular;
        Career.DiscoveryViews += discovery;
        Career.Reuploads++;
        Career.DaysWorked++;
        CheckMonetization();
        var revenue = Career.Monetized ? Math.Round(views / 1000m * video.Rpm, 2) : 0;
        Career.Revenue += revenue;
        Career.Videos[index] = video with { Views = views, SubscriberViews = regular, DiscoveryViews = discovery,
            PublishedDay = day, Reedited = true, Demonetized = false, Revenue = revenue,
            Feedback = "You trimmed whatever the bot might have meant, and it accepted the reupload. Half the original views returned. Subscribers were not counted twice." };
        Notice($"One day re-editing “{video.Title}”. Reupload: {views:N0} views, exactly half the original audience, rounded down.");
        return true;
    }

    public bool Withdraw()
    {
        if (!CanWithdraw) return false;
        var amount = (int)Math.Floor(Career.Unpaid);
        Career.PaidOut += amount;
        _wallet(amount);
        Notice($"${amount} moved to road cash. The leftover cents stay with the channel.");
        return true;
    }

    public bool WaitForOrders()
    {
        if (!Career.Started || Career.LicensePending || Career.Orders.Count == 0) return false;
        Career.DaysWorked++;
        Notice("A day watching the parcel tracker. The family still needs to eat.");
        return true;
    }

    private void CheckMonetization()
    {
        if (!Career.Monetized && Career.Subscribers >= SubscriberThreshold && Career.TotalViews >= ViewThreshold)
        {
            Career.Monetized = true;
            Notice("Partner status unlocked. Uploads from here on can earn ad money. Nothing earlier gets paid.");
        }
    }

    private void Notice(string text)
    {
        Career.Notices.Insert(0, new CreatorNotice(++Career.NoticeId, text));
        if (Career.Notices.Count > 8) Career.Notices.RemoveAt(8);
    }
}
