using UnityEngine.UIElements;
using Timberborn.Localization;
using System.Collections.Generic;

namespace Avernar.Gauge {
    public class GaugeConfigFragment {
        private static readonly string OffLocKey = "Avernar.AdvancedStreamGauge.Off";
        private static readonly string OnLocKey = "Avernar.AdvancedStreamGauge.On";
        private static readonly string CloseLocKey = "Avernar.AdvancedStreamGauge.Close";
        private static readonly string OpenLocKey = "Avernar.AdvancedStreamGauge.Open";
        private readonly ILoc _loc;
        private readonly List<string> _offOnOptions = new();
        private RadioButtonGroup _dry;
        private RadioButtonGroup _low;
        private RadioButtonGroup _normalWasLow;
        private RadioButtonGroup _normalWasHigh;
        private RadioButtonGroup _high;
        private RadioButtonGroup _overflow;
        private GaugeConfig _config;

        public GaugeConfigFragment(ILoc loc, bool closeOpen = false) {
            this._loc = loc;
            if (closeOpen) {
                this._offOnOptions.Add(_loc.T(CloseLocKey));
                this._offOnOptions.Add(_loc.T(OpenLocKey));
            }
            else {
                this._offOnOptions.Add(_loc.T(OffLocKey));
                this._offOnOptions.Add(_loc.T(OnLocKey));
            }
        }

        public void InitializeFragment(TemplateContainer container) {
            this._dry = container.Q<RadioButtonGroup>("Dry");
            this._low = container.Q<RadioButtonGroup>("Low");
            this._normalWasLow = container.Q<RadioButtonGroup>("NormalWasLow");
            this._normalWasHigh = container.Q<RadioButtonGroup>("NormalWasHigh");
            this._high = container.Q<RadioButtonGroup>("High");
            this._overflow = container.Q<RadioButtonGroup>("Overflow");

            this._dry.choices = this._offOnOptions;
            this._low.choices = this._offOnOptions;
            this._normalWasLow.choices = this._offOnOptions;
            this._normalWasHigh.choices = this._offOnOptions;
            this._high.choices = this._offOnOptions;
            this._overflow.choices = this._offOnOptions;

            this._dry.RegisterValueChangedCallback<int>(DryChanged);
            this._low.RegisterValueChangedCallback<int>(LowChanged);
            this._normalWasLow.RegisterValueChangedCallback<int>(NormalWasLowChanged);
            this._normalWasHigh.RegisterValueChangedCallback<int>(NormalWasHighChanged);
            this._high.RegisterValueChangedCallback<int>(HighChanged);
            this._overflow.RegisterValueChangedCallback<int>(OverflowChanged);
        }

        private void DryChanged(ChangeEvent<int> e) => this._config.Dry = e.newValue == 1;
        private void LowChanged(ChangeEvent<int> e) => this._config.Low = e.newValue == 1;
        private void NormalWasLowChanged(ChangeEvent<int> e) => this._config.NormalWasLow = e.newValue == 1;
        private void NormalWasHighChanged(ChangeEvent<int> e) => this._config.NormalWasHigh = e.newValue == 1;
        private void HighChanged(ChangeEvent<int> e) => this._config.High = e.newValue == 1;
        private void OverflowChanged(ChangeEvent<int> e) => this._config.Overflow = e.newValue == 1;

        public void ShowFragment(GaugeConfig config) {
            this._config = config;
            this._dry.value = (this._config.Dry ? 1 : 0);
            this._low.value = (this._config.Low ? 1 : 0);
            this._normalWasLow.value = (this._config.NormalWasLow ? 1 : 0);
            this._normalWasHigh.value = (this._config.NormalWasHigh ? 1 : 0);
            this._high.value = (this._config.High ? 1 : 0);
            this._overflow.value = (this._config.Overflow ? 1 : 0);
        }

        public void ClearFragment() {
            this._config = null;
        }
    }
}
