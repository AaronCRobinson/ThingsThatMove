using System.Reflection;
using Verse;
using Verse.AI;
using Harmony;

namespace ThingsThatMove.Utils
{
    static class ReachabilityHelper
    {
        private static readonly FieldInfo FI_Reachability_map = AccessTools.Field(typeof(Reachability), "map");

        public static Map GetMap(this Reachability reachability) => (Map)FI_Reachability_map.GetValue(reachability);

        public static bool CanReach(this Reachability reachability, IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams)
        {
            // NOTE: not checking reachability.working
            if (traverseParams.thing != null)
            {
                if (!traverseParams.thing.Spawned)
                    return false;
                // ERROR: thing.map != this.map
            }
            if (ReachabilityImmediate.CanReachImmediate(start, dest, reachability.GetMap(), peMode, traverseParams.thing))
                return true;
            if (!dest.IsValid)
                return false;
            if (dest.HasThing && dest.Thing.Map != reachability.GetMap())
                return false;
            if (!start.InBounds(reachability.GetMap()) || !dest.Cell.InBounds(reachability.GetMap()))
                return false;
            if (peMode == PathEndMode.OnCell || peMode == PathEndMode.Touch || peMode == PathEndMode.ClosestTouch)
            {
                Room room = RegionAndRoomQuery.RoomAtFast(start, reachability.GetMap(), RegionType.Set_Passable);
                if (room != null && room == RegionAndRoomQuery.RoomAtFast(dest.Cell, reachability.GetMap(), RegionType.Set_Passable))
                {
                    return true;
                }
            }

            return true; // TEMP
        }

    }

    // OVERWRITING: Verse.ReachabilityImmediate
    static class ReachabilityImmediate
    {
        // TODO: review logic
        public static bool CanReachImmediate(IntVec3 start, LocalTargetInfo target, Map map, PathEndMode peMode, Thing thing)
        {
            if (!target.IsValid) return false;

            // TODO
            //target = (LocalTargetInfo)GenPath.ResolvePathMode(pawn, target.ToTargetInfo(map), ref peMode);

            if (target.HasThing)
            {
                Thing thing2 = target.Thing;
                if (!thing2.Spawned)
                    return false;
                if (thing2.Map != map)
                    return false;
            }
            if (!target.HasThing || (target.Thing.def.size.x == 1 && target.Thing.def.size.z == 1))
            {
                if (start == target.Cell)
                    return true;
            }
            else if (start.IsInside(target.Thing))
                return true;
            return peMode == PathEndMode.Touch && TouchPathEndModeUtility.IsAdjacentOrInsideAndAllowedToTouch(start, target, map);
        }

        public static bool CanReachImmediate(this Thing thing, LocalTargetInfo target, PathEndMode peMode)
        {
            return thing.Spawned && ReachabilityImmediate.CanReachImmediate(thing.Position, target, thing.Map, peMode, thing);
        }

    }

}
