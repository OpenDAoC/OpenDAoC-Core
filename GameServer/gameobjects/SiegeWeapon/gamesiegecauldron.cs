using DOL.Database;
using DOL.GS.Keeps;

namespace DOL.GS
{
	/// <summary>
	/// GameMovingObject is a base class for boats and siege weapons.
	/// </summary>
	public class GameSiegeCauldron : GameSiegeWeapon
	{
		public GameKeepComponent Component = null;

		public GameSiegeCauldron()
			: base()
		{
			MeleeDamageType = eDamageType.Heat;
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
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Boiling Oil";
					spell.Radius = 350;
					spell.Range = WorldMgr.VISIBILITY_DISTANCE;
					spell.SpellID = 50005;
					spell.Target = "Area";
					spell.Type = eSpellType.SiegeDirectDamage.ToString();
					m_OilSpell = new Spell(spell, 50);
				}
				return m_OilSpell;
			}
		}
	}
}
