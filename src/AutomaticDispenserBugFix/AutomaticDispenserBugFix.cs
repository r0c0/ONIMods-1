﻿using Harmony;

namespace AutomaticDispenserBugFix
{
    [HarmonyPatch(typeof(ObjectDispenser), "Toggle")]
    public class AutomaticDispenser_Toggle
    {
        public static void Postfix(ObjectDispenser __instance, ObjectDispenser.Instance ___smi, bool ___switchedOn)
        {
            ___smi.SetSwitchState(___switchedOn);
        }
    }
}
