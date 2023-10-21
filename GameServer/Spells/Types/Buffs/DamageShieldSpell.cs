using System;
using Core.AI.Brain;
using Core.GS.PacketHandler;
using Core.Database;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Keeps;
using Core.GS.Languages;

namespace Core.GS.Spells
{
	/// <summary>
	/// Effect that stays on target and does addition
	/// damage on every attack against this target
	/// </summary>
	[SpellHandler("DamageShield")]
	public class DamageShieldSpell : ADamageAddSpell
	{
		public override void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			new DamageShieldEcsSpellEffect(initParams);
		}

		/// <summary>
		/// The event type to hook on
		/// </summary>
		protected override CoreEvent EventType { get { return GameLivingEvent.AttackedByEnemy; } }

		/// <summary>
		/// Handler fired whenever effect target is attacked
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		public override void EventHandler(CoreEvent e, object sender, EventArgs arguments)
		{
			AttackedByEnemyEventArgs args = arguments as AttackedByEnemyEventArgs;
			if (args == null) return;
			if (args.AttackData.AttackResult != EAttackResult.HitUnstyled
				&& args.AttackData.AttackResult != EAttackResult.HitStyle) return;
			if (!args.AttackData.IsMeleeAttack) return;
			GameLiving attacker = sender as GameLiving; //sender is target of attack, becomes attacker for damage shield
			if (attacker == null) return;
			if (attacker.ObjectState != GameObject.eObjectState.Active) return;
			if (attacker.IsAlive == false) return;
			GameLiving target = args.AttackData.Attacker; //attacker becomes target for damage shield
			if (target == null) return;
			if (target.ObjectState != GameObject.eObjectState.Active) return;
			if (target.IsAlive == false) return;

			int spread = m_minDamageSpread;
			spread += Util.Random(50);
			double damage = Spell.Damage * target.AttackSpeed(target.ActiveWeapon) * spread * 0.00001;
			double damageResisted = damage * target.GetResist(Spell.DamageType) * -0.01;

            if (!Spell.IsFocus)
            {
				var effectiveness = 1 + Caster.GetModified(EProperty.BuffEffectiveness) * 0.01;
				damage *= effectiveness;
			}
			

			if (Spell.Damage < 0)
			{
				damage = args.AttackData.Damage * Spell.Damage / -100.0;
				damageResisted = damage * target.GetResist(Spell.DamageType) * -0.01;
			}

			AttackData ad = new AttackData();
			ad.Attacker = attacker;
			ad.Target = target;
			ad.Damage = (int)(damage + damageResisted);
			ad.Modifier = (int)damageResisted;
			ad.DamageType = Spell.DamageType;
			ad.SpellHandler = this;
			ad.AttackType = EAttackType.Spell;
			ad.AttackResult = EAttackResult.HitUnstyled;

			GamePlayer owner = null;

			GameClient attackerClient = null;
			if ( attacker is GamePlayer ) attackerClient = ( (GamePlayer)attacker ).Client;

			if ( ad.Attacker is GameNpc )
			{
				IControlledBrain brain = ((GameNpc)ad.Attacker).Brain as IControlledBrain;
				if (brain != null)
				{
					owner = brain.GetPlayerOwner();
					if (owner != null && owner.ControlledBrain != null && ad.Attacker == owner.ControlledBrain.Body)
					{
                        MessageToLiving(owner, String.Format(LanguageMgr.GetTranslation( owner.Client, "DamageAddAndShield.EventHandlerDS.YourHitFor" ), ad.Attacker.Name, target.GetName(0, false), ad.Damage ), EChatType.CT_Spell);
                    }
				}
			}
			else if( attackerClient != null )
			{
                MessageToLiving(attacker, String.Format(LanguageMgr.GetTranslation( attackerClient, "DamageAddAndShield.EventHandlerDS.YouHitFor" ), target.GetName(0, false), ad.Damage ), EChatType.CT_Spell);
            }

			GameClient targetClient = null;
			if ( target is GamePlayer ) targetClient = ( (GamePlayer)target ).Client;

			//if ( targetClient != null )
			//	MessageToLiving(target, String.Format(LanguageMgr.GetTranslation( targetClient, "DamageAddAndShield.EventHandlerDS.DamageToYou" ), attacker.GetName(0, false), ad.Damage ), eChatType.CT_Spell);

            target.OnAttackedByEnemy(ad);
			attacker.DealDamage(ad);
			foreach (GamePlayer player in attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null)
					continue;
				player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x14, target.HealthPercent);
			}
			//			Log.Debug(String.Format("spell damage: {0}; damage: {1}; resisted damage: {2}; damage type {3}; minSpread {4}.", Spell.Damage, ad.Damage, ad.Modifier, ad.DamageType, m_minDamageSpread));
			//			Log.Debug(String.Format("dmg {0}; spread: {4}; resDmg: {1}; atkSpeed: {2}; resist: {3}.", damage, damageResisted, target.AttackSpeed(null), ad.Target.GetResist(Spell.DamageType), spread));
		}

		// constructor
		public DamageShieldSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
	}
}
