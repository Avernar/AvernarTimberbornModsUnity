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
    internal class ASGWaterPumpFragment : IEntityPanelFragment {
        private static readonly string ManualKey = "Avernar.AdvancedStreamGauge.Manual";
        private static readonly string AutomaticKey = "Avernar.AdvancedStreamGauge.Automatic";
        private static readonly string AndLocKey = "Avernar.AdvancedStreamGauge.And";
        private static readonly string OrLocKey = "Avernar.AdvancedStreamGauge.Or";
        private readonly ILoc _loc;
        private readonly IResourceAssetLoader _assetLoader;
        private readonly LinkPanelFragment _linkPanelFragment1;
        private readonly LinkPanelFragment _linkPanelFragment2;
        private readonly WeatherActionsFragment _weatherActionsFragment;
        private readonly GaugeConfigFragment _waterPumpConfigFragment1;
        private readonly GaugeConfigFragment _waterPumpConfigFragment2;
        private readonly List<string> _manualAutomaticOptions = new();
        private readonly List<string> _andOrOptions = new();
        private UIBuilder _timberApiUiBuilder;
        private VisualElement _root;
        private RadioButtonGroup _manualAutomatic;
        private VisualElement _automaticSettings;
        private TemplateContainer _firstGaugeSettings;
        private TemplateContainer _secondGaugeSettings;
        private VisualElement _andOrSettings;
        private RadioButtonGroup _andOr;
        private WaterPumpMonobehaviour _waterPump;

        public ASGWaterPumpFragment(ILoc loc, IResourceAssetLoader assetLoader, PickObjectTool pickObjectTool, SelectionManager selectionManager) {
            this._loc = loc;
            this._assetLoader = assetLoader;
            this._linkPanelFragment1 = new LinkPanelFragment(loc, assetLoader, pickObjectTool, selectionManager);
            this._linkPanelFragment2 = new LinkPanelFragment(loc, assetLoader, pickObjectTool, selectionManager);
            this._weatherActionsFragment = new WeatherActionsFragment(loc);
            this._waterPumpConfigFragment1 = new GaugeConfigFragment(loc);
            this._waterPumpConfigFragment2 = new GaugeConfigFragment(loc);
            this._manualAutomaticOptions.Add(_loc.T(ManualKey));
            this._manualAutomaticOptions.Add(_loc.T(AutomaticKey));
            this._andOrOptions.Add(_loc.T(AndLocKey));
            this._andOrOptions.Add(_loc.T(OrLocKey));
        }

        [Inject]
        public void InjectDependencies(UIBuilder timberApiUiBuilder) {
            this._timberApiUiBuilder = timberApiUiBuilder;
        }

        public VisualElement InitializeFragment() {
            this._root = this._assetLoader.Load<VisualTreeAsset>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/WaterPumpFragment").CloneTree().ElementAt(0);
            _timberApiUiBuilder.InitializeVisualElement(this._root);

            StyleSheet styleSheet = this._assetLoader.Load<StyleSheet>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/AdvancedStreamGaugeStyles");
            this._root.styleSheets.Add(styleSheet);

            this._root.ToggleDisplayStyle(false);

            this._manualAutomatic = this._root.Q<RadioButtonGroup>("ManualAutomatic", (string)null);
            this._manualAutomatic.choices = this._manualAutomaticOptions;
            this._manualAutomatic.RegisterValueChangedCallback(ManualAutomaticChanged);

            this._automaticSettings = this._root.Q<VisualElement>("AutomaticSettings", (string)null);

            VisualElement linkPanel1 = this._root.Q<VisualElement>("LinkPanel1", (string)null);
            this._linkPanelFragment1.Initialize<AdvancedStreamGauge>(linkPanel1, delegate(EntityLinker linker, EntityLinker linkee) {
                linker.GetComponent<WaterPumpMonobehaviour>().FirstLink = linkee;
            });

            VisualElement linkPanel2 = this._root.Q<VisualElement>("LinkPanel2", (string)null);
            this._linkPanelFragment2.Initialize<AdvancedStreamGauge>(linkPanel2, delegate (EntityLinker linker, EntityLinker linkee) {
                linker.GetComponent<WaterPumpMonobehaviour>().SecondLink = linkee;
            });

            TemplateContainer weatherActions = this._root.Q<TemplateContainer>("WeatherActions", (string)null);
            this._weatherActionsFragment.InitializeFragment(weatherActions);

            this._firstGaugeSettings = this._root.Q<TemplateContainer>("FirstGaugeSettings", (string)null);
            this._waterPumpConfigFragment1.InitializeFragment(this._firstGaugeSettings);

            this._secondGaugeSettings = this._root.Q<TemplateContainer>("SecondGaugeSettings", (string)null);
            this._waterPumpConfigFragment2.InitializeFragment(this._secondGaugeSettings);

            this._andOrSettings = this._root.Q<VisualElement>("AndOrSettings", (string)null);

            this._andOr = this._root.Q<RadioButtonGroup>("AndOr", (string)null);
            this._andOr.choices = this._andOrOptions;
            this._andOr.RegisterValueChangedCallback(AndOrChanged);

            return this._root;
        }

        private void ManualAutomaticChanged(ChangeEvent<int> e) {
            this._waterPump.Automatic = e.newValue == 1;
        }

        private void AndOrChanged(ChangeEvent<int> e) {
            this._waterPump.Or = e.newValue == 1;
        }

        public void ShowFragment(GameObject entity) {
            this._waterPump = entity.GetComponent<WaterPumpMonobehaviour>();
            if ((bool)(UnityEngine.Object)this._waterPump) {
                EntityLinker linker = entity.GetComponent<EntityLinker>();
                this._manualAutomatic.SetValueWithoutNotify(this._waterPump.Automatic ? 1 : 0);
                this._linkPanelFragment1.ShowFragment(linker, this._waterPump.FirstLink);
                this._linkPanelFragment2.ShowFragment(linker, this._waterPump.SecondLink);
                this._weatherActionsFragment.ShowFragment(this._waterPump.Actions);
                this._waterPumpConfigFragment1.ShowFragment(this._waterPump.Gauge1);
                this._waterPumpConfigFragment2.ShowFragment(this._waterPump.Gauge2);
                this._andOr.SetValueWithoutNotify(this._waterPump.Or ? 1 : 0);

                this._automaticSettings.ToggleDisplayStyle(this._waterPump.Automatic);
                this._firstGaugeSettings.ToggleDisplayStyle((bool)this._waterPump.FirstLink);
                this._andOrSettings.ToggleDisplayStyle((bool)this._waterPump.FirstLink && (bool)this._waterPump.SecondLink);
                this._secondGaugeSettings.ToggleDisplayStyle((bool)this._waterPump.SecondLink);
                this._root.ToggleDisplayStyle(true);
            }
        }
        public void ClearFragment() {
            if ((bool)(UnityEngine.Object)this._waterPump) {
                this._linkPanelFragment1.ClearFragment();
                this._linkPanelFragment2.ClearFragment();
                this._weatherActionsFragment.ClearFragment();
                this._waterPumpConfigFragment1.ClearFragment();
                this._waterPumpConfigFragment2.ClearFragment();
                this._waterPump = null;
            }
            this._root.ToggleDisplayStyle(false);
        }

        public void UpdateFragment() {
            if ((bool)(UnityEngine.Object)this._waterPump) {
                this._root.ToggleDisplayStyle(true);
                this._linkPanelFragment1.UpdateFragment();
                this._linkPanelFragment2.UpdateFragment();
                this._automaticSettings.ToggleDisplayStyle(this._waterPump.Automatic);
                this._firstGaugeSettings.ToggleDisplayStyle((bool)this._waterPump.FirstLink);
                this._andOrSettings.ToggleDisplayStyle((bool)this._waterPump.FirstLink && (bool)this._waterPump.SecondLink);
                this._secondGaugeSettings.ToggleDisplayStyle((bool)this._waterPump.SecondLink);
            }
            else {
                this._root.ToggleDisplayStyle(false);
            }
        }
    }
}
