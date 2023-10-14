using System.Collections;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler("Confusion")]
	public class ConfusionSpell : SpellHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ConfusionSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{}

		public ArrayList targetList = new ArrayList();

		public override void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			new ConfusionEcsSpellEffect(initParams);
		}

		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			if (target.HasAbility(Abilities.ConfusionImmunity))
			{
				MessageToCaster(target.Name + " can't be confused!", EChatType.CT_SpellResisted);
				SendEffectAnimation(target, 0, false, 0);
				return;
			}
			base.ApplyEffectOnTarget(target);
			target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			//if (effect.Owner == null) return;
			//if (!effect.Owner.IsAlive || effect.Owner.ObjectState != GameLiving.eObjectState.Active) return;

			////SendEffectAnimation(effect.Owner, 0, false, 1); //send the effect

			//if (effect.Owner is GamePlayer)
			//{
			//	/*
			//	 *Q: What does the confusion spell do against players?
			//	 *A: According to the magic man, �Confusion against a player interrupts their current action, whether it's a bow shot or spellcast.
			//	 */
			//	if (Spell.Value < 0 || Util.Chance(Convert.ToInt32(Math.Abs(Spell.Value))))
			//	{
			//		//Spell value below 0 means it's 100% chance to confuse.
			//		GamePlayer player = effect.Owner as GamePlayer;

			//		player.StartInterruptTimer(player.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
			//	}

			//	effect.Cancel(false);
			//}
			//else if (effect.Owner is GameNPC)
			//{
			//	//check if we should do anything at all.

			//	bool doConfuse = (Spell.Value < 0 || Util.Chance(Convert.ToInt32(Spell.Value)));

			//	if (!doConfuse)
			//		return;

			//	bool doAttackFriend = Spell.Value < 0 && Util.Chance(Convert.ToInt32(Math.Abs(Spell.Value)));
				
			//	GameNPC npc = effect.Owner as GameNPC;

			//	npc.IsConfused = true;

			//	if (log.IsDebugEnabled)
			//		log.Debug("CONFUSION: " + npc.Name + " was confused(true," + doAttackFriend.ToString() +")");

   //             if (npc is GameSummonedPet && npc.Brain != null && (npc.Brain as IControlledBrain) != null)
			//	{
			//		//it's a pet.
			//		GamePlayer playerowner = (npc.Brain as IControlledBrain).GetPlayerOwner();
			//		if (playerowner != null && playerowner.CharacterClass.ID == (int)eCharacterClass.Theurgist)
			//		{
			//			//Theurgist pets die.
			//			npc.Die(Caster);
			//			effect.Cancel(false);
			//			return;
			//		}
			//	}

			//	targetList.Clear();
			//	foreach (GamePlayer target in npc.GetPlayersInRadius(1000))
			//	{
			//		if (doAttackFriend)
			//			targetList.Add(target);
			//		else
			//		{
			//			//this should prevent mobs from attacking friends.
			//			if (GameServer.ServerRules.IsAllowedToAttack(npc, target, true))
			//				targetList.Add(target);
			//		}
			//	}

			//	foreach (GameNPC target in npc.GetNPCsInRadius(1000))
			//	{
			//		//don't agro yourself.
			//		if (target == npc)
			//			continue;

			//		if (doAttackFriend)
			//			targetList.Add(target);
			//		else
			//		{
			//			//this should prevent mobs from attacking friends.
			//			if (GameServer.ServerRules.IsAllowedToAttack(npc, target, true) && !GameServer.ServerRules.IsSameRealm(npc,target,true))
			//				targetList.Add(target);
			//		}
			//	}

			//	//targetlist should be full, start effect pulse.
			//	if (targetList.Count > 0)
			//	{
			//		npc.StopAttack();
			//		npc.StopCurrentSpellcast();

			//		GameLiving target = targetList[Util.Random(targetList.Count - 1)] as GameLiving;
			//		npc.StartAttack(target);
			//	}
			//}
		}

		public override void OnEffectPulse(GameSpellEffect effect)
		{
			//base.OnEffectPulse(effect);

			//if (targetList.Count > 0)
			//{
			//	GameNPC npc = effect.Owner as GameNPC;
			//	npc.StopAttack();
			//	npc.StopCurrentSpellcast();

			//	GameLiving target = targetList[Util.Random(targetList.Count - 1)] as GameLiving;

			//	npc.StartAttack(target);
			//}
		}

		protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
		{
			//every 5 seconds?
			return new GameSpellEffect(this, m_spell.Duration, 5000, 1);
		}

		public override bool HasPositiveEffect
		{
			get
			{
				return false;
			}
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			//if(effect != null && effect.Owner != null && effect.Owner is GameNPC)
			//{
			//	GameNPC npc = effect.Owner as GameNPC;
			//	npc.IsConfused = false;
			//}
			return base.OnEffectExpires(effect, noMessages);
		}
	}
}
