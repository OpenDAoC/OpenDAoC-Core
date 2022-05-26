/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using System.Collections;
/*1,Ballista,1,ammo,0.46,1
2,Catapult,2,ammo,0.39,1
3,Trebuchet,2,ammo,1.03,1
4,Scorpion,1,ammo,0.22,1
5,Oil,3,ammo,0,1*/
/*ID,Textual Name,Load start,Load End,Fire Start,Fire End,Release Point
1,Ballista,30,60,0,30,12
2,Ram,30,60,0,30,12
3,Mangonel,30,200,0,30,12
4,trebuchet,97,180,0,96,
5,Ballista Low,11,30,0,10,
6,Ballista High,20,90,0,19,
7,Catapult W,40,111,0,40,
8,Catapult G,40,111,0,40,
9,Ram High,0,12,13,80,
10,Ram Mid,0,12,13,80,
11,Ram Low,0,12,13,80,*/
namespace DOL.GS
{
	/// <summary>
	/// GameMovingObject is a base class for boats and siege weapons.
	/// </summary>
	public class GameSiegeBallista : GameSiegeWeapon
	{
		public GameSiegeBallista()
			: base()
		{
			MeleeDamageType = eDamageType.Thrust;
			Name = "field ballista";
			AmmoType = 0x18;
			this.Model = 0x0A55;
			this.Effect = 0x089A;
			ActionDelay = new int[]{
				0,//none
				5000,//aiming
				10000,//arming
				0,//loading
				0//fireing base delay
			};
			BaseDamage = 75;
			MinAttackRange = 50;
			MaxAttackRange = 4000;
			SIEGE_WEAPON_CONTROLE_DISTANCE=75; //since LOS is based on Owner, making distance smaller
			//en ms
			/*SpellLine siegeWeaponSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.SiegeWeapon_Spells);
			IList spells = SkillBase.GetSpellList(siegeWeaponSpellLine.KeyName);
			if (spells != null)
			{
				foreach (Spell spell in spells)
				{
					if (spell.ID == 2430) //TODO good id for balista
					{
						if(spell.Level <= Level)
						{
							m_spellHandler = ScriptMgr.CreateSpellHandler(this, spell, siegeWeaponSpellLine);
						}
						break;
					}
				}
			}*/
		}

		public override void Fire()
		{
			if (TargetObject == null) return;
			// if (TargetObject is GamePlayer) //Try check of LOS from target if target is a player
			// {
			// 	GamePlayer player = TargetObject as GamePlayer;
			// 	player.Out.SendCheckLOS(this, player, new CheckLOSResponse(FireCheckLOS));
			// }
			else if (Owner is GamePlayer)
			{
				//Owner.Out.SendCheckLOS(this, TargetObject, new CheckLOSResponse(FireCheckLOS));
				Owner.Out.SendCheckLOS(Owner, TargetObject, new CheckLOSResponse(FireCheckLOS)); //Have to use owner as "source" for now as checking with ballista the client always returns LOS of true
			}
		}
		/// <summary>
		/// FireCheckLOS is called after Fire method. Will check for LOS between siege and target
		/// </summary>
		private void FireCheckLOS(GamePlayer player, ushort response, ushort targetOID)
		{
			if ((response & 0x100) == 0x100)
			{
				base.Fire();
			} 
			else
			{
				if (Owner!=null)
					Owner.Out.SendMessage("Target is not in view!", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
			}
		}

		public override void DoDamage()
		{
			base.DoDamage();//anim mut be called after damage
			GameLiving target = (TargetObject as GameLiving);
			if (target == null) return;

			int damageAmount = CalcDamageToTarget(target) + Util.Random(50);

			AttackData ad = new AttackData();
			ad.Target = target;
			ad.AttackType = AttackData.eAttackType.Ranged;
			ad.AttackResult = eAttackResult.HitUnstyled;
			ad.Damage = damageAmount;
			ad.DamageType = MeleeDamageType;

			if(Owner != null)
			{
				ad.Attacker = Owner;
				target.TakeDamage(Owner, eDamageType.Crush, damageAmount, 0);
				target.OnAttackedByEnemy(ad);

				Owner.OnAttackEnemy(ad);
				Owner.Out.SendMessage("The " + this.Name + " hits " + target.Name + " for " + damageAmount + " damage!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
			}
			else
			{
				ad.Attacker = this;
				target.TakeDamage(this, eDamageType.Crush, damageAmount, 0);
				target.OnAttackedByEnemy(ad);
			}


			foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendCombatAnimation(this, target, 0x0000, 0x0000, 0x00, 0x00, 0x14, target.HealthPercent);


		}

		/// <summary>
		/// Calculates the damage based on the target type (door, siege, player)
		/// <summary>
		public override int CalcDamageToTarget(GameLiving target)
		{
			//Ballistas are good against siege and players/npc
			if(target is GameSiegeWeapon || target is GamePlayer || target is GameNPC)
				return BaseDamage * 3;
			else
				return BaseDamage;
		}

		public override bool ReceiveItem(GameLiving source, DOL.Database.InventoryItem item)
		{
			//todo check if bullet
			return base.ReceiveItem(source, item);
		}

	}
}
