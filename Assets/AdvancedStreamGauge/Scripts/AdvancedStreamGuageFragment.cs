using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bindito.Core;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.AssetSystem;
using Timberborn.PrefabSystem;
using Timberborn.SelectionSystem;
using TimberApi.UiBuilderSystem;
using TimberApi.EntityLinkerSystem;
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
        private readonly SelectionManager _selectionManager;
        private IResourceAssetLoader _assetLoader;
        private UIBuilder _timberApiUiBuilder;
        private EntityLinker _linker;
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
        private VisualTreeAsset _linkAsset;
        private AdvancedStreamGaugeBase _advancedStreamGaugeBase;
        private StyleSheet _styleSheet;
        private VisualElement _root;
        private VisualElement _linksContainer;
        private ScrollView _links;
        private List<VisualElement> _linkList;

        public AdvancedStreamGaugeFragment(ILoc loc, SelectionManager selectionManager) {
            this._loc = loc;
            this._selectionManager = selectionManager;
            this._linkList = new List<VisualElement>();
        }

        [Inject]
        public void InjectDependencies(IResourceAssetLoader assetLoader, UIBuilder timberApiUiBuilder) {
            this._assetLoader = assetLoader;
            this._timberApiUiBuilder = timberApiUiBuilder;
        }

        public VisualElement InitializeFragment() {
            this._styleSheet = this._assetLoader.Load<StyleSheet>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/AdvancedStreamGaugeStyles");
            this._linkAsset = this._assetLoader.Load<VisualTreeAsset>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/LinkButton");
            this._root = this._assetLoader.Load<VisualTreeAsset>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/AdvancedStreamGaugeFragment").CloneTree().ElementAt(0);
            this._timberApiUiBuilder.InitializeVisualElement(this._root);
            this._root.styleSheets.Add(this._styleSheet);

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
            this._linksContainer = this._root.Q<VisualElement>("LinksContainer", (string)null);
            this._links = this._root.Q<ScrollView>("Links", (string)null);

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

                this._linker = this._advancedStreamGaugeBase.GetComponent<EntityLinker>();
                AddLinksToContainer();
            }
        }

        public void ClearFragment() {
            ClearLinksContainer();
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

        private void AddLinksToContainer() {
            //var styleSheet = this._assetLoader.Load<StyleSheet>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/AdvancedStreamGaugeStyles");
            //var styleSheet = this._styleSheet;
            //Plugin.Log.LogWarning(string.Format("AddLinksToContainer {0} {1}", this._styleSheet.contentHash, styleSheet.contentHash));
            ReadOnlyCollection<EntityLink> links = (ReadOnlyCollection<EntityLink>)_linker.EntityLinks;
            foreach (var link in links) {
                var linkee = link.Linker == _linker ? link.Linkee :link.Linker;
                var linkeeGameObject = (linkee).gameObject;
                var prefab = linkeeGameObject.GetComponent<LabeledPrefab>();
                var linkElement = this._linkAsset.CloneTree().ElementAt(0);
                _timberApiUiBuilder.InitializeVisualElement(linkElement);
                linkElement.styleSheets.Add(this._styleSheet);

                var imageContainer = linkElement.Q<VisualElement>("ImageContainer");
                var img = new Image {
                    sprite = prefab.Image
                };
                imageContainer.Add(img);

                var targetButton = linkElement.Q<Button>("Target");
                targetButton.clicked += delegate {
                    _selectionManager.FocusOn(linkee.gameObject);
                };
                linkElement.Q<Button>("RemoveLinkButton").clicked += delegate {
                    link.Linker.DeleteLink(link);
                    ResetLinksContainer();
                };

                var targetButtonText = linkElement.Q<Label>("ButtonText");
                targetButtonText.text = _loc.T(prefab.DisplayNameLocKey);

                this._links.Add(linkElement);
                this._linkList.Add(linkElement);
            }
            this._linksContainer.ToggleDisplayStyle(links.Count > 0);
        }

        private void ResetLinksContainer() {
            ClearLinksContainer();
            AddLinksToContainer();
        }

        private void ClearLinksContainer() {
            foreach (var linkElement in this._linkList) {
                this._links.Remove(linkElement);
            }
            this._linkList.Clear();
        }
    }
}
