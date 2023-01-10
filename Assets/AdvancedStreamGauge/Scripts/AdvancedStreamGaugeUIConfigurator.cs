using Bindito.Core;
using TimberApi.ConfiguratorSystem;
using TimberApi.SceneSystem;
using Timberborn.EntityPanelSystem;

namespace Avernar.Gauge {
    [Configurator(SceneEntrypoint.InGame)]
    public class AdvancedStreamGaugeUIConfigurator : IConfigurator {
        public void Configure(IContainerDefinition containerDefinition) {
            containerDefinition.Bind<AdvancedStreamGaugeFragment>().AsSingleton();
            containerDefinition.MultiBind<EntityPanelModule>().ToProvider<AdvancedStreamGaugeUIConfigurator.EntityPanelModuleProvider>().AsSingleton();
        }

        private class EntityPanelModuleProvider : IProvider<EntityPanelModule> {
            private readonly AdvancedStreamGaugeFragment _advancedStreamGaugeFragment;

            public EntityPanelModuleProvider(AdvancedStreamGaugeFragment streamGaugeFragment) {
                this._advancedStreamGaugeFragment = streamGaugeFragment;
            }

            public EntityPanelModule Get() {
                EntityPanelModule.Builder builder = new EntityPanelModule.Builder();
                builder.AddTopFragment((IEntityPanelFragment)this._advancedStreamGaugeFragment);
                return builder.Build();
            }
        }
    }
}
