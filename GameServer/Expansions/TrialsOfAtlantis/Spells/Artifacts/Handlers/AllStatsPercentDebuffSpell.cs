using Core.GS.AI;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("AllStatsPercentDebuff")]
public class AllStatsPercentDebuffSpell : SpellHandler
{
    protected int StrDebuff = 0;
    protected int DexDebuff = 0;
    protected int ConDebuff = 0;
    protected int EmpDebuff = 0;
    protected int QuiDebuff = 0;
    protected int IntDebuff = 0;
    protected int ChaDebuff = 0;
    protected int PieDebuff = 0;

	public override int CalculateSpellResistChance(GameLiving target)
	{
		return 0;
	}
	public override void OnEffectStart(GameSpellEffect effect)
	{
		base.OnEffectStart(effect); 
		//effect.Owner.DebuffCategory[(int)eProperty.Dexterity] += (int)m_spell.Value;
        double percentValue = (m_spell.Value) / 100;
        StrDebuff = (int)((double)effect.Owner.GetModified(EProperty.Strength) * percentValue);
        DexDebuff = (int)((double)effect.Owner.GetModified(EProperty.Dexterity) * percentValue);
        ConDebuff = (int)((double)effect.Owner.GetModified(EProperty.Constitution) * percentValue);
        EmpDebuff = (int)((double)effect.Owner.GetModified(EProperty.Empathy) * percentValue);
        QuiDebuff = (int)((double)effect.Owner.GetModified(EProperty.Quickness) * percentValue);
        IntDebuff = (int)((double)effect.Owner.GetModified(EProperty.Intelligence) * percentValue);
        ChaDebuff = (int)((double)effect.Owner.GetModified(EProperty.Charisma) * percentValue);
        PieDebuff = (int)((double)effect.Owner.GetModified(EProperty.Piety) * percentValue);
        

        effect.Owner.DebuffCategory[(int)EProperty.Dexterity] += DexDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Strength] += StrDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Constitution] += ConDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Piety] += PieDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Empathy] += EmpDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Quickness] += QuiDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Intelligence] += IntDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Charisma] += ChaDebuff;

		if (effect.Owner is GamePlayer)
		{
			GamePlayer player = effect.Owner as GamePlayer;
			player.Out.SendCharStatsUpdate();
			player.UpdateEncumberance();
			player.UpdatePlayerStatus();
			player.Out.SendUpdatePlayer();
		}
	}
	public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
	{
        double percentValue = (m_spell.Value) / 100;

        effect.Owner.DebuffCategory[(int)EProperty.Dexterity] -= DexDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Strength] -= StrDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Constitution] -= ConDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Piety] -= PieDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Empathy] -= EmpDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Quickness] -= QuiDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Intelligence] -= IntDebuff;
        effect.Owner.DebuffCategory[(int)EProperty.Charisma] -= ChaDebuff;

		if (effect.Owner is GamePlayer)
		{
			GamePlayer player = effect.Owner as GamePlayer;
			player.Out.SendCharStatsUpdate();
			player.UpdateEncumberance();
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
			target.LastAttackedByEnemyTickPvE = GameLoopMgr.GameLoopTime;
			Caster.LastAttackTickPvE = GameLoopMgr.GameLoopTime;
		}
		else
		{
			target.LastAttackedByEnemyTickPvP = GameLoopMgr.GameLoopTime;
			Caster.LastAttackTickPvP = GameLoopMgr.GameLoopTime;
		}
		if (target is GameNpc)
		{
			IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
			if (aggroBrain != null)
				aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
		}
	}
    public AllStatsPercentDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}