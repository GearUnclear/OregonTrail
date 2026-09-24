using System;

namespace OregonTrailDotNet.Entity.Person
{
    /// <summary>Pounds of packed food per person per day, shared by consumption and the supply display.</summary>
    public static class FoodRations
    {
        public static decimal PoundsPerPerson(RationLevel ration) => ration switch
        {
            RationLevel.Filling => 2m,
            RationLevel.Meager => 1.5m,
            RationLevel.BareBones => 1m,
            _ => throw new ArgumentOutOfRangeException(nameof(ration), ration, null)
        };
    }
}
