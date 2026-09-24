using System.Text.RegularExpressions;

namespace OregonTrailDotNet.Module.Creator;

public sealed record CreatorOrder(string GearId, int OrderedDay, int DeliveryDay, int Price);
public sealed record CreatorDraft(VideoIdea Idea, string Location, int FilmedDay, int Ceiling, string Camera);
public sealed record CreatorNotice(int Id, string Text);
public sealed record CreatorVideo(int Id, string Title, string Premise, string Location, int FilmedDay,
    int PublishedDay, int Views, int SubscriberViews, int DiscoveryViews, int SubscribersGained, int SubscribersLost,
    int OriginalViews, int Ceiling, string Camera, bool Demonetized, string Incident, bool Reedited,
    decimal Revenue, decimal Rpm, string Feedback);

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
    public bool Monetized { get; internal set; }
    public string ActiveCameraId { get; internal set; } = "phone";
    public CreatorDraft Draft { get; internal set; }
    public List<CreatorVideo> Videos { get; } = new();
    public List<CreatorOrder> Orders { get; } = new();
    public HashSet<string> Owned { get; } = new() { "phone" };
    public List<CreatorNotice> Notices { get; } = new();
    internal HashSet<string> FilmedIdeas { get; } = new();
    internal double AlgorithmScore { get; set; } = 1;
    internal Random Random { get; set; }
    internal int NoticeId { get; set; }
    public decimal Unpaid => Revenue - PaidOut;
    public decimal Net => Revenue - GearSpent - ProductionSpent;
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
    internal const double BreakoutChance = .095;
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
    public bool CanFilm => Career.Started && Career.Draft == null && _balance() >= ProductionCost;
    public bool CanWithdraw => Career.Unpaid >= PayoutMinimum;

    private static readonly string[] Incidents =
    {
        "A family member swore in the background while you were explaining wholesome van life.",
        "A man living on the street exposed himself in the distant background. You missed it in the edit.",
        "The radio played a copyrighted chorus under your heartfelt monologue.",
        "Your passenger demonstrated a hand gesture the advertiser review bot understood immediately.",
        "A roadside billboard put an adult-service phone number directly behind your face.",
        "Your family argument survived the edit under a track called Peaceful Journey."
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
                ? "Select it in your camera bag to use it." : "Compatible supporting gear is fitted automatically."));
        }
    }

    public bool Equip(string id)
    {
        if (!Career.Started || id == Career.ActiveCameraId || !Career.Owned.Contains(id) ||
            id != "phone" && !CreatorCatalog.Gear.Any(g => g.Id == id && g.Category == "camera")) return false;
        Career.ActiveCameraId = id;
        Notice($"Camera bag: {Camera.Name}. Existing footage keeps the kit it was filmed on.");
        return true;
    }

    public bool Film(string locationId, bool arrived, string locationName, int day)
    {
        if (!CanFilm) return false;
        var eligible = _ideas.Where(i => CreatorCatalog.Eligible(i, locationId, arrived)).ToArray();
        if (eligible.Length == 0) return false;
        var fresh = eligible.Where(i => !Career.FilmedIdeas.Contains(i.Id)).ToArray();
        if (fresh.Length == 0) fresh = eligible;
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
        var topicLimit = potential < 36 ? 360 : potential < 80 ? 2400 : 2_000_000;
        var breakout = Random.NextDouble() < BreakoutChance;
        var interest = Random.NextDouble();
        var discoveryDemand = potential < 36 ? 12 + interest * 160 : potential < 80 ? 70 + interest * 950 :
            breakout ? 65000 + Math.Pow(interest, 2) * 650000 : 600 + interest * 5500;
        var regularDemand = (int)(Career.Subscribers * (.04 + Random.NextDouble() * .46));
        var outsiderDemand = (int)(discoveryDemand * Career.AlgorithmScore * (.65 + potential / 140.0));
        var views = Math.Min(Math.Min(topicLimit, draft.Ceiling), Math.Max(0, regularDemand + outsiderDemand));
        var regular = Math.Min(views, regularDemand);
        var discovery = views - regular;
        var conversionRoll = Random.NextDouble();
        var conversion = Random.NextDouble();
        var gained = conversionRoll < .28 ? 0 : (int)(discovery *
            (potential >= 80 ? .012 + conversion * .032 : .0005 + conversion * .006));
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
        var rpm = Math.Round((decimal)(3 + Random.NextDouble() * 4), 2);
        Career.Subscribers = Math.Clamp(Career.Subscribers + gained - lost, 0, 10_000_000);
        Career.Uploads++;
        Career.TotalViews += views;
        Career.SubscriberViews += regular;
        Career.DiscoveryViews += discovery;
        CheckMonetization();
        var revenue = Career.Monetized && !demonetized ? Math.Round(views / 1000m * rpm, 2) : 0;
        Career.Revenue += revenue;
        var feedback = views >= 10000 ? "Discovery picked it up. The family is suddenly referred to as the team." :
            potential < 36 ? "The people who clicked mostly wanted to know when it would end." :
            views < 500 ? "Uploaded successfully. The internet continued with its day." :
            "A few strangers stayed. Victor recommends a better lens.";
        var video = new CreatorVideo(Career.Uploads, draft.Idea.Title, draft.Idea.Premise, draft.Location,
            draft.FilmedDay, day, views, regular, discovery, gained, lost, views, draft.Ceiling, draft.Camera,
            demonetized, demonetized ? incident : null, false, revenue, rpm, feedback);
        Career.Videos.Insert(0, video);
        if (Career.Videos.Count > 60) Career.Videos.RemoveAt(60);
        Career.Draft = null;
        Notice($"Published “{video.Title}”: {views:N0} views; {gained:N0} new subscribers, {lost:N0} left." +
            (demonetized ? " Limited ads: review the incident in your video library." : ""));
        return true;
    }

    public bool Reedit(int id, int day)
    {
        var index = Career.Videos.FindIndex(v => v.Id == id && v.Demonetized && !v.Reedited);
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
            Feedback = "The offending moment is gone. Half the original views returned. Subscribers were not counted twice." };
        Notice($"One day re-editing “{video.Title}”. Reupload: {views:N0} views, exactly half the original audience, rounded down.");
        return true;
    }

    public bool Withdraw()
    {
        if (!CanWithdraw) return false;
        var amount = (int)Math.Floor(Career.Unpaid);
        Career.PaidOut += amount;
        _wallet(amount);
        Notice($"${amount} transferred to road cash. Any remaining cents stay in the channel account.");
        return true;
    }

    public bool WaitForOrders()
    {
        if (!Career.Started || Career.Orders.Count == 0) return false;
        Career.DaysWorked++;
        Notice("A day watching the parcel tracker. The family still needs to eat.");
        return true;
    }

    private void CheckMonetization()
    {
        if (!Career.Monetized && Career.Subscribers >= SubscriberThreshold && Career.TotalViews >= ViewThreshold)
        {
            Career.Monetized = true;
            Notice("Partner status unlocked. Eligible uploads can now earn ads; earlier unpaid videos are not paid retroactively.");
        }
    }

    private void Notice(string text)
    {
        Career.Notices.Insert(0, new CreatorNotice(++Career.NoticeId, text));
        if (Career.Notices.Count > 8) Career.Notices.RemoveAt(8);
    }
}
