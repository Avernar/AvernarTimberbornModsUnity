using Bindito.Core;
using System.Collections.ObjectModel;
using Timberborn.ConstructibleSystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using Timberborn.WeatherSystem;
using Timberborn.WaterBuildings;
using Timberborn.BlockSystem;
using TimberApi.EntityLinkerSystem;
using UnityEngine;

namespace Avernar.Gauge {
    public class FloodgateMonobehaviour : TickableComponent, IPersistentEntity, IFinishedStateListener {
        private static readonly float TriggerPoint1 = 0.65f;
        private static readonly float TriggerPoint2 = 0.15f;
        private static readonly ComponentKey FloodgateKey = new ComponentKey(nameof(FloodgateMonobehaviour));
        private static readonly PropertyKey<EntityLinker> FirstLinkKey = new(nameof(FirstLink));
        private static readonly PropertyKey<EntityLinker> SecondLinkKey = new(nameof(SecondLink));
        protected static readonly PropertyKey<WeatherActions> ActionsKey = new(nameof(Actions));
        protected static readonly PropertyKey<GaugeConfig> Gauge1Key = new(nameof(Gauge1));
        protected static readonly PropertyKey<bool> AutomaticKey = new(nameof(Automatic));

        protected BlockObject _blockObject;
        protected Floodgate _floodgate;

        private DroughtService _droughtServíce;
        private WeatherActionsSerializer _weatherActionsSerializer;
        private WaterPumpGaugeConfigSerializer _waterPumpGaugeConfigSerializer;

        public EntityLinker FirstLink;
        public EntityLinker SecondLink;
        public WeatherActions Actions;
        public GaugeConfig Gauge1;

        public int BaseZ => this._blockObject.CoordinatesAtBaseZ.z;

        public int FloodgateMaxHeight => _floodgate.MaxHeight;
        public bool Automatic { get; set; }
        public float ClosedLevel { get; set; }
        public float OpenedLevel { get; set; }

        FloodgateMonobehaviour() {
            this.Actions = new (SeasonSetting.Off, SeasonSetting.Off);
            this.Gauge1 = new (true, true, true, false, false, false);
            this.Automatic = false;
            this.ClosedLevel = (float)this.GetComponent<Floodgate>().MaxHeight;
            this.OpenedLevel = 0.0f;
        }

        [Inject]
        public void InjectDependencies(DroughtService droughtService, WeatherActionsSerializer weatherActionsSerializer, WaterPumpGaugeConfigSerializer waterPumpGaugeConfigSerializer) {
            this._droughtServíce = droughtService;
            this._weatherActionsSerializer = weatherActionsSerializer;
            this._waterPumpGaugeConfigSerializer = waterPumpGaugeConfigSerializer;
        }

        public void Awake() {
            this._blockObject = this.GetComponent<BlockObject>();
            this._floodgate = this.GetComponent<Floodgate>();
        }

        protected void Validate(ref EntityLinker linkee) {
            if ((bool)linkee) {
                bool found = false;
                EntityLinker linker = GetComponent<EntityLinker>();
                ReadOnlyCollection<EntityLink> links = (ReadOnlyCollection<EntityLink>)linker.EntityLinks;
                foreach (var link in links) {
                    if (linkee == link.Linkee) {
                        return;
                    }
                }
                if (!found) {
                    linkee = null;
                }
            }
        }

        public bool Open() {
            AdvancedStreamGaugeStatus status1 = (bool)this.FirstLink ? this.FirstLink.GetComponent<AdvancedStreamGaugeBase>().Status : AdvancedStreamGaugeStatus.Incomplete;

            switch (Actions.Get(_droughtServíce.IsDrought)) {
                case SeasonSetting.Off:
                    return false;
                case SeasonSetting.On:
                    return true;
                case SeasonSetting.Gauge:
                    return this.Gauge1.On(status1);
                default:
                    break;
            }
            return false;
        }

        private void SetHeight(float value) {
            if (Mathf.Round(this._floodgate.Height * 2) != Mathf.Round(value *2)) {
                this._floodgate.SetHeightAndSynchronize(value);
            }
        }

        public override void Tick() {
            if (Automatic) {
                Validate(ref this.FirstLink);
                Validate(ref this.SecondLink);
                if (Open()) {
                    if (this.SecondLink != null) {
                        var gauge = this.SecondLink.GetComponent<AdvancedStreamGaugeBase>();
                        float relativeWaterLevel = Mathf.Clamp(gauge.WaterLevel + (gauge.BaseZ - BaseZ), 0.0f, _floodgate.MaxHeight);
                        if (relativeWaterLevel > OpenedLevel) {
                            float temp = (int)relativeWaterLevel;
                            float targetLevel = Mathf.Max(0.0f,
                                relativeWaterLevel > temp + TriggerPoint1 ? temp + 0.5f :
                                relativeWaterLevel > temp + TriggerPoint2 ? temp :
                                temp - 0.5f
                                );

                            SetHeight(targetLevel);
                        }
                        else {
                            SetHeight(OpenedLevel);
                        }
                    }
                    else {
                        SetHeight(OpenedLevel);
                    }
                }
                else {
                    SetHeight(ClosedLevel);
                }
            }
        }

        public void Save(IEntitySaver entitySaver) {
            IObjectSaver objectSaver = entitySaver.GetComponent(FloodgateMonobehaviour.FloodgateKey);
            if (this.FirstLink) {
                objectSaver.Set(FloodgateMonobehaviour.FirstLinkKey, this.FirstLink);
            }
            if (this.SecondLink) {
                objectSaver.Set(FloodgateMonobehaviour.SecondLinkKey, this.SecondLink);
            }
            objectSaver.Set(ActionsKey, this.Actions, this._weatherActionsSerializer);
            objectSaver.Set(Gauge1Key, this.Gauge1, this._waterPumpGaugeConfigSerializer);
            objectSaver.Set(AutomaticKey, this.Automatic);
        }

        public void Load(IEntityLoader entityLoader) {
            if (!entityLoader.HasComponent(FloodgateKey)) {
                return;
            }
            IObjectLoader objectLoader = entityLoader.GetComponent(FloodgateKey);
            if (objectLoader.Has(FloodgateMonobehaviour.FirstLinkKey)) {
                FirstLink = objectLoader.Get(FloodgateMonobehaviour.FirstLinkKey);
            }
            if (objectLoader.Has(FloodgateMonobehaviour.SecondLinkKey)) {
                SecondLink = objectLoader.Get(FloodgateMonobehaviour.SecondLinkKey);
            }
            if (objectLoader.Has(ActionsKey)) {
                this.Actions = objectLoader.Get(ActionsKey, this._weatherActionsSerializer);
                this.Gauge1 = objectLoader.Get(Gauge1Key, this._waterPumpGaugeConfigSerializer);
                this.Automatic = objectLoader.Get(AutomaticKey);
            }
        }

        public void OnEnterFinishedState() {
        }

        public void OnExitFinishedState() {
        }
    }
}
