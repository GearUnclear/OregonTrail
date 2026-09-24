using OregonTrailDotNet.Module.Crypto;
using OregonTrailDotNet.Window.Travel.Crypto;

namespace OregonTrailDotNet.Web;

public sealed record CryptoDeskDto(string Phase, string Name, string Symbol, int Stake, int LaunchFee,
    int ExitFee, int Duration, string NarrativeId, CryptoMarketDto Market, CryptoWorkDto Work,
    IReadOnlyList<CryptoFundingDto> Funding, IReadOnlyList<CryptoNarrativeDto> Narratives,
    IReadOnlyList<CryptoCampaignDto> Campaigns, IReadOnlyList<CryptoCandle> Candles, IReadOnlyList<CryptoNews> News,
    CryptoEffectDto Effect, CryptoReceipt Receipt, CryptoCareerDto Career);
public sealed record CryptoMarketDto(int Elapsed, int Remaining, double Price, double PaperValue, double Liquidity,
    int ExitQuote, int TotalSpent, double Hype, double Trust, double Heat, int Holders, int ExitRemaining);
public sealed record CryptoWorkDto(string Id, string Name, int Remaining, int Duration);
public sealed record CryptoFundingDto(int Amount, string ActionId);
public sealed record CryptoNarrativeDto(string Id, string Name, string Detail, string ActionId);
public sealed record CryptoCampaignDto(string Id, string Name, string Detail, int Cost, int Duration,
    int Cooldown, string Status, string ActionId);
public sealed record CryptoEffectDto(int Id, string Kind, string Text);
public sealed record CryptoCareerDto(int Launches, int Wins, int Net, int Notoriety, IReadOnlyList<CryptoReceipt> History);

internal static partial class PresentationCatalog
{
    private static CryptoDeskDto BuildCrypto(CryptoDesk desk)
    {
        var exchange = desk?.Exchange;
        if (exchange == null) return null;
        var work = exchange.ActiveCampaign;
        var career = desk.Career;
        return new CryptoDeskDto(exchange.Phase, exchange.Name, exchange.Symbol, exchange.Stake,
            CryptoExchange.LaunchFee, CryptoExchange.ExitFee, CryptoExchange.SessionSeconds, exchange.Narrative.Id,
            new CryptoMarketDto(exchange.Elapsed, exchange.Remaining, exchange.Price, exchange.PaperValue,
                exchange.Liquidity, exchange.ExitQuote, exchange.TotalSpent, exchange.Hype, exchange.Trust,
                exchange.Heat, exchange.Holders, exchange.ExitRemaining),
            work == null ? null : new CryptoWorkDto(work.Id, work.Name, exchange.CampaignRemaining, work.Duration),
            CryptoExchange.Stakes.Select(amount => new CryptoFundingDto(amount, $"crypto.stake.{amount}")).ToArray(),
            CryptoExchange.Narratives.Select(n => new CryptoNarrativeDto(n.Id, n.Name, n.Detail, $"crypto.narrative.{n.Id}")).ToArray(),
            CryptoExchange.Campaigns.Select(c => new CryptoCampaignDto(c.Id, c.Name, c.Pitch, c.Cost, c.Duration,
                exchange.Cooldown(c), exchange.CampaignBlock(c), $"crypto.hype.{c.Id}")).ToArray(),
            exchange.Candles.ToArray(), exchange.News.ToArray(),
            new CryptoEffectDto(exchange.EffectId, exchange.Effect, exchange.EffectText), exchange.Receipt,
            new CryptoCareerDto(career.Launches, career.Wins, career.Net, career.Notoriety, career.History.ToArray()));
    }

    private static List<GameActionDto> BuildCryptoActions(CryptoDesk desk,
        IDictionary<string, PresentationActionBinding> bindings)
    {
        var actions = new List<GameActionDto>();
        var exchange = desk?.Exchange;
        if (exchange == null) return actions;
        void Add(string command, string label, bool enabled = true, bool selected = false,
            string kind = "select", InputSpecDto input = null)
        {
            var id = $"crypto.{command}";
            actions.Add(new GameActionDto(id, label, kind, enabled, selected));
            if (enabled) bindings[id] = new PresentationActionBinding(PresentationActionKind.Crypto, command, Input: input);
        }

        if (exchange.Phase == "lobby")
        {
            Add("rename", "Name your coin", input: new InputSpecDto("text", "Coin name", "PotholeCoin",
                true, null, null, 24, "crypto.rename"));
            foreach (var stake in CryptoExchange.Stakes)
                Add($"stake.{stake}", $"${stake} seed capital", selected: exchange.Stake == stake);
            foreach (var narrative in CryptoExchange.Narratives)
                Add($"narrative.{narrative.Id}", narrative.Name, selected: exchange.Narrative.Id == narrative.Id);
            Add("launch", $"Launch coin · ${exchange.Stake + CryptoExchange.LaunchFee}", exchange.CanLaunch, kind: "primary");
        }
        else if (exchange.Phase == "live")
        {
            Add("pull-out", "PULL OUT / RUG IT", kind: "primary");
            foreach (var campaign in CryptoExchange.Campaigns)
                Add($"hype.{campaign.Id}", campaign.Name, exchange.CanCampaign(campaign));
        }
        if (exchange.Phase is "lobby" or "result")
            Add("leave", exchange.Phase == "result" ? "Back to the road · spend 1 day" : "Close exchange", kind: "secondary");
        return actions;
    }
}
