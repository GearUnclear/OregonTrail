// Created for the 2028 Asphalt Trail re-skin — DoorDash gig mini-game.

using System;

namespace OregonTrailDotNet.Window.Travel.DoorDash
{
    /// <summary>
    ///     A single delivery offer the player can accept or reject during a DoorDash shift. The value shown to the player is a
    ///     hopeful estimate, not a promise: base pay is insulting, the tip is only an estimate (and is frequently tip-baited
    ///     down to nothing after delivery), and the mileage that actually burns the player's fuel and tires includes unpaid
    ///     "deadhead" miles driving to the restaurant that the offer never mentions.
    /// </summary>
    public sealed class DeliveryOffer
    {
        private static readonly string[] Restaurants =
        {
            "Wendy's", "Taco Bell", "Panda Express", "Chipotle", "Popeyes", "Olive Garden",
            "Cheesecake Factory", "a ghost kitchen", "Waffle House", "Raising Cane's"
        };

        private static readonly string[] Dropoffs =
        {
            "a gated subdivision", "apartment 4C (no elevator)", "a third-floor walkup", "a trailer park",
            "an office park", "a house with no numbers", "a dorm", "a locked lobby", "the far side of town"
        };

        /// <summary>How long (in shift ticks) the offer has been sitting unanswered.</summary>
        private int _decideTicks;

        /// <summary>
        ///     Initializes a new offer, scaled by <paramref name="offerQuality" /> (0.4 = the algorithm punishing a low
        ///     acceptance rate, 1.0 = the best it will ever hand you). Low quality means worse base+tip and longer unpaid
        ///     deadhead driving.
        /// </summary>
        /// <param name="offerQuality">Quality factor in roughly [0.4, 1.0].</param>
        public DeliveryOffer(double offerQuality)
        {
            var game = GameSimulationApp.Instance;

            Restaurant = Restaurants[game.Random.Next(Restaurants.Length)];
            Dropoff = Dropoffs[game.Random.Next(Dropoffs.Length)];

            // The platform's insulting contribution: a fixed $2.00 - $3.50 regardless of distance.
            BasePay = 2.0 + game.Random.NextDouble() * 1.5;

            // Estimated tip dangled to make you accept; scaled by quality so a punished driver sees stingier estimates.
            EstimatedTip = game.Random.NextDouble() * 12.0 * (0.5 + 0.5 * offerQuality);

            // Miles you are actually paid for, then the unpaid drive-to-restaurant miles that still cost you fuel + tires.
            PaidDistance = game.Random.Next(2, 9);
            DeadheadMiles = game.Random.Next(1, 6) + (offerQuality < 0.6 ? game.Random.Next(0, 4) : 0);

            // Unpaid restaurant wait, charged straight to your shift clock when you accept.
            WaitTicks = game.Random.Next(0, 5);

            // How long before the offer is handed to another Dasher.
            DecideTicksMax = game.Random.Next(3, 7);
        }

        /// <summary>Restaurant the order is picked up from.</summary>
        public string Restaurant { get; }

        /// <summary>Where the order is dropped off.</summary>
        public string Dropoff { get; }

        /// <summary>Fixed platform base pay in dollars (small on purpose).</summary>
        public double BasePay { get; }

        /// <summary>Tip estimate shown to the player. The realized tip is usually lower — see <see cref="RealizedTip" />.</summary>
        public double EstimatedTip { get; }

        /// <summary>Paid distance in miles to the customer.</summary>
        public int PaidDistance { get; }

        /// <summary>Unpaid miles driven to the restaurant (and back into service) that still burn fuel and wear.</summary>
        public int DeadheadMiles { get; }

        /// <summary>Shift ticks lost to waiting at the restaurant if this offer is accepted.</summary>
        public int WaitTicks { get; }

        /// <summary>Maximum shift ticks the offer stays on screen before it is withdrawn.</summary>
        public int DecideTicksMax { get; }

        /// <summary>Total miles this delivery puts on the SUV: paid distance plus unpaid deadhead.</summary>
        public int TotalMiles => PaidDistance + DeadheadMiles;

        /// <summary>TRUE once the offer has sat unanswered past its decision window and should be withdrawn.</summary>
        public bool Expired => _decideTicks >= DecideTicksMax;

        /// <summary>Fraction of the decision window already elapsed, for the "another Dasher is circling" meter.</summary>
        public decimal DecidePercentage => DecideTicksMax <= 0 ? 1m : _decideTicks / (decimal) DecideTicksMax;

        /// <summary>Advances the decision timer one shift tick.</summary>
        public void TickDecide()
        {
            _decideTicks++;
        }

        /// <summary>
        ///     Rolls the tip you actually collect on delivery — which is usually NOT the estimate. ~15% of the time the customer
        ///     tip-baits you down to loose change, ~10% is a genuine windfall, and the rest lands well below the estimate.
        /// </summary>
        /// <returns>Realized tip in dollars.</returns>
        public double RealizedTip()
        {
            var game = GameSimulationApp.Instance;
            var roll = game.Random.NextDouble();

            // Tip-bait: the customer set a big tip to jump the queue, then removed it after delivery.
            if (roll < 0.15)
                return game.Random.NextDouble() * 1.0;

            // Unicorn: an actually generous human being.
            if (roll < 0.25)
                return 8.0 + game.Random.NextDouble() * 7.0;

            // The common case: real tip lands at ~55-70% of what was dangled.
            return EstimatedTip * (0.55 + game.Random.NextDouble() * 0.15);
        }
    }
}
