using Timberborn.Persistence;
using UnityEngine;

namespace Avernar.Gauge {
    public class WeatherActionsSerializer : IObjectSerializer<WeatherActions> {
        protected static readonly PropertyKey<SeasonSetting> TemperateKey = new(nameof(WeatherActions.Temperate));
        protected static readonly PropertyKey<SeasonSetting> DroughtKey = new(nameof(WeatherActions.Drought));

        private EnumObjectSerializer<SeasonSetting> _seasonSettingSerializer;

        public WeatherActionsSerializer(EnumObjectSerializer<SeasonSetting> seasonSettingSerializer) {
            this._seasonSettingSerializer = seasonSettingSerializer;
        }

        public virtual Obsoletable<WeatherActions> Deserialize(IObjectLoader objectLoader) {
            SeasonSetting temperate = objectLoader.Get(TemperateKey, _seasonSettingSerializer);
            SeasonSetting drought = objectLoader.Get(DroughtKey, _seasonSettingSerializer);
            return new WeatherActions(temperate, drought);
        }

        public virtual void Serialize(WeatherActions value, IObjectSaver objectSaver) {
            objectSaver.Set(TemperateKey, value.Temperate, _seasonSettingSerializer);
            objectSaver.Set(DroughtKey, value.Drought, _seasonSettingSerializer);
        }
    }
}
