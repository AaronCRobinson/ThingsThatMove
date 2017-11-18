using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ThingsThatMove
{
    public class ThingTweener
    {
        private const float SpringTightness = 0.09f;

        private IMovableThing thing;
        private Vector3 tweenedPos = new Vector3(0f, 0f, 0f);
        private Vector3 lastTickSpringPos;
        private int lastDrawFrame = -1;

        public Vector3 TweenedPos { get => this.tweenedPos; }

        public Vector3 LastTickTweenedVelocity { get => this.TweenedPos - this.lastTickSpringPos; }

        // TODO: look at erroring here?
        public ThingTweener(Thing thing) => this.thing = thing as IMovableThing;

        // TODO: Review
        public void PreDrawPosCalculation()
        {
            if (this.lastDrawFrame == RealTime.frameCount)
                return;
            if (this.lastDrawFrame < RealTime.frameCount - 1)
                this.ResetTweenedPosToRoot();
            else
            {
                this.lastTickSpringPos = this.tweenedPos;
                float tickRateMultiplier = Find.TickManager.TickRateMultiplier;
                if (tickRateMultiplier < 5f)
                {
                    Vector3 a = this.TweenedPosRoot() - this.tweenedPos;
                    float num = 0.09f * (RealTime.deltaTime * 60f * tickRateMultiplier);
                    if (RealTime.deltaTime > 0.05f)
                        num = Mathf.Min(num, 1f);
                    this.tweenedPos += a * num;
                }
                else
                    this.tweenedPos = this.TweenedPosRoot();
            }
            this.lastDrawFrame = RealTime.frameCount;
        }

        public void ResetTweenedPosToRoot()
        {
            this.tweenedPos = this.TweenedPosRoot();
            this.lastTickSpringPos = this.tweenedPos;
        }

        private Vector3 TweenedPosRoot()
        {
            if (!this.thing.Spawned)
                return this.thing.Position.ToVector3Shifted();
            float num = this.MovedPercent();
            //return this.thing.pather.nextCell.ToVector3Shifted() * num + this.thing.Position.ToVector3Shifted() * (1f - num) + PawnCollisionTweenerUtility.PawnCollisionPosOffsetFor(this.pawn);
            return this.thing.Pather.nextCell.ToVector3Shifted() * num + this.thing.Position.ToVector3Shifted() * (1f - num);
        }

        private float MovedPercent()
        {
            if (!this.thing.Pather.Moving)
                return 0f;
            /*if (this.pawn.stances.FullBodyBusy)
                return 0f;*/
            if (this.thing.Pather.BuildingBlockingNextPathCell() != null)
                return 0f;
            if (this.thing.Pather.NextCellDoorToManuallyOpen() != null)
                return 0f;
            if (this.thing.Pather.WillCollideWithThingOnNextPathCell())
                return 0f;
            return 1f - this.thing.Pather.nextCellCostLeft / this.thing.Pather.nextCellCostTotal;
        }
    }
}
