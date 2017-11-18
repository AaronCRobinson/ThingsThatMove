using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ThingsThatMove
{
    public class Thing_DrawTracker
    {
        private Thing thing;
        private ThingTweener tweener;
        private ThingRenderer renderer;

        public Thing_DrawTracker(Thing thing)
        {
            this.thing = thing;
            this.tweener = new ThingTweener(thing);
            //this.jitterer = new JitterHandler();
            //this.leaner = new PawnLeaner(pawn);

            this.renderer = new ThingRenderer(thing);

            //this.ui = new PawnUIOverlay(pawn);
            //this.footprintMaker = new PawnFootprintMaker(pawn);
            //this.breathMoteMaker = new PawnBreathMoteMaker(pawn);
        }

        public Vector3 DrawPos
        {
            get
            {
                this.tweener.PreDrawPosCalculation();
                Vector3 vector = this.tweener.TweenedPos;
                //vector += this.jitterer.CurrentOffset;
                //vector += this.leaner.LeanOffset;
                vector.y = this.thing.def.Altitude;
                return vector;
            }
        }

        public void DrawTrackerTick()
        {
            if (!this.thing.Spawned)
                return;
            if (Current.ProgramState == ProgramState.Playing && !Find.CameraDriver.CurrentViewRect.ExpandedBy(3).Contains(this.thing.Position))
                return;
            //this.jitterer.JitterHandlerTick();
            //this.footprintMaker.FootprintMakerTick();
            //this.breathMoteMaker.BreathMoteMakerTick();
            //this.leaner.LeanerTick();

            this.renderer.RendererTick();
        }

        public void DrawAt(Vector3 loc)
        {
            this.renderer.RenderThingAt(loc);
        }

        public void Notify_Spawned() => this.tweener.ResetTweenedPosToRoot();
    }
}
