using System;
using Core.AI.Brain;
using Core.GS.Keeps;
using Core.Database;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells
{
	/// <summary>
	/// Effect that stays on target and does additional
	/// damage after each melee attack
	/// </summary>
	[SpellHandler("DamageAdd")]
	public class DamageAddSpell : ADamageAddSpell
	{
		public override void CreateECSEffect(EcsGameEffectInitParams initParams)
		{
			new DamageAddEcsSpellEffect(initParams);
		}

		/// <summary>
		/// The event type to hook on
		/// </summary>
		protected override CoreEvent EventType { get { return GameLivingEvent.AttackFinished; } }

		public virtual double DPSCap(int Level)
		{
			return (1.2 + 0.3 * Level) * 0.7;
		}
		
		/// <summary>
		/// Handler fired on every melee attack by effect target
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		public void EventHandler(CoreEvent e, object sender, EventArgs arguments, double effectiveness)
		{
			AttackFinishedEventArgs atkArgs = arguments as AttackFinishedEventArgs;
			if (atkArgs == null) return;

			if (atkArgs.AttackData.AttackResult != EAttackResult.HitUnstyled
				&& atkArgs.AttackData.AttackResult != EAttackResult.HitStyle) return;

			GameLiving target = atkArgs.AttackData.Target;
			if (target == null) return;

			if (target.ObjectState != GameObject.eObjectState.Active) return;
			if (target.IsAlive == false) return;
			if (target is GameKeepComponent || target is GameKeepDoor) return;

			GameLiving attacker = sender as GameLiving;
			if (attacker == null) return;

			if (attacker.ObjectState != GameObject.eObjectState.Active) return;
			if (attacker.IsAlive == false) return;

			double minVariance;
			double maxVariance;
			CalculateDamageVariance(target, out minVariance, out maxVariance);
			//spread += Util.Random(50);
			double dpsCap = DPSCap(attacker.Level);
			double dps = IgnoreDamageCap ? Spell.Damage : Math.Min(Spell.Damage, dpsCap);
			double damage = Util.Random((int)(minVariance * dps * atkArgs.AttackData.WeaponSpeed * 0.1), (int)(maxVariance * dps * atkArgs.AttackData.WeaponSpeed * 0.1)); ; // attack speed is 10 times higher (2.5spd=25)
			double damageResisted = damage * target.GetResist(Spell.DamageType) * -0.01;

			//Console.WriteLine("dps: {0}, damage: {1}, damageResisted: {2}, minDamageSpread: {3}", dps, damage, damageResisted, m_minDamageSpread);

			if (Spell.Damage < 0)
			{
				damage = atkArgs.AttackData.Damage * Spell.Damage / -100.0;
				damageResisted = damage * target.GetResist(Spell.DamageType) * -0.01;
			}

			AttackData ad = new AttackData();
			ad.Attacker = attacker;
			ad.Target = target;
			ad.Damage = (int)((damage + damageResisted) * effectiveness);
			ad.Modifier = (int)damageResisted;
			ad.DamageType = Spell.DamageType;
			ad.AttackType = EAttackType.Spell;
			ad.SpellHandler = this;
			ad.AttackResult = EAttackResult.HitUnstyled;

			if ( ad.Attacker is GameNpc )
			{
				IControlledBrain brain = ((GameNpc)ad.Attacker).Brain as IControlledBrain;
				if (brain != null)
				{
					GamePlayer owner = brain.GetPlayerOwner();
					if (owner != null)
					{
                        MessageToLiving(owner, String.Format(LanguageMgr.GetTranslation( owner.Client, "DamageAddAndShield.EventHandlerDA.YourHitFor" ), ad.Attacker.Name, target.GetName(0, false), ad.Damage ), EChatType.CT_Spell);
                    }
				}
			}
			else
			{
				GameClient attackerClient = null;
				if ( attacker is GamePlayer ) attackerClient = ( (GamePlayer)attacker ).Client;

				if ( attackerClient != null )
				{
					MessageToLiving( attacker, String.Format( LanguageMgr.GetTranslation( attackerClient, "DamageAddAndShield.EventHandlerDA.YouHitExtra" ), target.GetName( 0, false ), ad.Damage ), EChatType.CT_Spell );
				}
            }

			GameClient targetClient = null;
			if ( target is GamePlayer ) targetClient = ( (GamePlayer)target ).Client;

			if ( targetClient != null )
			{
				MessageToLiving( target, String.Format( LanguageMgr.GetTranslation( targetClient, "DamageAddAndShield.EventHandlerDA.DamageToYou" ), attacker.GetName( 0, false ), ad.Damage ), EChatType.CT_Spell );
			}

            target.OnAttackedByEnemy(ad);
			attacker.DealDamage(ad);

			foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null) continue;
				player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x0A, target.HealthPercent);
			}
		}

        public override void EventHandler(CoreEvent e, object sender, EventArgs arguments)
        {
            throw new NotImplementedException();
        }

        // constructor
        public DamageAddSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
	}
}
