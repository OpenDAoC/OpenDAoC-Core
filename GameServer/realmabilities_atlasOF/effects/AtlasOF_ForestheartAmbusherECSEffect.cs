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
        public override ushort Icon => 4268;

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
            if (Owner.ControlledBrain is ForestheartAmbusherBrain)
            {
                Owner.ControlledBrain.Body.TakeDamage(null, eDamageType.Natural, 9999,0);
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