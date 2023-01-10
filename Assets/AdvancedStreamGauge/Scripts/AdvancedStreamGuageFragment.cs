using System;
using Bindito.Core;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.AssetSystem;
using TimberApi.UiBuilderSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Avernar.Gauge {
    internal class AdvancedStreamGaugeFragment : Fragment, IEntityPanelFragment {
        private static readonly string GaugeHeightLocKey = "Avernar.AdvancedStreamGauge.GaugeHeight";
        private static readonly string GaugeStatusLocKey = "Avernar.AdvancedStreamGauge.GaugeStatus";
        private static readonly string WaterLevelLocKey = "Avernar.AdvancedStreamGauge.WaterLevel";
        private static readonly string HighestWaterLevelLocKey = "Avernar.AdvancedStreamGauge.HighestWaterLevel";
        private static readonly string WaterCurrentLocKey = "Avernar.AdvancedStreamGauge.WaterCurrent";
        private static readonly string HighSetPointLocKey = "Avernar.AdvancedStreamGauge.HighSetPoint";
        private static readonly string LowSetPointLocKey = "Avernar.AdvancedStreamGauge.LowSetPoint";
        private static readonly string AntiSloshLocKey = "Avernar.AdvancedStreamGauge.AntiSlosh";
        private static readonly float SliderStep = 0.05f;
        private readonly ILoc _loc;
        private IResourceAssetLoader _assetLoader;
        private UIBuilder _timberApiUiBuilder;
        private Label _gaugeHeightLabel;
        private Label _gaugeStatusLabel;
        private Label _waterLevelLabel;
        private Label _highestWaterLevelLabel;
        private Label _waterCurrentLabel;
        private Label _highSetPointLabel;
        private Label _lowSetPointLabel;
        private Label _antiSloshLabel;
        private Slider _highSetPointSlider;
        private Slider _lowSetPointSlider;
        private Slider _antiSloshSlider;
        private AdvancedStreamGaugeBase _advancedStreamGaugeBase;
        private VisualElement _root;

        public AdvancedStreamGaugeFragment(ILoc loc) {
            this._loc = loc;
        }

        [Inject]
        public void InjectDependencies(IResourceAssetLoader assetLoader, UIBuilder timberApiUiBuilder) {
            this._assetLoader = assetLoader;
            this._timberApiUiBuilder = timberApiUiBuilder;
        }

        public VisualElement InitializeFragment() {
            this._root = this._assetLoader.Load<VisualTreeAsset>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/AdvancedStreamGaugeFragment").CloneTree().ElementAt(0);
            _timberApiUiBuilder.InitializeVisualElement(this._root);

            this._root.Q<Button>("ResetHighestWaterLevelButton", (string)null).clicked += (Action)(() => this._advancedStreamGaugeBase.ResetHighestWaterLevel());
            this._root.ToggleDisplayStyle(false);

            this._gaugeHeightLabel = this._root.Q<Label>("GaugeHeightLabel", (string)null);
            this._gaugeStatusLabel = this._root.Q<Label>("GaugeStatusLabel", (string)null);
            this._waterLevelLabel = this._root.Q<Label>("WaterLevelLabel", (string)null);
            this._highestWaterLevelLabel = this._root.Q<Label>("HighestWaterLevelLabel", (string)null);
            this._waterCurrentLabel = this._root.Q<Label>("WaterCurrentLabel", (string)null);
            this._highSetPointLabel = this._root.Q<Label>("HighSetPointLabel", (string)null);
            this._lowSetPointLabel = this._root.Q<Label>("LowSetPointLabel", (string)null);
            this._antiSloshLabel = this._root.Q<Label>("AntiSloshLabel", (string)null);
            this._highSetPointSlider = this._root.Q<Slider>("HighSetPointSlider", (string)null);
            this._lowSetPointSlider = this._root.Q<Slider>("LowSetPointSlider", (string)null);
            this._antiSloshSlider = this._root.Q<Slider>("AntiSloshSlider", (string)null);

            this._highSetPointSlider.RegisterValueChangedCallback(HighSetPointChange);
            this._lowSetPointSlider.RegisterValueChangedCallback(LowSetPointChange);
            this._antiSloshSlider.RegisterValueChangedCallback(AntiSloshChange);

            RegisterMouseCallbacks(this._highSetPointSlider);
            RegisterMouseCallbacks(this._lowSetPointSlider);
            RegisterMouseCallbacks(this._antiSloshSlider);

            return this._root;
        }

        public void ShowFragment(GameObject entity) {
            AdvancedStreamGauge asg = entity.GetComponent<AdvancedStreamGauge>();
            if ((bool)(UnityEngine.Object)asg) {
                this._advancedStreamGaugeBase = asg.GetBase();
                this._advancedStreamGaugeBase.Calculate();

                this._highSetPointSlider.SetValueWithoutNotify(this._highSetPointSlider.lowValue);
                this._highSetPointSlider.lowValue = this._advancedStreamGaugeBase.MinLevel;
                this._highSetPointSlider.highValue = this._advancedStreamGaugeBase.MaxLevel;
                this._highSetPointSlider.SetValueWithoutNotify(this._advancedStreamGaugeBase.HighSetPoint);
                UpdateHighSetPointLabel();

                this._lowSetPointSlider.SetValueWithoutNotify(this._lowSetPointSlider.lowValue);
                this._lowSetPointSlider.lowValue = this._advancedStreamGaugeBase.MinLevel;
                this._lowSetPointSlider.highValue = this._advancedStreamGaugeBase.MaxLevel;
                this._lowSetPointSlider.SetValueWithoutNotify(this._advancedStreamGaugeBase.LowSetPoint);
                UpdateLowSetPointLabel();

                this._antiSloshSlider.SetValueWithoutNotify(0);
                this._antiSloshSlider.lowValue = 0;
                this._antiSloshSlider.highValue = 5;
                this._antiSloshSlider.SetValueWithoutNotify(this._advancedStreamGaugeBase.AntiSlosh);
                UpdateAntiSloshLabel();
            }
        }
        public void ClearFragment() {
            this._advancedStreamGaugeBase = (AdvancedStreamGaugeBase)null;
            this._root.ToggleDisplayStyle(false);
        }

        public void UpdateFragment() {
            if ((bool)(UnityEngine.Object)this._advancedStreamGaugeBase && this._advancedStreamGaugeBase.enabled) {
                this._root.ToggleDisplayStyle(true);
                this._gaugeHeightLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.GaugeHeightLocKey, this._advancedStreamGaugeBase.GaugeHeight.ToString());
                this._gaugeStatusLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.GaugeStatusLocKey, this._advancedStreamGaugeBase.StatusText());
                this._waterLevelLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.WaterLevelLocKey, this._advancedStreamGaugeBase.WaterLevel.ToString("0.00"));
                this._highestWaterLevelLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.HighestWaterLevelLocKey, this._advancedStreamGaugeBase.HighestWaterLevel.ToString("0.00"));
                this._waterCurrentLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.WaterCurrentLocKey, this._advancedStreamGaugeBase.WaterCurrent.ToString("0.0"));
            }
            else {
                this._root.ToggleDisplayStyle(false);
            }
        }

        private void UpdateHighSetPointLabel() {
            this._highSetPointLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.HighSetPointLocKey, this._advancedStreamGaugeBase.HighSetPoint.ToString("0.00"));
        }

        private void UpdateLowSetPointLabel() {
            this._lowSetPointLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.LowSetPointLocKey, this._advancedStreamGaugeBase.LowSetPoint.ToString("0.00"));
        }

        private void UpdateAntiSloshLabel() {
            this._antiSloshLabel.text = this._loc.T<string>(AdvancedStreamGaugeFragment.AntiSloshLocKey, this._advancedStreamGaugeBase.AntiSlosh.ToString());
        }

        private void HighSetPointChange(ChangeEvent<float> changeEvent) {
            float newValue = SnapSliderValue(changeEvent, SliderStep);
            this._highSetPointSlider.SetValueWithoutNotify(newValue);
            if (this._advancedStreamGaugeBase.HighSetPoint != newValue) {
                this._advancedStreamGaugeBase.UpdateHighSetPoint(newValue);
                UpdateHighSetPointLabel();
                if (this._advancedStreamGaugeBase.LowSetPoint > newValue) {
                    this._advancedStreamGaugeBase.UpdateLowSetPoint(newValue);
                    UpdateLowSetPointLabel();
                    this._lowSetPointSlider.SetValueWithoutNotify(newValue);
                }
            }
        }

        private void LowSetPointChange(ChangeEvent<float> changeEvent) {
            float newValue = SnapSliderValue(changeEvent, SliderStep);
            this._lowSetPointSlider.SetValueWithoutNotify(newValue);
            if (this._advancedStreamGaugeBase.LowSetPoint != newValue) {
                this._advancedStreamGaugeBase.UpdateLowSetPoint(newValue);
                UpdateLowSetPointLabel();
                if (this._advancedStreamGaugeBase.HighSetPoint < newValue) {
                    this._advancedStreamGaugeBase.UpdateHighSetPoint(newValue);
                    UpdateHighSetPointLabel();
                    this._highSetPointSlider.SetValueWithoutNotify(newValue);
                }
            }
        }

        private void AntiSloshChange(ChangeEvent<float> changeEvent) {
            float newValue = SnapSliderValue(changeEvent, 1);
            this._antiSloshSlider.SetValueWithoutNotify(newValue);
            this._advancedStreamGaugeBase.UpdateAntiSlosh((int)MathF.Round(newValue));
            UpdateAntiSloshLabel();
        }
    }
}
