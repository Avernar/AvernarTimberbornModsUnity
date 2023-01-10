using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Avernar.Gauge {
    public class Fragment {
        private readonly ILoc _loc;
        private bool _usingDragger;
        private readonly float _sliderStep;
        private readonly float _sliderStepInv;

        public Fragment(ILoc loc, float sliderStep = 1.0f) {
            this._sliderStep = sliderStep;
            this._sliderStepInv = 1.0f / this._sliderStep;
            this._loc = loc;
        }

        protected void RegisterMouseCallbacks(Slider slider) {
            VisualElement dragger = slider.Q<VisualElement>("unity-dragger", (string)null);
            dragger.RegisterCallback<MouseDownEvent>(DraggerMouseDownCallback, TrickleDown.TrickleDown);

            VisualElement dragContainer = slider.Q<VisualElement>("unity-drag-container", (string)null);
            dragContainer.RegisterCallback<MouseDownEvent>(DragContainerMouseDownCallback, TrickleDown.TrickleDown);
            dragContainer.RegisterCallback<MouseUpEvent>(DragContainerMouseUpCallback, TrickleDown.TrickleDown);
        }

        private void DraggerMouseDownCallback(MouseDownEvent e) {
            this._usingDragger = true;
        }

        private void DragContainerMouseDownCallback(MouseDownEvent e) {
            this._usingDragger = false;
        }

        private void DragContainerMouseUpCallback(MouseUpEvent e) {
            this._usingDragger = false;
        }

        protected float SnapSliderValue(ChangeEvent<float> changeEvent) {
            float newValue = 0.0f;
            if (this._usingDragger) {
                newValue = changeEvent.newValue;
            }
            else {
                float diff = changeEvent.newValue - changeEvent.previousValue;
                if (diff > 0.0f) {
                    newValue = changeEvent.previousValue + this._sliderStep;
                }
                else if (diff < 0.0f) {
                    newValue = changeEvent.previousValue - this._sliderStep;
                }
            }
            newValue = Mathf.Round(newValue * this._sliderStepInv) / this._sliderStepInv;
            return newValue;
        }
    }
}
