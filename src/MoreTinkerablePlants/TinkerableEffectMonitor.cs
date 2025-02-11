﻿using Klei.AI;

namespace MoreTinkerablePlants
{
    public class TinkerableEffectMonitor : KMonoBehaviour
    {
        public const string FARMTINKEREFFECTID = "FarmTinker";

        [MyCmpReq]
        protected Effects effects;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.EffectAdded, OnEffectChanged);
            Subscribe((int)GameHashes.EffectRemoved, OnEffectChanged);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.EffectAdded, OnEffectChanged);
            Unsubscribe((int)GameHashes.EffectRemoved, OnEffectChanged);
            base.OnCleanUp();
        }

        private void OnEffectChanged(object data)
        {
            ApplyModifier();
        }

        public virtual void ApplyModifier()
        {
        }
    }
}
