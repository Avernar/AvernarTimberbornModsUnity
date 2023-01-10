using UnityEngine;
using Bindito.Core;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using Timberborn.Common;
using Timberborn.BlockSystem;
using Timberborn.TickSystem;
using Timberborn.ConstructibleSystem;


namespace Avernar.Gauge
{
    public class AdvancedStreamGauge : TickableComponent, IFinishedStateListener
    {
        protected BlockObject _blockObject;
        protected Vector2Int _coordinates;
        protected BlockService _blockService;

        internal int Height => _blockObject.BlocksSpecification.Size.z;


        [Inject]
        public void InjectDependencies(BlockService blockService) {
            this._blockService = blockService;
        }

        public void OnEnterFinishedState() {
            this._coordinates = this.GetCoordinatesXY();
            this.enabled = true;
        }

        public void OnExitFinishedState() => this.enabled = false;

        internal virtual bool IsBase() => false;
        internal virtual bool IsMiddle() => false;
        internal virtual bool IsTop() => false;

        protected void Awake()
        {
            this._blockObject = this.GetComponent<BlockObject>();
            this.enabled = false;
        }

        internal AdvancedStreamGauge GetBelow () {
            if (!IsBase()) {
                Vector3Int coordinatesBelow = _blockObject.Coordinates - new Vector3Int(0, 0, 1);
                ReadOnlyCollection<BlockObject> objectsAt = _blockService.GetObjectsAt(coordinatesBelow);
                for (int index = 0; index < objectsAt.Count; ++index) {
                    BlockObject blockObject = objectsAt[index];
                    AdvancedStreamGauge asg = blockObject.GetComponent<AdvancedStreamGauge>();
                    if (asg) {
                        return asg;
                    }
                }
            }
            return null;
        }

        internal AdvancedStreamGauge GetAbove() {
            if (!IsTop()) {
                Vector3Int coordinatesAbove = _blockObject.Coordinates + new Vector3Int(0, 0, this.Height);
                ReadOnlyCollection<BlockObject> objectsAt = _blockService.GetObjectsAt(coordinatesAbove);
                for (int index = 0; index < objectsAt.Count; ++index) {
                    BlockObject blockObject = objectsAt[index];
                    AdvancedStreamGauge asg = blockObject.GetComponent<AdvancedStreamGauge>();
                    if (asg) {
                        return asg;
                    }
                }
            }
            return null;
        }

        public AdvancedStreamGaugeBase GetBase() {
            AdvancedStreamGauge asg = this;
            do {
                if (asg.IsBase()) {
                    return (AdvancedStreamGaugeBase)asg;
                }
                asg = asg.GetBelow();
            } while (asg);
            return null;
        }

        internal AdvancedStreamGaugeBase GetTop() {
            AdvancedStreamGauge asg = this;
            do {
                if (asg.IsBase()) {
                    return (AdvancedStreamGaugeBase)asg;
                }
                asg = asg.GetAbove();
            } while (asg);
            return null;
        }

        public override void Tick()
        {
        }

        protected Vector2Int GetCoordinatesXY() => this._blockObject.PositionedBlocks.GetOccupiedCoordinates().Select<Vector3Int, Vector2Int>((Func<Vector3Int, Vector2Int>)(coords => coords.XY())).Distinct<Vector2Int>().First<Vector2Int>();

    }
}
