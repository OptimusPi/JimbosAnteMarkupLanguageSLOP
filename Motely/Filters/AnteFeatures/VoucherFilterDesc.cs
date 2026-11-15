using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using static Motely.MotelyVectorUtils;

namespace Motely.Filters;

[YamlDotNet.Serialization.YamlSerializable]
public sealed partial class VoucherClause: IMotelyClause, IAnteScopedClause, IRollScopedClause
{
    public MotelyVoucher[]? Voucher { get => Vouchers; set => Vouchers = value ?? []; }

    /// <summary><c>ante: 2</c> or <c>ante: [1, 2]</c> is <c>antes:</c>.</summary>
    public int[]? Ante { get => null; set => Antes = value ?? Antes; }

    public string? Label { get; set; }
    public int Min { get; set; } = 1;
    public int? Max { get; set; }
    public int Score { get; set; } = 1;
    public int[] Antes { get; set; } = [1, 2, 3, 4, 5, 6, 7, 8];

    /// <summary>The vouchers that count; empty (<c>any</c>) counts every draw.</summary>
    public MotelyVoucher[] Vouchers { get; set; } = [];

    /// <summary>
    /// Voucher-stream indices per ante: 0 = ante award, 1+ = further draws on that ante's
    /// voucher stream (Hieroglyph bonus, voucher-tag shop extras, etc.).
    /// </summary>
    public int[] Rolls { get; set; } = [0];

    public IMotelySeedFilterDesc CreateFilterDesc() => new VoucherFilterDesc(this);
}

public struct VoucherFilterDesc(VoucherClause clause)
    : IMotelySeedFilterDesc<VoucherFilterDesc.VoucherFilter>
{
    private readonly VoucherClause _clause = clause;

    public readonly VoucherFilter CreateFilter(ref MotelyFilterCreationContext ctx)
    {
        int maxAnte = 0;
        for (int i = 0; i < _clause.Antes.Length; i++)
        {
            if (_clause.Antes[i] > maxAnte)
                maxAnte = _clause.Antes[i];
        }

        // Cache all antes 1..maxAnte so state-building passes on non-target antes work.
        for (int ante = 1; ante <= maxAnte; ante++)
            ctx.CacheAnteFirstVoucher(ante);

        return new VoucherFilter(_clause, maxAnte);
    }

    public struct VoucherFilter(VoucherClause clause, int maxAnte) : IMotelySeedFilter
    {
        private readonly VoucherClause _clause = clause;
        private readonly int _maxAnte = maxAnte;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly VectorMask Filter(ref MotelyVectorSearchContext ctx)
        {
            var clause = _clause;
            int maxAnte = _maxAnte;

            Vector256<int> matchCounts = Vector256<int>.Zero;
            var voucherState = ctx.Deck.GetDefaultVectorRunState();

            for (int ante = 1; ante <= maxAnte; ante++)
            {
                bool isTarget = false;
                for (int i = 0; i < clause.Antes.Length; i++)
                {
                    if (clause.Antes[i] == ante)
                    {
                        isTarget = true;
                        break;
                    }
                }
                if (!isTarget)
                {
                    voucherState.ActivateVoucher(ctx.GetAnteFirstVoucher(ante, voucherState));
                    continue;
                }

                // Roll 0 is the first draw of this ante's stream; later rolls continue it.
                var voucherStream = ctx.CreateVoucherStream(ante);
                var vouchers = ctx.GetNextVoucher(ref voucherStream, voucherState);
                voucherState.ActivateVoucher(vouchers);

                matchCounts = AccumulateVoucherRolls(
                    ref ctx,
                    ref voucherStream,
                    ref voucherState,
                    vouchers,
                    clause,
                    matchCounts
                );
            }

            return SimdPackSupport.MeetsMinMaxMask(matchCounts, clause.Min, clause.Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<int> AccumulateVoucherRolls(
            ref MotelyVectorSearchContext ctx,
            ref MotelyVectorVoucherStream voucherStream,
            ref MotelyVectorRunState voucherState,
            VectorEnum256<MotelyVoucher> anteVouchers,
            VoucherClause clause,
            Vector256<int> matchCounts
        )
        {
            // Walk the voucher stream once, index 0..maxRoll, mirroring the scalar
            // CountVoucherOccurrences. Index 0 is the ante award, already drawn from
            // voucherStream; 1+ are the draws after it on the same stream.
            // (stackalloc of VectorEnum256 is the same pattern TagFilter uses for its tag-stream
            // draws.)
            int maxRoll = clause.Rolls.Max();
            Span<VectorEnum256<MotelyVoucher>> draws =
                stackalloc VectorEnum256<MotelyVoucher>[maxRoll + 1];
            draws[0] = anteVouchers;

            for (int i = 1; i <= maxRoll; i++)
                draws[i] = ctx.GetNextVoucher(ref voucherStream, voucherState);

            foreach (var roll in clause.Rolls)
                matchCounts = AddVoucherMatches(matchCounts, draws[roll], clause);

            return matchCounts;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<int> AddVoucherMatches(
            Vector256<int> counts,
            VectorEnum256<MotelyVoucher> vouchers,
            VoucherClause clause,
            VectorMask? includeMask = null
        )
        {
            // Vector256.Equals lanes are all-ones (-1) or 0, so the union of the per-name
            // masks is a bitwise OR; a signed Max would pick 0 over -1 and empty the union.
            // No names at all is `any`: every draw counts.
            Vector256<int> matchMask = clause.Vouchers.Length == 0 ? Vector256<int>.AllBitsSet : Vector256<int>.Zero;
            foreach (var v in clause.Vouchers)
                matchMask = Vector256.BitwiseOr(matchMask, VectorEnum256.Equals(vouchers, v));

            if (includeMask.HasValue)
            {
                matchMask = Vector256.BitwiseAnd(
                    matchMask,
                    MotelyVectorUtils.VectorMaskToConditionalSelectMask(includeMask.Value)
                );
            }

            return Vector256.Add(
                counts,
                Vector256.ConditionalSelect(matchMask, Vector256.Create(1), Vector256<int>.Zero)
            );
        }
    }
}
