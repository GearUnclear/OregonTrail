namespace OregonTrailDotNet.Web
{
    /// <summary>
    ///     Complete semantic state consumed by the browser client. No member contains a rendered terminal frame or a
    ///     legacy input token.
    /// </summary>
    public sealed record GameSnapshotDto(
        long Revision,
        bool Running,
        GameHudDto Hud,
        IReadOnlyList<PartyMemberDto> Party,
        IReadOnlyList<InventoryItemDto> Inventory,
        GameProgressDto Progress,
        StoreDto Store,
        GameScreenDto Screen,
        GameScoreDto Score,
        DrivingStateDto Driving = null,
        string JourneyId = null,
        CryptoDeskDto Crypto = null,
        CreatorDeskDto Creator = null);

    public sealed record DrivingStateDto(
        bool IsDriving,
        string Status,
        string VehicleId,
        string Destination,
        int MilesRemaining,
        string Pace);

    public sealed record GameScoreDto(
        int BasePoints,
        int ChoiceScoreDelta,
        int Multiplier,
        int FinalPoints,
        string Rating,
        IReadOnlyList<ScoreLineDto> Lines,
        IReadOnlyList<string> Epilogue);

    public sealed record ScoreLineDto(int Quantity, string Description, int Points);

    public sealed record GameHudDto(
        string Date,
        int Turns,
        decimal Balance,
        string VehicleName,
        string VehicleStatus,
        string LocationName,
        string LocationStatus,
        string Weather,
        string Health,
        string Pace,
        string Rations,
        int CargoWeight,
        int CargoCapacity,
        int LivingPartyCount,
        decimal FoodPerDay,
        int FoodDaysRemaining,
        string VehicleIssue,
        string VehicleId = null,
        decimal? FoodPoundsRemaining = null);

    public sealed record PartyMemberDto(
        string Name,
        bool IsLeader,
        string Profession,
        string Health);

    public sealed record InventoryItemDto(
        string ItemId,
        string Name,
        int Quantity,
        string Unit,
        int UnitWeight,
        int TotalWeight);

    public sealed record GameProgressDto(
        int MilesTraveled,
        int TotalMiles,
        int MilesToNextLocation,
        string NextLocation,
        string ActivityLabel,
        int? ActivityCurrent,
        int? ActivityTotal,
        int CurrentStopIndex,
        IReadOnlyList<RouteStopDto> Stops);

    public sealed record RouteStopDto(string Name, string Kind);

    public sealed record StoreDto(
        string Location,
        decimal Balance,
        decimal PendingTotal,
        int CargoWeight,
        int CargoCapacity,
        IReadOnlyList<StoreRowDto> Rows);

    public sealed record StoreRowDto(
        string ItemId,
        string Name,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice,
        int MinQuantity,
        int MaxQuantity,
        bool Selected,
        string DecreaseActionId,
        string IncreaseActionId,
        string SetActionId);

    /// <summary>A discriminated screen payload. <see cref="Kind" /> selects the browser presentation.</summary>
    public sealed record GameScreenDto(
        string Kind,
        string Id,
        string Title,
        string Description,
        InputSpecDto Input,
        IReadOnlyList<GameActionDto> Actions,
        IReadOnlyList<string> Story = null,
        IReadOnlyList<LeaderboardEntryDto> Leaderboard = null);

    public sealed record LeaderboardEntryDto(string Name, int Points, string Rating);

    public sealed record InputSpecDto(
        string Kind,
        string Label,
        string Placeholder,
        bool Required,
        int? Min,
        int? Max,
        int? MaxLength,
        string ActionId = null);

    /// <summary>
    ///     An action advertised for the current screen. ActionId is stable presentation vocabulary; legacy command values
    ///     remain in the server-only binding catalog.
    /// </summary>
    public sealed record GameActionDto(
        string ActionId,
        string Label,
        string Kind,
        bool Enabled,
        bool Selected,
        string Detail = null,
        IReadOnlyList<ActionFactDto> Facts = null,
        string Group = null,
        string VehicleId = null);

    public sealed record ActionFactDto(string Label, string Value);

    public sealed record GameActionRequest(
        string ActionId,
        long ExpectedRevision,
        string Text,
        int? Value,
        string ExpectedJourneyId = null);

    public sealed record GameActionResponseDto(
        bool Accepted,
        string ErrorCode,
        string Message,
        GameSnapshotDto State);
}
