using Bindito.Core;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.AssetSystem;
using Timberborn.PickObjectToolSystem;
using Timberborn.SelectionSystem;
using TimberApi.UiBuilderSystem;
using TimberApi.EntityLinkerSystem;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Avernar.Gauge {
    internal class FloodgateFragment : Fragment, IEntityPanelFragment {
        private static readonly string ManualKey = "Avernar.AdvancedStreamGauge.Manual";
        private static readonly string AutomaticKey = "Avernar.AdvancedStreamGauge.Automatic";
        private static readonly string OffLocKey = "Avernar.AdvancedStreamGauge.Off";
        private static readonly string OnLocKey = "Avernar.AdvancedStreamGauge.On";
        private static readonly string ClosedLocKey = "Avernar.AdvancedStreamGauge.Closed";
        private static readonly string OpenedLocKey = "Avernar.AdvancedStreamGauge.Opened";
        private static readonly float SliderStep = 0.5f;
        private readonly ILoc _loc;
        private readonly IResourceAssetLoader _assetLoader;
        private readonly LinkPanelFragment _linkPanelFragment1;
        private readonly LinkPanelFragment _linkPanelFragment2;
        private readonly WeatherActionsFragment _weatherActionsFragment;
        private readonly GaugeConfigFragment _waterPumpConfigFragment1;
        private readonly List<string> _manualAutomaticOptions = new();
        private readonly List<string> _offOnOptions = new();
        private UIBuilder _timberApiUiBuilder;
        private VisualElement _root;
        private RadioButtonGroup _manualAutomatic;
        private RadioButtonGroup _backflowPrevention;
        private Label _closedLabel;
        private Label _openedLabel;
        private Slider _closedSlider;
        private Slider _openedSlider;
        private FloodgateMonobehaviour _floodgate;

        public FloodgateFragment(ILoc loc, IResourceAssetLoader assetLoader, PickObjectTool pickObjectTool, SelectionManager selectionManager) {
            this._loc = loc;
            this._assetLoader = assetLoader;
            this._linkPanelFragment1 = new LinkPanelFragment(loc, assetLoader, pickObjectTool, selectionManager);
            this._linkPanelFragment2 = new LinkPanelFragment(loc, assetLoader, pickObjectTool, selectionManager);
            this._weatherActionsFragment = new WeatherActionsFragment(loc, true);
            this._waterPumpConfigFragment1 = new GaugeConfigFragment(loc, true);
            this._manualAutomaticOptions.Add(_loc.T(ManualKey));
            this._manualAutomaticOptions.Add(_loc.T(AutomaticKey));
            this._offOnOptions.Add(_loc.T(OffLocKey));
            this._offOnOptions.Add(_loc.T(OnLocKey));
        }

        [Inject]
        public void InjectDependencies(UIBuilder timberApiUiBuilder) {
            this._timberApiUiBuilder = timberApiUiBuilder;
        }

        public VisualElement InitializeFragment() {
            this._root = this._assetLoader.Load<VisualTreeAsset>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/FloodgateFragment").CloneTree().ElementAt(0);
            _timberApiUiBuilder.InitializeVisualElement(this._root);

            StyleSheet styleSheet = this._assetLoader.Load<StyleSheet>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/AdvancedStreamGaugeStyles");
            this._root.styleSheets.Add(styleSheet);

            this._root.ToggleDisplayStyle(false);

            this._manualAutomatic = this._root.Q<RadioButtonGroup>("ManualAutomatic", (string)null);
            this._manualAutomatic.choices = this._manualAutomaticOptions;
            this._manualAutomatic.RegisterValueChangedCallback(ManualAutomaticChanged);

            this._backflowPrevention = this._root.Q<RadioButtonGroup>("BackflowPrevention", (string)null);
            this._backflowPrevention.choices = this._offOnOptions;
            this._backflowPrevention.RegisterValueChangedCallback(BackflowPreventionChanged);

            this._closedLabel = this._root.Q<Label>("ClosedLabel", (string)null);
            this._openedLabel = this._root.Q<Label>("OpenedLabel", (string)null);
            this._closedSlider = this._root.Q<Slider>("ClosedSlider", (string)null);
            this._openedSlider = this._root.Q<Slider>("OpenedSlider", (string)null);

            this._closedSlider.RegisterValueChangedCallback(ClosedChange);
            this._openedSlider.RegisterValueChangedCallback(OpenedChange);

            RegisterMouseCallbacks(this._closedSlider);
            RegisterMouseCallbacks(this._openedSlider);

            VisualElement linkPanel1 = this._root.Q<VisualElement>("LinkPanel1", (string)null);
            this._linkPanelFragment1.Initialize<AdvancedStreamGauge>(linkPanel1, delegate (EntityLinker linker, EntityLinker linkee) {
                linker.GetComponent<FloodgateMonobehaviour>().FirstLink = linkee;
            });

            VisualElement linkPanel2 = this._root.Q<VisualElement>("LinkPanel2", (string)null);
            this._linkPanelFragment2.Initialize<AdvancedStreamGauge>(linkPanel2, delegate (EntityLinker linker, EntityLinker linkee) {
                linker.GetComponent<FloodgateMonobehaviour>().SecondLink = linkee;
            });

            TemplateContainer weatherActions = this._root.Q<TemplateContainer>("WeatherActions", (string)null);
            this._weatherActionsFragment.InitializeFragment(weatherActions);

            TemplateContainer firstGaugeSettings = this._root.Q<TemplateContainer>("FirstGaugeSettings", (string)null);
            this._waterPumpConfigFragment1.InitializeFragment(firstGaugeSettings);

            return this._root;
        }
        private void ManualAutomaticChanged(ChangeEvent<int> e) {
            this._floodgate.Automatic = e.newValue == 1;
        }

        private void BackflowPreventionChanged(ChangeEvent<int> e) {
            this._floodgate.BackflowPrevention = e.newValue == 1;
        }

        public void ShowFragment(GameObject entity) {
            this._floodgate = entity.GetComponent<FloodgateMonobehaviour>();
            if ((bool)(UnityEngine.Object)this._floodgate) {
                EntityLinker linker = entity.GetComponent<EntityLinker>();
                this._linkPanelFragment1.ShowFragment(linker, this._floodgate.FirstLink, this._floodgate.SecondLink);
                this._linkPanelFragment2.ShowFragment(linker, this._floodgate.SecondLink, this._floodgate.FirstLink);
                this._weatherActionsFragment.ShowFragment(this._floodgate.Actions);
                this._waterPumpConfigFragment1.ShowFragment(this._floodgate.Gauge1);
                this._manualAutomatic.SetValueWithoutNotify(this._floodgate.Automatic ? 1 : 0);

                this._closedSlider.SetValueWithoutNotify(0);
                this._closedSlider.highValue = this._floodgate.FloodgateMaxHeight;
                this._closedSlider.SetValueWithoutNotify(this._floodgate.ClosedLevel);
                UpdateClosedLabel();

                this._openedSlider.SetValueWithoutNotify(0);
                this._openedSlider.highValue = this._floodgate.FloodgateMaxHeight;
                this._openedSlider.SetValueWithoutNotify(this._floodgate.OpenedLevel);
                UpdateOpenedLabel();

                this._root.ToggleDisplayStyle(true);
            }
        }
        public void ClearFragment() {
            if ((bool)(UnityEngine.Object)this._floodgate) {
                this._linkPanelFragment1.ClearFragment();
                this._linkPanelFragment2.ClearFragment();
                this._weatherActionsFragment.ClearFragment();
                this._waterPumpConfigFragment1.ClearFragment();
                this._floodgate = null;
            }
            this._root.ToggleDisplayStyle(false);
        }

        public void UpdateFragment() {
            if ((bool)(UnityEngine.Object)this._floodgate && this._floodgate.enabled) {
                this._root.ToggleDisplayStyle(true);
                this._linkPanelFragment1.UpdateFragment();
                this._linkPanelFragment2.UpdateFragment();
            }
            else {
                this._root.ToggleDisplayStyle(false);
            }
        }

        private void UpdateClosedLabel() {
            this._closedLabel.text = this._loc.T<string>(ClosedLocKey, this._floodgate.ClosedLevel.ToString("0.00"));
        }

        private void UpdateOpenedLabel() {
            this._openedLabel.text = this._loc.T<string>(OpenedLocKey, this._floodgate.OpenedLevel.ToString("0.0"));
        }

        private void ClosedChange(ChangeEvent<float> changeEvent) {
            float newValue = SnapSliderValue(changeEvent, SliderStep);
            this._closedSlider.SetValueWithoutNotify(newValue);
            if (this._floodgate.ClosedLevel != newValue) {
                this._floodgate.ClosedLevel = newValue;
                UpdateClosedLabel();
                if (this._floodgate.OpenedLevel > newValue) {
                    this._floodgate.OpenedLevel = newValue;
                    UpdateOpenedLabel();
                    this._openedSlider.SetValueWithoutNotify(newValue);
                }
            }
        }

        private void OpenedChange(ChangeEvent<float> changeEvent) {
            float newValue = SnapSliderValue(changeEvent, SliderStep);
            this._openedSlider.SetValueWithoutNotify(newValue);
            if (this._floodgate.OpenedLevel != newValue) {
                this._floodgate.OpenedLevel = newValue;
                UpdateOpenedLabel();
                if (this._floodgate.ClosedLevel < newValue) {
                    this._floodgate.ClosedLevel = newValue;
                    UpdateClosedLabel();
                    this._closedSlider.SetValueWithoutNotify(newValue);
                }
            }
        }
    }
}
