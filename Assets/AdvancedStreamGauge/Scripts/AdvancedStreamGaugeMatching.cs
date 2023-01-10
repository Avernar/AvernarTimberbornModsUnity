using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bindito.Core;
using TimberApi.ConfiguratorSystem;
using TimberApi.SceneSystem;
using Timberborn.PreviewSystem;
using Timberborn.BlockSystem;
using Timberborn.Localization;
using Timberborn.PrefabSystem;
using UnityEngine;

namespace Avernar.Gauge {
    [Configurator(SceneEntrypoint.InGame)]
    public class AdvancedStreamGagueMatchingConfigurator : IConfigurator {
        public void Configure(IContainerDefinition containerDefinition) => containerDefinition.MultiBind<IPreviewsValidator>().To<AdvancedStreamGagueMatchingPrefabPreviewsValidator>().AsSingleton();
    }

    internal class AdvancedStreamGagueMatchingPrefabPreviewsValidator : IPreviewsValidator {
        private static readonly string UnsuitableBelowLocKey = "Avernar.AdvancedStreamGauge.UnsuitableBelow";
        private static readonly string CanNotBePlacedLocKey = "Avernar.AdvancedStreamGauge.CanNotBePlaced";
        private readonly BlockService _blockService;
        private readonly ILoc _loc;
        private readonly PrefabNameMapper _prefabNameMapper;

        public AdvancedStreamGagueMatchingPrefabPreviewsValidator(
          BlockService blockService,
          ILoc loc,
          PrefabNameMapper prefabNameMapper) {
            this._blockService = blockService;
            this._loc = loc;
            this._prefabNameMapper = prefabNameMapper;
        }

        private bool IsStackable(BlockObject blockObject) {
            AdvancedStreamGauge asg = blockObject.GetComponent<AdvancedStreamGauge>();
            return asg != null && !asg.IsBase();
        }
        public bool PreviewsAreValid(IReadOnlyList<Preview> previews, out string errorMessage) {
            foreach (Component preview in (IEnumerable<Preview>)previews) {
                BlockObject blockObject = preview.GetComponent<BlockObject>();
                IEnumerable<Vector3Int> foundation = blockObject.PositionedBlocks.GetFoundationCoordinates();
                if (this.IsStackable(blockObject)) {
                    if (!IsAdvancedStreamGaugeBelow(foundation)) { 
                        errorMessage = this._loc.T(UnsuitableBelowLocKey);
                        return false;
                    }
                }
                else {
                    if (IsAdvancedStreamGaugeBelow(foundation)) {
                        errorMessage = this._loc.T(CanNotBePlacedLocKey);
                        return false;
                    }
                }
            }
            errorMessage = (string)null;
            return true;
        }

        private bool IsAdvancedStreamGaugeBelow(IEnumerable<Vector3Int> foundation) {
            return foundation.Any<Vector3Int>((Func<Vector3Int, bool>)(coordinates => IsAdvancedStreamGaugeBelow(coordinates)));
        }

        private bool IsAdvancedStreamGaugeBelow(Vector3Int coordinates) {
            Vector3Int coordinatesBelow = coordinates - new Vector3Int(0, 0, 1);
            ReadOnlyCollection<BlockObject> objectsAt = _blockService.GetObjectsAt(coordinatesBelow);
            for (int index = 0; index < objectsAt.Count; ++index) {
                if (objectsAt[index].GetComponent<AdvancedStreamGauge>() != null) {
                    return true;
                }
            }
            return false;
        }
    }
}
