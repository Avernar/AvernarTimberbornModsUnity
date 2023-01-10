using Bindito.Core;
using TimberApi.ConfiguratorSystem;
using TimberApi.SceneSystem;
using Timberborn.EntityPanelSystem;

namespace Avernar.Gauge {
    [Configurator(SceneEntrypoint.InGame)]
    public class AdvancedStreamGaugeUIConfigurator : IConfigurator {
        public void Configure(IContainerDefinition containerDefinition) {
            containerDefinition.Bind<AdvancedStreamGaugeFragment>().AsSingleton();
            containerDefinition.Bind<FloodgateFragment>().AsSingleton();
            containerDefinition.Bind<WaterPumpFragment>().AsSingleton();
            containerDefinition.MultiBind<EntityPanelModule>().ToProvider<AdvancedStreamGaugeUIConfigurator.EntityPanelModuleProvider>().AsSingleton();
        }

        private class EntityPanelModuleProvider : IProvider<EntityPanelModule> {
            private readonly AdvancedStreamGaugeFragment _advancedStreamGaugeFragment;
            private readonly FloodgateFragment _floodgateFragment;
            private readonly WaterPumpFragment _waterPumpFragment;

            public EntityPanelModuleProvider(AdvancedStreamGaugeFragment streamGaugeFragment, FloodgateFragment floodgateFragment, WaterPumpFragment waterPumpFragment) {
                this._advancedStreamGaugeFragment = streamGaugeFragment;
                this._floodgateFragment = floodgateFragment;
                this._waterPumpFragment = waterPumpFragment;
            }

            public EntityPanelModule Get() {
                EntityPanelModule.Builder builder = new EntityPanelModule.Builder();
                builder.AddTopFragment((IEntityPanelFragment)this._advancedStreamGaugeFragment);
                builder.AddMiddleFragment((IEntityPanelFragment)this._floodgateFragment);
                builder.AddMiddleFragment((IEntityPanelFragment)this._waterPumpFragment);
                return builder.Build();
            }
        }
    }
}
