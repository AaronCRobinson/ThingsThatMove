using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Harmony;
using ThingsThatMove.AI;

namespace ThingsThatMove
{

    /*[StaticConstructorOnStartup]
    public static class Testing
    {
        static Testing()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.whyisthat.thingsthatmove.testing");

            harmony.Patch(AccessTools.Method(typeof(SelectionDrawer), "DrawSelectionBracketFor"), new HarmonyMethod(typeof(Testing), nameof(Prefix)), null);
        }

        static void Prefix(object obj)
        {
            Log.Message($"DrawSelectionBracketFor {obj}");
        }
    }*/

    // TODO: expose data

    // NOTE: consider Verse.AttachableThing 
    public class MovableThing : ThingWithComps, IMovableThing
    {
        private Thing_DrawTracker drawer;
        public Thing_PathFollower pather;
        public Thing_RotationTracker rotationTracker;

        // NOTE: consider lazy loading?
        public Thing_PathFollower Pather { get => pather; set => pather = value; }
        public Thing_RotationTracker RotationTracker { get => rotationTracker; set => rotationTracker = value; }

        public override Vector3 DrawPos
        {
            get => this.Drawer.DrawPos;
        }

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

        public override void DrawAt(Vector3 drawLoc, bool flip = false) => this.Drawer.DrawAt(drawLoc);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<Thing_RotationTracker>(ref this.rotationTracker, "rotationTracker", new object[]{this});
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (this.pather.curPath != null)
                this.pather.curPath.DrawPath(this);
        }

        // TODO: needs image
        // Selector.HandleMapClicks
        /*public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            // this.pather.StartPath(actor.jobs.curJob.GetTarget(ind), peMode);

            yield return new Command_Action
            {
                defaultLabel = "Move",
                action = () =>
                {
                    Log.Message("bang"); // TODO: handle map clicks
                }
            };
        }*/
    }
}
