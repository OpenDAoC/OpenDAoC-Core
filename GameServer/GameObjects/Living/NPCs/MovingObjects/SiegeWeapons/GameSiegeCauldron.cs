using Core.Database;
using Core.Database.Tables;
using Core.GS.Keeps;
using Core.GS.PacketHandler;

namespace Core.GS
{
	public class GameSiegeCauldron : GameSiegeWeapon
	{
		public GameKeepComponent Component = null;

		public GameSiegeCauldron()
			: base()
		{
			MeleeDamageType = EDamageType.Heat;
			Name = "cauldron of boiling oil";
			AmmoType = 0x3B;
			EnableToMove = false;
			Effect = 0x8A1;
			Model = 0xA2F;
			CurrentState = eState.Aimed;
			SetGroundTarget(X, Y, Z - 100);
			ActionDelay = new int[]
                {
                    0, //none
                    0, //aiming
                    15000, //arming
                    0, //loading
                    1000 //fireing
                }; //en ms
		}

		public override bool AddToWorld()
		{
			SetGroundTarget(X, Y, Component.Keep.Z);
			return base.AddToWorld();
		}

		public override void DoDamage()
		{
			//todo remove ammo + spell in db and uncomment
			//m_spellHandler.StartSpell(player);
			base.DoDamage(); //anim mut be called after damage
			CastSpell(OilSpell, SiegeSpellLine);
		}

		private static Spell m_OilSpell;

		public static Spell OilSpell
		{
			get
			{
				if (m_OilSpell == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.ClientEffect = 2209; //2209? 5909? 7086? 7091?
					spell.Damage = 1000;
					spell.DamageType = (int)EDamageType.Heat;
					spell.Name = "Boiling Oil";
					spell.Radius = 350;
					spell.Range = WorldMgr.VISIBILITY_DISTANCE;
					spell.SpellID = 50005;
					spell.Target = "Area";
					spell.Type = ESpellType.SiegeDirectDamage.ToString();
					m_OilSpell = new Spell(spell, 50);
				}
				return m_OilSpell;
			}
		}
	}
}

namespace Core.GS.Spells
{
    /// <summary>
    /// 
    /// </summary>
    [SpellHandler("SiegeDirectDamage")]
	public class SiegeDirectDamageSpell : DirectDamageSpell
	{

		/// <summary>
		/// Calculates chance of spell getting resisted
		/// </summary>
		/// <param name="target">the target of the spell</param>
		/// <returns>chance that spell will be resisted for specific target</returns>
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		public override int CalculateToHitChance(GameLiving target)
		{
			return 100;
		}

		public override bool CasterIsAttacked(GameLiving attacker)
		{
			return false;
		}

		public override void CalculateDamageVariance(GameLiving target, out double min, out double max)
		{
			min = 1;
			max = 1;
		}

		public override AttackData CalculateDamageToTarget(GameLiving target)
		{
			AttackData ad = base.CalculateDamageToTarget(target);
			if (target is GamePlayer)
			{
				GamePlayer player = target as GamePlayer;
				int id = player.PlayerClass.ID;
				//50% reduction for tanks
				if (id == (int)EPlayerClass.Armsman || id == (int)EPlayerClass.Warrior || id == (int)EPlayerClass.Hero)
					ad.Damage /= 2;
				//3000 spec
				//ram protection
				//lvl 0 50%
				//lvl 1 60%
				//lvl 2 70%
				//lvl 3 80%
				if (player.IsRiding && player.Steed is GameSiegeRam)
				{
					ad.Damage = (int)((double)ad.Damage * (1.0 - (50.0 + (double)player.Steed.Level * 10.0) / 100.0));
				}
			}
			return ad;
		}

		public override void SendDamageMessages(AttackData ad)
		{
			string modmessage = "";
			if (ad.Modifier > 0)
				modmessage = " (+" + ad.Modifier + ")";
			if (ad.Modifier < 0)
				modmessage = " (" + ad.Modifier + ")";

			if (Caster is GameSiegeWeapon)
			{
				GameSiegeWeapon siege = (Caster as GameSiegeWeapon);
				if (siege.Owner != null)
				{
					siege.Owner.Out.SendMessage(string.Format("You hit {0} for {1}{2} damage!", ad.Target.GetName(0, false), ad.Damage, modmessage), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
				}
			}

			if (Caster is GamePlayer p)
			{
				p.Out.SendMessage(string.Format("You hit {0} for {1}{2} damage!", ad.Target.GetName(0, false), ad.Damage, modmessage), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
			}
		}

		public override void DamageTarget(AttackData ad, bool showEffectAnimation, int attackResult)
		{
			if (Caster is GameSiegeWeapon)
			{
				GameSiegeWeapon siege = (Caster as GameSiegeWeapon);
				if (siege.Owner != null)
				{
					ad.Attacker = siege.Owner;
				}
			}
			base.DamageTarget(ad, showEffectAnimation, attackResult);
		}

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			return true;
		}

		public override bool CheckEndCast(GameLiving target)
		{
			return true;
		}


		// constructor
		public SiegeDirectDamageSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
