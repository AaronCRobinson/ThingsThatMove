using UnityEngine;
using Verse;

namespace ThingsThatMove
{
    // TODO: what to do here...
    public class ThingRenderer
    {
        private Thing thing;

        public ThingRenderer(Thing thing) => this.thing = thing;

        // TODO: needs extension
        public void RenderThingAt(Vector3 drawLoc, bool flip = false)
        {
            /*if (!this.graphics.AllResolved)
                this.graphics.ResolveAllGraphics();*/

            if (this.thing.Spawned)
            {
                this.thing.Graphic.Draw(drawLoc, (!flip) ? this.thing.Rotation : this.thing.Rotation.Opposite, this.thing, 0f);

                if (this.thing is IMovableThing movableThing)
                {
                    movableThing.Pather.PatherDraw();
                }
            }
        }

        public void RendererTick()
        {
            //this.wiggler.WigglerTick();
            //this.effecters.EffectersTick();
        }

    }
}