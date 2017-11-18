using Verse;
using RimWorld;

namespace ThingsThatMove.Utils
{
    public static class BuildingUtility
    {
        // Verse.Thing.BlocksPawn
        public static bool BlocksThing(this Building edifice) => edifice.def.passability == Traversability.Impassable;
    }

    // TODO: polish
    public static class BuildingDoorHelper
    {
        public static bool ThingCanOpen(this Building_Door door, Thing thing)
        {
            // RimWorld.Building_Door.PawnCanOpen
            // TODO: finish
            return (door.Faction == null);
        }
    }
}
