// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Window.Travel.TalkToPeople
{
    /// <summary>
    ///     References all the advice that people will offer up to the player when they talk to people. The difference pieces
    ///     of advice are broken up into chunks based the type of location and status on the trail they might represent. These
    ///     are suggestions and definitely not guidelines the text could say anything, all I did was want to re-create it
    ///     faithfully as it existed before.
    /// </summary>
    public static class AdviceRegistry
    {
        /// <summary>
        ///     Advice intended for new players that are starting out on the trail, normally this advice is used on the first
        ///     location.
        /// </summary>
        public static Advice[] Tutorial
        {
            get
            {
                var startingAdvice = new[]
                {
                    new Advice("A guy named Jimmy",
                        "Better grab extra crates of leggings from the hun before you go. The local\nguide at the flooded interstates only barters in MLM stock -- says he isn't\ndriving, he's traveling, and he won't take cash. Lady down the road mortgaged\nher house for ten crates of 'em; figure you may as well get some use out of\nyours."),
                    new Advice("A traveler",
                        "Saw it on the news this morning -- some folks set out for Seattle without a\nspare tire, not even an alternator. Out here a breakdown's on the shoulder of\na 55-mph US highway with no sidewalk. Faith-walk streamer got hit on the\nshoulder of US-40 doing the same thing. Hope they find a tow before someone\nfinds them."),
                    new Advice("A guy at the pump",
                        "Some folks think one five-gallon can of gas'll get 'em out of Florida.\nStations are dark half the time when the grid drops, and you can't roll out of\nthis lot on empty. I wouldn't leave with less than a full rack of cans -- they\ngo missing the second your back's turned."),
                    new Advice("A woman in line",
                        "Interstate's in the river again, so everybody's waiting on the National Guard\nhigh-water convoy. Could be stranded here days. They'll run you across, but\nthe bill shows up later like a $9,000 ambulance ride. I'd rather wait for FEMA\nto reopen it than gun it through the high water in a paid-off car.")
                };
                return startingAdvice;
            }
        }

        /// <summary>
        ///     Advice intended to be used at the first river crossing the player vehicle encounters.
        /// </summary>
        public static Advice[] River
        {
            get
            {
                var advice = new[]
                {
                    new Advice("Sam from the next car over",
                        "Can't afford the convoy fee. We're sealing the doors with tape and taking the\nwashed-out shoulder detour. Move the snacks up high and hope it don't rain\nmore -- Francine already put I-10 under water once. Floodline's halfway up the\ndoors as it is."),
                    new Advice("A National Guard driver",
                        "Don't try to gun it through anything deeper than the door sills -- about two\nand a half feet. You'll float the car and lose every supply in it. You can\nseal the doors and take the detour -- or be smart and pay for the high-water\nconvoy and let us haul you over."),
                    new Advice("A family heading back south",
                        "We've had enough. Helene took I-40 into the Pigeon River back in '24, fix\nwon't be done till '28, and now this stretch is gone too. Baking heat all day,\noceans of floodwater all night. Whole interstate system's coming apart and\neverybody just reroutes."),
                    new Advice("A tired mom",
                        "This valley's pretty enough with the water glittering over the road. But\nthere's too much of it -- the whole roadbed's just gone. I miss a town with a\nreal grid, lights that stay on, a hospital that's in-network. I wonder how\nmany days till the next one."),
                    new Advice("A guy with a winch",
                        "Careful you don't redline that engine through the flood. Keep it rolling but\neasy. Push it too hard in the water and you'll cook the transmission, and a\ndead transmission's about as much use to you out here as no car at all."),
                    new Advice("A FEMA volunteer",
                        "The detours from all the closed interstates -- I-75, I-40, the toll bridges --\nfunnel down to this one crossing. We staged here after the last atmospheric\nriver to get folks across the washouts headed north and west."),
                    new Advice("A trucker",
                        "Once you're past the floodline this stretch runs natural and easy alongside\nthe river clear to the next fuel stop. Everybody bound for the coast takes\nthis road. Could be the smoothest miles of the whole trip -- long as the\nroadbed holds."),
                    new Advice("A salvage diver",
                        "Plenty of cars still down in this water, but they're getting harder to reach.\nWith this many washouts a year I don't expect the road to last more'n a few\nseasons. Folks just drive their cars in, grab what floats, and leave the rest\nto rust in the sun."),
                    new Advice("A woman with kids in the back",
                        "I hear terrible stories of families running out of snacks before the coast --\nwhole party going hungry on a closed interstate. We check our supplies often;\nthe reroutes take longer than the map says. Always plan for the worst, I say.")
                };
                return advice;
            }
        }

        /// <summary>
        ///     Advice that is given out at any given landmark, the information here is non-specific and will make sense at any
        ///     landmark. Most of the advice is people warning about things they heard about other people ahead of them.
        /// </summary>
        public static Advice[] Landmark
        {
            get
            {
                var advice = new[]
                {
                    new Advice("A woman with a selfie stick",
                        "Cadillac Ranch at sunset is something. Ten Caddies buried nose-down in a\nfield, painted over fresh every morning. Folks come from all over with a can\nof spray paint. Out here a man buries ten cars as art and nobody blinks."),
                    new Advice("A retiree in a lawn chair",
                        "About noon we came up on Carhenge -- thirty-nine junk cars stacked up like\nStonehenge, gray as stone. Couldn't stop looking. Some fella welded the whole\nthing together as a monument to his dad. Out here that's just what you do with\nold cars."),
                    new Advice("A man rinsing his eyes",
                        "Mind the truck three cars back -- he paid extra to make it dump black smoke on\ncommand. They call it rolling coal. Just did it to a pack of cyclists for the\nvideo. Engine's running fine; that's the part he paid for."),
                    new Advice("A woman flagging cars",
                        "Be warned, stranger -- don't pull into a driveway to turn around out here. We\nburied my husband last week; he just stopped to check the map. Homeowner fired\ntwice. They call it standing your ground. Could use a hand with this tire, if\nyou've a minute."),
                    new Advice("A man in a parka in July",
                        "These folks heading north don't know what an ice storm does to a city with no\ngrid. Lights went out for four days during Uri and people froze to death\nindoors, in Texas, in their own beds. Cold doesn't care it's the South."),
                    new Advice("A kid with a phone",
                        "I tagged my name way up the leg of the sixty-two-foot Styrofoam Jesus --\nhundreds of names up there! Folks call it Touchdown Jesus. Lightning hit it\nonce and burned the whole thing to the rebar, lightning rods and all. They\nrebuilt it bigger."),
                    new Advice("A grandma at a folding table",
                        "No fresh fruit since Florida -- just deep-fried butter and a 600-pound butter\ncow at the state fair, carved every year like a saint. I'd rather have a full\npantry than our names on a monument. Still, it's cheerier than the GoFundMe\nheadstones we passed."),
                    new Advice("A guy loading a cooler",
                        "Goodbye prairie, goodbye three thousand Wall Drug billboards! Now we climb\ninto the heat dome -- Phoenix hit a hundred and ten degrees thirty-one days\nrunning last year. Once we're over the passes it's a long dry run to the\ncoast."),
                    new Advice("A man with a card table of pamphlets",
                        "My church and forty families are caravanning west to plant a new ministry. The\npastor says commercial flights are full of demons, that's why he needs a third\njet. Sow a seed and you'll be blessed. We're changing the desert into a\ncampus."),
                    new Advice("A man with a clipboard",
                        "When folks first started passing through, the land was open. Now they crowd\nevery road, the grid can't carry them, the aquifer's drying up. Half the towns\nback east are uninsurable now. My people talk of moving on too."),
                    new Advice("A girl outside an RV",
                        "My father's real sick and we're resting here till he's better -- diabetic, and\nthe insulin GoFundMe came up fifty dollars short last time. We pushed too hard\nand his health gave out. When he's able again we'll go slower."),
                    new Advice("A woman traveling alone",
                        "One child got trampled in a Black Friday door-buster stampede back home. My\nhusband took a falling celebratory bullet -- came down a mile from where it\nwas fired into the air at midnight. Now I drive alone with my five kids. The\neldest, Caleb, is eleven.")
                };
                return advice;
            }
        }

        /// <summary>
        ///     Statements that will generally be said around areas that are more civilized along the route. There is a fair
        ///     mixture of different kinds of people experiencing different types of problems from beginning to end of the trail.
        /// </summary>
        public static Advice[] Settlement
        {
            get
            {
                var advice = new[]
                {
                    new Advice("A cashier",
                        "This Buc-ee's is the world's largest -- a hundred and twenty pumps and\nseventy-four thousand square feet. Brisket wall, a thousand bags of jerky, and\nthe firearms and ammo are right there in the cart next to the flour. No\nlicense, no wait. Just Florida being Florida."),
                    new Advice("A woman comparing receipts",
                        "Should've stocked up two stops back! Prices climb at every place along the way\n-- surge pricing, they call it. Snacks aren't fit to eat, much less pay for.\nThere's a hun by the door who'll trade you a crate of leggings for the shirt\noff your back if you let her."),
                    new Advice("A man restocking shelves",
                        "When folks first started passing through here we didn't mind -- traded snacks,\nhelped 'em cross the floods. Now there's too many cars and not enough road,\nand the highway patrol runs a checkpoint that takes your cash if their dog so\nmuch as sneezes at it."),
                    new Advice("A man at the next pump",
                        "Nine dollars a gallon on the express lane, and they don't show you the toll\ntill you've already committed -- like a surprise ambulance bill. We'll stay on\nthe side roads till we find cheaper. What little money we've got left, we'll\nkeep."),
                    new Advice("A boy outside a clinic",
                        "My family didn't buy enough snacks back in Florida, so we've been on tiny\nrations and our health's poor. My sister caught a fever and the nearest\nin-network hospital's three states away, so we're stopped here a while."),
                    new Advice("A man with a phone live",
                        "Whole crowd here is dumping their savings into a dying mall store's stock to\nstick it to the hedge funds -- diamond hands, they call it. Could moon, could\ngo to zero. Either way I've heard taking the shortcut's worth the risk!"),
                    new Advice("A woman in a folding chair",
                        "My, this Wall Drug's something -- free ice water and three thousand billboards\njust to get you in the door. Felt good to rest and not be jostled all day.\nWhen I get to Seattle I'll sleep in a real bed and never sleep in this car\nagain!"),
                    new Advice("A boy with a livestream",
                        "My job every day's keeping the cans full. Stations go dark when the grid\ndrops, so I hoard extra five-gallon cans in a box under the back seat. Back in\nFlorida I siphoned what I could whenever a pump still worked."),
                    new Advice("A man shouldering a rifle",
                        "Well, friend, this is where we part -- I'm headed for the open-carry Walmart\nto do my AR-15 civics test before the passes. And you've got the Sovereign\nCitizen crossing ahead, which I hear is no picnic. Text us soon as you reach\nSeattle."),
                    new Advice("A woman loading groceries",
                        "Hear there's a No Kings march rolling through -- couple million folks,\npeaceful, point being we don't have a king, been saying it two hundred and\nfifty years. Thank heaven for this Walmart restock. Sorry to be saying goodbye\nto the folks turning off for California."),
                    new Advice("A fellow traveler",
                        "Springfield Walmart's a busy one -- aisle of AR-15s, aisle of diapers, and a\nfella open-carrying through both like it's a civics test. As for me, I'll\npatch the radiator. Amanda's anxious to wash everything that didn't drown at\nthe last crossing."),
                    new Advice("A worried wife",
                        "It says right here in the guidebook you have to hire a local to pilot you\nacross the washed-out interstate, it being dangerous if not perfectly\nunderstood. The guy says he isn't driving, he's traveling, and he only takes\nleggings -- but my husband insists on crossing without a guide!")
                };
                return advice;
            }
        }

        /// <summary>
        ///     Advice that is generally used on locations that are inserted into the trail from forks in the road. The people in
        ///     these comments mostly show anger about toll roads and difficulty with getting over the mountains.
        /// </summary>
        public static Advice[] Mountain
        {
            get
            {
                var advice = new[]
                {
                    new Advice("A road-tripper",
                        "Down there past the express lanes is the I-405 toll gantry. They don't post\nthe price till you've committed -- saw it jump to forty-six dollars in traffic\nonce. We've got dry heat-dome miles ahead, so top off your cans before the\nclimb!"),
                    new Advice("A guy at a pull-off",
                        "See that orange haze over the pass? Wildfire smoke -- AQI hit four-eighty-four\nover New York the year the sky went orange. Sky looks like that here now most\nof the summer. We'll be breathing it over the Cascades. Take care up top."),
                    new Advice("A tow operator with a flatbed",
                        "You'll not get that car over these passes on bald tires, mister. Pieces of\nbreakdowns litter the shoulder -- 55-mph, no sidewalk, left by folks who\ndidn't carry a spare. Get yerself a real tire and an alternator before you try\nit."),
                    new Advice("A woman with a receipt roll",
                        "Every toll's been higher than the last, and they only show you after you've\ncommitted -- dynamic pricing, surprise ambulance billing, the works. It's\noutrageous. If I had it to do again, I'd have stocked up at the Buc-ee's."),
                    new Advice("A faith-walk streamer",
                        "Every night, sore as I am, my head's full of the followers and the GoFundMe\nwaiting in Seattle. I'm walking the whole way for the clout -- I'll build a\nfine channel and I'm certain I'll be verified within five years!"),
                    new Advice("A coughing traveler",
                        "Since the last pass it's been nothing but smoke and heat. Visibility drops\neach day -- thick as fog at times. Folks with bad lungs choke on it;\nrespiratory failure, the locals call it, like it's just weather. Keep your\nwindows up.")
                };
                return advice;
            }
        }

        /// <summary>
        ///     Advice from travelers that have been along most of the trail and are now deciding about which trail would be a
        ///     better risk for their vehicle party and offer up advice about their past decisions for replay value.
        /// </summary>
        public static Advice[] Ending
        {
            get
            {
                var advice = new[]
                {
                    new Advice("A woman scrubbing soot off a hood",
                        "We followed the smoke line from the heat dome to the wall of the Cascades.\nGrades dreadful steep, brakes smoking the whole way down -- rode 'em low and\nslow and got down safe. Poor engine. No clean air or open station for days."),
                    new Advice("A faith-walk streamer",
                        "This Tacoma rally town's the best sight I've seen in months -- 'No Kings'\nsigns everywhere, folks marching peaceful, point being there's no king. If a\nrally's this fine, Seattle must be twice as fine. We'll be sitting pretty with\nthe followers we picked up!"),
                    new Advice("A young mother",
                        "I've traveled in fear of the wrong-doorbell, wrong-driveway thing since\nFlorida. Seen little of it -- most folks just helped us cross the floods or\nshared snacks. Still I fear. I've read the GoFundMe headstones and heard the\nstories from these passes."),
                    new Advice("A streamer counting subs",
                        "My cousin's been documenting the road since Florida -- survived a forfeiture\nstop, a Black Friday stampede, a rolling-coal pass, not to mention an\nIShowSpeed street takeover that turned the off-ramp into donuts and fireworks\nfor an hour."),
                    new Advice("A Portland resident",
                        "You ask about the brawl downtown. I ask why the police stood back and watched\nProud Boys and antifascists go at it in the street. Both sides livestreamed\nit. Nobody charged, nobody helped. We just rerouted around the block like it\nwas roadwork."),
                    new Advice("A trucker at a viewpoint",
                        "These last hundred miles into Seattle are the roughest -- either the I-5\nbridge over the flooded Columbia or the long way over the smoke-choked Cascade\npasses. If you take the water crossing, pay the local guide; the leggings are\nworth it."),
                    new Advice("A streamer with a tripod",
                        "My buddy Lydia tried the Columbia I-5 bridge during an atmospheric river --\nseventeen people and gear in one stalled van. Water came up over the deck and\nthey had to wait it out. Near dark the convoy finally winched them all to\nshore safe."),
                    new Advice("A toll-lane attendant",
                        "I run the I-405 express gantry -- a bargain at twice the price, they say,\nthough we don't show you the price till you're in the lane. Used to be one\nfree interstate into Seattle. Now you can pay your way right into downtown --\nif the bridge is open.")
                };
                return advice;
            }
        }
    }
}