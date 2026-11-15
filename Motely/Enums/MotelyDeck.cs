namespace Motely.Enums;

public enum MotelyDeck
{
    Red,
    Blue,
    Yellow,
    Green,
    Black,
    Magic,
    Nebula,
    Ghost,
    Abandoned,
    Checkered,
    Zodiac,
    Painted,
    Anaglyph,
    Plasma,
    Erratic,
}

public static class MotelyDeckExt
{
    public static MotelyRunState GetDefaultRunState(this MotelyDeck deck)
    {
        MotelyRunState state = new();

        switch (deck)
        {
            case MotelyDeck.Magic:
                state.ActivateVoucher(MotelyVoucher.CrystalBall);
                break;
            case MotelyDeck.Nebula:
                state.ActivateVoucher(MotelyVoucher.Telescope);
                break;
            case MotelyDeck.Zodiac:
                state.ActivateVoucher(MotelyVoucher.TarotMerchant);
                state.ActivateVoucher(MotelyVoucher.PlanetMerchant);
                state.ActivateVoucher(MotelyVoucher.Overstock);
                break;
        }

        return state;
    }

    public static MotelyVectorRunState GetDefaultVectorRunState(this MotelyDeck deck)
    {
        MotelyVectorRunState state = new();

        switch (deck)
        {
            case MotelyDeck.Magic:
                state.ActivateVoucher(MotelyVoucher.CrystalBall);
                break;
            case MotelyDeck.Nebula:
                state.ActivateVoucher(MotelyVoucher.Telescope);
                break;
            case MotelyDeck.Zodiac:
                state.ActivateVoucher(MotelyVoucher.TarotMerchant);
                state.ActivateVoucher(MotelyVoucher.PlanetMerchant);
                state.ActivateVoucher(MotelyVoucher.Overstock);
                break;
        }

        return state;
    }

    /// <summary>
    /// The deck's run state with <paramref name="held"/> redeemed on top: the run a clause's
    /// <c>with: { vouchers: [...] }</c> describes. The shop reads it, scalar and SIMD alike. A
    /// SIMD filter builds it once, when the filter is created; a scoring walk once, above its antes.
    /// </summary>
    public static MotelyRunState GetRunState(this MotelyDeck deck, ReadOnlySpan<MotelyVoucher> held)
    {
        MotelyRunState state = deck.GetDefaultRunState();
        foreach (MotelyVoucher voucher in held)
            state.ActivateVoucher(voucher);
        return state;
    }

    public static uint GetStartingVoucherMask(this MotelyDeck deck) =>
        unchecked((uint)deck.GetDefaultRunState().VoucherBitfield);
}

