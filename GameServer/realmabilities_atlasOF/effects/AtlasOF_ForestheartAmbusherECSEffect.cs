using DOL.AI.Brain;

namespace DOL.GS.Effects
{
    public class AtlasOF_ForestheartAmbusherECSEffect : ECSGameAbilityEffect
    {
        public AtlasOF_ForestheartAmbusherECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.ForestheartAmbusher;
            EffectService.RequestStartEffect(this);
        }
        public override ushort Icon => 11129; // correct icon should be 4268 but it won't work

        public override string Name => "Forestheart Ambusher";

        public override bool HasPositiveEffect => true;

        public override void OnStartEffect()
        {
            SpellLine RAspellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
            Spell ForestheartAmbusher = SkillBase.GetSpellByID(90802);
            
            if (ForestheartAmbusher != null)
            {
                Owner.CastSpell(ForestheartAmbusher, RAspellLine);
            }
            base.OnStartEffect();
        }

        public override void OnStopEffect()
        {
            foreach (var pet in WorldMgr.GetNPCsByType(typeof(TheurgistPet), Owner.Realm))
            {
                var ambusher = pet as TheurgistPet;
                if (ambusher?.Owner != Owner) continue;
                if (ambusher?.Brain is not ForestheartAmbusherBrain) continue;
                ambusher.TakeDamage(null, eDamageType.Natural, 9999, 0);
            }

            base.OnStopEffect();
        }
        
        public void Cancel(bool playerCancel)
        {
            EffectService.RequestImmediateCancelEffect(this, playerCancel);
            OnStopEffect();
        }
        
    }
}