using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ThingsThatMove.AI
{
    public class ThingPath : IDisposable
    {
        private List<IntVec3> nodes = new List<IntVec3>(128);
        private float totalCostInt;
        private int curNodeIndex;
        public bool inUse;

        public bool Found { get => this.totalCostInt >= 0f; }
        public float TotalCostInt { get => this.totalCostInt; }
        public int NodesLeftCount { get => this.curNodeIndex + 1; }
        public List<IntVec3> NodesReversed { get => this.nodes; }
        public IntVec3 FirstNode { get => this.nodes[this.nodes.Count - 1]; }
        public IntVec3 LastNode { get => this.nodes[0]; }
        public static ThingPath NotFound { get => ThingPathPool.NotFoundPath; }
        public void AddNode(IntVec3 nodePosition) => this.nodes.Add(nodePosition);

        public void SetupFound(float totalCost)
        {
            if (this == ThingPath.NotFound)
            {
                Log.Warning("Calling SetupFound with totalCost=" + totalCost + " on PawnPath.NotFound");
                return;
            }
            this.totalCostInt = totalCost;
            this.curNodeIndex = this.nodes.Count - 1;
        }

        public void Dispose() => this.ReleaseToPool();

        public void ReleaseToPool()
        {
            if (this != ThingPath.NotFound)
            {
                this.totalCostInt = 0f;
                this.nodes.Clear();
                this.inUse = false;
            }
        }

        public static ThingPath NewNotFound() => new ThingPath { totalCostInt = -1f };

        public IntVec3 ConsumeNextNode()
        {
            IntVec3 result = this.Peek(1);
            this.curNodeIndex--;
            return result;
        }

        public IntVec3 Peek(int nodesAhead) => this.nodes[this.curNodeIndex - nodesAhead];

        public override string ToString()
        {
            if (!this.Found)
                return "ThingPath(not found)";

            if (!this.inUse)
                return "ThingPath(not in use)";

            return string.Concat(new object[]
            {
                "ThingPath(nodeCount= ",
                this.nodes.Count,
                (this.nodes.Count <= 0) ? string.Empty : string.Concat(new object[]
                {
                    " first=",
                    this.FirstNode,
                    " last=",
                    this.LastNode
                }),
                " cost=",
                this.totalCostInt,
                " )"
            });
        }

        public void DrawPath(Thing pathingThing)
        {
            if (!this.Found)
                return;

            float y = Altitudes.AltitudeFor(AltitudeLayer.Item);
            if (this.NodesLeftCount > 0)
            {
                for (int i = 0; i < this.NodesLeftCount - 1; i++)
                {
                    Vector3 a = this.Peek(i).ToVector3Shifted();
                    a.y = y;
                    Vector3 b = this.Peek(i + 1).ToVector3Shifted();
                    b.y = y;
                    GenDraw.DrawLineBetween(a, b);
                }
                if (pathingThing != null)
                {
                    Vector3 drawPos = pathingThing.DrawPos;
                    drawPos.y = y;
                    Vector3 b2 = this.Peek(0).ToVector3Shifted();
                    b2.y = y;
                    if ((drawPos - b2).sqrMagnitude > 0.01f)
                    {
                        GenDraw.DrawLineBetween(drawPos, b2);
                    }
                }
            }
        }
    }
}
