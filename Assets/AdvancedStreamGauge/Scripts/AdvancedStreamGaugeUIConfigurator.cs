using Bindito.Core;
using TimberApi.ConfiguratorSystem;
using TimberApi.SceneSystem;
using Timberborn.EntityPanelSystem;

namespace Avernar.Gauge {
    [Configurator(SceneEntrypoint.InGame)]
    public class AdvancedStreamGaugeUIConfigurator : IConfigurator {
        public void Configure(IContainerDefinition containerDefinition) {
            containerDefinition.Bind<AdvancedStreamGaugeFragment>().AsSingleton();
            containerDefinition.Bind<ASGFloodgateFragment>().AsSingleton();
            containerDefinition.Bind<ASGWaterPumpFragment>().AsSingleton();
            containerDefinition.MultiBind<EntityPanelModule>().ToProvider<AdvancedStreamGaugeUIConfigurator.EntityPanelModuleProvider>().AsSingleton();
        }

        private class EntityPanelModuleProvider : IProvider<EntityPanelModule> {
            private readonly AdvancedStreamGaugeFragment _advancedStreamGaugeFragment;
            private readonly ASGFloodgateFragment _floodgateFragment;
            private readonly ASGWaterPumpFragment _waterPumpFragment;

            public EntityPanelModuleProvider(AdvancedStreamGaugeFragment streamGaugeFragment, ASGFloodgateFragment floodgateFragment, ASGWaterPumpFragment waterPumpFragment) {
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
