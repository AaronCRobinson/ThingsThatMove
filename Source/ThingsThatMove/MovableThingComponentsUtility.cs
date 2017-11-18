using ThingsThatMove.AI;

namespace ThingsThatMove
{
    // TODO: does this need to be separate here?
    public class MovableThingComponentsUtility
    {
        public static void AddComponentsForSpawn(MovableThing thing)
        {
            if (thing.rotationTracker == null)
                thing.rotationTracker = new Thing_RotationTracker(thing);
            if (thing.pather == null)
                thing.pather = new Thing_PathFollower(thing);
        }

        public static void RemoveComponentsOnDespawned(MovableThing thing)
        {
            thing.rotationTracker = null;
            thing.pather = null;
        }
    }
}
