using UnityEngine;
using Bindito.Core;
using Timberborn.Persistence;
using Timberborn.WaterBuildings;
using Timberborn.WaterSystem;

namespace Avernar.Gauge
{
    public class AdvancedStreamGaugeBase : AdvancedStreamGauge, IPersistentEntity
    {
        private static readonly ComponentKey StreamGaugeKey = new ComponentKey(nameof(AdvancedStreamGaugeBase));
        private static readonly PropertyKey<bool> CompleteKey = new PropertyKey<bool>(nameof(Complete));
        private static readonly PropertyKey<int> GaugeHeightKey = new PropertyKey<int>(nameof(GaugeHeight));
        private static readonly PropertyKey<float> HighSetPointKey = new PropertyKey<float>(nameof(HighSetPoint));
        private static readonly PropertyKey<float> LowSetPointKey = new PropertyKey<float>(nameof(LowSetPoint));
        private static readonly PropertyKey<bool> WasHighNotWasLowKey = new PropertyKey<bool>(nameof(_wasHighNotWasLow));
        private static readonly PropertyKey<float> WaterLevelKey = new PropertyKey<float>(nameof(_waterLevel));
        private static readonly PropertyKey<float> WaterCurrentKey = new PropertyKey<float>(nameof(_waterCurrent));
        private static readonly PropertyKey<float> HighestWaterLevelKey = new PropertyKey<float>(nameof(HighestWaterLevel));

        private bool _wasHighNotWasLow;
        private float _waterLevel;
        private float _waterCurrent;
        private IWaterService _waterService;
        protected StreamGaugeAnimationController _streamGaugeAnimationController;

        public bool Complete { get; private set; }
        public int GaugeHeight { get; private set; }
        public float HighestWaterLevel { get; private set; }
        public float HighSetPoint { get; private set; }
        public float LowSetPoint { get; private set; }
        public int MinLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public float WaterLevel { get; private set; }
        public float WaterCurrent { get; private set; }


        // Status for Pump and Floodgate control.
        public bool Overflow { get; private set; }
        public bool High { get; private set; }
        public bool NormalWasHigh { get; private set; }
        public bool NormalWasLow { get; private set; }
        public bool Low { get; private set; }
        public bool Dry { get; private set; }


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
        public void InjectDependencies(IWaterService waterService) => this._waterService = waterService;

        public new void Awake() {
            this._streamGaugeAnimationController = this.GetComponent<StreamGaugeAnimationController>();
            base.Awake();
            Complete = false;
            GaugeHeight = Height;
            HighestWaterLevel = 0.0f;
            HighSetPoint = GaugeHeight;
            LowSetPoint = 0;
            UpdateStatusVariables();
        }

        private void UpdateStatusVariables() {
            this.MinLevel = 0;
            this.MaxLevel = this.GaugeHeight;
            this.WaterLevel = this.Complete ? Mathf.Min(this._waterLevel, (float)this.GaugeHeight) : 0.0f;
            this.WaterCurrent = this.Complete ? this._waterCurrent : 0.0f;
            this.Overflow = this.Complete && this._waterLevel > (float)this.GaugeHeight;
            this.High = this.Complete && this.WaterLevel > HighSetPoint;
            this.Low = this.Complete && this.WaterLevel < LowSetPoint;
            this.Dry = this.Complete && this.WaterLevel == 0;
            if(this.High || this.Overflow) {
                this._wasHighNotWasLow = true;
            }
            if (this.Low || this.Dry) {
                this._wasHighNotWasLow = false;
            }
            this.NormalWasHigh = this.Complete && (this.WaterLevel >= LowSetPoint && this.WaterLevel <= HighSetPoint) && this._wasHighNotWasLow;
            this.NormalWasLow = this.Complete && (this.WaterLevel >= LowSetPoint && this.WaterLevel <= HighSetPoint) && !this._wasHighNotWasLow;
        }

        public override void Tick() {
            if (this.HighestWaterLevel > (float)this.GaugeHeight) {
                this.HighestWaterLevel = (float)this.GaugeHeight;
                this.UpdateMarkerHeight();
            }

            this._waterLevel = Mathf.Max(this._waterService.WaterHeight(this._coordinates) - (float)this._blockObject.CoordinatesAtBaseZ.z, 0.0f);
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
            entitySaver.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Set(AdvancedStreamGaugeBase.WasHighNotWasLowKey, this._wasHighNotWasLow);
            entitySaver.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Set(AdvancedStreamGaugeBase.HighSetPointKey, this.HighSetPoint);
            entitySaver.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Set(AdvancedStreamGaugeBase.LowSetPointKey, this.LowSetPoint);
            entitySaver.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Set(AdvancedStreamGaugeBase.HighestWaterLevelKey, this.HighestWaterLevel);
        }

        public void Load(IEntityLoader entityLoader)
        {
            this._wasHighNotWasLow = entityLoader.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Get(AdvancedStreamGaugeBase.WasHighNotWasLowKey);
            this.HighSetPoint = entityLoader.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Get(AdvancedStreamGaugeBase.HighSetPointKey);
            this.LowSetPoint = entityLoader.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Get(AdvancedStreamGaugeBase.LowSetPointKey);
            this.HighestWaterLevel = entityLoader.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey).Get(AdvancedStreamGaugeBase.HighestWaterLevelKey);

            this._streamGaugeAnimationController.SetHeight(this.HighestWaterLevel);
        }

        private void UpdateMarkerHeight() => this._streamGaugeAnimationController.SetHeight(Mathf.Min(this.HighestWaterLevel, (float)this.GaugeHeight - 0.02f));
    }
}
