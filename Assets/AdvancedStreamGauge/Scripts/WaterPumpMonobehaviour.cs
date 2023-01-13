using Bindito.Core;
using System.Collections.ObjectModel;
using Timberborn.ConstructibleSystem;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using Timberborn.WeatherSystem;
using Timberborn.Buildings;
using TimberApi.EntityLinkerSystem;
using UnityEngine;


namespace Avernar.Gauge {
    public class WaterPumpMonobehaviour : TickableComponent, IPersistentEntity, IFinishedStateListener {
        private static readonly ComponentKey WaterPumpKey = new(nameof(WaterPumpMonobehaviour));
        protected static readonly PropertyKey<bool> AutomaticKey = new(nameof(Automatic));
        private static readonly PropertyKey<EntityLinker> FirstLinkKey = new(nameof(FirstLink));
        private static readonly PropertyKey<EntityLinker> SecondLinkKey = new(nameof(SecondLink));
        protected static readonly PropertyKey<WeatherActions> ActionsKey = new(nameof(Actions));
        protected static readonly PropertyKey<GaugeConfig> Gauge1Key = new(nameof(Gauge1));
        protected static readonly PropertyKey<GaugeConfig> Gauge2Key = new(nameof(Gauge2));
        protected static readonly PropertyKey<bool> OrKey = new(nameof(Or));

        private bool _finished;
        private EntityLinker _linker;
        private PausableBuilding _pausable;
        private DroughtService _droughtServíce;
        private WeatherActionsSerializer _weatherActionsSerializer;
        private WaterPumpGaugeConfigSerializer _waterPumpGaugeConfigSerializer;

        public bool Automatic;
        public EntityLinker FirstLink;
        public EntityLinker SecondLink;
        public WeatherActions Actions;
        public GaugeConfig Gauge1;
        public GaugeConfig Gauge2;
        public bool Or;

        WaterPumpMonobehaviour() {
            this.Automatic = false;
            this.Actions = new(SeasonSetting.On, SeasonSetting.On);
            this.Gauge1 = new(true, true, true, false, false, false);
            this.Gauge2 = new(false, true, true, true, true, true);
            this.Or = false;
        }

        [Inject]
        public void InjectDependencies(DroughtService droughtService, WeatherActionsSerializer weatherActionsSerializer, WaterPumpGaugeConfigSerializer waterPumpGaugeConfigSerializer) {
            this._droughtServíce = droughtService;
            this._weatherActionsSerializer = weatherActionsSerializer;
            this._waterPumpGaugeConfigSerializer = waterPumpGaugeConfigSerializer;
        }

        public void Awake() {
            this._linker = GetComponent<EntityLinker>();
            this._pausable = GetComponent<PausableBuilding>();
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

        public bool On() {
            switch (Actions.Get(_droughtServíce.IsDrought)) {
                case SeasonSetting.Off:
                    return false;
                case SeasonSetting.On:
                    return true;
                case SeasonSetting.Gauge:
                    AdvancedStreamGaugeBase gauge1 = GetLink<AdvancedStreamGaugeBase>(ref this.FirstLink);
                    AdvancedStreamGaugeBase gauge2 = GetLink<AdvancedStreamGaugeBase>(ref this.SecondLink);
                    AdvancedStreamGaugeStatus status1 = (bool)gauge1 ? gauge1.Status : AdvancedStreamGaugeStatus.Incomplete;
                    AdvancedStreamGaugeStatus status2 = (bool)gauge2 ? gauge2.Status : AdvancedStreamGaugeStatus.Incomplete;
                    bool on1 = this.Gauge1.On(status1);
                    bool on2 = this.Gauge2.On(status2);
                    return
                        status1 == AdvancedStreamGaugeStatus.Incomplete ? on2 :
                        status2 == AdvancedStreamGaugeStatus.Incomplete ? on1 :
                        this.Or ? on1 || on2 :
                        on1 && on2;
                default:
                    break;
            }
            return false;
        }

        public override void Tick() {
            if (_finished && Automatic) {
                if (On()) {
                    _pausable.Resume();
                }
                else {
                    _pausable.Pause();
                }
            }
        }

        public void Save(IEntitySaver entitySaver) {
            IObjectSaver objectSaver = entitySaver.GetComponent(WaterPumpMonobehaviour.WaterPumpKey);
            if (this.FirstLink) {
                objectSaver.Set(WaterPumpMonobehaviour.FirstLinkKey, this.FirstLink);
            }
            if (this.SecondLink) {
                objectSaver.Set(WaterPumpMonobehaviour.SecondLinkKey, this.SecondLink);
            }
            objectSaver.Set(ActionsKey, this.Actions, this._weatherActionsSerializer);
            objectSaver.Set(Gauge1Key, this.Gauge1, this._waterPumpGaugeConfigSerializer);
            objectSaver.Set(Gauge2Key, this.Gauge2, this._waterPumpGaugeConfigSerializer);
            objectSaver.Set(OrKey, this.Or);
            objectSaver.Set(AutomaticKey, this.Automatic);
        }

        public void Load(IEntityLoader entityLoader) {
            if (!entityLoader.HasComponent(WaterPumpKey)) {
                return;
            }
            IObjectLoader objectLoader = entityLoader.GetComponent(WaterPumpKey);
            if (objectLoader.Has(FirstLinkKey)) {
                FirstLink = objectLoader.Get(FirstLinkKey);
            }
            if (objectLoader.Has(SecondLinkKey)) {
                SecondLink = objectLoader.Get(SecondLinkKey);
            }
            if (objectLoader.Has(ActionsKey)) {
                this.Actions = objectLoader.Get(ActionsKey, this._weatherActionsSerializer);
                this.Gauge1 = objectLoader.Get(Gauge1Key, this._waterPumpGaugeConfigSerializer);
                this.Gauge2 = objectLoader.Get(Gauge2Key, this._waterPumpGaugeConfigSerializer);
                this.Or = objectLoader.Get(OrKey);
            }
            else {
                this.Actions.Temperate = SeasonSetting.On;
                this.Actions.Drought = SeasonSetting.On;
            }
            if (objectLoader.Has(AutomaticKey)) {
                this.Automatic = objectLoader.Get(AutomaticKey);
            }
            else {
                this.Automatic = true;
            }
        }
    }
}
