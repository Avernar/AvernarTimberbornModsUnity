using UnityEngine;
using UnityEngine.UIElements;
using Timberborn.Localization;
using System.Collections.Generic;

namespace Avernar.Gauge {
    public class WeatherActionsFragment {
        private static readonly string OffLocKey = "Avernar.AdvancedStreamGauge.Off";
        private static readonly string OnLocKey = "Avernar.AdvancedStreamGauge.On";
        private static readonly string CloseLocKey = "Avernar.AdvancedStreamGauge.Close";
        private static readonly string OpenLocKey = "Avernar.AdvancedStreamGauge.Open";
        private static readonly string GaugeLocKey = "Avernar.AdvancedStreamGauge.Gauge";
        protected RadioButtonGroup _temperate;
        protected RadioButtonGroup _drought;
        private readonly ILoc _loc;
        private readonly List<string> _offOnGaugeOptions = new();
        private WeatherActions _weatherActions;

        public WeatherActionsFragment(ILoc loc, bool closeOpen = false) {
            this._loc = loc;
            if (closeOpen) {
                this._offOnGaugeOptions.Add(_loc.T(CloseLocKey));
                this._offOnGaugeOptions.Add(_loc.T(OpenLocKey));
            }
            else {
                this._offOnGaugeOptions.Add(_loc.T(OffLocKey));
                this._offOnGaugeOptions.Add(_loc.T(OnLocKey));
            }
            this._offOnGaugeOptions.Add(_loc.T(GaugeLocKey));
        }

        public void InitializeFragment(VisualElement weatherActions) {
            this._temperate = weatherActions.Q<RadioButtonGroup>("Temperate");
            this._drought = weatherActions.Q<RadioButtonGroup>("Drought");

            this._temperate.choices = this._offOnGaugeOptions;
            this._drought.choices = this._offOnGaugeOptions;

            this._temperate.RegisterValueChangedCallback(TemperateChanged);
            this._drought.RegisterValueChangedCallback(DroughtChanged);
        }

        private void TemperateChanged(ChangeEvent<int> e) => this._weatherActions.Temperate = (SeasonSetting)e.newValue;
        private void DroughtChanged(ChangeEvent<int> e) => this._weatherActions.Drought = (SeasonSetting)e.newValue;

        public virtual void ShowFragment(WeatherActions weatherActions) {
            this._weatherActions = weatherActions;
            this._temperate.SetValueWithoutNotify((int)weatherActions.Temperate);
            this._drought.SetValueWithoutNotify((int)weatherActions.Drought);
        }

        public virtual void ClearFragment() {
            this._weatherActions = null;
        }

    }
}
