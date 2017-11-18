using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ThingsThatMove.Utils
{
    // NOTE: RimWorld already has a ThingUtility. Avoiding name conflict here.
    public static class ThingHelper
    {
        // TODO: clean-up, refactor, etc.

        // RimWorld.PawnUtility.AnyPawnBlockingPathAt
        public static bool AnyThingBlockingPathAt(IntVec3 c, Thing thing)
        {
            return ThingHelper.ThingBlockingPathAt(c, thing) != null;
        }

        // RimWorld.PawnUtility.PawnBlockingPathAt
        public static Thing ThingBlockingPathAt(IntVec3 c, Thing thing)
        {
            List<Thing> thingList = c.GetThingList(thing.Map);
            if (thingList.Count == 0)
                return null;

            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing2 = thingList[i];
                if (thing.def.EverHaulable)
                    if (GenConstruct.BlocksConstruction(thing, thing2)) //&& thing != pawnToIgnore && thing != thingToIgnore)
                        return thing;
            }

            return null;
        }
    }
}
