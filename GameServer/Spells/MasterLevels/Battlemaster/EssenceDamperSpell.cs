using Core.AI.Brain;
using Core.GS.Effects;

namespace Core.GS.Spells
{
    //for ML9 in the database u have to add  EssenceDampenHandler  in type (its a new method customly made) 
    #region Battlemaster-9
    [SpellHandler("EssenceDampenHandler")]
    public class EssenceDampenSpell : SpellHandler
    {
        protected int DexDebuff = 0;
        protected int QuiDebuff = 0;
        public override int CalculateSpellResistChance(GameLiving target) { return 0; }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            double percentValue = (m_spell.Value) / 100;//15 / 100 = 0.15 a.k (15%) 100dex * 0.15 = 15dex debuff 
            DexDebuff = (int)((double)effect.Owner.GetModified(EProperty.Dexterity) * percentValue);
            QuiDebuff = (int)((double)effect.Owner.GetModified(EProperty.Quickness) * percentValue);
            GameLiving living = effect.Owner as GameLiving;
            living.DebuffCategory[(int)EProperty.Dexterity] += DexDebuff;
            living.DebuffCategory[(int)EProperty.Quickness] += QuiDebuff;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner as GameLiving;
            living.DebuffCategory[(int)EProperty.Dexterity] -= DexDebuff;
            living.DebuffCategory[(int)EProperty.Quickness] -= QuiDebuff;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        public override void ApplyEffectOnTarget(GameLiving target)
        {
            base.ApplyEffectOnTarget(target);
            if (target.Realm == 0 || Caster.Realm == 0)
            {
                target.LastAttackedByEnemyTickPvE = target.CurrentRegion.Time;
                Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = target.CurrentRegion.Time;
                Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
            }
            if (target is GameNpc)
            {
                IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
            }
        }
        public EssenceDampenSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //ml10 in database Type shood be RandomBuffShear
}