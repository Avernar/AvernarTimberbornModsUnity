using UnityEngine;
using Bindito.Core;
using Timberborn.Persistence;
using Timberborn.WaterSystem;

namespace Avernar.Gauge {
    public class AdvancedStreamGaugeTop : AdvancedStreamGauge, IPersistentEntity {
        internal override bool IsTop() => true;

        public new void Awake() {
            base.Awake();
        }

        public void Save(IEntitySaver entitySaver) {
        }

        public void Load(IEntityLoader entityLoader) {
        }
    }
}