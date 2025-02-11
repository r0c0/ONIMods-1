﻿using System.Collections.Generic;
using UnityEngine;
using TUNING;

namespace ButcherStation
{
    public class ButcherStationConfig : IBuildingConfig
    {
        public const string ID = "ButcherStation";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID, 
                width: 3, 
                height: 3, 
                anim: "butcher_station_kanim",   //  "metalreclaimer_kanim"
                hitpoints: BUILDINGS.HITPOINTS.TIER2, 
                construction_time: BUILDINGS.CONSTRUCTION_TIME_SECONDS.TIER1, 
                construction_mass: BUILDINGS.CONSTRUCTION_MASS_KG.TIER4, 
                construction_materials: MATERIALS.ALL_METALS, 
                melting_point: BUILDINGS.MELTING_POINT_KELVIN.TIER1, 
                build_location_rule: BuildLocationRule.OnFloor, 
                decor: BUILDINGS.DECOR.PENALTY.TIER4, 
                noise: NOISE_POLLUTION.NOISY.TIER1);
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = BUILDINGS.ENERGY_CONSUMPTION_WHEN_ACTIVE.TIER4;
            buildingDef.ExhaustKilowattsWhenActive = BUILDINGS.EXHAUST_ENERGY_ACTIVE.TIER3;
            buildingDef.SelfHeatKilowattsWhenActive = BUILDINGS.SELF_HEAT_KILOWATTS.TIER4;
            buildingDef.Floodable = true;
            buildingDef.Entombable = true;
            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "large";
            buildingDef.OverheatTemperature = BUILDINGS.OVERHEAT_TEMPERATURES.HIGH_1;
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
            storage.storageFilters = new List<Tag> { ButcherStation.ButcherableCreature };
            storage.allowSettingOnlyFetchMarkedItems = false;
            go.AddOrGet<TreeFilterable>();
            ButcherStation butcherStation = go.AddOrGet<ButcherStation>();
            butcherStation.creatureEligibleTag = ButcherStation.ButcherableCreature;
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
                //creature_go.GetSMI<DeathMonitor.Instance>()?.Kill(Db.Get().Deaths.Generic);
                ButcherStation.ButchCreature(creature_go);
            };
            def.getTargetRanchCell = delegate (RanchStation.Instance smi)
            {
                int num = Grid.InvalidCell;
                if (!smi.IsNullOrStopped())
                {
                    num = Grid.PosToCell(smi.transform.GetPosition());
                    if (!smi.targetRanchable.IsNullOrStopped())
                    {
                        if (smi.targetRanchable.HasTag(GameTags.Creatures.Flyer))
                        {
                            num = Grid.CellAbove(num);
                        }
                    }
                }
                return num;
            };
            def.interactLoopCount = 2;
            def.rancherInteractAnim = "anim_interacts_shearingstation_kanim";
            def.ranchedPreAnim = "grooming_pre";
            //def.ranchedLoopAnim = "grooming_loop";
            def.ranchedLoopAnim = "hit";
            def.ranchedPstAnim = "grooming_pst";
            def.synchronizeBuilding = true;
            Prioritizable.AddRef(go);
        }
    }
}
