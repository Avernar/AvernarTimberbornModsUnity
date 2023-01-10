using UnityEngine;
using Bindito.Core;
using Timberborn.Persistence;
using Timberborn.WaterBuildings;
using Timberborn.WaterSystem;
using Timberborn.Localization;

namespace Avernar.Gauge
{
    public class AdvancedStreamGaugeBase : AdvancedStreamGauge, IPersistentEntity
    {
        private static readonly string IncompleteLocKey = "Avernar.AdvancedStreamGauge.Incomplete";
        private static readonly string OverflowLocKey = "Avernar.AdvancedStreamGauge.Overflow";
        private static readonly string HighLocKey = "Avernar.AdvancedStreamGauge.High";
        private static readonly string NormalWasHighLocKey = "Avernar.AdvancedStreamGauge.NormalWasHigh";
        private static readonly string NormalWasLowLocKey = "Avernar.AdvancedStreamGauge.NormalWasLow";
        private static readonly string LowLocKey = "Avernar.AdvancedStreamGauge.Low";
        private static readonly string DryLocKey = "Avernar.AdvancedStreamGauge.Dry";
        private static readonly ComponentKey StreamGaugeKey = new(nameof(AdvancedStreamGaugeBase));
        private static readonly PropertyKey<bool> CompleteKey = new(nameof(Complete));
        private static readonly PropertyKey<int> GaugeHeightKey = new(nameof(GaugeHeight));
        private static readonly PropertyKey<float> HighSetPointKey = new(nameof(HighSetPoint));
        private static readonly PropertyKey<float> LowSetPointKey = new(nameof(LowSetPoint));
        private static readonly PropertyKey<bool> WasHighNotWasLowKey = new(nameof(_wasHighNotWasLow));
        private static readonly PropertyKey<float> WaterLevelKey = new(nameof(_waterLevel));
        private static readonly PropertyKey<float> WaterCurrentKey = new(nameof(_waterCurrent));
        private static readonly PropertyKey<float> HighestWaterLevelKey = new(nameof(HighestWaterLevel));
        private ILoc _loc;

        private bool _wasHighNotWasLow;
        private float _waterLevel;
        private float _waterCurrent;
        private IWaterService _waterService;
        protected StreamGaugeAnimationController _streamGaugeAnimationController;

        public int BaseZ => this._blockObject.CoordinatesAtBaseZ.z;

        public bool Complete { get; private set; }
        public int GaugeHeight { get; private set; }
        public float HighestWaterLevel { get; private set; }
        public float HighSetPoint { get; private set; }
        public float LowSetPoint { get; private set; }
        public int MinLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public float WaterLevel { get; private set; }
        public float WaterCurrent { get; private set; }


        public AdvancedStreamGaugeStatus Status { get; private set; }

        public string StatusText() {
            switch (this.Status) {
                case AdvancedStreamGaugeStatus.Incomplete:
                    return this._loc.T(AdvancedStreamGaugeBase.IncompleteLocKey);

                case AdvancedStreamGaugeStatus.Dry:
                    return this._loc.T(AdvancedStreamGaugeBase.DryLocKey);

                case AdvancedStreamGaugeStatus.Low:
                    return this._loc.T(AdvancedStreamGaugeBase.LowLocKey);

                case AdvancedStreamGaugeStatus.NormalWasLow:
                    return this._loc.T(AdvancedStreamGaugeBase.NormalWasLowLocKey);

                case AdvancedStreamGaugeStatus.NormalWasHigh:
                    return this._loc.T(AdvancedStreamGaugeBase.NormalWasHighLocKey);

                case AdvancedStreamGaugeStatus.High:
                    return this._loc.T(AdvancedStreamGaugeBase.HighLocKey);

                case AdvancedStreamGaugeStatus.Overflow:
                    return this._loc.T(AdvancedStreamGaugeBase.OverflowLocKey);
            }
            return "";
        }

        public void ResetHighestWaterLevel() {
            this.HighestWaterLevel = 0.0f;
            this.UpdateMarkerHeight();
        }

        public void UpdateHighSetPoint(float f) {
            HighSetPoint = f;
            UpdateStatusVariables();
        }

        public void UpdateLowSetPoint(float f) {
            LowSetPoint = f;
            UpdateStatusVariables();
        }

        [Inject]
        public void InjectDependencies(ILoc loc, IWaterService waterService) {
            this._loc = loc;
            this._waterService = waterService;
        }

        public new void Awake() {
            this._streamGaugeAnimationController = this.GetComponent<StreamGaugeAnimationController>();
            base.Awake();
            this.Complete = false;
            this.GaugeHeight = Height;
            this.HighestWaterLevel = 0.0f;
            this.HighSetPoint = GaugeHeight;
            this.LowSetPoint = 0;
            this.UpdateStatusVariables();
        }

        private void UpdateStatusVariables() {
            this.MinLevel = 0;
            this.MaxLevel = this.GaugeHeight;

            if (Complete) {
                this.WaterLevel = Mathf.Min(this._waterLevel, (float)this.GaugeHeight);
                this.WaterCurrent = this._waterCurrent;
                if (this.WaterLevel == 0.0f) {
                    this.Status = AdvancedStreamGaugeStatus.Dry;
                    this._wasHighNotWasLow = false;
                }
                else if (this.WaterLevel < LowSetPoint) {
                    this.Status = AdvancedStreamGaugeStatus.Low;
                    this._wasHighNotWasLow = false;
                }
                else if (this._waterLevel > (float)this.GaugeHeight) {
                    this.Status = AdvancedStreamGaugeStatus.Overflow;
                    this._wasHighNotWasLow = true;
                }
                else if (this.WaterLevel > HighSetPoint) {
                    this.Status = AdvancedStreamGaugeStatus.High;
                    this._wasHighNotWasLow = true;
                }
                else if (this._wasHighNotWasLow) {
                    this.Status = AdvancedStreamGaugeStatus.NormalWasHigh;
                }
                else {
                    this.Status = AdvancedStreamGaugeStatus.NormalWasLow;
                }
            }
            else {
                this.WaterLevel = 0.0f;
                this.WaterCurrent = 0.0f;
                this.Status = AdvancedStreamGaugeStatus.Incomplete;
            }
        }

        public override void Tick() {
            if (this.HighestWaterLevel > (float)this.GaugeHeight) {
                this.HighestWaterLevel = (float)this.GaugeHeight;
                this.UpdateMarkerHeight();
            }

            this._waterLevel = Mathf.Max(this._waterService.WaterHeight(this._coordinates) - (float)this.BaseZ, 0.0f);
            this._waterCurrent = Mathf.Max(Mathf.Abs(this._waterService.WaterFlowDirection(this._coordinates).x), Mathf.Abs(this._waterService.WaterFlowDirection(this._coordinates).y));

            (this.Complete, this.GaugeHeight) = this.CalculateTotalHeight();
            
            UpdateStatusVariables();

            if (this.HighSetPoint > this.MaxLevel) {
                this.HighSetPoint = this.MaxLevel;
            }

            if (this.LowSetPoint > this.MaxLevel) {
                this.LowSetPoint = this.MaxLevel;
            }

            if ((double)this.HighestWaterLevel < (double)this.WaterLevel) {
                this.HighestWaterLevel = this.WaterLevel;
                this.UpdateMarkerHeight();
            }
        }

        private (bool, int) CalculateTotalHeight() {
            int totalHeight = 0;
            bool enabled = true;
            AdvancedStreamGauge asg = this;
            do {
                totalHeight += asg.Height;
                if (!asg.enabled) {
                    enabled = false;
                }
                if (asg.IsTop()) {
                    return (enabled, totalHeight);
                }
                asg = asg.GetAbove();
            } while (asg);
            return (false, totalHeight);
        }

        internal override bool IsBase() => true;

        public void Save(IEntitySaver entitySaver) {
            IObjectSaver component = entitySaver.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey);
            component.Set(AdvancedStreamGaugeBase.WasHighNotWasLowKey, this._wasHighNotWasLow);
            component.Set(AdvancedStreamGaugeBase.HighSetPointKey, this.HighSetPoint);
            component.Set(AdvancedStreamGaugeBase.LowSetPointKey, this.LowSetPoint);
            component.Set(AdvancedStreamGaugeBase.HighestWaterLevelKey, this.HighestWaterLevel);
        }

        public void Load(IEntityLoader entityLoader) {
            IObjectLoader component = entityLoader.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey);
            this._wasHighNotWasLow = component.Get(AdvancedStreamGaugeBase.WasHighNotWasLowKey);
            this.HighSetPoint = component.Get(AdvancedStreamGaugeBase.HighSetPointKey);
            this.LowSetPoint = component.Get(AdvancedStreamGaugeBase.LowSetPointKey);
            this.HighestWaterLevel = component.Get(AdvancedStreamGaugeBase.HighestWaterLevelKey);

            this._streamGaugeAnimationController.SetHeight(this.HighestWaterLevel);
        }

        private void UpdateMarkerHeight() => this._streamGaugeAnimationController.SetHeight(Mathf.Min(this.HighestWaterLevel, (float)this.GaugeHeight - 0.02f));
    }
}
