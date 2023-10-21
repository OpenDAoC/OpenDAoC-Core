namespace Core.GS
{
    public class RezSicknessEcsSpellEffect : EcsGameSpellEffect
    {
        public RezSicknessEcsSpellEffect(EcsGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            GamePlayer gPlayer = Owner as GamePlayer;
            if (gPlayer != null)
            {
                gPlayer.Effectiveness -= SpellHandler.Spell.Value * 0.01;
                gPlayer.Out.SendUpdateWeaponAndArmorStats();
                gPlayer.Out.SendStatusUpdate();
            }
        }

        public override void OnStopEffect()
        {
            GamePlayer gPlayer = Owner as GamePlayer;
            if (gPlayer != null)
            {
                gPlayer.Effectiveness += SpellHandler.Spell.Value * 0.01;
                gPlayer.Out.SendUpdateWeaponAndArmorStats();
                gPlayer.Out.SendStatusUpdate();
            }
        }
    }
}
