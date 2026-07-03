// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Collections.Generic;
using OregonTrailDotNet.Entity.Location;
using OregonTrailDotNet.Entity.Location.Point;
using OregonTrailDotNet.Entity.Location.Weather;
using OregonTrailDotNet.Window.Travel.RiverCrossing;

namespace OregonTrailDotNet.Module.Trail
{
    /// <summary>
    ///     Complete trails the player can travel on using the simulation. Some are remakes and others new.
    /// </summary>
    public static class TrailRegistry
    {
        /// <summary>
        ///     The Asphalt Trail (2028 American Roadtrip): a family flees a climate-uninsurable Cape Coral, Florida
        ///     and drives a paid-off SUV to Seattle, fording flooded Interstates and collapsing toll bridges along the
        ///     way. The Oregon Trail's settlements, landmarks, river fords, forks, and toll road are reskinned 1:1 onto
        ///     2028 roadside America; the location types, climates (overloaded by region), and ordering are preserved.
        /// </summary>
        public static Trail AsphaltTrail
        {
            get
            {
                var asphaltTrail = new Location[]
                {
                    new Settlement("Cape Coral, FL", Climate.Moderate),
                    new RiverCrossing("I-40 Pigeon River Gorge Washout", Climate.Continental, RiverOption.FerryOperator),
                    new RiverCrossing("I-10 Francine Flood Crossing", Climate.Continental),
                    new Settlement("Buc-ee's, Sevierville TN", Climate.Continental),
                    new Landmark("Touchdown Jesus, Monroe OH", Climate.Moderate),
                    new Settlement("Wall Drug, SD", Climate.Moderate),
                    new Landmark("Carhenge, Alliance NE", Climate.Moderate),
                    new ForkInRoad("The I-44 Texas Detour", Climate.Dry, new List<Location>
                    {
                        new Settlement("Big Texan Steak Ranch", Climate.Dry),
                        new Landmark("Cadillac Ranch", Climate.Dry)
                    }),
                    new RiverCrossing("Great Salt Lake Causeway Flood Crossing", Climate.Dry),
                    new Landmark("Iowa State Fair Butter Cow", Climate.Dry),
                    new Settlement("Open-Carry Walmart, Springfield MO", Climate.Moderate),
                    new RiverCrossing("Columbia/Snake 'Sovereign Citizen' Crossing", Climate.Moderate, RiverOption.IndianGuide),
                    new Settlement("Portland, OR", Climate.Polar),
                    new ForkInRoad("The Cascades: I-90 vs Highway 1 Fork", Climate.Polar, new List<Location>
                    {
                        new Settlement("Tacoma 'No Kings' Rally Town", Climate.Polar),
                        new ForkInRoad("The Gorge Fork", Climate.Polar, new List<Location>
                        {
                            new RiverCrossing("Columbia I-5 Bridge", Climate.Moderate),
                            new TollRoad("I-405 Express Toll Lanes", Climate.Moderate)
                        })
                    }),
                    new Settlement("Seattle, WA", Climate.Moderate)
                };

                return new Trail(asphaltTrail, 32, 164);
            }
        }

        /// <summary>
        ///     Debugging trail for quickly getting to the end of the game for points tabulation and high-score tests.
        /// </summary>
        public static Trail WinTrail
        {
            get
            {
                var testPoints = new Location[]
                {
                    new Settlement("Start Of Test", Climate.Moderate),
                    new Settlement("End Of Test", Climate.Dry)
                };

                return new Trail(testPoints, 50, 100);
            }
        }
    }
}