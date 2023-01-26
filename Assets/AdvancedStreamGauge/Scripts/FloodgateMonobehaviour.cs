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
        protected static readonly PropertyKey<float> ClosedLevelKey = new(nameof(ClosedLevel));
        protected static readonly PropertyKey<float> OpenedLevelKey = new(nameof(OpenedLevel));
        protected static readonly PropertyKey<bool> BackflowPreventionKey = new(nameof(BackflowPrevention));

        protected bool _finished;
        protected EntityLinker _linker;
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
        public float FloodgateHeight => _floodgate.Height;

        public bool Automatic { get; set; }
        public float ClosedLevel { get; set; }
        public float OpenedLevel { get; set; }
        public bool BackflowPrevention { get; set; }

        FloodgateMonobehaviour() {
            this.Automatic = false;
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
            this._linker = GetComponent<EntityLinker>();
            this._blockObject = this.GetComponent<BlockObject>();
            this._floodgate = this.GetComponent<Floodgate>();
            _finished = false;
        }

        public void OnEnterFinishedState() => this._finished = true;

        public void OnExitFinishedState() => this.enabled = false;

        T GetLink<T>(ref EntityLinker linkee) where T : Object {
            if ((bool)linkee) {
                bool found = false;
                ReadOnlyCollection<EntityLink> links = (ReadOnlyCollection<EntityLink>)_linker.EntityLinks;
                foreach (var link in links) {
                    if (linkee == link.Linkee) {
                        return linkee.GetComponent<T>();
                    }
                }
                if (!found) {
                    linkee = null;
                }
            }
            return default;
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
            if (_finished && Automatic) {
                var setting = Actions.Get(_droughtServíce.IsDrought);
                if (setting == SeasonSetting.Off) {
                    SetHeight(ClosedLevel);
                    return;
                }

                AdvancedStreamGaugeBase gauge1 = GetLink<AdvancedStreamGaugeBase>(ref this.FirstLink);

                if (setting == SeasonSetting.Gauge) {
                    AdvancedStreamGaugeStatus status1 = gauge1 == null ? AdvancedStreamGaugeStatus.Incomplete : gauge1.Status;
                    if (!this.Gauge1.On(status1)) {
                        SetHeight(ClosedLevel);
                        return;
                    }
                }

                AdvancedStreamGaugeBase gauge2 = GetLink<AdvancedStreamGaugeBase>(ref this.SecondLink);
                if (gauge2 != null) {
                    if (gauge2.Status != AdvancedStreamGaugeStatus.Incomplete) {
                        if (BackflowPrevention && gauge1 != null && gauge1 != gauge2) {
                            if (gauge1.AbsoluteWaterLevel >= gauge2.AbsoluteWaterLevel) {
                                SetHeight(ClosedLevel);
                                return;
                            }
                        }

                        float relativeWaterLevel = Mathf.Clamp(gauge2.AbsoluteWaterLevel + -BaseZ, 0.0f, _floodgate.MaxHeight);
                        if (relativeWaterLevel > OpenedLevel) {
                            float temp = (int)relativeWaterLevel;
                            float targetLevel = Mathf.Max(0.0f,
                                relativeWaterLevel > temp + TriggerPoint1 ? temp + 0.5f :
                                relativeWaterLevel > temp + TriggerPoint2 ? temp :
                                temp - 0.5f
                                );

                            SetHeight(targetLevel);
                            return;
                        }
                    }
                }
                SetHeight(OpenedLevel);
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
            objectSaver.Set(ClosedLevelKey, this.ClosedLevel);
            objectSaver.Set(OpenedLevelKey, this.OpenedLevel);
            objectSaver.Set(BackflowPreventionKey, this.BackflowPrevention);
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
            if (objectLoader.Has(ClosedLevelKey)) {
                this.ClosedLevel = objectLoader.Get(ClosedLevelKey);
            }
            if (objectLoader.Has(OpenedLevelKey)) {
                this.OpenedLevel = objectLoader.Get(OpenedLevelKey);
            }
            if (objectLoader.Has(BackflowPreventionKey)) {
                this.BackflowPrevention = objectLoader.Get(BackflowPreventionKey);
            }
        }
    }
}
