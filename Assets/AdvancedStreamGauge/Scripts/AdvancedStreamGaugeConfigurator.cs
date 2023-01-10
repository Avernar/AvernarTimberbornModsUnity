using Bindito.Core;
using Bindito.Unity;
using HarmonyLib;
using System;
using TimberApi.ConfiguratorSystem;
using TimberApi.DependencyContainerSystem;
using TimberApi.SceneSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.TemplateSystem;
using Timberborn.WaterBuildings;
using UnityEngine;

namespace Avernar.Gauge {
    [Configurator(SceneEntrypoint.InGame)]
    public class FloodgateEntityActionConfigurator : IConfigurator {
        public void Configure(IContainerDefinition containerDefinition) {
            containerDefinition.Bind<EnumObjectSerializer<SeasonSetting>>().AsSingleton();
            containerDefinition.Bind<WeatherActionsSerializer>().AsSingleton();
            containerDefinition.Bind<WaterPumpGaugeConfigSerializer>().AsSingleton();
            containerDefinition.MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
        }

        private static TemplateModule ProvideTemplateModule() {
            TemplateModule.Builder builder = new TemplateModule.Builder();
            builder.AddDecorator<Floodgate, FloodgateMonobehaviour>();
            return builder.Build();
        }
    }

    [HarmonyPatch(typeof(EntityService), "Instantiate", typeof(GameObject), typeof(Guid))]
    class PumpPatch {
        public static void Postfix(GameObject __result) {
            if ((__result.GetComponent<WaterInput>() != null || __result.GetComponent<WaterOutput>() != null || __result.GetComponent("Timberborn.IrrigationSystem.IrrigationTower")) && __result.name.ToLower().Contains("shower") == false) {
                var instantiator = DependencyContainer.GetInstance<IInstantiator>();
                instantiator.AddComponent<WaterPumpMonobehaviour>(__result);
            }
        }
    }
}
