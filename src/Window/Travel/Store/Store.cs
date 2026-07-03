// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Item;
using OregonTrailDotNet.Entity.Location;
using OregonTrailDotNet.UI;
using OregonTrailDotNet.Window.Travel.Dialog;
using OregonTrailDotNet.Window.Travel.Store.Help;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Store
{
    /// <summary>
    ///     Manages a Buc-ee's travel center where the player can buy snacks, MLM leggings, ammo (by the flour), and spare
    ///     parts for their SUV. Florida is permitless-carry, so the firearms sit in the cart next to everything else.
    ///     Quantity for the highlighted item is adjusted right here with Left/Right (or by typing a number) rather than
    ///     on a separate screen, so the player never has to leave the aisle to change how many they're buying.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class Store : Form<TravelInfo>
    {
        /// <summary>
        ///     Every item category the store actually sells, in display order.
        /// </summary>
        private static readonly Entities[] PurchasableItems =
        {
            Entities.Animal, Entities.Food, Entities.Clothes, Entities.Ammo,
            Entities.Wheel, Entities.Axle, Entities.Tongue
        };

        /// <summary>
        ///     Sentinel value for the "Leave store" row's <see cref="ArrowMenuOption" />, distinct from any item's enum
        ///     name and from any typed number so <see cref="OnInputBufferReturned" /> can tell them apart.
        /// </summary>
        private const string LeaveStoreValue = "leave";

        /// <summary>
        ///     String builder that will hold all the generated data about store inventory and selections for player to make.
        /// </summary>
        private StringBuilder _storePrompt;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the store's purchasable items and the "Leave store" option.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Which item the highlighted row corresponds to, synced from <see cref="_menu" /> every render. Null when
        ///     "Leave store" is highlighted (nothing to adjust quantity on).
        /// </summary>
        private Entities? _highlightedItem;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Store" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        public Store(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            // Will hold representation of this store for rendering.
            _storePrompt = new StringBuilder();
        }

        /// <summary>
        ///     Returns the current, freshly-priced template <see cref="SimItem" /> for a purchasable category - the same
        ///     items each Buy* method used to hand off to the retired StorePurchase screen.
        /// </summary>
        private static SimItem GetTemplate(Entities item)
        {
            switch (item)
            {
                case Entities.Animal:
                    // Location-scaled fuel price (cheap mid-trip, dear near the end); inventory still stores $25 cans.
                    return Parts.GasCans(FuelPricing.CurrentCost());
                case Entities.Food:
                    return Resources.Food;
                case Entities.Clothes:
                    return Resources.Clothing;
                case Entities.Ammo:
                    return Resources.Bullets;
                case Entities.Wheel:
                    return Parts.Wheel;
                case Entities.Axle:
                    return Parts.Axle;
                case Entities.Tongue:
                    return Parts.Tongue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(item), item, "Item is not purchasable in the store.");
            }
        }

        /// <summary>
        ///     The absolute quantity of <paramref name="item" /> the player could hold given current cash and cargo room,
        ///     including whatever of it is already pending - so this is a total, not "headroom beyond what's pending".
        /// </summary>
        private int ComputeMaxQuantity(Entities item, SimItem template)
        {
            var vehicle = GameSimulationApp.Instance.Vehicle;
            var currentQuantity = UserData.Store.Transactions[item].Quantity;

            // Money available for this item if its own pending cost were backed out of the running total first.
            var balanceExcludingThis = vehicle.Balance - UserData.Store.TotalTransactionCost + currentQuantity*template.Cost;
            var maxByMoney = template.Cost > 0 ? (int) (balanceExcludingThis/template.Cost) : int.MaxValue;

            // Cargo room available for this item if its own pending weight were backed out first.
            var cargoExcludingThis = vehicle.CargoWeight + UserData.Store.PendingCargoWeight - currentQuantity*template.Weight;
            var remainingCargo = vehicle.Model.CargoCapacity - cargoExcludingThis;
            var maxByCargo = template.Weight > 0 ? remainingCargo/template.Weight : int.MaxValue;

            var max = Math.Min(maxByMoney, Math.Min(maxByCargo, template.MaxQuantity));
            return max < 0 ? 0 : max;
        }

        /// <summary>
        ///     Sets an item's pending quantity to an absolute value, clamped to what's actually affordable/carryable.
        ///     Quantities of zero remove the item from the receipt entirely (SimItem's quantity floor is
        ///     <c>MinQuantity</c>, not zero, so zero has to go through <see cref="StoreGenerator.RemoveItem" />).
        /// </summary>
        private void SetQuantity(Entities item, int desiredQuantity)
        {
            var template = GetTemplate(item);
            var max = ComputeMaxQuantity(item, template);
            var next = Math.Min(Math.Max(desiredQuantity, 0), max);

            if (next <= 0)
                UserData.Store.RemoveItem(template);
            else
                UserData.Store.AddItem(template, next);
        }

        /// <summary>
        ///     Adjusts an item's pending quantity by a relative amount (used by Left/Right and by a bare Enter).
        /// </summary>
        private void AdjustQuantity(Entities item, int delta)
        {
            SetQuantity(item, UserData.Store.Transactions[item].Quantity + delta);
        }

        /// <summary>
        ///     Returns a text only representation of the current game Windows state. Could be a statement, information, question
        ///     waiting input, etc.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string OnRenderForm()
        {
            // Rebuilt every render pass (not just on form creation) so the arrow-key highlight - and the fuel
            // price/cargo/bill figures, which already depended on rebuilding - stays current.
            UpdateStore();
            return _storePrompt.ToString();
        }

        /// <summary>
        ///     Creates store from enumeration of simulation entities and ignoring the ones the player cannot purchase like
        ///     vehicle, people, and cash itself.
        /// </summary>
        private void UpdateStore()
        {
            // Re-price gas to the current location's fuel cost while preserving any pending quantity on the receipt. This
            // keeps the displayed row, the total bill, and the affordability math on the station's curved price without
            // touching the $25 value that gas carries once it is in the vehicle inventory (see FuelPricing / Parts.Oxen).
            var pendingGas = UserData.Store.Transactions[Entities.Animal].Quantity;
            var repricedGas = Parts.GasCans(FuelPricing.CurrentCost());
            // The copy-ctor clamps quantity up to the minimum (1), so only use it when gas is actually on the receipt;
            // otherwise keep the fresh zero-quantity item so an empty receipt stays empty.
            UserData.Store.Transactions[Entities.Animal] =
                pendingGas > 0 ? new SimItem(repricedGas, pendingGas) : repricedGas;

            // Clear previous prompt and rebuild it.
            _storePrompt.Clear();
            _storePrompt.AppendLine("--------------------------------");
            _storePrompt.AppendLine($"{GameSimulationApp.Instance.Trail.CurrentLocation?.Name} Travel Center");
            _storePrompt.AppendLine($"{GameSimulationApp.Instance.Time.Date}");
            _storePrompt.AppendLine($"Fuel: {FuelPricing.CurrentCost():C2}/can {FuelPricing.Trend()}");
            // Cargo used = whatever is actually loaded in the vehicle PLUS everything still sitting on the pending
            // receipt (purchases don't reach Vehicle.Inventory until the player leaves the store).
            var cargoUsed = GameSimulationApp.Instance.Vehicle.CargoWeight + UserData.Store.PendingCargoWeight;
            _storePrompt.AppendLine(
                $"Cargo: {cargoUsed}/{GameSimulationApp.Instance.Vehicle.Model.CargoCapacity} lbs");
            _storePrompt.AppendLine("--------------------------------");

            // Build the purchasable items into arrow-navigable options. Each item's Value is its own enum name -
            // unique per row (so ArrowMenu.SetOptions keeps the highlight on the same row across renders) and never
            // parseable as a number (so OnInputBufferReturned can always tell a typed quantity apart from the
            // Enter-injected "buy one more of the highlighted item" sentinel).
            var options = new List<ArrowMenuOption>();
            var itemsInOrder = new List<Entities?>();

            foreach (var item in PurchasableItems)
            {
                var template = GetTemplate(item);
                var transaction = UserData.Store.Transactions[item];
                var label = transaction.Quantity > 0
                    ? $"{template.Name.PadRight(18)}x{transaction.Quantity}  {(transaction.Quantity*transaction.Cost):C2}"
                    : $"{template.Name.PadRight(18)}$0.00";

                options.Add(new ArrowMenuOption($"{itemsInOrder.Count + 1}. {label}", item.ToString()));
                itemsInOrder.Add(item);
            }

            options.Add(new ArrowMenuOption($"{itemsInOrder.Count + 1}. Leave store", LeaveStoreValue));
            itemsInOrder.Add(null);

            _menu.SetOptions(options);
            GameSimulationApp.Instance.ActiveMenu = _menu;
            _highlightedItem = itemsInOrder[_menu.SelectedIndex];

            // Left/Right adjust whichever item is currently highlighted; harmless no-op while "Leave store" is
            // highlighted since there's nothing to adjust there.
            if (_highlightedItem.HasValue)
            {
                var item = _highlightedItem.Value;
                GameSimulationApp.Instance.OnLeftPressed = () => AdjustQuantity(item, -1);
                GameSimulationApp.Instance.OnRightPressed = () => AdjustQuantity(item, +1);
            }

            _storePrompt.Append(_menu.Render());
            _storePrompt.AppendLine("(Left/Right to change quantity, or type a number)");

            // Footer text for below menu.
            _storePrompt.AppendLine("--------------------------------");

            // Calculate how much monies the player has and the total amount of monies owed to store for pending transaction receipt.
            var totalBill = UserData.Store.TotalTransactionCost;
            var amountPlayerHas = GameSimulationApp.Instance.Vehicle.Balance - totalBill;

            // If at first location we show the total cost of the bill so far the player has racked up.
            _storePrompt.Append(GameSimulationApp.Instance.Trail.IsFirstLocation &&
                                (GameSimulationApp.Instance.Trail.CurrentLocation?.Status == LocationStatus.Unreached)
                ? $"Total bill:            {totalBill:C2}" +
                  $"{Environment.NewLine}Amount you have:       {amountPlayerHas:C2}"
                : $"You have {GameSimulationApp.Instance.Vehicle.Balance:C2} to spend.");
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Skip if the input is null or empty.
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
                return;

            if (input == LeaveStoreValue)
            {
                LeaveStore();
                return;
            }

            // A real typed number always means "set the highlighted item's quantity to this many" - checked before
            // the enum-name sentinel below since Enum.TryParse also accepts numeric strings, which would otherwise
            // misread a typed quantity as a direct hit on that item's underlying enum value.
            if (int.TryParse(input, out var typedQuantity))
            {
                if (_highlightedItem.HasValue)
                    SetQuantity(_highlightedItem.Value, typedQuantity);
                return;
            }

            // Otherwise this is the Enter-injected sentinel (the highlighted item's enum name) meaning "buy one
            // more of it" - the same convenience Right already offers, for players who reach for Enter out of habit.
            if (Enum.TryParse(input, out Entities selectedItem) && PurchasableItems.Contains(selectedItem))
                AdjustQuantity(selectedItem, +1);
        }

        /// <summary>
        ///     Attempts to leave the store state, if the player does not have enough gas in the cans to keep the SUV moving then
        ///     it will complain.
        /// </summary>
        private void LeaveStore()
        {
            // Complain if user doesn't have enough gas cans to keep their SUV moving.
            if (UserData.Store.MissingImportantItems)
            {
                UserData.Store.SelectedItem = Parts.Oxen;
                SetForm(typeof(RequiredItem));
                return;
            }

            // Check if player can afford the items they have selected.
            var totalBill = UserData.Store.TotalTransactionCost;
            if (GameSimulationApp.Instance.Vehicle.Balance < totalBill)
            {
                SetForm(typeof(StoreDebtWarning));
                return;
            }

            // Quantities only ever sit on the pending receipt until the player actually leaves - true for the
            // pre-departure shopping trip and for a mid-trip restock alike, so commit here either way.
            UserData.Store.PurchaseItems();

            // Travel Windows waits until it is by itself on first location and first turn.
            if (GameSimulationApp.Instance.Trail.IsFirstLocation &&
                (GameSimulationApp.Instance.Trail.CurrentLocation?.Status == LocationStatus.Unreached))
            {
                // Sets up vehicle, location, and all other needed variables for simulation.
                GameSimulationApp.Instance.Trail.ArriveAtNextLocation();

                // Attach state that will ask if we want to check status or keep driving on trail.
                SetForm(typeof(LocationArrive));
            }
            else
            {
                // Normal store operation just returns to travel Windows menu.
                ClearForm();
            }
        }
    }
}
