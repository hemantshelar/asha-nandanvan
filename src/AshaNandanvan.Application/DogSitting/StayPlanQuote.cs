namespace AshaNandanvan.Application.DogSitting;

public sealed record StayRatePlanView(
    int Id,
    string Code,
    string Name,
    decimal FirstDogPerNight,
    decimal ExtraDogPerNight,
    int MinNights,
    int? MaxNights,
    int SortOrder,
    bool IsDefault)
{
    public string BandLabel =>
        MaxNights is int max ? $"{MinNights}–{max} nights" : $"{MinNights}+ nights";

    public string Tagline => Code switch
    {
        "Regular" =>
            $"The backyard rate for most visits. The first dog is {Money(FirstDogPerNight)} a night; a second or third is {Money(ExtraDogPerNight)} a night — same care, a fairer household total.",
        "Standard" =>
            $"A longer stay, a kinder household rate. The first dog is {Money(FirstDogPerNight)} a night; each extra dog is {Money(ExtraDogPerNight)}.",
        "Silver" =>
            $"Settled-in stays. The first dog is {Money(FirstDogPerNight)} a night; companions are {Money(ExtraDogPerNight)}.",
        "Gold" =>
            $"Our longest stays, or a rate we have already agreed with you. The first dog is {Money(FirstDogPerNight)} a night; each extra dog is {Money(ExtraDogPerNight)}.",
        _ =>
            $"First dog {Money(FirstDogPerNight)} a night; each extra dog {Money(ExtraDogPerNight)}."
    };

    public string Example =>
        $"Two dogs on this plan: the second is {Money(ExtraDogPerNight)} a night.";

    public string MostStaysLabel => IsDefault ? "Most stays" : string.Empty;

    private static string Money(decimal amount) => $"${amount:0.##}";
}

public sealed record StayPlanQuote(
    StayRatePlanView Plan,
    int Nights,
    int DogCount,
    bool Pinned,
    bool Trial,
    decimal FirstDogTotal,
    decimal ExtraDogTotal)
{
    public decimal ExtraCount => Math.Max(0, DogCount - 1);
    public decimal Total => Trial ? 0 : FirstDogTotal + ExtraDogTotal;
    public decimal LeadNightly => Plan.FirstDogPerNight;
    public decimal ExtraNightly => Plan.ExtraDogPerNight;

    public decimal NightlyFor(bool companion) =>
        Trial ? 0 : companion ? ExtraNightly : LeadNightly;

    public decimal StayTotalFor(bool companion) =>
        Trial ? 0 : NightlyFor(companion) * Math.Max(1, Nights);

    public string Chip =>
        Trial
            ? "Free trial night — one dog, no companion rate."
            : Pinned
                ? $"Asha has you on {Plan.Name}. First dog {Money(LeadNightly)}, each extra {Money(ExtraNightly)}."
                : $"{Nights} night{(Nights == 1 ? "" : "s")} — {Plan.Name}. First dog {Money(LeadNightly)}, each extra {Money(ExtraNightly)}.";

    private static string Money(decimal amount) => $"${amount:0.##}";
}

public sealed record StayDogDraft(string Name, string Breed);

public sealed record StayRatePlanEdit(int Id, decimal FirstDogPerNight, decimal ExtraDogPerNight);

public static class StayPlanPicker
{
    public static StayRatePlanView Auto(IReadOnlyList<StayRatePlanView> plans, int nights)
    {
        var match = plans.FirstOrDefault(p =>
            nights >= p.MinNights && (p.MaxNights is null || nights <= p.MaxNights));
        return match ?? plans.First(p => p.IsDefault);
    }

    public static StayPlanQuote Quote(
        StayRatePlanView plan,
        int nights,
        int dogs,
        bool pinned,
        bool trial)
    {
        nights = Math.Max(1, nights);
        dogs = Math.Max(1, dogs);
        if (trial)
        {
            return new StayPlanQuote(plan, nights, 1, pinned, true, 0, 0);
        }

        var extras = dogs - 1;
        return new StayPlanQuote(
            plan,
            nights,
            dogs,
            pinned,
            false,
            plan.FirstDogPerNight * nights,
            plan.ExtraDogPerNight * nights * extras);
    }
}
