﻿using System.Collections.Generic;
using UnityEngine;
using TUNING;

namespace ButcherStation
{
    public class FishingStationConfig : IBuildingConfig
    {
        public const string ID = "FishingStation";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID, 
                width: 3, 
                height: 2, 
                anim: "fishingstation_kanim", 
                hitpoints: BUILDINGS.HITPOINTS.TIER2, 
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1, 
                construction_mass: new float[] { BUILDINGS.CONSTRUCTION_MASS_KG.TIER4[0], BUILDINGS.CONSTRUCTION_MASS_KG.TIER1[0] },
                construction_materials: new string[] { "Metal", "Algae" }, 
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1, 
                build_location_rule: BuildLocationRule.OnFloor, 
                decor: BUILDINGS.DECOR.NONE, 
                noise: NOISE_POLLUTION.NOISY.TIER1
                );
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER2;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER1;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER1;
            buildingDef.Floodable = true;
            buildingDef.Entombable = true;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
//            buildingDef.OverheatTemperature = BUILDINGS.OVERHEAT_TEMPERATURES.HIGH_1;
            buildingDef.UtilityInputOffset = new CellOffset(0, 0);
            buildingDef.UtilityOutputOffset = new CellOffset(0, 0);
            buildingDef.DefaultAnimState = "off";
            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.CreatureRelocator, false);
            Storage storage = go.AddOrGet<Storage>();
            storage.allowItemRemoval = false;
            storage.showDescriptor = false;
            storage.storageFilters = new List<Tag> { ButcherStation.FisherableCreature };
            storage.allowSettingOnlyFetchMarkedItems = false;
            go.AddOrGet<TreeFilterable>();
            ButcherStation butcherStation = go.AddOrGet<ButcherStation>();
            butcherStation.creatureEligibleTag = ButcherStation.FisherableCreature;
            go.AddOrGet<LoopingSounds>();
            go.AddOrGet<BuildingComplete>().isManuallyOperated = true;
            go.GetComponent<KPrefabID>().AddTag(RoomConstraints.ConstraintTags.RanchStation, false);
            RoomTracker roomTracker = go.AddOrGet<RoomTracker>();
            roomTracker.requiredRoomType = Db.Get().RoomTypes.CreaturePen.Id;
            roomTracker.requirement = RoomTracker.Requirement.Required;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            RanchStation.Def def = go.AddOrGetDef<RanchStation.Def>();
            def.isCreatureEligibleToBeRanchedCb = delegate (GameObject creature_go, RanchStation.Instance ranch_station_smi)
            {
                ButcherStation butcherStation = ranch_station_smi.GetComponent<ButcherStation>();
                if (butcherStation != null)
                {
                    return butcherStation.IsCreatureEligibleToBeButched(creature_go);
                }
                return false;
            };
            def.onRanchCompleteCb = delegate (GameObject creature_go)
            {
                /*
                RanchStation.Instance smi = creature_go.GetSMI<RanchableMonitor.Instance>()?.targetRanchStation;
                if (smi != null)
                {
                    creature_go.transform.SetPosition(smi.transform.GetPosition());
                }
                creature_go.GetSMI<DeathMonitor.Instance>()?.Kill(Db.Get().Deaths.Generic);
                */
                ButcherStation.ButchCreature(creature_go, true);
            };
            def.getTargetRanchCell = delegate (RanchStation.Instance smi)
            {
                int num = Grid.InvalidCell;
                if (!smi.IsNullOrStopped())
                {
                    bool water;
                    num = Grid.CellBelow(Grid.PosToCell(smi.transform.GetPosition()));
                    num = Grid.OffsetCell(num, 0, - FishingStationGuide.GetDepthAvailable(smi.gameObject, out water));
                }
                return num;
            };
            def.interactLoopCount = 1;
            def.rancherInteractAnim = "anim_interacts_fishingstation_kanim";
            def.ranchedPreAnim = "bitehook";
            def.ranchedLoopAnim = "caught_loop";
            def.ranchedPstAnim = "trapped_pre"; 
            def.synchronizeBuilding = true;
            Prioritizable.AddRef(go);

            AddGuide(go: go.GetComponent<Building>().Def.BuildingPreview, preview: true, occupy_tiles: false);
            AddGuide(go: go.GetComponent<Building>().Def.BuildingPreview, foundament: true);

            AddGuide(go: go.GetComponent<Building>().Def.BuildingUnderConstruction, preview: true, occupy_tiles: true);
            AddGuide(go: go.GetComponent<Building>().Def.BuildingUnderConstruction, foundament: true);

            AddGuide(go: go.GetComponent<Building>().Def.BuildingComplete, preview: false, occupy_tiles: true);
        }

        public static void AddGuide(GameObject go, bool preview = true, bool occupy_tiles = false, bool foundament = false)
        {
            GameObject gameObject = new GameObject();
            gameObject.transform.parent = go.transform;
            gameObject.transform.SetLocalPosition(Vector3.zero);
            KBatchedAnimController kbatchedAnimController = gameObject.AddComponent<KBatchedAnimController>();
            kbatchedAnimController.Offset = go.GetComponent<Building>().Def.GetVisualizerOffset();
            kbatchedAnimController.AnimFiles = new KAnimFile[]
            {
                Assets.GetAnim(new HashedString("fishing_line_kanim"))
            };
            kbatchedAnimController.initialAnim = preview ? ( foundament ? "foundament" : "place" ) : "hook";
            kbatchedAnimController.visibilityType = KAnimControllerBase.VisibilityType.OffscreenUpdate;
            kbatchedAnimController.sceneLayer = Grid.SceneLayer.BuildingBack;
            kbatchedAnimController.isMovable = true;
            kbatchedAnimController.PlayMode = preview ? KAnim.PlayMode.Once : KAnim.PlayMode.Loop;
            if (!foundament)
            {
                FishingStationGuide fishingStationGuide = gameObject.AddComponent<FishingStationGuide>();
                fishingStationGuide.parent = go;
                fishingStationGuide.occupyTiles = occupy_tiles;
                fishingStationGuide.isPreview = preview;
            }
        }
    }
}
