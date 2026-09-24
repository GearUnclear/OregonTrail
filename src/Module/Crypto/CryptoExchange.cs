namespace OregonTrailDotNet.Module.Crypto;

public sealed record CryptoCampaign(string Id, string Name, string Pitch, int Cost, int Duration,
    int Cooldown, double Hype, double Trust, double Heat, int Reach);
public sealed record CryptoNarrative(string Id, string Name, string Detail, double Volatility,
    double Audience, double StartingTrust);
public sealed record CryptoCandle(int Tick, double Open, double High, double Low, double Close, int Volume);
public sealed record CryptoNews(int Id, int Tick, string Tone, string Text);
public sealed record CryptoReceipt(string Name, int Stake, int LaunchFee, int Marketing, int Returned,
    int Net, int BestExit, int RequestedExit, int Duration, string Reason);

/// <summary>Persists for this road trip, including after leaving the exchange.</summary>
public sealed class CryptoCareer
{
    private readonly List<CryptoReceipt> _history = [];
    public IReadOnlyList<CryptoReceipt> History => _history.AsReadOnly();
    public int Launches { get; private set; }
    public int Wins { get; private set; }
    public int Net { get; private set; }
    public int Notoriety { get; private set; }

    internal void Record(CryptoReceipt receipt)
    {
        Launches++;
        if (receipt.Net > 0) Wins++;
        Net += receipt.Net;
        Notoriety = Math.Min(40, Notoriety + (receipt.Reason == "pulled" ? 7 : 3));
        _history.Insert(0, receipt);
        if (_history.Count > 8) _history.RemoveAt(8);
    }
}

/// <summary>
/// Fictional, server-owned market. One tick is one active second, never a trail day.
/// Cash uses whole dollars to match the vehicle inventory. Debit before work; credit once at settlement.
/// Injected randomness and wallet operations let balancing runs exercise the actual rules.
/// </summary>
public sealed class CryptoExchange
{
    public const int SessionSeconds = 120;
    public const int LaunchFee = 12;
    public const int ExitFee = 3;
    public const int ExitSeconds = 3;
    public const double LaunchPrice = .01;
    public static readonly IReadOnlyList<int> Stakes = Array.AsReadOnly(new[] { 25, 75, 150 });
    public static readonly IReadOnlyList<CryptoNarrative> Narratives = Array.AsReadOnly(new[]
    {
        new CryptoNarrative("meme", "Highway meme", "Fast attention. Restless holders. Extremely online.", 1.15, 1.2, 28),
        new CryptoNarrative("ai", "AI vaporware", "Bigger swings. The roadmap is a loading spinner.", 1.5, 1.05, 22),
        new CryptoNarrative("utility", "Roadside utility", "Less volatile. Slower reach. Allegedly solves parking.", .75, .8, 46)
    });
    public static readonly IReadOnlyList<CryptoCampaign> Campaigns = Array.AsReadOnly(new[]
    {
        new CryptoCampaign("memes", "Meme barrage", "Cheap reach. Attention fades fast; repetition wears people out.", 4, 4, 6, 15, -2, 4, 32),
        new CryptoCampaign("bots", "Rent a bot army", "Big numbers, hollow conviction. Bots leave after 14 seconds.", 12, 6, 14, 32, -9, 17, 110),
        new CryptoCampaign("influencer", "Influencer shoutout", "Expensive reach. A sponsor might bring buyers—or dump on them.", 24, 9, 20, 39, -3, 12, 160),
        new CryptoCampaign("ama", "Parking-lot livestream", "Build trust and attract patient liquidity. Takes your full attention.", 6, 8, 12, 12, 15, -7, 42),
        new CryptoCampaign("paper", "Publish a whitepaper", "A slow credibility play. The market keeps moving while you write.", 8, 12, 16, 8, 24, -12, 28),
        new CryptoCampaign("burn", "Token-burn spectacle", "A quick price jolt. The crowd starts watching the founder wallet.", 10, 5, 15, 23, -4, 10, 56)
    });

    private readonly Random _random;
    private readonly Func<int> _balance;
    private readonly Action<int> _changeCash;
    private readonly CryptoCareer _career;
    private readonly List<CryptoCandle> _candles = [];
    private readonly List<CryptoNews> _news = [];
    private readonly Dictionary<string, int> _readyAt = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _uses = new(StringComparer.Ordinal);
    private double _momentum;
    private double _marketDrift;
    private int _newsId;
    private int _botsExpireAt;
    private int _bestExit;
    private int _requestedExit;

    public CryptoExchange(Random random, Func<int> balance, Action<int> changeCash, CryptoCareer career)
    {
        _random = random;
        _balance = balance;
        _changeCash = changeCash;
        _career = career;
    }

    public string Phase { get; private set; } = "lobby";
    public string Name { get; private set; } = "PotholeCoin";
    public string Symbol => new(Name.Where(char.IsAsciiLetterOrDigit).Take(6).Select(char.ToUpperInvariant).ToArray());
    public int Stake { get; private set; } = 25;
    public CryptoNarrative Narrative { get; private set; } = Narratives[0];
    public int Elapsed { get; private set; }
    public int Remaining => Math.Max(0, SessionSeconds - Elapsed);
    public double Price { get; private set; } = LaunchPrice;
    public double Hype { get; private set; } = 22;
    public double Trust { get; private set; }
    public double Heat { get; private set; }
    public int Holders { get; private set; } = 18;
    public double Liquidity { get; private set; }
    public int Marketing { get; private set; }
    public int TotalSpent => Phase == "lobby" ? 0 : Stake + LaunchFee + Marketing;
    public CryptoCampaign ActiveCampaign { get; private set; }
    public int CampaignRemaining { get; private set; }
    public int ExitRemaining { get; private set; }
    public CryptoReceipt Receipt { get; private set; }
    public IReadOnlyList<CryptoCandle> Candles => _candles.AsReadOnly();
    public IReadOnlyList<CryptoNews> News => _news.AsReadOnly();
    public int EffectId { get; private set; }
    public string Effect { get; private set; } = "idle";
    public string EffectText { get; private set; } = "An empire begins with parking-lot Wi-Fi.";
    public bool CanLaunch => Phase == "lobby" && _balance() >= Stake + LaunchFee;
    public double PaperValue => Stake * Price / LaunchPrice;
    // The displayed token price is the marginal price. Selling the founder bag moves it substantially.
    public int ExitQuote => Math.Max(0, (int)Math.Floor(Math.Min(Liquidity * .88,
        PaperValue * Liquidity / Math.Max(1, Liquidity + PaperValue * .5))) - ExitFee);

    public bool Rename(string name)
    {
        name = name?.Trim();
        if (Phase != "lobby" || string.IsNullOrEmpty(name) || name.Length > 24 ||
            !name.Any(char.IsAsciiLetterOrDigit) || name.Any(c => !char.IsAsciiLetterOrDigit(c) && c != ' ' && c != '-'))
            return false;
        Name = name;
        return true;
    }

    public bool SetStake(int stake)
    {
        if (Phase != "lobby" || !Stakes.Contains(stake)) return false;
        Stake = stake;
        return true;
    }

    public bool SetNarrative(string id)
    {
        var narrative = Narratives.FirstOrDefault(n => n.Id == id);
        if (Phase != "lobby" || narrative == null) return false;
        Narrative = narrative;
        return true;
    }

    public bool Launch()
    {
        if (!CanLaunch) return false;
        _changeCash(-Stake - LaunchFee);
        Phase = "live";
        Trust = Narrative.StartingTrust;
        Heat = 8 + _career.Notoriety;
        Liquidity = Stake * 1.55;
        // A minority of launches catch a favorable market. Nothing guarantees a profitable exit.
        _marketDrift = _random.NextDouble() < .2 ? .012 : -.008;
        _candles.Add(new CryptoCandle(0, Price, Price, Price, Price, 0));
        _bestExit = ExitQuote;
        AddNews("neutral", $"{Name} launches at $0.0100. Founder allocation: {Stake / LaunchPrice:N0} tokens.");
        AddNews("warning", $"${LaunchFee} launch fee paid. The chart is a promise; exit liquidity is cash.");
        Burst("launch", "WE ARE SO EARLY");
        return true;
    }

    public int Cooldown(CryptoCampaign campaign) => Math.Max(0, _readyAt.GetValueOrDefault(campaign.Id) - Elapsed);
    public bool CanCampaign(CryptoCampaign campaign) => Phase == "live" && ActiveCampaign == null &&
        Remaining > campaign.Duration && Cooldown(campaign) == 0 && _balance() >= campaign.Cost;

    public string CampaignBlock(CryptoCampaign campaign) => Phase != "live" ? "Market closed"
        : ActiveCampaign != null ? "Another campaign is running"
        : Remaining <= campaign.Duration ? "Not enough time left"
        : Cooldown(campaign) > 0 ? $"Cooling down · {Cooldown(campaign)}s"
        : _balance() < campaign.Cost ? "Not enough road cash" : "Ready";

    public bool StartCampaign(string id)
    {
        var campaign = Campaigns.FirstOrDefault(c => c.Id == id);
        if (campaign == null || !CanCampaign(campaign)) return false;
        _changeCash(-campaign.Cost);
        Marketing += campaign.Cost;
        ActiveCampaign = campaign;
        CampaignRemaining = campaign.Duration;
        _uses[id] = _uses.GetValueOrDefault(id) + 1;
        AddNews("neutral", $"{campaign.Name} started. ${campaign.Cost} spent; impact in {campaign.Duration}s.");
        Burst("work", "HYPE IN PRODUCTION");
        return true;
    }

    public bool PullOut()
    {
        if (Phase != "live") return false;
        Phase = "exiting";
        ExitRemaining = ExitSeconds;
        _requestedExit = ExitQuote;
        if (ActiveCampaign != null) AddNews("warning", "Campaign abandoned. The money was already spent.");
        ActiveCampaign = null;
        CampaignRemaining = 0;
        AddNews("warning", $"Withdrawal broadcast. Estimated ${_requestedExit}; {ExitSeconds} market ticks to settle.");
        Burst("exit", "EVERYONE CAN SEE YOUR WALLET");
        return true;
    }

    public void Tick()
    {
        if (Phase is not ("live" or "exiting")) return;
        Elapsed++;
        var open = Price;
        if (ActiveCampaign != null && --CampaignRemaining <= 0) CompleteCampaign();
        if (_botsExpireAt > 0 && Elapsed >= _botsExpireAt)
        {
            _botsExpireAt = 0;
            Hype *= .6;
            Holders = Math.Max(5, Holders - 90);
            Price *= .78;
            Liquidity *= .82;
            AddNews("bad", "The rented bot army clocks out. Engagement was not liquidity.");
            Burst("dump", "99 ACCOUNTS WENT OFFLINE");
        }
        Hype = Clamp(Hype - .65);
        Heat = Clamp(Heat + .12 + Hype * .003);
        Trust = Clamp(Trust - .08);
        var noise = (_random.NextDouble() + _random.NextDouble() - 1) * .105 * Narrative.Volatility;
        _momentum = _momentum * .55 + noise + _marketDrift + Hype * .00032 + Trust * .00009 - .013 - Elapsed * .000035;
        Price *= Math.Exp(Math.Clamp(_momentum, -.25, .22));
        Liquidity += Stake * ((Hype * .00027 + Trust * .00013) * Narrative.Audience - .012 - Heat * .00015);
        if (Elapsed % 8 == 0) MarketEvent();

        // Negatively skewed jumps make waiting dangerous even after a run of green candles.
        if (_random.NextDouble() < .003 + Heat * .00018 + Elapsed * .000025)
        {
            Price *= .25 + _random.NextDouble() * .27;
            Liquidity *= .42;
            Hype *= .6;
            AddNews("bad", "A whale sells into the pool. Your exit just got much smaller.");
            Burst("dump", "WHALE DUMP");
        }

        if (Phase == "exiting")
        {
            Price *= .94;
            Liquidity *= .97;
        }
        Price = Math.Clamp(Price, .00001, 2);
        Liquidity = Math.Clamp(Liquidity, 0, Stake * 30);
        Holders = Math.Clamp(Holders + (int)(Hype / 15) - _random.Next(0, 6), 1, 100000);
        var volume = Math.Max(1, (int)(Holders * (.08 + Math.Abs(_momentum) * 4) + _random.Next(2, 20)));
        _candles.Add(new CryptoCandle(Elapsed, open, Math.Max(open, Price) * (1 + _random.NextDouble() * .018),
            Math.Min(open, Price) * (1 - _random.NextDouble() * .018), Price, volume));
        _bestExit = Math.Max(_bestExit, ExitQuote);

        if (Phase == "exiting" && --ExitRemaining <= 0) Settle("pulled", ExitQuote);
        else if (Price <= LaunchPrice * .035 || Liquidity < 2) Settle("collapsed", 0);
        else if (Elapsed >= SessionSeconds) Settle("expired", (int)(ExitQuote * .4));
    }

    private void CompleteCampaign()
    {
        var campaign = ActiveCampaign;
        ActiveCampaign = null;
        CampaignRemaining = 0;
        _readyAt[campaign.Id] = Elapsed + campaign.Cooldown;
        var novelty = 1 / (1 + (_uses[campaign.Id] - 1) * .55);
        Hype = Clamp(Hype + campaign.Hype * novelty);
        Trust = Clamp(Trust + campaign.Trust);
        Heat = Clamp(Heat + campaign.Heat);
        Holders += (int)(campaign.Reach * novelty * Narrative.Audience);
        // Real engagement adds buyers; purchased follower counts barely add to the pool.
        Liquidity += Stake * (campaign.Id == "bots" ? .025 : campaign.Reach / 260.0 * novelty);
        Price *= 1 + campaign.Hype * .005 * novelty;
        if (campaign.Id == "bots") _botsExpireAt = Elapsed + 14;
        if (campaign.Id == "burn") Price *= 1.12;
        AddNews("good", $"{campaign.Name} lands. {(_uses[campaign.Id] > 1 ? "The crowd has seen this one before." : "The feed notices you.")}");
        Burst("pump", campaign.Id == "influencer" ? "THE FEED IS ON FIRE" : "NUMBER GO UP");
        if (campaign.Id == "influencer" && _random.NextDouble() < .45)
        {
            Price *= .62;
            Liquidity *= .7;
            AddNews("bad", "Your influencer discloses a position. By selling it.");
            Burst("dump", "SPONSORED EXIT LIQUIDITY");
        }
    }

    private void MarketEvent()
    {
        var roll = _random.NextDouble();
        if (roll < .18)
        {
            var boost = 1.15 + _random.NextDouble() * .4;
            Price *= boost;
            Liquidity += Stake * .32 * Narrative.Audience;
            Hype = Clamp(Hype + 9);
            AddNews("good", "A stranger posts a rocket emoji. Actual buyers follow for once.");
            Burst("pump", "UNSOLICITED ROCKET EMOJIS");
        }
        else if (roll < .4 || Heat > 75)
        {
            Price *= .72 + _random.NextDouble() * .15;
            Liquidity *= .85;
            AddNews("bad", Heat > 60 ? "Wallet watchers link the founder to an older coin. Holders head for the exit."
                : "An early holder takes profit. They thank you for building the community.");
            Burst("dump", "SOMEONE ELSE GOT OUT FIRST");
        }
        else if (roll < .65)
        {
            Trust = Clamp(Trust - 5);
            AddNews("warning", "Someone asks what the token actually does. The chat gets very quiet.");
        }
        else
            AddNews("neutral", "The community declares this a healthy consolidation. Nobody knows what that means.");
    }

    private void Settle(string reason, int returned)
    {
        if (Receipt != null) return;
        Phase = "result";
        ActiveCampaign = null;
        CampaignRemaining = 0;
        ExitRemaining = 0;
        _changeCash(returned);
        Receipt = new CryptoReceipt(Name, Stake, LaunchFee, Marketing, returned, returned - TotalSpent,
            _bestExit, _requestedExit, Elapsed, reason);
        _career.Record(Receipt);
        AddNews(returned > TotalSpent ? "good" : "bad", reason switch
        {
            "pulled" => $"Withdrawal settled: ${returned}. Your net: {Receipt.Net:+$0;-$0;$0}.",
            "collapsed" => "The liquidity pool collapses. Nothing comes back to the car.",
            _ => $"The listing expires. A distressed sale salvages ${returned}."
        });
        Burst(returned > TotalSpent ? "win" : "loss", returned > TotalSpent ? "YOU WERE THE EARLY ONE" : "YOU WERE THE EXIT LIQUIDITY");
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);
    private void Burst(string effect, string text) { EffectId++; Effect = effect; EffectText = text; }
    private void AddNews(string tone, string text)
    {
        _news.Insert(0, new CryptoNews(++_newsId, Elapsed, tone, text));
        if (_news.Count > 12) _news.RemoveAt(12);
    }
}
