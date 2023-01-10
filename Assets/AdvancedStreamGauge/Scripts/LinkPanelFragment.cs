using System;
using System.Collections.ObjectModel;
using Timberborn.CoreUI;
using Timberborn.Localization;
using Timberborn.AssetSystem;
using Timberborn.EntitySystem;
using Timberborn.PickObjectToolSystem;
using Timberborn.SelectionSystem;
using TimberApi.EntityLinkerSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Avernar.Gauge {
    public class LinkPanelFragment {
        private static readonly string LinkTextKey = "Avernar.AdvancedStreamGauge.LinkText";
        private static readonly string StartLinkingTipLocKey = "Avernar.AdvancedStreamGauge.StartLinkingTip";
        private static readonly string StartLinkingTitleLocKey = "Avernar.AdvancedStreamGauge.StartLinkingTitle";
        private readonly ILoc _loc;
        private readonly PickObjectTool _pickObjectTool;
        private readonly SelectionManager _selectionManager;
        private IResourceAssetLoader _assetLoader;
        private Sprite _icon;

        private VisualElement _buttonContainer;
        private VisualElement _targetContainer;
        private Label _targetButtonText;

        private Action<EntityLinker, EntityLinker> _setLinkCallback;
        private EntityLinker _linker;
        private EntityLinker _linkee;
        private EntityLinker _otherLinkee;

        public LinkPanelFragment(ILoc loc, IResourceAssetLoader assetLoader, PickObjectTool pickObjectTool, SelectionManager selectionManager) {
            this._loc = loc;
            this._assetLoader = assetLoader;
            this._pickObjectTool = pickObjectTool;
            this._selectionManager = selectionManager;
        }

        public virtual void Initialize<T>(VisualElement linkPanel, Action<EntityLinker, EntityLinker> setLinkCallback) where T : MonoBehaviour, IRegisteredComponent {
            this._setLinkCallback = setLinkCallback;

            this._icon = this._assetLoader.Load<Sprite>("avernar.advancedstreamgauge/avernar_advancedstreamgauge/AdvancedStreamGaugeIcon");

            Button linkButton = linkPanel.Q<Button>("LinkButton", (string)null);
            linkButton.clicked += delegate {
                StartLinkEntities<T>(this._linker, setLinkCallback);
            };
            this._buttonContainer = linkPanel.Q<VisualElement>("ButtonContainer");
            this._targetContainer = linkPanel.Q<VisualElement>("TargetContainer");

            var imageContainer = this._targetContainer.Q<VisualElement>("ImageContainer");
            var img = new Image {
                sprite = this._icon
            };
            imageContainer.Add(img);

            var targetButton = this._targetContainer.Q<Button>("Target");
            targetButton.clicked += delegate {
                _selectionManager.FocusOn(this._linkee.gameObject);
            };
            this._targetContainer.Q<Button>("RemoveLinkButton").clicked += delegate {
                RemoveLink();
            };

            this._targetButtonText = this._targetContainer.Q<Label>("ButtonText");
        }

        public void ShowFragment(EntityLinker linker, EntityLinker linkee, EntityLinker otherLinkee = null) {
            this._linker = linker;
            if ((bool)this._linker) {
                this._linkee = linkee;
                this._otherLinkee = otherLinkee;
                bool haveLink = FindLink() != null;
                if (haveLink) {
                    var gauge = this._linkee.GetComponent<AdvancedStreamGaugeBase>();
                    gauge.Tick();
                    this._targetButtonText.text = _loc.T(LinkTextKey, gauge.WaterLevel.ToString("0.00"), gauge.StatusText());
                    this._buttonContainer.ToggleDisplayStyle(false);
                    this._targetContainer.ToggleDisplayStyle(true);
                }
                else {
                    this._buttonContainer.ToggleDisplayStyle(true);
                    this._targetContainer.ToggleDisplayStyle(false);
                }
            }
        }

        public virtual void ClearFragment() {
            this._linkee = null;
            this._linker = null;
        }

        public virtual void UpdateFragment() {
            if (this._linkee == null) {
                this._targetButtonText.text = "";
            }
            else {
                var gauge = this._linkee.GetComponent<AdvancedStreamGaugeBase>();
                this._targetButtonText.text = _loc.T(LinkTextKey, gauge.StatusText(), gauge.WaterLevel.ToString("0.00"));
            }
        }

        protected virtual void StartLinkEntities<T>(EntityLinker linker, Action<EntityLinker, EntityLinker> createdLinkCallback) where T : MonoBehaviour, IRegisteredComponent {
            _pickObjectTool.StartPicking<T>(
                _loc.T(StartLinkingTitleLocKey),
                _loc.T(StartLinkingTipLocKey),
                (GameObject gameobject) => ValidateLinkee(linker, gameobject),
                delegate (GameObject linkee) {
                    linkee = linkee.GetComponent<AdvancedStreamGauge>().GetBase().gameObject;
                    FinishLinkSelection(linker, linkee, createdLinkCallback);
                });
        }

        public EntityLink FindLink() {
            ReadOnlyCollection<EntityLink> links = (ReadOnlyCollection<EntityLink>)this._linker.EntityLinks;
            foreach (var link in links) {
                var linkee = link.Linker == this._linker ? link.Linkee : link.Linker;
                if (linkee == this._linkee) {
                    return link;
                }
            }
            return null;
        }

        public virtual void RemoveLink() {
            if (this._linkee != this._otherLinkee) {
                EntityLink target = FindLink();
                if (target != null) {
                    this._linker.DeleteLink(target);
                }
            }
            this._setLinkCallback(this._linker, null);
            this._linkee = null;
            this._buttonContainer.ToggleDisplayStyle(true);
            this._targetContainer.ToggleDisplayStyle(false);
        }

        private void FinishLinkSelection(EntityLinker linker, GameObject gameObject, Action<EntityLinker, EntityLinker> setLinkCallback) {
            if (ValidateLinkee(linker, gameObject) == "") {
                EntityLinker linkee = gameObject.GetComponent<EntityLinker>();
                if (linkee != this._otherLinkee) {
                    linker.CreateLink(linkee);
                }
                setLinkCallback(linker, linkee);
            }
        }

        private string ValidateLinkee(EntityLinker linker, GameObject gameObject) {
            EntityLinker linkee = gameObject.GetComponent<AdvancedStreamGauge>().GetBase().gameObject.GetComponent<EntityLinker>();
            if (linkee != this._otherLinkee) {
                ReadOnlyCollection<EntityLink> links = (ReadOnlyCollection<EntityLink>)linker.EntityLinks;
                foreach (var link in links) {
                    if (linkee == link.Linkee) {
                        return "Already linked.";
                    }
                }
            }
            return "";
        }
    }
}
