using Timberborn.Persistence;
using UnityEngine;

namespace Avernar.Gauge {
    public class GaugeConfig : MonoBehaviour {
        public bool Dry;
        public bool Low;
        public bool NormalWasLow;
        public bool NormalWasHigh;
        public bool High;
        public bool Overflow;

        public GaugeConfig(bool Dry, bool Low, bool NormalWasLow, bool NormalWasHigh, bool High, bool Overflow) {
            this.Dry = Dry;
            this.Low = Low;
            this.NormalWasLow = NormalWasLow;
            this.NormalWasHigh = NormalWasHigh;
            this.High = High;
            this.Overflow = Overflow;
        }

        public bool On(AdvancedStreamGaugeStatus status) {
            switch (status) {
                case AdvancedStreamGaugeStatus.Incomplete:
                    return false;

                case AdvancedStreamGaugeStatus.Dry:
                    return this.Dry;

                case AdvancedStreamGaugeStatus.Low:
                    return this.Low;

                case AdvancedStreamGaugeStatus.NormalWasLow:
                    return this.NormalWasLow;

                case AdvancedStreamGaugeStatus.NormalWasHigh:
                    return this.NormalWasHigh;

                case AdvancedStreamGaugeStatus.High:
                    return this.High;

                case AdvancedStreamGaugeStatus.Overflow:
                    return this.Overflow;
            }
            return false;
        }
    }

    public class WaterPumpGaugeConfigSerializer : IObjectSerializer<GaugeConfig> {
        protected static readonly PropertyKey<bool> DryKey = new(nameof(GaugeConfig.Dry));
        protected static readonly PropertyKey<bool> LowKey = new(nameof(GaugeConfig.Low));
        protected static readonly PropertyKey<bool> NormalWasLowKey = new(nameof(GaugeConfig.NormalWasLow));
        protected static readonly PropertyKey<bool> NormalWasHighKey = new(nameof(GaugeConfig.NormalWasHigh));
        protected static readonly PropertyKey<bool> HighKey = new(nameof(GaugeConfig.High));
        protected static readonly PropertyKey<bool> OverflowKey = new(nameof(GaugeConfig.Overflow));

        public virtual Obsoletable<GaugeConfig> Deserialize(IObjectLoader objectLoader) {
            bool dry = objectLoader.Get(DryKey);
            bool low = objectLoader.Get(LowKey);
            bool normalWasLow = objectLoader.Get(NormalWasLowKey);
            bool normalWasHigh = objectLoader.Get(NormalWasHighKey);
            bool high = objectLoader.Get(HighKey);
            bool overflow = objectLoader.Get(OverflowKey);
            return new GaugeConfig(dry, low, normalWasLow, normalWasHigh, high, overflow);
        }

        public virtual void Serialize(GaugeConfig value, IObjectSaver objectSaver) {
            objectSaver.Set(DryKey, value.Dry);
            objectSaver.Set(LowKey, value.Low);
            objectSaver.Set(NormalWasLowKey, value.NormalWasLow);
            objectSaver.Set(NormalWasHighKey, value.NormalWasHigh);
            objectSaver.Set(HighKey, value.High);
            objectSaver.Set(OverflowKey, value.Overflow);
        }
    }
}
