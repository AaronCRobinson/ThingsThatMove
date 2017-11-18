using Verse;
using Verse.AI;

namespace ThingsThatMove
{
    public static class GenPath
    {
        public static TargetInfo ResolvePathMode(Thing thing, TargetInfo dest, ref PathEndMode peMode)
        {
            if (dest.HasThing && !dest.Thing.Spawned)
            {
                peMode = PathEndMode.Touch;
                return dest;
            }
            if (peMode == PathEndMode.InteractionCell)
            {
                if (!dest.HasThing)
                {
                    Log.Error("Pathed to cell " + dest + " with PathEndMode.InteractionCell.");
                }
                peMode = PathEndMode.OnCell;
                return new TargetInfo(dest.Thing.InteractionCell, dest.Thing.Map, false);
            }

            // TODO: polish
            if (peMode == PathEndMode.ClosestTouch)
                //peMode = GenPath.ResolveClosestTouchPathMode(pawn, dest.Map, dest.Cell);
                Log.Error("Unsupported PathEndMode");

            return dest;
        }
    }

}
