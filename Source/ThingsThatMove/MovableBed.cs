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
        // From Thing -> IThing & use inheritance??
        bool Spawned { get; }
        IntVec3 Position { get; }
        /*
        Graphic Graphic { get; }
        Rot4 Rotation { get; }*/

        Vector3 DrawPos { get; }
        Thing_DrawTracker Drawer { get; }
        Thing_PathFollower Pather { get; set; }
        Thing_RotationTracker RotationTracker { get; set; }
        int TicksPerMoveCardinal { get; }
        int TicksPerMoveDiagonal { get; }
    }

    public class MovableBed : Building_Bed, IMovableThing
    {
        private Thing_DrawTracker drawer;
        private Thing_PathFollower pather;
        public Thing_RotationTracker rotationTracker;

        public Thing_PathFollower Pather { get => pather; set => pather = value; }
        public Thing_RotationTracker RotationTracker { get => rotationTracker; set => rotationTracker = value; }
        public override Vector3 DrawPos { get => Drawer.DrawPos; }

        public Thing_DrawTracker Drawer
        {
            get
            {
                if (drawer == null)
                    drawer = new Thing_DrawTracker(this);
                return drawer;
            }
        }

        public int TicksPerMoveCardinal { get => this.TicksPerMove(false); }
        public int TicksPerMoveDiagonal { get => this.TicksPerMove(true); }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.MovableThing_SpawnSetup();
        }

        public override void DeSpawn()
        {
            base.DeSpawn();
            this.MovableThing_DeSpawn();
        }

        public override void Tick()
        {
            base.Tick();
            this.MovableThing_Tick();
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false) => Drawer.DrawAt(drawLoc);

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (this.pather.curPath != null)
                this.pather.curPath.DrawPath(this);

        }
    }

}
