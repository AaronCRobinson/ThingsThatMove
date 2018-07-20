using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using ThingsThatMove.AI;

namespace ThingsThatMove
{
    public static class MovableThingUtility
    {
        // TODO: review
        // TODO: naming/understanding
        // TODO: carry tracker
        public static int TicksPerMove(this Thing thing, bool diagonal)
        {
            float num = thing.GetStatValue(StatDefOf.MoveSpeed, true);
            // NOTE: can things be InRestraints? 

            /*if (this.carryTracker != null && this.carryTracker.CarriedThing != null && this.carryTracker.CarriedThing.def.category == ThingCategory.Pawn)
                num *= 0.6f;*/

            float num2 = num / 60f;
            float num3;
            if (num2 == 0f)
                num3 = 450f;
            else
            {
                num3 = 1f / num2;
                if (thing.Spawned && !thing.Map.roofGrid.Roofed(thing.Position))
                    num3 /= thing.Map.weatherManager.CurMoveSpeedMultiplier;
                if (diagonal)
                    num3 *= 1.41421f;
            }
            int value = Mathf.RoundToInt(num3);
            return Mathf.Clamp(value, 1, 450);
        }

        public static void MovableThing_SpawnSetup(this Thing thing)
        {
            if (thing is IMovableThing movableThing)
            {
                if (movableThing.RotationTracker == null)
                    movableThing.RotationTracker = new Thing_RotationTracker(thing);
                if (movableThing.Pather == null)
                    movableThing.Pather = new Thing_PathFollower(thing);
                movableThing.Drawer.Notify_Spawned();
                movableThing.RotationTracker.Notify_Spawned();
                movableThing.Pather.ResetToCurrentPosition();
            }
        }

        public static void MovableThing_DeSpawn(this Thing thing)
        {
            if (thing is IMovableThing movableThing)
            {
                if (movableThing.Pather != null)
                    movableThing.Pather.StopDead();
                movableThing.RotationTracker = null;
                movableThing.Pather = null;
            }
        }

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
