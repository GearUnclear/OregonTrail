// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Person
{
    /// <summary>
    ///     Called when one of your party members dies that is not the leader of the group; the trip can still continue without
    ///     this person. This fires from Person.Damage for ANY non-leader health death (starvation, illness, exposure), so
    ///     instead of one fixed cause it prints a random absurd-but-real death from AbsurdDeaths. Every entry is transplanted
    ///     from a genuine, documented (or famously traditional) historical death onto the 2028 road trip; some carry a
    ///     thematically-matched knock-on effect on the vehicle's supplies (see the Effect delegates).
    /// </summary>
    [DirectorEvent(EventCategory.Person, EventExecution.ManualOnly)]
    public sealed class DeathCompanion : EventProduct
    {
        /// <summary>
        ///     The _passenger death.
        /// </summary>
        private StringBuilder _passengerDeath;

        /// <summary>
        ///     One entry in the absurd-death pool: the flavor line, plus an optional knock-on effect that mutates the vehicle
        ///     and returns a consequence sentence to append (or null / empty when nothing was actually taken).
        /// </summary>
        private sealed class AbsurdDeath
        {
            public AbsurdDeath(string text, Func<Entity.Vehicle.Vehicle, string> effect = null)
            {
                Text = text;
                Effect = effect;
            }

            /// <summary>Flavor text printed after "{Name} has died. ".</summary>
            public string Text { get; }

            /// <summary>Optional knock-on effect; null for flavor-only deaths.</summary>
            public Func<Entity.Vehicle.Vehicle, string> Effect { get; }
        }

        /// <summary>
        ///     Removes roughly 1/divisor of a supply category (min 1 when any exists), clamped by SimItem.ReduceQuantity to the
        ///     item's floor so it can never go negative. Returns how much was actually taken.
        /// </summary>
        private static int Deplete(Entity.Vehicle.Vehicle vehicle, Entities category, int divisor)
        {
            var item = vehicle.Inventory[category];
            var before = item.Quantity;
            if (before <= 0)
                return 0;

            var take = before/divisor;
            if (take < 1)
                take = 1;

            item.ReduceQuantity(take);
            return before - item.Quantity;
        }

        /// <summary>
        ///     Pool of absurd-but-real deaths. Each is based on an actual historical death (comment gives the real person/year
        ///     and reliability); the printed line recreates the manner of death on the 2028 American road trip. Five carry a
        ///     supply knock-on that matches the real cause of death.
        /// </summary>
        private static readonly AbsurdDeath[] AbsurdDeaths =
        {
            // Aeschylus, Greek tragedian (c. 456 BC) - eagle dropped a tortoise on his bald head. Traditional account (Pliny, Valerius Maximus).
            new AbsurdDeath(
                "Stopped at a scenic overlook outside Flagstaff, a bald eagle carrying a desert\ntortoise dropped it onto his bald head from sixty feet, mistaking the gleam\nfor a cracking rock - the ranger at the booth said it was the second time this\nseason."),

            // Chrysippus, Stoic philosopher (c. 206 BC) - died of laughter at a donkey eating figs. Traditional account (Diogenes Laertius).
            new AbsurdDeath(
                "Spotted a rest stop donkey eating Funyuns out of a trash barrel, announced to\nthe convoy it needed a cold Sprite to wash them down, and laughed at their own\njoke until they didn't."),

            // Draco, Athenian lawgiver (c. 620 BC) - smothered by cloaks/hats thrown by admirers. Traditional account (the Suda).
            new AbsurdDeath(
                "At the truck-stop signing, admirers kept pitching baseball caps and zip-ups\nuntil the pile stopped moving. The official report described the incident as\nan outpouring of community appreciation."),

            // George Plantagenet, Duke of Clarence (1478) - reputedly drowned in a butt of Malmsey wine. Traditional account (Croyland Chronicle).
            new AbsurdDeath(
                "Toppled headfirst into the half-ton decorative wine barrel outside the\nvineyard rest stop and drowned before anyone thought to tip it over; the\nwinery has pulled the barrel from the welcome display and is not commenting\nfurther."),

            // Tycho Brahe, astronomer (1601) - burst bladder after being too polite to leave a banquet. Traditional account (Kepler).
            new AbsurdDeath(
                "Would not get up from the six-hour rest stop dinner to use the restroom, too\npolite to interrupt the table, and held it the entire time; the bladder gave\nout eleven days later, which is how a rest area off I-40 ended up being the\nlast stop on the itinerary."),

            // Sir Francis Bacon (1626) - pneumonia from stuffing a chicken with snow to test refrigeration. Traditional account (Aubrey).
            new AbsurdDeath(
                "Pulled over at a rest stop to pack a gas-station rotisserie bird with\nparking-lot snow as a cold-preservation experiment, spent an hour crouching in\nthe sleet writing up observations, and was dead of pneumonia before the convoy\ncrossed the state line.",
                v =>
                {
                    var n = Deplete(v, Entities.Food, 6);
                    return n > 0 ? $"The snow-packed experiment bird spoiled the cooler with it; {n}\npounds of snacks went in the dumpster." : null;
                }),

            // Franz Reichelt, the "Flying Tailor" (1912) - jumped off the Eiffel Tower testing a homemade parachute suit. Documented (filmed).
            new AbsurdDeath(
                "The hand-sewn parachute vest, debuted off the scenic overlook railing with a\ncrowd watching and two phones rolling, wrapped itself shut on exit and\nconverted the wearer into a dart; the landing is coned off and the full video\nis still up."),

            // The 21 victims of the Great Boston Molasses Flood (1919) - killed by a wave from a burst molasses tank. Documented.
            new AbsurdDeath(
                "A bulk corn-syrup storage tank behind the travel plaza fuel apron ruptured\nwithout warning and the wave it released crossed the lot at thirty-five miles\nan hour, taking everyone on the apron with it; it's in the incident report,\nit's accurate, and no one ever believes it.",
                v =>
                {
                    var n = Deplete(v, Entities.Clothes, 3);
                    return n > 0 ? $"Everything on the roof rack came out glazed and ruined; {n} crates\nof leggings were a total loss." : null;
                }),

            // Basil Brown, health-food enthusiast (1974) - vitamin A poisoning from ~10 gallons of carrot juice. Documented (coroner's inquest).
            new AbsurdDeath(
                "Went through ten gallons of cold-pressed carrot juice in nine days as a\ncleanse experiment. By Flagstaff the skin had gone full road-cone orange, and\nthe urgent care at the Flying J had a word for what that does to a liver.",
                v =>
                {
                    var n = Deplete(v, Entities.Food, 5);
                    return n > 0 ? $"The cleanse ran through the convoy's produce first; {n} pounds of\nsnacks juiced and gone." : null;
                }),

            // Hans Steininger, Austrian burgomaster (1567) - tripped over his record-length beard fleeing a fire, broke his neck. Documented (the beard is a museum artifact).
            new AbsurdDeath(
                "He normally kept four and a half feet of beard coiled inside his jacket. The\ntruck stop fire caught him in a hurry and he forgot the coiling part, tripped\non it crossing the lot, and broke his neck before he reached the on-ramp.",
                v =>
                {
                    var n = Deplete(v, Entities.Clothes, 5);
                    return n > 0 ? $"The same fire got into the cargo; {n} crates of leggings burned with\nthe rest stop." : null;
                }),

            // Jennifer Strange (2007) - water intoxication in the "Hold Your Wee for a Wii" radio contest. Documented.
            new AbsurdDeath(
                "Drank two gallons of water at a truck stop radio promo to win a free charging\nbundle, took first place by a wide margin, and was dead of it before the\nconvoy crossed the state line."),

            // Garry Hoy, Toronto lawyer (1993) - fell through an "unbreakable" skyscraper window while demonstrating it. Documented.
            new AbsurdDeath(
                "Bragged that the top-floor breezeway window at the highway motel was\nunbreakable and threw a shoulder into it to prove it to the group; the pane\nheld both times - it always does - but the second hit popped the frame clean\nout of the wall, and the drop to the lot below was exactly as far as it\nlooked."),

            // Jimi Heselden, owner of Segway Inc. (2010) - rode a Segway off a cliff. Documented.
            new AbsurdDeath(
                "Took the visitor center demo Segway out to the river gorge overlook and kept\ngoing; they found the Segway downstream, still powered on."),

            // King Adolf Frederick of Sweden (1771) - "ate himself to death", capping a feast with fourteen servings of semla. Traditional account.
            new AbsurdDeath(
                "Cleared the buffet line twice, then went back for a fourteenth bowl of warm\nmilk bread pudding; the convoy left a note on the windshield and pulled out at\ndawn.",
                v =>
                {
                    var n = Deplete(v, Entities.Food, 3);
                    return n > 0 ? $"The buffet they cleared twice was mostly yours; {n} pounds of snacks\nleft with them." : null;
                }),
        };

        /// <summary>
        ///     Fired when the event is created by the event factory, but before it is executed. Acts as a constructor mostly but
        ///     used in this way so that only the factory will call the method and there is no worry of it accidentally getting
        ///     called by creation.
        /// </summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();

            _passengerDeath = new StringBuilder();
        }

        /// <summary>
        ///     Fired when the event handler associated with this enum type triggers action on target entity. Implementation is
        ///     left completely up to handler.
        /// </summary>
        /// <param name="eventExecutor">
        ///     Entities which the event is going to directly affect. This way there is no confusion about
        ///     what entity the event is for. Will require casting to correct instance type from interface instance.
        /// </param>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            // Cast the source entity as a passenger from vehicle.
            var sourcePerson = eventExecutor.SourceEntity as Entity.Person.Person;
            if (sourcePerson == null)
                throw new ArgumentNullException(nameof(eventExecutor),
                    "Could not cast source entity as passenger of vehicle.");

            // Check to make sure this player is not the leader (aka the player).
            if (sourcePerson.Leader)
                throw new ArgumentException("Cannot kill this person because it is the player!");

            // Pick a random absurd-but-real death from the pool.
            var game = GameSimulationApp.Instance;
            var death = AbsurdDeaths[game.Random.Next(AbsurdDeaths.Length)];

            _passengerDeath.AppendLine($"{sourcePerson.Name} has died. {death.Text}");

            // Apply the matched knock-on effect (if any) and print what it actually cost the convoy.
            var consequence = death.Effect?.Invoke(game.Vehicle);
            if (!string.IsNullOrWhiteSpace(consequence))
            {
                _passengerDeath.AppendLine();
                _passengerDeath.AppendLine(consequence);
            }
        }

        /// <summary>
        ///     Fired when the simulation would like to render the event, typically this is done AFTER executing it but this could
        ///     change depending on requirements of the implementation.
        /// </summary>
        /// <param name="userData"></param>
        /// <returns>Text user interface string that can be used to explain what the event did when executed.</returns>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _passengerDeath.ToString();
        }
    }
}
