using UnityEngine;

namespace Avernar.Gauge {
    public class WeatherActions : MonoBehaviour {
        public SeasonSetting Temperate;
        public SeasonSetting Drought;

        public WeatherActions(SeasonSetting Temperate, SeasonSetting Drought) {
            this.Temperate = Temperate;
            this.Drought = Drought;
        }

        public SeasonSetting Get(bool isDrought) {
            return isDrought ? Drought : Temperate;
        }
    }
}
