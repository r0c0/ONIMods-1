﻿using System;
using Klei.AI;
using KSerialization;
using STRINGS;
using UnityEngine;

namespace ButcherStation
{
    public class ButcherStation : StateMachineComponent<ButcherStation.SMInstance>, IIntSliderControl, ISliderControl, IUserControlledCapacity, ICheckboxControl
    {
        public class SMInstance : GameStateMachine<States, SMInstance, ButcherStation, object>.GameInstance
        {
            public SMInstance(ButcherStation master) : base(master)
            {
            }
        }

        public class States : GameStateMachine<States, SMInstance, ButcherStation>
        {
		    public override void InitializeStates(out BaseState default_state)
            {
                default_state = root;
                root.Update("RefreshCreatureCount", delegate (SMInstance smi, float dt)
                {
                    smi.master.RefreshCreatureCount(null);
                }, UpdateRate.SIM_1000ms, false);
            }
        }

        public static readonly Tag ButcherableCreature = TagManager.Create("ButcherableCreature");
        public static readonly Tag FisherableCreature  = TagManager.Create("FisherableCreature");

        public Tag creatureEligibleTag = ButcherableCreature;

        public const int CREATURELIMIT = 20;
        public const float EXTRAMEATPERRANCHINGATTRIBUTE = 0.025f;

        [Serialize]
        private int creatureLimit = Config.Get().MAXCREATURELIMIT;
        private int storedCreatureCount;

        [Serialize]
        private float ageButchThresold = 0.85f;

        [Serialize]
        bool autoButchSurplus = false;

        [MyCmpReq]
        TreeFilterable treeFilterable;

        private static StatusItem capacityStatusItem;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            if (capacityStatusItem == null)
            {
                capacityStatusItem = new StatusItem("StorageLocker", "BUILDING", string.Empty, StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID, true, 129022)
                {
                    resolveStringCallback = delegate (string str, object data)
                    {
                        IUserControlledCapacity userControlledCapacity = (IUserControlledCapacity)data;
                        string newValue = Util.FormatWholeNumber(Mathf.Floor(userControlledCapacity.AmountStored));
                        float userMaxCapacity = userControlledCapacity.UserMaxCapacity;
                        string newValue2 = Util.FormatWholeNumber(userMaxCapacity);
                        str = str.Replace("{Stored}", newValue).Replace("{Capacity}", newValue2).Replace("{Units}", userControlledCapacity.CapacityUnits);
                        return str;
                    }
                };
            }
            GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, capacityStatusItem, this);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            base.smi.StartSM();
            Subscribe((int)GameHashes.CopySettings, new Action<object>(this.OnCopySettings));
            RefreshCreatureCount(null);
        }

        protected override void OnCleanUp()
        {
            base.smi.StopSM("OnCleanUp");
            base.OnCleanUp();
        }

        private void OnCopySettings(object data)
        {
            GameObject gameObject = (GameObject)data;
            if (gameObject != null)
            {
                ButcherStation component = gameObject.GetComponent<ButcherStation>();
                if (component != null)
                {
                    creatureLimit = component.creatureLimit;
                    ageButchThresold = component.ageButchThresold;
                }
            }
        }

        private void RefreshCreatureCount(object data = null)
        {
            int cell = this.GetSMI<RanchStation.Instance>().GetTargetRanchCell();
            CavityInfo cavityForCell = Game.Instance.roomProber.GetCavityForCell(cell);
            int num = storedCreatureCount;
            storedCreatureCount = 0;
            if (cavityForCell != null)
            {
                foreach (KPrefabID creature in cavityForCell.creatures)
                {
                    if (!creature.HasTag(GameTags.Creatures.Bagged) && !creature.HasTag(GameTags.Trapped))
                    {
                        storedCreatureCount++;
                    }
                }
            }
        }

        public bool IsCreatureEligibleToBeButched(GameObject creature_go)
        {
            if (!creature_go.HasTag(creatureEligibleTag))
            {
                return false;
            }
            bool flag = treeFilterable != null && treeFilterable.ContainsTag(creature_go.GetComponent<KPrefabID>().PrefabTag);
            if (autoButchSurplus && (!flag || storedCreatureCount > creatureLimit))
            {
                return true;
            }
            AmountInstance age = Db.Get().Amounts.Age.Lookup(creature_go);
            if (age != null)
            {
                return flag && ageButchThresold < age.value / age.GetMax();
            }
            return false;
        }

        public static void ButchCreature(GameObject creature_go, bool moveCreatureToButcherStation = false)
        {
            RanchStation.Instance targetRanchStation = creature_go.GetSMI<RanchableMonitor.Instance>()?.targetRanchStation;
            if (targetRanchStation != null)
            {
                if (moveCreatureToButcherStation)
                {
                    creature_go.transform.SetPosition(targetRanchStation.transform.GetPosition());
                }

                ExtraMeatSpawner extraMeatSpawner = creature_go.GetComponent<ExtraMeatSpawner>();
                if (extraMeatSpawner != null)
                {
                    RancherChore.RancherChoreStates.Instance smi = targetRanchStation.GetSMI<RancherChore.RancherChoreStates.Instance>();
                    GameObject rancher = smi.sm.rancher.Get(smi);
                    extraMeatSpawner.onDeathDropMultiplier = rancher.GetAttributes().Get(Db.Get().Attributes.Ranching.Id).GetTotalValue() * Config.Get().EXTRAMEATPERRANCHINGATTRIBUTE;
                }
            }
            creature_go.GetSMI<DeathMonitor.Instance>()?.Kill(Db.Get().Deaths.Generic);
        }

        float IUserControlledCapacity.UserMaxCapacity { get => (float)creatureLimit; set => creatureLimit = Mathf.RoundToInt(value);  }
        float IUserControlledCapacity.AmountStored => (float)storedCreatureCount;
        float IUserControlledCapacity.MinCapacity => 0;
        float IUserControlledCapacity.MaxCapacity => Config.Get().MAXCREATURELIMIT;
        bool IUserControlledCapacity.WholeValues => true;
        LocString IUserControlledCapacity.CapacityUnits => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.UNITS_SUFFIX;


        //string ISliderControl.SliderTitleKey => STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TITLE.key.String;
        string ISliderControl.SliderTitleKey => "STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TITLE";
        string ISliderControl.SliderUnits => UI.UNITSUFFIXES.PERCENT;

        float ISliderControl.GetSliderMax(int index)
        {
            return 100f;
        }

        float ISliderControl.GetSliderMin(int index)
        {
            return 0f;
        }

        string ISliderControl.GetSliderTooltip()
        {
            string s = string.Empty;
            foreach (float max_age in new float[] {
                TUNING.CREATURES.LIFESPAN.TIER1,
                TUNING.CREATURES.LIFESPAN.TIER2,
                TUNING.CREATURES.LIFESPAN.TIER3,
                TUNING.CREATURES.LIFESPAN.TIER4,
                    })
            {
                s = s + "\n" + Math.Floor(ageButchThresold * max_age) + STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP_OUTOF + Math.Floor(max_age) + STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP_CYCLES;
            }
            return string.Format(STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP, ageButchThresold * 100f) + s; 
        }

        string ISliderControl.GetSliderTooltipKey(int index)
        {
            //return STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP.key.String; 
            return "STRINGS.UI.UISIDESCREENS.BUTCHERSTATIONSIDESCREEN.TOOLTIP";
        }

        float ISliderControl.GetSliderValue(int index)
        {
            return ageButchThresold * 100f;
        }

        void ISliderControl.SetSliderValue(float percent, int index)
        {
            ageButchThresold = percent / 100f;
        }

        int ISliderControl.SliderDecimalPlaces(int index)
        {
            return 0;
        }


        string ICheckboxControl.CheckboxTitleKey => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.TITLE.key.String;
        string ICheckboxControl.CheckboxLabel => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.AUTOWRANGLE;
        string ICheckboxControl.CheckboxTooltip => UI.UISIDESCREENS.CAPTURE_POINT_SIDE_SCREEN.AUTOWRANGLE_TOOLTIP;

        bool ICheckboxControl.GetCheckboxValue()
        {
            return autoButchSurplus;
        }

        void ICheckboxControl.SetCheckboxValue(bool value)
        {
            autoButchSurplus = value;
        }

    }
}
