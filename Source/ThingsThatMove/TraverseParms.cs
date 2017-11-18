using System;
using Verse;

namespace ThingsThatMove
{
    public struct TraverseParms : IEquatable<TraverseParms>
    {
        public Thing thing;
        public TraverseMode mode;
        public Danger maxDanger;
        public bool canBash;

        public static TraverseParms For(Thing thing, Danger maxDanger = Danger.Deadly, TraverseMode mode = TraverseMode.ByPawn, bool canBash = false)
        {
            if (thing == null)
            {
                Log.Error("TraverseParms for null pawn.");
                return TraverseParms.For(TraverseMode.NoPassClosedDoors, maxDanger, canBash);
            }
            return new TraverseParms
            {
                thing = thing,
                maxDanger = maxDanger,
                mode = mode,
                canBash = canBash
            };
        }

        public static TraverseParms For(TraverseMode mode, Danger maxDanger = Danger.Deadly, bool canBash = false)
        {
            return new TraverseParms
            {
                thing = null,
                mode = mode,
                maxDanger = maxDanger,
                canBash = canBash
            };
        }

        // public void Validate()

        public static implicit operator TraverseParms(TraverseMode m)
        {
            if (m == TraverseMode.ByPawn)
            {
                throw new InvalidOperationException("Cannot implicitly convert TraverseMode.ByPawn to RegionTraverseParameters.");
            }
            return TraverseParms.For(m, Danger.Deadly, false);
        }

        public static bool operator ==(TraverseParms a, TraverseParms b)
        {
            return a.thing == b.thing && a.mode == b.mode && a.canBash == b.canBash && a.maxDanger == b.maxDanger;
        }

        public static bool operator !=(TraverseParms a, TraverseParms b)
        {
            return a.thing != b.thing || a.mode != b.mode || a.canBash != b.canBash || a.maxDanger != b.maxDanger;
        }

        public override bool Equals(object obj)
        {
            return obj is TraverseParms && this.Equals((TraverseParms)obj);
        }

        public bool Equals(TraverseParms other)
        {
            return other.thing == this.thing && other.mode == this.mode && other.canBash == this.canBash && other.maxDanger == this.maxDanger;
        }

        public override int GetHashCode()
        {
            int seed = (!this.canBash) ? 0 : 1;
            if (this.thing != null)
                seed = Gen.HashCombine<Thing>(seed, this.thing);
            else
                seed = Gen.HashCombineStruct<TraverseMode>(seed, this.mode);
            return Gen.HashCombineStruct<Danger>(seed, this.maxDanger);
        }

    }
}
