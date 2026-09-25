using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Item;
using OregonTrailDotNet.Module.Scoring;
using OregonTrailDotNet.Window.GameOver;
using OregonTrailDotNet.Window.Graveyard;
using OregonTrailDotNet.Window.MainMenu;
using OregonTrailDotNet.Window.RandomEvent;
using OregonTrailDotNet.Window.Travel;
using OregonTrailDotNet.Window.Travel.Store;

namespace OregonTrailDotNet.Web
{
    internal enum PresentationActionKind
    {
        FoodGrab,
        Submit,
        Select,
        AdjustLeft,
        AdjustRight,
        StoreAdjust,
        StoreSet,
        SkipIntro,
        PrepareDeparture,
        Crypto,
        Creator,
        CreatorOpen,
        Restart
    }

    internal sealed record PresentationActionBinding(
        PresentationActionKind Kind,
        string LegacyValue = null,
        int MenuIndex = -1,
        Entities? StoreItem = null,
        int Delta = 0,
        InputSpecDto Input = null);

    internal sealed record PresentationFrame(
        GameSnapshotDto Snapshot,
        IReadOnlyDictionary<string, PresentationActionBinding> Bindings,
        string SurfaceKey,
        object CurrentForm);

    /// <summary>
    ///     Adapts the focused legacy form and authoritative domain objects into a semantic presentation union. It never reads
    ///     the scene graph, calls a render method, or parses a terminal frame. Legacy input values are retained only in the
    ///     private action bindings returned to <see cref="GameSession" />.
    /// </summary>
    internal static partial class PresentationCatalog
    {
        private static readonly Regex LeadingNumber = new Regex(@"^\s*\d+\.\s*", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex CurrencySymbolBeforeNumber =
            new Regex(@"\p{Sc}(?=\s*\d)", RegexOptions.Compiled);

        public static PresentationFrame Build(GameSimulationApp game, long revision)
        {
            if (game == null)
                return Ended(revision);

            var focusedWindow = Property(game.WindowManager, "FocusedWindow");
            var currentForm = Property(focusedWindow, "CurrentForm") ?? Property(focusedWindow, "Form");
            var userData = Property(focusedWindow, "UserData") ?? Property(currentForm, "UserData");
            var formType = currentForm?.GetType();
            var windowType = focusedWindow?.GetType();
            var simpleName = formType?.Name ?? windowType?.Name ?? "PreparingTrip";
            var fullName = formType?.FullName ?? windowType?.FullName ?? simpleName;
            var screenId = Slug(fullName);

            var descriptor = Describe(simpleName, fullName, userData, currentForm, game);
            var input = ResolveInput(simpleName, currentForm, game.ActiveMenu, descriptor.Input, screenId);
            var bindings = new Dictionary<string, PresentationActionBinding>(StringComparer.Ordinal);
            var cryptoDesk = currentForm as Window.Travel.Crypto.CryptoDesk;
            var creatorDesk = currentForm as Window.Travel.Creator.CreatorDesk;
            var sweep = (currentForm as Window.Travel.Hunt.Hunting)?.Sweep;
            var actions = sweep != null
                ? BuildFoodActions(sweep, bindings)
                : creatorDesk != null
                ? BuildCreatorActions(creatorDesk, bindings)
                : cryptoDesk != null
                ? BuildCryptoActions(cryptoDesk, bindings)
                : descriptor.Kind == "store"
                ? BuildStoreActions(game, bindings)
                : simpleName == "DoorDash"
                    ? BuildDoorDashActions(userData as TravelInfo, bindings)
                : BuildActions(game, currentForm, screenId, input, bindings);
            var store = descriptor.Kind == "store"
                ? BuildStore(userData as TravelInfo, game, currentForm, bindings)
                : null;

            // Store row controls are first-class actions too. Keeping them in Screen.Actions lets clients use one dispatch
            // mechanism, while Store.Rows provides the table-shaped composition metadata.
            if (store != null)
            {
                foreach (var row in store.Rows)
                {
                    actions.Add(new GameActionDto(
                        row.DecreaseActionId,
                        $"Decrease {row.Name}",
                        "adjust",
                        bindings.ContainsKey(row.DecreaseActionId),
                        false));
                    actions.Add(new GameActionDto(
                        row.IncreaseActionId,
                        $"Increase {row.Name}",
                        "adjust",
                        bindings.ContainsKey(row.IncreaseActionId),
                        false));
                    actions.Add(new GameActionDto(
                        row.SetActionId,
                        $"Set {row.Name} quantity",
                        "set-value",
                        bindings.ContainsKey(row.SetActionId),
                        false));

                    // The browser renders row steppers, but a trip needs many days of food and fuel.
                    // Offer larger semantic increments so stocking the car does not require hundreds of clicks.
                    var bulk = row.ItemId switch
                    {
                        "snacks" => (Amount: 50, Item: Entities.Food),
                        "gas" => (Amount: 5, Item: Entities.Animal),
                        "ammunition" => (Amount: 10, Item: Entities.Ammo),
                        _ => (Amount: 0, Item: Entities.Cash)
                    };
                    if (bulk.Amount > 0 && row.Quantity < row.MaxQuantity)
                    {
                        var bulkActionId = $"store.{row.ItemId}.add-{bulk.Amount}";
                        actions.Add(new GameActionDto(
                            bulkActionId,
                            $"Add up to {bulk.Amount} {row.Name.ToLowerInvariant()}",
                            "bulk",
                            true,
                            false));
                        bindings[bulkActionId] = new PresentationActionBinding(
                            PresentationActionKind.StoreAdjust,
                            StoreItem: bulk.Item,
                            Delta: bulk.Amount);
                    }
                }
            }

            var snapshot = new GameSnapshotDto(
                revision,
                true,
                BuildHud(game, userData as NewGameInfo),
                BuildParty(game),
                BuildInventory(game),
                BuildProgress(game, simpleName, currentForm, userData),
                store,
                new GameScreenDto(
                    descriptor.Kind,
                    screenId,
                    descriptor.Title,
                    descriptor.Description,
                    input,
                    actions,
                    (currentForm as GameIntro)?.SemanticStoryPages,
                    BuildLeaderboard(game, simpleName)),
                BuildScore(userData as GameOverInfo),
                BuildDriving(game, currentForm),
                Crypto: BuildCrypto(cryptoDesk),
                Creator: BuildCreator(creatorDesk, game),
                FoodSweep: sweep == null ? null : new FoodSweepDto(sweep.Round, Window.Travel.Hunt.HuntManager.TrayCount,
                    sweep.TrayName, sweep.TrayPounds, sweep.ZoneStart, sweep.ZoneEnd,
                    Window.Travel.Hunt.HuntManager.PassMilliseconds, sweep.KillWeight, sweep.Feedback,
                    sweep.Resolved, $"food.grab.{sweep.Round}"));

            // Revision describes the entire semantic snapshot, not only the action surface. This lets timed activity,
            // inventory, health, and route progress update incrementally even while the focused form stays the same. The
            // revision itself is zeroed before serialization so it does not make its own fingerprint unstable.
            var surfaceKey = JsonSerializer.Serialize(snapshot with { Revision = 0 });

            return new PresentationFrame(snapshot, bindings, surfaceKey, currentForm);
        }

        private static List<GameActionDto> BuildFoodActions(Window.Travel.Hunt.HuntManager sweep,
            IDictionary<string, PresentationActionBinding> bindings)
        {
            var id = $"food.grab.{sweep.Round}";
            if (!sweep.Resolved)
                bindings[id] = new PresentationActionBinding(PresentationActionKind.FoodGrab,
                    Input: new InputSpecDto("number", "Grab time", "", true, 0,
                        Window.Travel.Hunt.HuntManager.PassMilliseconds, null));
            return new List<GameActionDto> { new GameActionDto(id, "Grab!", "primary", !sweep.Resolved, false) };
        }

        private static DrivingStateDto BuildDriving(GameSimulationApp game, object currentForm)
        {
            var vehicle = game.Vehicle;
            var driving = currentForm is Window.Travel.Command.ContinueOnTrail &&
                          vehicle?.Status == Entity.Vehicle.VehicleStatus.Moving && vehicle.BrokenPart == null;
            var status = driving ? "driving" : vehicle?.BrokenPart != null || vehicle?.Status == Entity.Vehicle.VehicleStatus.Disabled
                ? "disabled" : currentForm is Window.Travel.Rest.Resting ? "resting" : "parked";
            return new DrivingStateDto(driving, status, vehicle?.Model?.Choice.ToString().ToLowerInvariant(),
                game.Trail?.NextLocation?.Name ?? string.Empty, game.Trail?.DistanceToNextLocation ?? 0,
                vehicle?.Pace.ToString() ?? string.Empty);
        }

        private static List<GameActionDto> BuildStoreActions(
            GameSimulationApp game,
            IDictionary<string, PresentationActionBinding> bindings)
        {
            var actions = new List<GameActionDto>();
            var menu = game.ActiveMenu;
            if (!(menu?.HasOptions ?? false))
                return actions;

            for (var index = 0; index < menu.Options.Count; index++)
            {
                var option = menu.Options[index];
                if (!string.Equals(option.Value, "leave", StringComparison.OrdinalIgnoreCase))
                    continue;

                const string actionId = "store.checkout";
                actions.Add(new GameActionDto(actionId, "Leave store", "primary", option.Enabled, false));
                if (option.Enabled)
                {
                    bindings[actionId] = new PresentationActionBinding(
                        PresentationActionKind.Select,
                        option.Value,
                        index);
                }
                break;
            }

            return actions;
        }

        private static List<GameActionDto> BuildDoorDashActions(
            TravelInfo travelInfo,
            IDictionary<string, PresentationActionBinding> bindings)
        {
            var actions = new List<GameActionDto>();
            var shift = travelInfo?.DoorDash;
            if (shift == null || shift.ShouldEndShift)
                return actions;

            if (shift.HasOffer)
            {
                const string acceptId = "activity.doordash.accept";
                const string rejectId = "activity.doordash.reject";
                actions.Add(new GameActionDto(acceptId, "Accept offer", "primary", true, false));
                actions.Add(new GameActionDto(rejectId, "Reject offer", "secondary", true, false));
                bindings[acceptId] = new PresentationActionBinding(PresentationActionKind.Submit, "accept");
                bindings[rejectId] = new PresentationActionBinding(PresentationActionKind.Submit, "reject");
            }

            const string clockOutId = "activity.doordash.clock-out";
            actions.Add(new GameActionDto(clockOutId, "Clock out", "secondary", true, false));
            bindings[clockOutId] = new PresentationActionBinding(PresentationActionKind.Submit, "quit");
            return actions;
        }

        private static PresentationFrame Ended(long revision)
        {
            const string actionId = "game.restart";
            var actions = new[] { new GameActionDto(actionId, "Start a new journey", "restart", true, false) };
            var bindings = new Dictionary<string, PresentationActionBinding>(StringComparer.Ordinal)
            {
                [actionId] = new PresentationActionBinding(PresentationActionKind.Restart)
            };
            var snapshot = new GameSnapshotDto(
                revision,
                false,
                EmptyHud(),
                Array.Empty<PartyMemberDto>(),
                Array.Empty<InventoryItemDto>(),
                new GameProgressDto(0, 0, 0, string.Empty, string.Empty, null, null, 0,
                    Array.Empty<RouteStopDto>()),
                null,
                new GameScreenDto(
                    "game-over",
                    "game-over",
                    "End of the road",
                    "This trip has ended. Start a new journey when you are ready.",
                    null,
                    actions),
                null);
            return new PresentationFrame(snapshot, bindings, "game-over|game.restart", null);
        }

        private static GameHudDto EmptyHud()
        {
            return new GameHudDto(
                string.Empty, 0, 0, string.Empty, string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty, 0, 0, 0, 0, 0, null);
        }

        private static GameScoreDto BuildScore(GameOverInfo info)
        {
            if (info?.FinalScore == null)
                return null;

            return new GameScoreDto(
                info.BasePoints,
                info.ChoiceScoreDelta,
                info.Multiplier,
                info.FinalScore.Points,
                info.FinalScore.Rating,
                info.ScoreLines?.Select(line => new ScoreLineDto(
                    line.Quantity, line.Description, line.Points)).ToArray() ?? Array.Empty<ScoreLineDto>(),
                info.Epilogue ?? Array.Empty<string>());
        }

        private static IReadOnlyList<LeaderboardEntryDto> BuildLeaderboard(GameSimulationApp game, string formName)
        {
            var scores = formName switch
            {
                "CurrentTopTen" => game.Scoring.TopTen,
                "OriginalTopTen" => ScoringModule.DefaultTopTen,
                _ => null
            };
            return scores?.Select(score => new LeaderboardEntryDto(
                score.Name, score.Points, score.Rating)).ToArray();
        }

        private static GameHudDto BuildHud(GameSimulationApp game, NewGameInfo setup)
        {
            var vehicle = game.Vehicle;
            var model = setup != null ? Entity.Vehicle.VehicleModels.Get(setup.VehiclePick) : vehicle?.Model;
            var location = game.Trail?.CurrentLocation;
            var foodPerDay = vehicle?.FoodPerDay ?? 0m;
            var foodQuantity = vehicle?.FoodPoundsRemaining ?? 0m;
            return new GameHudDto(
                game.Time?.Date?.ToString() ?? string.Empty,
                game.TotalTurns,
                Money(vehicle?.Balance ?? 0),
                model?.Name ?? "Choose a ride",
                vehicle?.Status.ToString() ?? "Stopped",
                location?.Name ?? "Preparing route",
                Humanize(location?.Status.ToString() ?? "Unreached"),
                Humanize(location?.Weather.ToString() ?? string.Empty),
                Humanize(vehicle?.PassengerHealthStatus.ToString() ?? string.Empty),
                Humanize(vehicle?.Pace.ToString() ?? string.Empty),
                Humanize(vehicle?.Ration.ToString() ?? string.Empty),
                vehicle?.CargoWeight ?? 0,
                model?.CargoCapacity ?? 0,
                vehicle?.PassengerLivingCount ?? 0,
                foodPerDay,
                foodPerDay > 0 ? (int)(foodQuantity / foodPerDay) : 0,
                vehicle?.BrokenPart?.Name,
                model?.Choice.ToString().ToLowerInvariant(),
                foodQuantity);
        }

        private static IReadOnlyList<PartyMemberDto> BuildParty(GameSimulationApp game)
        {
            return game.Vehicle?.Passengers
                .Select(person => new PartyMemberDto(
                    person.Name,
                    person.Leader,
                    ProfessionName(person.Profession.ToString()),
                    Humanize(person.HealthStatus.ToString())))
                .ToArray() ?? Array.Empty<PartyMemberDto>();
        }

        private static IReadOnlyList<InventoryItemDto> BuildInventory(GameSimulationApp game)
        {
            return game.Vehicle?.Inventory
                .Where(pair => pair.Key != Entities.Cash)
                .OrderBy(pair => pair.Key)
                .Select(pair => new InventoryItemDto(
                    ItemId(pair.Key),
                    pair.Value.Name,
                    pair.Value.Quantity,
                    pair.Value.DelineatingUnit,
                    pair.Value.Weight,
                    pair.Value.TotalWeight))
                .ToArray() ?? Array.Empty<InventoryItemDto>();
        }

        private static GameProgressDto BuildProgress(
            GameSimulationApp game,
            string simpleName,
            object currentForm,
            object userData)
        {
            var activityLabel = string.Empty;
            int? activityCurrent = null;
            int? activityTotal = null;

            if (simpleName == "Hunting" && userData is TravelInfo huntInfo && huntInfo.Hunt != null)
            {
                activityLabel = "Trays";
                activityCurrent = huntInfo.Hunt.Round;
                activityTotal = Window.Travel.Hunt.HuntManager.TrayCount;
            }
            else if (simpleName == "DoorDash" && userData is TravelInfo dashInfo && dashInfo.DoorDash != null)
            {
                var remaining = Field<int>(dashInfo.DoorDash, "_secondsRemaining");
                activityLabel = "Delivery shift";
                activityCurrent = Math.Clamp(Window.Travel.DoorDash.DoorDashManager.SHIFTTIME - remaining, 0,
                    Window.Travel.DoorDash.DoorDashManager.SHIFTTIME);
                activityTotal = Window.Travel.DoorDash.DoorDashManager.SHIFTTIME;
            }
            else if (simpleName == "CrossingTick" && userData is TravelInfo crossingInfo && crossingInfo.River != null)
            {
                activityLabel = "River crossing";
                activityCurrent = Field<int>(currentForm, "_riverCrossingOfTotalWidth");
                activityTotal = crossingInfo.River.RiverWidth;
            }
            else if (simpleName == "Resting" && userData is TravelInfo restInfo)
            {
                var rested = Field<int>(currentForm, "_daysRested");
                activityLabel = "Resting";
                activityCurrent = rested;
                activityTotal = rested + Math.Max(0, restInfo.DaysToRest);
            }
            else if (simpleName == "EventSkipDay" && userData is RandomEventInfo eventInfo)
            {
                activityLabel = "Delay";
                activityCurrent = Math.Max(0, eventInfo.DaysToSkip);
            }

            var trail = game.Trail;
            return new GameProgressDto(
                game.Vehicle?.Odometer ?? 0,
                trail?.Length ?? 0,
                trail?.DistanceToNextLocation ?? 0,
                trail?.NextLocation?.Name ?? trail?.CurrentLocation?.Name ?? string.Empty,
                activityLabel,
                activityCurrent,
                activityTotal,
                trail?.LocationIndex ?? 0,
                trail?.Locations.Select(location => new RouteStopDto(
                    location.Name,
                    Slug(Humanize(location.GetType().Name)))).ToArray() ?? Array.Empty<RouteStopDto>());
        }

        private static StoreDto BuildStore(
            TravelInfo travelInfo,
            GameSimulationApp game,
            object currentForm,
            IDictionary<string, PresentationActionBinding> bindings)
        {
            if (travelInfo?.Store == null || game.Vehicle?.Model == null)
                return null;

            var menu = game.ActiveMenu;
            var rows = new List<StoreRowDto>();
            foreach (var pair in travelInfo.Store.Transactions.OrderBy(pair => pair.Key))
            {
                if (pair.Key == Entities.Cash)
                    continue;

                var item = pair.Value;
                var template = StoreTemplate(currentForm, pair.Key, item);
                var maxQuantity = StoreMaximum(currentForm, pair.Key, template, item.MaxQuantity);
                var itemId = ItemId(pair.Key);
                var decreaseId = $"store.{itemId}.decrease";
                var increaseId = $"store.{itemId}.increase";
                var setId = $"store.{itemId}.set";
                if (item.Quantity > 0)
                {
                    bindings[decreaseId] = new PresentationActionBinding(
                        PresentationActionKind.StoreAdjust, StoreItem: pair.Key, Delta: -1);
                }
                if (item.Quantity < maxQuantity)
                {
                    bindings[increaseId] = new PresentationActionBinding(
                        PresentationActionKind.StoreAdjust, StoreItem: pair.Key, Delta: 1);
                }
                if (maxQuantity > 0 || item.Quantity > 0)
                {
                    bindings[setId] = new PresentationActionBinding(
                        PresentationActionKind.StoreSet,
                        StoreItem: pair.Key,
                        Input: new InputSpecDto("number", $"{item.Name} quantity", "0", true, 0,
                            maxQuantity, null, setId));
                }

                var selected = false;
                if (menu?.HasOptions ?? false)
                {
                    var selectedValue = menu.Options[menu.SelectedIndex].Value;
                    selected = string.Equals(selectedValue, pair.Key.ToString(), StringComparison.OrdinalIgnoreCase);
                }

                rows.Add(new StoreRowDto(
                    itemId,
                    item.Name,
                    item.Quantity,
                    Money(template.Cost),
                    Money(item.Quantity * template.Cost),
                    0,
                    maxQuantity,
                    selected,
                    decreaseId,
                    increaseId,
                    setId));
            }

            return new StoreDto(
                game.Trail?.CurrentLocation?.Name ?? "Travel center",
                Money(game.Vehicle.Balance),
                Money(travelInfo.Store.TotalTransactionCost),
                game.Vehicle.CargoWeight + travelInfo.Store.PendingCargoWeight,
                game.Vehicle.Model.CargoCapacity,
                rows);
        }

        private static SimItem StoreTemplate(object currentForm, Entities item, SimItem fallback)
        {
            var method = currentForm?.GetType().GetMethod(
                "GetTemplate",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(Entities) },
                null);
            return method?.Invoke(null, new object[] { item }) as SimItem ?? fallback;
        }

        private static int StoreMaximum(object currentForm, Entities item, SimItem template, int fallback)
        {
            var method = currentForm?.GetType().GetMethod(
                "ComputeMaxQuantity",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(Entities), typeof(SimItem) },
                null);
            return method?.Invoke(currentForm, new object[] { item, template }) is int maximum
                ? maximum
                : fallback;
        }

        private static List<GameActionDto> BuildActions(
            GameSimulationApp game,
            object currentForm,
            string screenId,
            InputSpecDto input,
            IDictionary<string, PresentationActionBinding> bindings)
        {
            var actions = new List<GameActionDto>();
            if (currentForm is Window.Travel.Trade.Trading { SemanticCanTrade: true } trading && trading.SemanticOffer != null)
            {
                foreach (var (suffix, label, value) in new[]
                         { ("accept", "Accept the trade", "y"), ("decline", "Pass on the offer", "n") })
                {
                    var actionId = $"trade.{suffix}";
                    actions.Add(new GameActionDto(actionId, label, "select", true, false));
                    bindings[actionId] = new PresentationActionBinding(PresentationActionKind.Submit, value);
                }
                return actions;
            }
            if (currentForm is GameIntro)
            {
                const string actionId = "setup.begin-choices";
                actions.Add(new GameActionDto(actionId, "Choose your background", "primary", true, false));
                bindings[actionId] = new PresentationActionBinding(PresentationActionKind.SkipIntro);
                return actions;
            }

            if (currentForm?.GetType().Name == "InitialItemsHelp")
            {
                const string actionId = "setup.pack-car";
                actions.Add(new GameActionDto(actionId, "Pack the car at home", "primary", true, false));
                bindings[actionId] = new PresentationActionBinding(PresentationActionKind.PrepareDeparture);
                return actions;
            }

            var menu = game.ActiveMenu;
            if (menu?.HasOptions ?? false)
            {
                for (var index = 0; index < menu.Options.Count; index++)
                {
                    var option = menu.Options[index];
                    var actionId = $"{screenId}.select.{index}";
                    if (currentForm is TravelMenu travelMenu && travelMenu.IsCreatorOption(index))
                        actionId = "creator.open";
                    var enabled = SemanticActionEnabled(game, currentForm, index, option.Enabled);
                    actions.Add(new GameActionDto(
                        actionId,
                        SemanticActionLabel(currentForm?.GetType().Name, index, MenuLabel(option.Text)),
                        SemanticActionType(currentForm?.GetType().Name, index),
                        enabled,
                        index == menu.SelectedIndex,
                        SemanticActionDetail(currentForm?.GetType().Name, index),
                        SemanticActionFacts(currentForm?.GetType().Name, index),
                        SemanticActionGroup(currentForm?.GetType().Name, index),
                        currentForm?.GetType().Name == "VehicleSelector" && index < 4
                            ? ((Entity.Vehicle.VehicleChoice) (index + 1)).ToString().ToLowerInvariant()
                            : null));
                    if (enabled)
                    {
                        bindings[actionId] = new PresentationActionBinding(
                            PresentationActionKind.Select,
                            option.Value,
                            index);
                    }
                }

                if (game.OnLeftPressed != null)
                {
                    var actionId = $"{screenId}.adjust.decrease";
                    actions.Add(new GameActionDto(actionId, "Decrease selection", "adjust", true, false));
                    bindings[actionId] = new PresentationActionBinding(PresentationActionKind.AdjustLeft);
                }

                if (game.OnRightPressed != null)
                {
                    var actionId = $"{screenId}.adjust.increase";
                    actions.Add(new GameActionDto(actionId, "Increase selection", "adjust", true, false));
                    bindings[actionId] = new PresentationActionBinding(PresentationActionKind.AdjustRight);
                }

                return actions;
            }

            var allowInput = BoolProperty(currentForm, "AllowInput", true);
            if (!allowInput)
                return actions;

            if (currentForm is Window.Travel.Command.ContinueOnTrail)
            {
                actions.Add(new GameActionDto("creator.open", "Pull over / YouTube travel channel", "secondary", true, false));
                bindings["creator.open"] = new PresentationActionBinding(PresentationActionKind.CreatorOpen);
            }

            if (input != null)
            {
                var actionId = input.ActionId;
                actions.Add(new GameActionDto(actionId, "Submit", "submit", true, false));
                bindings[actionId] = new PresentationActionBinding(PresentationActionKind.Submit, Input: input);
            }
            else
            {
                var actionId = $"{screenId}.continue";
                var label = currentForm?.GetType().Name == "ContinueOnTrail"
                    ? "Stop driving and check status"
                    : "Continue";
                actions.Add(new GameActionDto(actionId, label, "continue", true, false));
                bindings[actionId] = new PresentationActionBinding(PresentationActionKind.Submit, string.Empty);
            }

            return actions;
        }

        private static InputSpecDto ResolveInput(
            string simpleName,
            object currentForm,
            OregonTrailDotNet.UI.ArrowMenu menu,
            InputSpecDto declared,
            string screenId)
        {
            if (menu?.HasOptions ?? false)
                return null;

            var actionId = $"{screenId}.submit";
            if (declared != null)
                return declared with { ActionId = actionId };

            // These forms use the legacy input buffer only as an acknowledgement/page-turn, never as user-authored text.
            if (simpleName is "GameIntro" or "DoorDash" or "Trading")
                return null;

            return BoolProperty(currentForm, "InputFillsBuffer", false)
                ? new InputSpecDto("text", "Response", "Type your response", false, null, null, 120, actionId)
                : null;
        }

        private static ScreenDescriptor Describe(
            string name,
            string fullName,
            object userData,
            object currentForm,
            GameSimulationApp game)
        {
            var location = game.Trail?.CurrentLocation?.Name ?? "the road";
            var nextLocation = game.Trail?.NextLocation?.Name ?? "Seattle";
            var input = InputFor(name, userData);

            return name switch
            {
                "MainMenuScreen" => new ScreenDescriptor("setup", "The Asphalt Trail",
                    "Plan a new road trip, review the guide, or manage local records.", null),
                "GameIntro" => IntroDescriptor(currentForm as GameIntro),
                "ProfessionSelector" => new ScreenDescriptor("setup", "Choose your background",
                    "Your background determines starting cash and the score multiplier at the end of the trip.", null),
                "VehicleSelector" => new ScreenDescriptor("setup", "Choose a ride",
                    "Vehicle price, seating, cargo capacity, speed, and fuel efficiency shape the whole journey.", null),
                "InputPlayerNames" => new ScreenDescriptor("setup", "Name your party",
                    NamePrompt(userData as NewGameInfo, game), input),
                "ConfirmPlayerNames" => new ScreenDescriptor("setup", "Confirm your party",
                    "Review the passengers who will make the trip.", null),
                "SelectStartingMonthState" or "StartingMonth" => new ScreenDescriptor("setup", "Choose a departure month",
                    "Departure month changes weather and conditions along the route.", null),
                "CurrentTopTen" => new ScreenDescriptor("status", "Clout leaderboard",
                    "The current top ten, including journeys finished in this session.", null),
                "OriginalTopTen" => new ScreenDescriptor("status", "Original leaderboard",
                    "The default road trip scores before this session's journeys.", null),
                "MainMenu" => new ScreenDescriptor("setup", "The Asphalt Trail", "Plan a new journey.", null),
                "RulesHelp" => new ScreenDescriptor("dialog", "How the road works",
                    $"The drive from Cape Coral to Seattle covers about {game.Trail?.Length ?? 0:N0} miles. Gas keeps the vehicle moving; snacks protect party health; ammunition handles trouble; leggings pay guides; and spare tires, alternators, and transmissions repair breakdowns.", null),
                "ProfessionHelp" => new ScreenDescriptor("dialog", "Choose the life behind the wheel",
                    "The crypto bro starts with the most cash and the smallest score multiplier. The DoorDash driver has middling funds and earns double clout at Seattle. The faith-walk streamer starts poorest, faces the hardest road, and earns triple clout for arriving.", null),
                "VehicleHelp" => new ScreenDescriptor("dialog", "Compare the rides",
                    "The minivan is balanced and seats four. The pickup carries the most but is slow and thirsty. The hybrid is quick and efficient with three seats. The EV hatchback is fastest and most efficient, but has the least cargo room and also seats three.", null),
                "StartMonthHelp" => new ScreenDescriptor("dialog", "When to leave",
                    "Leave too early and heat domes and fuel shortages are still severe. Leave too late and mountain passes may ice over before Seattle. A middle departure balances clear roads with cooler weather.", null),
                "InitialItemsHelp" => new ScreenDescriptor("dialog", "Prepare before leaving",
                    $"Pack the car at home, then stock up before leaving Cape Coral. The party has {Usd(game.Vehicle?.Balance ?? 0)}, but does not need to spend it all at the first travel center.", null),
                "StoreHelp" => new ScreenDescriptor("dialog", "Shopping at the travel center",
                    "Use each row's quantity controls to build a receipt. Cash and cargo limits update immediately; supplies are loaded when the party leaves the store.", null),
                "PointsDistributionHelp" => new ScreenDescriptor("dialog", "Party score",
                    "Every person who reaches Seattle earns clout, and a healthy arrival is worth more.", null),
                "PointsAwardHelp" => new ScreenDescriptor("dialog", "Supply score",
                    "Supplies carried safely to Seattle add to the final clout score and help the party start over in the new city.", null),
                "PointsMultiplyerHelp" => new ScreenDescriptor("dialog", "Background multiplier",
                    "Harder starting circumstances multiply the final score: DoorDash drivers earn double clout and faith-walk streamers earn triple clout on arrival.", null),

                "Store" => new ScreenDescriptor("store", $"{location} Travel Center",
                    "At filling rations, each traveler eats 3 lb of snacks per travel day. Use the bulk buttons to pack for several days, then leave the store to purchase within your cash and cargo limits.", null),
                "StoreWelcome" => new ScreenDescriptor("dialog", "Stock up for the road",
                    StoreWelcomeDescription(currentForm, location), null),
                "StoreDebtWarning" => new ScreenDescriptor("dialog", "That receipt costs too much",
                    "Reduce the pending quantities before checking out.", null),
                "RequiredItem" => new ScreenDescriptor("dialog", "A required supply is missing",
                    RequiredItemDescription(userData as TravelInfo), null),

                "TravelMenu" => new ScreenDescriptor("travel", location,
                    game.Trail?.CurrentLocation?.Status.ToString() == "Departed"
                        ? $"You are {game.Trail.DistanceToNextLocation:N0} miles from {nextLocation}."
                        : "Choose what the party should do before the next leg.", null),
                "ContinueOnTrail" => new ScreenDescriptor("activity", $"Driving toward {nextLocation}",
                    "The party is moving. Size up the situation at any time to pull over and review the journey.", null),
                "TalkToPeople" => new ScreenDescriptor("dialog", "A voice from the roadside",
                    ConversationDescription(currentForm as Window.Travel.TalkToPeople.TalkToPeople), null),
                "Trading" => new ScreenDescriptor("choice", "The parking-lot economy",
                    TradeDescription(currentForm as Window.Travel.Trade.Trading), null),
                "TollRoadQuestion" => new ScreenDescriptor("choice", "The price of getting through",
                    TollDescription(userData as TravelInfo, game), null),
                "CheckSupplies" => new ScreenDescriptor("status", "Supplies",
                    "Review every item currently loaded in the vehicle.", null),
                "LookAtMap" => new ScreenDescriptor("status", "Route progress",
                    $"The party has reached {game.Trail?.LocationIndex + 1 ?? 0} of {game.Trail?.Locations.Count ?? 0} route locations.", null),
                "ChangePace" => new ScreenDescriptor("choice", "Change travel pace",
                    $"The current pace is {game.Vehicle?.Pace}. Faster travel puts more strain on the party.", null),
                "ChangeRations" => new ScreenDescriptor("choice", "Change rations",
                    $"The current ration level is {game.Vehicle?.Ration}. Smaller meals conserve food at a health cost.", null),
                "PaceHelp" => new ScreenDescriptor("dialog", "Travel pace guide",
                    "Steady travel makes reliable progress in roughly eight hours without extra fatigue. Strenuous travel means about twelve hours and greater fatigue. Grueling travel pushes toward sixteen hours, little sleep, and serious health costs.", null),
                "RestAmount" => new ScreenDescriptor("activity", "Rest the party",
                    "Choose between zero and nine days to rest.", input),
                "Resting" => new ScreenDescriptor("activity", "Resting",
                    $"The party is recovering. {Math.Max(0, (userData as TravelInfo)?.DaysToRest ?? 0)} days remain.", null),

                "RiverCross" => new ScreenDescriptor("river", $"Crossing at {location}",
                    RiverDescription(userData as TravelInfo), null),
                "CrossingTick" => new ScreenDescriptor("activity", "Crossing the washed-out roadbed",
                    RiverDescription(userData as TravelInfo), null),
                "CrossingResult" => new ScreenDescriptor("river", "Crossing complete",
                    RiverDescription(userData as TravelInfo), null),
                "UseFerryConfirm" or "FerryHelp" or "FerryNoMonies" => new ScreenDescriptor("river", "Ferry crossing",
                    RiverDescription(userData as TravelInfo), null),
                "IndianGuidePrompt" or "UseIndianConfirm" => new ScreenDescriptor("river", "Guided crossing",
                    RiverDescription(userData as TravelInfo), null),
                "FordRiverHelp" or "CaulkRiverHelp" or "RiverCrossHelp" => new ScreenDescriptor("river", "Crossing guide",
                    RiverDescription(userData as TravelInfo), null),

                "Hunting" => new ScreenDescriptor("activity", "Food sweep",
                    HuntingDescription(userData as TravelInfo), input),
                "HuntingResult" => new ScreenDescriptor("activity", "Food sweep results",
                    HuntingResultDescription(userData as TravelInfo), null),

                "DoorDash" => new ScreenDescriptor("activity", "Delivery shift",
                    DoorDashDescription(userData as TravelInfo), null),
                "CryptoDesk" => new ScreenDescriptor("crypto", "RUG.RUN",
                    "Launch a coin. Manufacture a movement. Try to leave before everyone else.", null),
                "CreatorDesk" => new ScreenDescriptor("creator", "DEAD AIR",
                    "A YouTube travel channel. A family on the road. An equipment budget with no adult supervision.", null),
                "DoorDashPrompt" => new ScreenDescriptor("dialog", "Drive for delivery apps",
                    "A delivery shift earns cash while consuming fuel, tires, and time.", null),
                "DoorDashResult" => new ScreenDescriptor("activity", "Shift results",
                    DoorDashResultDescription(userData as TravelInfo), null),

                "EventExecutor" => new ScreenDescriptor("event", EventTitle(userData as RandomEventInfo),
                    EventDescription(userData as RandomEventInfo), null),
                "EventSkipDay" => new ScreenDescriptor("activity", "Recovering from an event",
                    EventDelayDescription(userData as RandomEventInfo), null),
                "VehicleBrokenPrompt" or "VehicleNoSparePart" or "VehicleUseSparePart" =>
                    new ScreenDescriptor("event", "Vehicle trouble", EventDescription(userData as RandomEventInfo), null),
                "TomHollandEncounter" or "ScammerReveal" or "BeroSentiment" =>
                    new ScreenDescriptor("event", Humanize(name), EventDescription(userData as RandomEventInfo), input),

                "GameWin" => new ScreenDescriptor("game-over", "Seattle reached",
                    "Congratulations! You have made it to Seattle!\n\n" +
                    "Sunshine State Mutual regrets to inform you your new ZIP code is also under review.\n\n" +
                    "Let's tally your final Net Worth and Clout score.", null),
                "GameFail" => new ScreenDescriptor("game-over", "The trip ended early",
                    "The party can no longer continue.", null),
                "FinalPoints" => new ScreenDescriptor("game-over", "Final score",
                    "Review the party, supplies, and route decisions that determine the final clout score.", null),
                "EpitaphEditor" => new ScreenDescriptor("game-over", "Write an epitaph",
                    $"Add one word at a time. Submit an empty line when the eulogy is done. The memorial holds 64 characters.\n\nEpitaph so far: {(currentForm as EpitaphEditor)?.SemanticEpitaph ?? string.Empty}", input),
                "EpitaphConfirm" => new ScreenDescriptor("game-over", "Their roadside memorial",
                    $"{MemorialDescription(userData as TombstoneInfo, game)}\n\nWould you like to change this epitaph?", null),
                "TombstoneView" => new ScreenDescriptor(game.Vehicle?.PassengerLivingCount > 0 ? "dialog" : "game-over", "By the side of the road",
                    MemorialDescription(userData as TombstoneInfo, game), null),
                "EpitaphQuestion" => new ScreenDescriptor("game-over", "Leave a few words behind",
                    "Would you like to write an epitaph for the roadside memorial?", null),
                "TombstoneQuestion" => new ScreenDescriptor("dialog", "Someone stopped here for good",
                    "You pass a roadside memorial. Would you like to look closer?", null),

                "LocationArrive" => new ScreenDescriptor("dialog", $"Arrived at {location}",
                    game.Trail?.IsFirstLocation == true
                        ? "Supplies are loaded. The party is ready to leave Cape Coral."
                        : "Choose whether to stop and look around before continuing.", null),
                "LocationDepart" => new ScreenDescriptor("dialog", $"Leaving {location}",
                    $"The next leg heads toward {nextLocation}.", null),
                "LocationFork" => new ScreenDescriptor("choice", "Choose a route",
                    "The route divides here. Each branch changes the locations ahead.", null),
                "UnableToContinue" => new ScreenDescriptor("dialog", "The vehicle cannot continue",
                    game.Vehicle?.BrokenPart == null
                        ? "The party must address the vehicle problem before moving."
                        : $"The {game.Vehicle.BrokenPart.Name} needs attention before the party can move.", null),

                _ when fullName.Contains(".Decision.", StringComparison.Ordinal) =>
                    new ScreenDescriptor("choice", DecisionTitle(name), DecisionDescription(name), input),
                _ when fullName.Contains(".Help.", StringComparison.Ordinal) =>
                    new ScreenDescriptor("dialog", Humanize(name), "Review this guidance, then continue.", null),
                _ when fullName.Contains(".GameOver.", StringComparison.Ordinal) ||
                       fullName.Contains(".Graveyard.", StringComparison.Ordinal) =>
                    new ScreenDescriptor("game-over", Humanize(name), "Review the result and continue when ready.", input),
                _ when fullName.Contains(".RandomEvent.", StringComparison.Ordinal) =>
                    new ScreenDescriptor("event", EventTitle(userData as RandomEventInfo), EventDescription(userData as RandomEventInfo), input),
                _ when fullName.Contains(".RiverCrossing.", StringComparison.Ordinal) =>
                    new ScreenDescriptor("river", Humanize(name), RiverDescription(userData as TravelInfo), input),
                _ => new ScreenDescriptor(game.ActiveMenu?.HasOptions == true ? "choice" : "dialog",
                    Humanize(name), DefaultDescription(game.ActiveMenu?.HasOptions == true), input)
            };
        }

        private static InputSpecDto InputFor(string name, object userData)
        {
            return name switch
            {
                "InputPlayerNames" => new InputSpecDto("text", "Passenger name", "Enter a first name", false,
                    null, null, 40),
                "EpitaphEditor" => new InputSpecDto("text", "Next epitaph word", "A word, or leave blank to finish", false,
                    null, null, 30),
                "RestAmount" => new InputSpecDto("number", "Days to rest", "0–9", true, 0, 9, null),
                "TomHollandEncounter" => new InputSpecDto("text", "Response", "Type your response", false,
                    null, null, 80),
                _ => null
            };
        }

        private static ScreenDescriptor IntroDescriptor(GameIntro intro)
        {
            return new ScreenDescriptor(
                "setup",
                "The road ahead",
                "Your Cape Coral home has been declared uninsurable. Read the story of the move west, then choose the life you are bringing with you.",
                null);
        }

        private static string StoreWelcomeDescription(object currentForm, string location)
        {
            return Field<int>(currentForm, "_adviceCount") <= 0
                ? $"Page 1 of 2. The {location} travel center carries five-gallon gas cans to keep the car moving and crates of MLM leggings for cold weather and trade."
                : "Page 2 of 2. Stock snacks for the party, ammunition for roadside trouble, and spare tires, alternators, and transmissions for breakdowns.";
        }

        private static string DecisionTitle(string name)
        {
            return name switch
            {
                "PackTheCarDecision" => "What fits in the car?",
                "BuceesHaulDecision" => "The cathedral of snacks",
                "CaravanDecision" => "The others at Carhenge",
                "CheckpointDecision" => "The Cascadia line",
                "ArmYourselfDecision" => "Sporting Goods, Aisle 12",
                _ => Humanize(name.EndsWith("Decision", StringComparison.Ordinal)
                    ? name[..^"Decision".Length]
                    : name)
            };
        }

        private static string DecisionDescription(string name)
        {
            return name switch
            {
                "PackTheCarDecision" =>
                    "You are still at home in Cape Coral, packing before the first supply stop. Resellables add $600 for shopping but displace 80 lb of snacks at checkout; traveling light protects party health and documents. Mateo can join if there is an open seat.",
                "BuceesHaulDecision" =>
                    "Choose between a $400 mega-haul, a $40 fuel-and-snack stop, or a freight shift that earns $500 but costs three days and party health.",
                "ArmYourselfDecision" =>
                    "A pistol costs $300, refusing costs nothing, and a trauma kit with a vest costs $120 and restores party health. This choice also affects the Cascadia checkpoint.",
                "CaravanDecision" =>
                    "Joining the slower convoy changes safety and the later checkpoint; traveling alone is faster but more exposed.",
                "CheckpointDecision" =>
                    "Your documents, caravan, and Sporting Goods choice all affect the cost and risk of complying, asserting passage, or taking the backroad.",
                _ => "This choice immediately changes supplies, risk, or the final score."
            };
        }

        private static bool SemanticActionEnabled(
            GameSimulationApp game,
            object currentForm,
            int index,
            bool legacyEnabled)
        {
            if (!legacyEnabled || currentForm?.GetType().Name != "VehicleSelector" || index < 0 || index > 3)
                return legacyEnabled;

            var info = Property(currentForm, "UserData") as NewGameInfo;
            if (info == null)
                return legacyEnabled;

            var choice = (Entity.Vehicle.VehicleChoice) (index + 1);
            return Entity.Vehicle.VehicleModels.Get(choice).Cost < info.StartingMonies;
        }

        private static string SemanticActionLabel(string screenName, int index, string label)
        {
            if (screenName == "MainMenuScreen")
                return index switch { 2 => "Clout leaderboard", 3 => "Local records", 4 => "End session", _ => label };
            if (screenName == "CurrentTopTen")
                return index == 0 ? "How points are earned" : "Back to menu";

            if (screenName == "VehicleSelector" && index >= 0 && index < 4)
                return Entity.Vehicle.VehicleModels.Get((Entity.Vehicle.VehicleChoice) (index + 1)).Name;

            return (screenName, index) switch
            {
                ("PackTheCarDecision", 0) => $"{label} (+$600 cash)",
                ("PackTheCarDecision", 1) => $"{label} (restore party health)",
                ("PackTheCarDecision", 2) => $"{label} (requires an open seat)",
                ("BuceesHaulDecision", 0) => $"{label} (-$400; +300 snacks)",
                ("BuceesHaulDecision", 1) => $"{label} (-$40; +80 snacks)",
                ("BuceesHaulDecision", 2) => $"{label} (+$500; -3 days; party health cost)",
                ("ArmYourselfDecision", 0) => $"{label} (-$300)",
                ("ArmYourselfDecision", 1) => $"{label} (no cash cost)",
                ("ArmYourselfDecision", 2) => $"{label} (-$120; restore party health)",
                _ => label
            };
        }

        private static string SemanticActionDetail(string screenName, int index)
        {
            if (screenName == "VehicleSelector" && index >= 0 && index < 4)
                return Entity.Vehicle.VehicleModels.Get((Entity.Vehicle.VehicleChoice) (index + 1)).FlavorText;

            return (screenName, index) switch
            {
                ("ProfessionSelector", 0) => "The most money for supplies, with a standard final score.",
                ("ProfessionSelector", 1) => "Less starting cash, but a doubled score if the party reaches Seattle.",
                ("ProfessionSelector", 2) => "The hardest start, with a tripled score if the party arrives.",
                _ => null
            };
        }

        private static IReadOnlyList<ActionFactDto> SemanticActionFacts(string screenName, int index)
        {
            if (screenName == "VehicleSelector" && index >= 0 && index < 4)
            {
                var model = Entity.Vehicle.VehicleModels.Get((Entity.Vehicle.VehicleChoice) (index + 1));
                return new[]
                {
                    new ActionFactDto("Price", Usd(model.Cost)),
                    new ActionFactDto("Seats", model.MaxPartySize.ToString()),
                    new ActionFactDto("Cargo", $"{model.CargoCapacity} lb"),
                    new ActionFactDto("Speed", $"×{model.SpeedMultiplier:0.##}"),
                    new ActionFactDto("Fuel range", $"×{model.FuelEfficiencyMultiplier:0.##}")
                };
            }

            return (screenName, index) switch
            {
                ("ProfessionSelector", 0) => new[]
                {
                    new ActionFactDto("Starting cash", "$8,000"),
                    new ActionFactDto("Final score", "×1")
                },
                ("ProfessionSelector", 1) => new[]
                {
                    new ActionFactDto("Starting cash", "$4,000"),
                    new ActionFactDto("Final score", "×2")
                },
                ("ProfessionSelector", 2) => new[]
                {
                    new ActionFactDto("Starting cash", "$2,000"),
                    new ActionFactDto("Final score", "×3")
                },
                _ => null
            };
        }

        private static string SemanticActionGroup(string screenName, int index)
        {
            if (screenName != "TravelMenu")
                return null;

            return index switch
            {
                0 => "drive",
                1 or 2 => "review",
                3 or 4 or 5 => "manage",
                _ => "local"
            };
        }

        private static string SemanticActionType(string screenName, int index)
        {
            return (screenName, index) switch
            {
                ("ProfessionSelector", 3) => "help",
                ("VehicleSelector", 4) => "help",
                _ => "select"
            };
        }

        private static string NamePrompt(NewGameInfo info, GameSimulationApp game)
        {
            if (info == null)
                return "Enter the names of the people making the trip.";

            var seat = Math.Min(info.PlayerNameIndex + 1, game.Vehicle?.MaxPartySize ?? GameSimulationApp.MAXPLAYERS);
            return seat == 1
                ? "Enter the driver's first name, or submit an empty name to generate the party."
                : $"Enter passenger {seat}'s first name, or submit an empty name to generate the remaining party.";
        }

        private static string RequiredItemDescription(TravelInfo info)
        {
            var item = info?.Store?.SelectedItem;
            return item == null
                ? "Add the required road supply before leaving the store."
                : $"Add {item.Name} before leaving the store.";
        }

        private static string RiverDescription(TravelInfo info)
        {
            var river = info?.River;
            if (river == null)
                return "Review the crossing conditions and choose a safe route.";

            return $"The washed-out roadbed is {river.RiverWidth:N0} feet across and {river.RiverDepth:N0} feet deep. " +
                   "Choose a crossing method based on cost, delay, and risk.";
        }

        private static string HuntingDescription(TravelInfo info)
        {
            return "Catch passing trays before the crowd does.";
        }

        private static string HuntingResultDescription(TravelInfo info)
        {
            var pounds = info?.Hunt?.KillWeight ?? 0;
            return pounds > 0
                ? $"The party recovered {pounds:N0} pounds of food. One trail day spent. Continue to load the haul."
                : "The party did not recover any food this time.";
        }

        private static string DoorDashDescription(TravelInfo info)
        {
            var shift = info?.DoorDash;
            if (shift == null)
                return "Wait for a delivery offer or clock out.";

            if (!shift.HasOffer)
            {
                return $"Waiting for an offer. The shift has earned {Usd(shift.GrossEarned)} from " +
                       $"{shift.DeliveriesDone} deliveries. You can clock out at any time.";
            }

            var offer = Field<Window.Travel.DoorDash.DeliveryOffer>(shift, "_currentOffer");
            return offer == null
                ? "A delivery offer is waiting. Accept it, reject it, or clock out."
                : $"{offer.Restaurant} to {offer.Dropoff}: {Usd(offer.BasePay)} base pay plus an estimated " +
                  $"{Usd(offer.EstimatedTip)} tip for {offer.PaidDistance} paid miles. Accept, reject, or clock out.";
        }

        private static string DoorDashResultDescription(TravelInfo info)
        {
            var shift = info?.DoorDash;
            if (shift == null)
                return "The delivery shift is complete.";

            return $"The shift earned {Usd(shift.GrossEarned)} from {shift.DeliveriesDone} deliveries, " +
                   $"using {shift.CansBurned} gas cans and {shift.TiresWorn} spare tires.";
        }

        private static string EventTitle(RandomEventInfo info)
        {
            var name = Normalize(info?.DirectorEvent?.Name);
            if (name.Length <= 0)
                return "Road event";

            // These events retain original Oregon Trail class names for simulation compatibility even though their authored
            // 2028 stories describe something else. The browser should never expose those implementation identifiers.
            return name switch
            {
                "BrokenArm" => "Black Friday stampede",
                "Cholera" => "Diabetic emergency",
                "Concussion" => "Highway pedestrian strike",
                "Dysentery" => "Untreated infection",
                "Fever" => "Wildfire-smoke emergency",
                "Gangrene" => "Rattlesnake bite",
                "Measles" => "Cancer diagnosis",
                "MountainFever" => "Indoor hypothermia",
                "SprainedMuscle" => "Surprise ambulance bill",
                "SprainedShoulder" => "Wrong-doorbell shooting",
                "StyrofoamJesus" => "Roadside-statue collapse",
                "SufferingExhaustion" => "Celebratory gunfire",
                "TyphoidFever" => "Heat exhaustion",
                "MlmHun" => "MLM roadside pitch",
                _ => Humanize(name)
            };
        }

        private static string EventDescription(RandomEventInfo info)
        {
            var description = Prose(info?.EventText);
            if (!string.IsNullOrEmpty(description))
                return description;

            var source = info?.SourceEntity?.Name;
            return string.IsNullOrWhiteSpace(source)
                ? "An event has changed the trip. Review the outcome before continuing."
                : $"An event involving {source} has changed the trip. Review the outcome before continuing.";
        }

        private static string EventDelayDescription(RandomEventInfo info)
        {
            var eventText = EventDescription(info);
            return info == null || info.DaysToSkip <= 0
                ? eventText
                : $"{eventText} {info.DaysToSkip} days remain in the delay.";
        }

        private static string DefaultDescription(bool hasChoices)
        {
            return hasChoices
                ? "Choose one of the available actions to continue."
                : "Review the current situation and continue when ready.";
        }

        private static string MenuLabel(string text)
        {
            var label = LeadingNumber.Replace(Normalize(text), string.Empty);
            return CurrencySymbolBeforeNumber.Replace(label, "$");
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : Whitespace.Replace(text, " ").Trim();
        }

        private static string Prose(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : string.Join("\n\n",
                Regex.Split(text.Trim(), @"\r?\n\s*\r?\n").Select(Normalize));
        }

        private static string ConversationDescription(Window.Travel.TalkToPeople.TalkToPeople conversation)
        {
            return string.IsNullOrWhiteSpace(conversation?.SemanticQuote)
                ? "A stranger stops to talk."
                : $"{conversation.SemanticSpeaker}\n\n{Prose(conversation.SemanticQuote)}";
        }

        private static string TradeDescription(Window.Travel.Trade.Trading trading)
        {
            var offer = trading?.SemanticOffer;
            return offer == null ? "Nobody's working their downline here right now."
                : $"A hun from the parking-lot MLM wants {offer.WantedItem.Quantity:N0} {offer.WantedItem.Name.ToLowerInvariant()}. " +
                  $"She'll swap you {offer.OfferedItem.Quantity:N0} {offer.OfferedItem.Name.ToLowerInvariant()}, hon — ground floor, only goes up from here.\n\n" +
                  (trading.SemanticCanTrade ? "Are you willing to swap?" : "You don't have the supplies for this trade.");
        }

        private static string TollDescription(TravelInfo info, GameSimulationApp game)
        {
            if (info?.Toll == null) return "The express lane has a price. Review the crossing before continuing.";
            return $"The express lane just billed {Usd(info.Toll.Cost)} to use {info.Toll.Road?.Name ?? game.Trail.CurrentLocation.Name}.\n\n" +
                (game.Vehicle.Balance >= info.Toll.Cost ? "The price showed up only after you committed. Pay it?"
                    : "You don't have enough cash for the express lane.");
        }

        private static string MemorialDescription(TombstoneInfo info, GameSimulationApp game)
        {
            Module.Tombstone.Tombstone tombstone;
            if (game.Vehicle?.PassengerLivingCount > 0)
                game.Tombstone.FindTombstone(game.Vehicle.Odometer, out tombstone);
            else
                tombstone = info?.Tombstone;
            return tombstone == null ? "A memorial on the shoulder."
                : $"Here lies {tombstone.PlayerName}.\n\n{tombstone.Epitaph}\n\nMile {tombstone.MileMarker:N0}.";
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length + 8);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]))
                    builder.Append(' ');
                builder.Append(character);
            }

            return builder.ToString();
        }

        private static string ProfessionName(string profession)
        {
            return profession switch
            {
                "Banker" => "Crypto bro",
                "Carpenter" => "DoorDash driver",
                "Farmer" => "Faith-walk streamer",
                _ => Humanize(profession)
            };
        }

        private static string ItemId(Entities item)
        {
            return item switch
            {
                Entities.Animal => "gas",
                Entities.Food => "snacks",
                Entities.Clothes => "leggings",
                Entities.Ammo => "ammunition",
                Entities.Wheel => "tire",
                Entities.Axle => "alternator",
                Entities.Tongue => "transmission",
                Entities.Cash => "cash",
                _ => Slug(item.ToString())
            };
        }

        internal static string Slug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unknown";

            var builder = new StringBuilder(value.Length);
            var needsDash = false;
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (needsDash && builder.Length > 0)
                        builder.Append('-');
                    builder.Append(char.ToLowerInvariant(character));
                    needsDash = false;
                }
                else
                {
                    needsDash = true;
                }
            }

            return builder.ToString();
        }

        private static decimal Money(float value)
        {
            return decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal Money(double value)
        {
            return decimal.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
        }

        private static string Usd(float value)
        {
            return $"${Money(value):N2}";
        }

        private static string Usd(double value)
        {
            return $"${Money(value):N2}";
        }

        internal static object Property(object target, string name)
        {
            if (target == null)
                return null;

            return target.GetType()
                .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);
        }

        internal static T Field<T>(object target, string name)
        {
            if (target == null)
                return default;

            var value = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);
            return value is T typed ? typed : default;
        }

        private static bool BoolProperty(object target, string name, bool fallback)
        {
            return Property(target, name) is bool value ? value : fallback;
        }

        private sealed record ScreenDescriptor(string Kind, string Title, string Description, InputSpecDto Input);
    }
}
