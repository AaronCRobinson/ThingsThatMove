using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ThingsThatMove
{
    public static class MovableThingUtility
    {
        public static void MovableThing_Tick(this Thing thing)
        {
            if (!ThingOwnerUtility.ContentsFrozen(thing.ParentHolder) && thing is IMovableThing movableThing)
            {
                if (thing.Spawned)
                    movableThing?.Pather.PatherTick();
                if (thing.Spawned)
                {
                    movableThing?.Drawer.DrawTrackerTick();
                    movableThing?.RotationTracker.RotationTrackerTick();
                }
            }
        }
    }
}
