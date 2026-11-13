namespace Motely;

// Where a stream stands, as values. A stream struct is a handful of PRNG streams plus the keys
// and flags it was created with; its position is only the PRNG streams' states. Each record
// below names every one of them, so a stream created the usual way and then given a State picks
// up exactly where another one stopped, with nothing rolled to get there.
//
// A double is that PRNG stream's MotelySinglePrngStream.State. -1 is the engine's own mark for
// a stream that does not exist (excluded by a flag) or has not been created yet.

/// <summary>
/// A resample stream: the first draw's stream, then each resample stream created so far, in the
/// order <c>_resample2</c>, <c>_resample3</c>, … The game re-reads a resample stream it has
/// already opened, so their states are part of the position.
/// </summary>
public sealed record MotelyResampleStreamState(double Initial, IReadOnlyList<double> Resamples)
{
    public bool Equals(MotelyResampleStreamState? other) =>
        other is not null
        && Initial.Equals(other.Initial)
        && Resamples.SequenceEqual(other.Resamples);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Initial);
        foreach (double resample in Resamples)
            hash.Add(resample);
        return hash.ToHashCode();
    }
}

/// <summary>
/// A rarity-rolling joker stream. The three resample states are present only on a stream created
/// with duplicate handling (Buffoon packs).
/// </summary>
public sealed record MotelyJokerStreamState(
    double Rarity,
    double Edition,
    double EternalPerishable,
    double Rental,
    double Common,
    double Uncommon,
    double Rare,
    MotelyResampleStreamState? CommonResample = null,
    MotelyResampleStreamState? UncommonResample = null,
    MotelyResampleStreamState? RareResample = null
);

/// <summary>A joker stream of one fixed rarity (Wraith, Riff-Raff, the tag jokers, a rarity's shop queue).</summary>
public sealed record MotelyJokerFixedRarityStreamState(
    double Joker,
    double Edition,
    double EternalPerishable,
    double Rental
);

public sealed record MotelyTarotStreamState(MotelyResampleStreamState Cards, double Soul);

public sealed record MotelyPlanetStreamState(MotelyResampleStreamState Cards, double BlackHole);

public sealed record MotelySpectralStreamState(MotelyResampleStreamState Cards, double SoulBlackHole);

/// <summary>
/// A shop queue: the slot-type roll and every source a slot can draw from. The rates are not
/// here; they come from the run state the shop is created with.
/// </summary>
public sealed record MotelyShopItemStreamState(
    double ItemType,
    MotelyJokerStreamState Jokers,
    MotelyTarotStreamState Tarots,
    MotelyPlanetStreamState Planets,
    MotelySpectralStreamState Spectrals,
    double StandardCards
);
