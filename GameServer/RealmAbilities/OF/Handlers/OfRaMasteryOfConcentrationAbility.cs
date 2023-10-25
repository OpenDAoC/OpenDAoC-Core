using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.RealmAbilities;

public class OfRaMasteryOfConcentrationAbility : TimedRealmAbility
{
	public OfRaMasteryOfConcentrationAbility(DbAbility dba, int level) : base(dba, level) { }
	public const Int32 Duration = 15000; // 15s in ms

	public override int MaxLevel { get { return 1; } }
	public override int CostForUpgrade(int level) { return 14; }
	public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugAcuityLevel(player) >= 3; }
	public override int GetReUseDelay(int level) { return 1800; } // 30 mins
	public virtual int GetAmountForLevel(int level) { return 100; } // OF MoC = always 100% effectiveness.

	public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
		GamePlayer caster = living as GamePlayer;

		if (caster == null)
			return;

		EffectListService.TryCancelFirstEffectOfTypeOnTarget(caster, EEffect.MasteryOfConcentration);

		SendCasterSpellEffectAndCastMessage(living, 7007, true);
		foreach (GamePlayer player in caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			if (caster.IsWithinRadius(player, WorldMgr.INFO_DISTANCE))
			{
				if (player == caster)
				{
					player.MessageToSelf("You cast " + this.Name + "!", EChatType.CT_Spell);
					player.MessageToSelf("You become steadier in your casting abilities!", EChatType.CT_Spell);
				}
				else
				{
					player.MessageFromArea(caster, caster.Name + " casts a spell!", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
					player.Out.SendMessage(caster.Name + "'s castings have perfect poise!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
			}
		}

		DisableSkill(living);

		new OfRaMasteryOfConcentrationEcsEffect(new EcsGameEffectInitParams(caster, Duration, 1));
	}
}