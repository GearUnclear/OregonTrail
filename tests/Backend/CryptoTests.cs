using OregonTrailDotNet.Module.Crypto;

internal static class CryptoTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception($"Crypto: {message}");
    }

    private sealed class Wallet(int balance)
    {
        public int Balance = balance;
        public int Credits;
        public CryptoExchange Open(int seed, CryptoCareer career = null) => new(new Random(seed), () => Balance,
            delta => { Balance += delta; if (delta > 0) Credits++; Check(Balance >= 0, "wallet cannot overdraw"); },
            career ?? new CryptoCareer());
    }

    public static void Run()
    {
        var poor = new Wallet(36);
        var noFunds = poor.Open(1);
        Check(!noFunds.Launch() && poor.Balance == 36 && noFunds.Phase == "lobby", "unaffordable launch has no side effects");
        noFunds.Tick();
        Check(noFunds.Elapsed == 0 && !noFunds.PullOut(), "lobby cannot tick or cash out");
        Check(!noFunds.Rename("<script>") && !noFunds.Rename("---") && !noFunds.Rename(new string('x', 25)), "invalid names rejected");
        Check(!noFunds.SetStake(-100) && !noFunds.SetNarrative("missing"), "only advertised configuration allowed");

        var wallet = new Wallet(1000);
        var career = new CryptoCareer();
        var exchange = wallet.Open(34, career);
        Check(exchange.Rename("Gas Money-2") && exchange.Symbol == "GASMON", "safe custom coin naming");
        Check(exchange.SetStake(75) && exchange.Launch() && wallet.Balance == 913, "launch debits seed and fee once");
        Check(!exchange.Launch() && !exchange.SetStake(150) && !exchange.Rename("Late"), "live setup is immutable");
        Check(exchange.StartCampaign("memes") && wallet.Balance == 909, "campaign debits cash up front");
        Check(!exchange.StartCampaign("paper") && wallet.Balance == 909, "cannot overlap campaigns");
        for (var tick = 0; tick < 3; tick++) exchange.Tick();
        Check(exchange.ActiveCampaign?.Id == "memes", "campaign does not finish early");
        exchange.Tick();
        Check(exchange.ActiveCampaign == null && !exchange.StartCampaign("memes"), "campaign lands then cools down");
        Check(exchange.News.Any(n => n.Text.Contains("Meme barrage lands")), "completed campaigns produce feedback");
        var beforeExit = wallet.Balance;
        Check(exchange.PullOut() && !exchange.PullOut() && !exchange.StartCampaign("ama"), "withdrawal locks future orders");
        Check(wallet.Balance == beforeExit && exchange.Receipt == null, "clicking pull does not credit the quote");
        exchange.Tick();
        exchange.Tick();
        Check(exchange.Receipt == null, "exit requires three market ticks");
        exchange.Tick();
        Check(exchange.Receipt != null && exchange.Phase == "result", "exit settles");
        var receipt = exchange.Receipt;
        Check(wallet.Balance == 1000 + receipt.Net && receipt.Marketing == 4, "net accounts for every cost");
        Check(career.Launches == 1 && career.History.Count == 1 && career.Notoriety == 7, "career records a rug once");
        var settledBalance = wallet.Balance;
        for (var tick = 0; tick < 200; tick++) exchange.Tick();
        Check(wallet.Balance == settledBalance && career.Launches == 1, "late ticks cannot duplicate settlement");
        Check(exchange.Candles.Count == exchange.Elapsed + 1, "one authoritative candle per market tick");

        var emptyWallet = new Wallet(37);
        var allIn = emptyWallet.Open(2);
        Check(allIn.Launch() && !allIn.StartCampaign("memes") && emptyWallet.Balance == 0, "hype cannot spend absent road cash");
        for (var tick = 0; tick < 130; tick++) allIn.Tick();
        Check(allIn.Receipt?.Reason is "expired" or "collapsed", "abandoned launches finish without user input");

        // Replay identical actions against identical seeds to catch clocks or rendering leaking into the market.
        var twinA = new Wallet(1000).Open(423);
        var twinB = new Wallet(1000).Open(423);
        twinA.Launch(); twinB.Launch();
        for (var tick = 0; tick < 125; tick++)
        {
            twinA.Tick(); twinB.Tick();
            Check(twinA.Price == twinB.Price && twinA.ExitQuote == twinB.ExitQuote, "seeded market must be reproducible");
        }

        // Fixed seed cohorts use ordinary strategies, not hindsight. Check actual loss frequency and rare upside.
        foreach (var policy in new[] { "hold", "hype", "timed" })
        {
            var losses = 0;
            var wins = 0;
            long net = 0;
            const int count = 3000;
            for (var seed = 0; seed < count; seed++)
            {
                var funds = new Wallet(1000);
                var game = funds.Open(seed);
                game.SetStake(CryptoExchange.Stakes[seed % 3]);
                game.SetNarrative(CryptoExchange.Narratives[seed % 3].Id);
                game.Launch();
                for (var tick = 0; tick < 124; tick++)
                {
                    if (policy != "hold")
                    {
                        if (tick == 0) game.StartCampaign("memes");
                        if (tick == 9) game.StartCampaign("ama");
                        if (tick == 25) game.StartCampaign("influencer");
                        if (tick == 45 && policy == "hype") game.StartCampaign("bots");
                    }
                    if (policy == "timed" && (game.ExitQuote >= game.TotalSpent * 1.25 || tick >= 42)) game.PullOut();
                    if (policy == "hype" && tick == 70) game.PullOut();
                    game.Tick();
                    Check(double.IsFinite(game.Price) && game.Price > 0 && game.Liquidity >= 0 && game.ExitQuote >= 0,
                        "prices, liquidity and quotes remain finite and nonnegative");
                }
                Check(game.Receipt != null && funds.Balance == 1000 + game.Receipt.Net, "all simulated launches reconcile");
                Check(game.Candles.Count <= 121 && game.News.Count <= 12, "history and feed stay bounded");
                if (game.Receipt.Net < 0) losses++;
                if (game.Receipt.Net > 0) wins++;
                net += game.Receipt.Net;
            }
            Check(losses > count * .65, $"{policy}: most launches must lose money");
            if (policy != "hold") Check(wins > 0, $"{policy}: profitable exits must remain possible");
            Console.WriteLine($"PASS crypto balance / {policy}: {losses}/{count} losses ({100.0 * losses / count:F1}%), {wins} wins, mean net ${net / (double)count:F2}");
        }
        Console.WriteLine("PASS crypto validation, campaign timing, delayed exits, wallet accounting, terminal settlement, reproducibility");
    }
}
