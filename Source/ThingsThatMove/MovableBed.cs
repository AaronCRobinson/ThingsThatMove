using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

using ThingsThatMove.AI;

namespace ThingsThatMove
{
    public interface IMovableThing
    {
        bool Spawned { get; }
        IntVec3 Position { get; }
        /*
        Graphic Graphic { get; }
        Rot4 Rotation { get; }*/

        Vector3 DrawPos { get; }
        Thing_DrawTracker Drawer { get; }
        Thing_PathFollower Pather { get; }
        Thing_RotationTracker RotationTracker { get; }
        int TicksPerMoveCardinal { get; }
        int TicksPerMoveDiagonal { get; }
        //int TicksPerMove { get; }
    }

    public class MovableBed : Building_Bed, IMovableThing
    {
        private Thing_DrawTracker drawer;
        private Thing_PathFollower pather;
        public Thing_RotationTracker rotationTracker;

        public Thing_PathFollower Pather { get => pather; }
        public Thing_RotationTracker RotationTracker { get => rotationTracker; }
        public override Vector3 DrawPos { get => this.Drawer.DrawPos; }

        public Thing_DrawTracker Drawer
        {
            get
            {
                if (this.drawer == null)
                    this.drawer = new Thing_DrawTracker(this);
                return this.drawer;
            }
        }

        public int TicksPerMoveCardinal { get => this.TicksPerMove(false); }
        public int TicksPerMoveDiagonal { get => this.TicksPerMove(true); }

        // TODO: !!! move into static utility !!!
        // TODO: naming/understanding
        // TODO: carry tracker
        private int TicksPerMove(bool diagonal)
        {
            float num = this.GetStatValue(StatDefOf.MoveSpeed, true);
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
                if (base.Spawned && !base.Map.roofGrid.Roofed(base.Position))
                    num3 /= base.Map.weatherManager.CurMoveSpeedMultiplier;
                if (diagonal)
                    num3 *= 1.41421f;
            }
            int value = Mathf.RoundToInt(num3);
            return Mathf.Clamp(value, 1, 450);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (this.rotationTracker == null)
                this.rotationTracker = new Thing_RotationTracker(this);
            if (this.pather == null)
                this.pather = new Thing_PathFollower(this);
            this.Drawer.Notify_Spawned();
            this.rotationTracker.Notify_Spawned();
            this.Pather.ResetToCurrentPosition();
        }

        public override void DeSpawn()
        {
            base.DeSpawn();
            if (this.Pather != null)
                this.Pather.StopDead();
            this.rotationTracker = null;
            this.pather = null;
        }

        public override void Tick()
        {
            base.Tick();
            this.MovableThing_Tick();
        }
    }
}
