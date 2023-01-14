using Timberborn.Persistence;

namespace Avernar.Gauge { 
    public class AdvancedStreamGaugeMiddle : AdvancedStreamGauge, IPersistentEntity {
        internal override bool IsMiddle() => true;

        public new void Awake() {
            base.Awake();
        }

        public void Save(IEntitySaver entitySaver) {
        }

        public void Load(IEntityLoader entityLoader) {
        }
    }
}
