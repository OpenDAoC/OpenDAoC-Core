using DOL.AI.Brain;

namespace DOL.GS.Effects
{
    public class AtlasOF_JuggernautECSEffect : ECSGameAbilityEffect
    {
        public AtlasOF_JuggernautECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Juggernaut;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon => 4261;

        public override string Name => "Juggernaut";

        public override bool HasPositiveEffect => true;

        public override void OnStartEffect()
        {
            SpellLine RAspellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
            Spell Juggernaut = SkillBase.GetSpellByID(90801);


            if (Juggernaut != null)
            {
                Owner.CastSpell(Juggernaut, RAspellLine);
            }

            base.OnStartEffect();
        }

        public override void OnStopEffect()
        {
            if (Owner.ControlledBrain != null)
            {
                if (Owner.ControlledBrain is JuggernautBrain)
                {
                    Owner.ControlledBrain.Body.Die(null);
                }
            }

            base.OnStopEffect();
        }
    }
}