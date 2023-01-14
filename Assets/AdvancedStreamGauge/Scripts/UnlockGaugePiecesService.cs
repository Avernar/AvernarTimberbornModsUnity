using Timberborn.BlockObjectTools;
using Timberborn.Buildings;
using Timberborn.CoreUI;
using Timberborn.Localization;
using Timberborn.PrefabSystem;
using Timberborn.ScienceSystem;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;

namespace Avernar.Gauge {
    public class UnlockGaguePiecesService : IPostLoadableSingleton {
        private static readonly string UnlockPromptLocKey = "Avernar.AdvancedStreamGauge.UnlockPrompt";
        private readonly EventBus _eventBus;
        private readonly BuildingService _buildingService;
        private readonly BuildingUnlockingService _buildingUnlockingService;
        private readonly PrefabNameMapper _prefabNameMapper;
        private readonly ToolButtonService _toolButtonService;
        private readonly ILoc _loc;
        private readonly DialogBoxShower _dialogBoxShower;
        private bool _gaugeUnlocked;

        public UnlockGaguePiecesService(
          EventBus eventBus,
          BuildingService buildingService,
          BuildingUnlockingService buildingUnlockingService,
          PrefabNameMapper prefabNameMapper,
          ToolButtonService toolButtonService,
          ILoc loc,
          DialogBoxShower dialogBoxShower) {
            _eventBus = eventBus;
            _buildingService = buildingService;
            _buildingUnlockingService = buildingUnlockingService;
            _prefabNameMapper = prefabNameMapper;
            _toolButtonService = toolButtonService;
            _loc = loc;
            _dialogBoxShower = dialogBoxShower;
        }

        public void PostLoad() {
            _eventBus.Register(this);
            TryUnlockingTrains();
            LockTrains();
        }

        [OnEvent]
        public void OnBuildingUnlocked(BuildingUnlockedEvent buildingUnlockedEvent) {
            TryUnlockingTrains(_prefabNameMapper.GetPrefab(_buildingService.GetPrefabName(buildingUnlockedEvent.Building)).GetComponent<AdvancedStreamGaugeBase>());
            UnlockTrains();
        }

        private void TryUnlockingTrains() {
            foreach (Building building in _buildingService.Buildings) {
                if (_buildingUnlockingService.Unlocked(building))
                    TryUnlockingTrains(building.GetComponent<AdvancedStreamGaugeBase>());
            }
        }

        private void TryUnlockingTrains(AdvancedStreamGaugeBase basePiece) {
            if (!(basePiece != null))
                return;
            _gaugeUnlocked = true;
        }

        private void LockTrains() {
            foreach (ToolButton toolButton in _toolButtonService.ToolButtons) {
                BlockObjectTool blockObjectTool = toolButton.Tool as BlockObjectTool;

                var gaugePiece = blockObjectTool?.Prefab.GetComponent<AdvancedStreamGauge>();
                var basePiece = blockObjectTool?.Prefab.GetComponent<AdvancedStreamGaugeBase>();
                if (gaugePiece != null && !basePiece && GuageIsLocked()) {
                    blockObjectTool.Locked = true;
                    toolButton.ButtonClicked += (_1, _2) => OnButtonClicked(blockObjectTool);
                }
            }
        }

        private void UnlockTrains() {
            foreach (ToolButton toolButton in _toolButtonService.ToolButtons) {
                BlockObjectTool blockObjectTool = toolButton.Tool as BlockObjectTool;

                var gaugePiece = blockObjectTool?.Prefab.GetComponent<AdvancedStreamGauge>();
                var basePiece = blockObjectTool?.Prefab.GetComponent<AdvancedStreamGaugeBase>();
                if (gaugePiece != null && !basePiece && !GuageIsLocked()) {
                    blockObjectTool.Locked = false;
                }
            }
        }

        private void OnButtonClicked(BlockObjectTool blockObjectTool) {
            if (!GuageIsLocked())
                return;
            ShowLockedMessage(blockObjectTool);
        }

        private bool GuageIsLocked() => !_gaugeUnlocked;

        private void ShowLockedMessage(BlockObjectTool blockObjectTool) {
            string displayNameLocKey = blockObjectTool.Prefab.GetComponent<LabeledPrefab>().DisplayNameLocKey;
            string text = _loc.T(UnlockPromptLocKey, _loc.T("Avernar.AdvancedStreamGaugeBase.DisplayName"), _loc.T(displayNameLocKey));
            _dialogBoxShower.Create().SetMessage(text).Show();
        }
    }
}
