using UnityEngine;
using Bindito.Core;
using Timberborn.Persistence;
using Timberborn.WaterBuildings;
using Timberborn.WaterSystem;
using Timberborn.Localization;
using System.Collections.Generic;

namespace Avernar.Gauge {
    public class AdvancedStreamGaugeBase : AdvancedStreamGauge, IPersistentEntity {
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
        private static readonly PropertyKey<int> AntiSloshKey = new(nameof(AntiSlosh));
        private static readonly ListKey<float> HistoryKey = new(nameof(History));
        private ILoc _loc;

        private bool _wasHighNotWasLow;
        private float _waterLevel;
        private float _waterCurrent;
        private IWaterService _waterService;
        protected StreamGaugeAnimationController _streamGaugeAnimationController;

        private List<float> History;
        private List<float> _sortedHistory;
        private int _q1Index;
        private int _q3Index;

        public bool Complete { get; private set; }
        public int GaugeHeight { get; private set; }
        public float HighestWaterLevel { get; private set; }
        public float HighSetPoint { get; private set; }
        public float LowSetPoint { get; private set; }
        public int AntiSlosh { get; private set; }
        public int MinLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public float WaterLevel { get; private set; }
        public float WaterCurrent { get; private set; }

        public float AbsoluteWaterLevel => WaterLevel + this._blockObject.CoordinatesAtBaseZ.z;

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

        public void UpdateAntiSlosh(int n) {
            if (AntiSlosh != n) {
                AntiSlosh = n;
                History.Clear();
                _sortedHistory.Clear();
                if (AntiSlosh > 0) {
                    int size = 3 + (AntiSlosh - 1) * 4;
                    for (int i = 0; i < size; i++) {
                        History.Add(WaterLevel);
                        _sortedHistory.Add(WaterLevel);
                    }
                    _q1Index = AntiSlosh - 1;
                    _q3Index = 2 + (AntiSlosh - 1) * 3;
                }
                else {
                    History.Add(WaterLevel);
                    _sortedHistory.Add(WaterLevel);
                }
            }
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
            this.History = new List<float> { 0f };
            this._sortedHistory = new List<float> { 0f };
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
            Calculate(true);
        }

        public void Calculate(bool isTick = false) {
            (this.Complete, this.GaugeHeight) = this.CalculateTotalHeight();

            if (!this.Complete) {
                if (this._sortedHistory.Count > 0 && this._sortedHistory[this._sortedHistory.Count - 1] != 0f) {
                    for (int i = 0; i < this._sortedHistory.Count; i++) { this._sortedHistory[i] = 0f; }
                    for (int i = 0; i < this.History.Count; i++) { this.History[i] = 0f; }
                }
                this._waterLevel = 0f;
                this._waterCurrent = 0f;
                this.HighestWaterLevel = 0f;
                this.UpdateMarkerHeight();
                this.UpdateStatusVariables();
            }
            else {
                if (this.HighestWaterLevel > (float)this.GaugeHeight) {
                    this.HighestWaterLevel = (float)this.GaugeHeight;
                    this.UpdateMarkerHeight();
                }

                float level = Mathf.Max(this._waterService.WaterHeight(this._coordinates) - (float)this._blockObject.CoordinatesAtBaseZ.z, 0.0f);
                if (isTick) {
                    if (AntiSlosh > 0) {
                        var index = _sortedHistory.IndexOf(History[0]);
                        if (index != -1) {
                            _sortedHistory.RemoveAt(index);
                            var index2 = _sortedHistory.BinarySearch(level);
                            if (index2 < 0) index2 = ~index2;
                            _sortedHistory.Insert(index2, level);
                        }
                        History.RemoveAt(0);
                        History.Add(level);
                        if (index == -1) {
                            _sortedHistory = new List<float>(History);
                            _sortedHistory.Sort();
                            Plugin.Log.LogWarning("_sortedHistory element not found");
                        }
                        //Plugin.Log.LogInfo(string.Format("{0} | {1}", string.Join(" ", History), string.Join(" ", _sortedHistory)));

                        float q1 = _sortedHistory[_q1Index];
                        float q3 = _sortedHistory[_q3Index];
                        float iqr = q3 - q1;
                        float lowerFence = q1 - (1.5f * iqr);
                        float upperFence = q3 + (1.5f * iqr);
                        float acc = 0.0f;
                        int count = 0;
                        foreach (float f in _sortedHistory) {
                            if (lowerFence <= f && f <= upperFence) {
                                acc += f;
                                count++;
                            }
                        }
                        if (count > 0) {
                            level = acc / count;
                        }
                        //Plugin.Log.LogInfo(string.Format("q1={0} q3={1} iqr={2} lowerFence={3} upperFence={4} acc={5} count={6} level={7}", q1.ToString("0.000"), q3.ToString("0.000"), iqr.ToString("0.000"), lowerFence.ToString("0.000"), upperFence.ToString("0.000"), acc.ToString("0.000"), count.ToString(""), level.ToString("0.000")));
                    }
                    else {
                        History[0] = level;
                        _sortedHistory[0] = level;
                    }
                }
                this._waterLevel = level;

                this._waterCurrent = Mathf.Max(Mathf.Abs(this._waterService.WaterFlowDirection(this._coordinates).x), Mathf.Abs(this._waterService.WaterFlowDirection(this._coordinates).y));

                this.UpdateStatusVariables();

                if ((double)this.HighestWaterLevel < (double)this.WaterLevel) {
                    this.HighestWaterLevel = this.WaterLevel;
                    this.UpdateMarkerHeight();
                }
            }

            if (this.HighSetPoint > this.MaxLevel) {
                this.HighSetPoint = this.MaxLevel;
            }

            if (this.LowSetPoint > this.MaxLevel) {
                this.LowSetPoint = this.MaxLevel;
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
            IObjectSaver objectLoader = entitySaver.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey);
            objectLoader.Set(AdvancedStreamGaugeBase.WasHighNotWasLowKey, this._wasHighNotWasLow);
            objectLoader.Set(AdvancedStreamGaugeBase.HighSetPointKey, this.HighSetPoint);
            objectLoader.Set(AdvancedStreamGaugeBase.LowSetPointKey, this.LowSetPoint);
            objectLoader.Set(AdvancedStreamGaugeBase.HighestWaterLevelKey, this.HighestWaterLevel);
            objectLoader.Set(AdvancedStreamGaugeBase.AntiSloshKey, this.AntiSlosh);
            objectLoader.Set(AdvancedStreamGaugeBase.HistoryKey, this.History);
        }

        public void Load(IEntityLoader entityLoader) {
            IObjectLoader objectLoader = entityLoader.GetComponent(AdvancedStreamGaugeBase.StreamGaugeKey);
            this._wasHighNotWasLow = objectLoader.Get(AdvancedStreamGaugeBase.WasHighNotWasLowKey);
            this.HighSetPoint = objectLoader.Get(AdvancedStreamGaugeBase.HighSetPointKey);
            this.LowSetPoint = objectLoader.Get(AdvancedStreamGaugeBase.LowSetPointKey);
            this.HighestWaterLevel = objectLoader.Get(AdvancedStreamGaugeBase.HighestWaterLevelKey);
            if (objectLoader.Has(AntiSloshKey)) {
                this.AntiSlosh = objectLoader.Get(AdvancedStreamGaugeBase.AntiSloshKey);
            }
            if (objectLoader.Has(HistoryKey)) {
                this.History = objectLoader.Get(AdvancedStreamGaugeBase.HistoryKey);
                this._sortedHistory = this.History;
                this._sortedHistory.Sort();
            }
            this._streamGaugeAnimationController.SetHeight(this.HighestWaterLevel);
        }

        private void UpdateMarkerHeight() => this._streamGaugeAnimationController.SetHeight(Mathf.Min(this.HighestWaterLevel, (float)this.GaugeHeight - 0.02f));
    }
}
