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
using DOL.GS.Keeps;

namespace DOL.GS
{
	/// <summary>
	/// GameMovingObject is a base class for boats and siege weapons.
	/// </summary>
	public class GameSiegeTrebuchet : GameSiegeCatapult
	{

		public GameSiegeTrebuchet()
			: base()
		{
			MeleeDamageType = eDamageType.Crush;
			Name = "trebuchet";
			AmmoType = 0x3A;
			EnableToMove = false;
			this.Model = 0xA2E;
			this.Effect = 0x89C;
			ActionDelay = new int[]
			{
				0,//none
				5000,//aiming
				15000,//arming
				0,//loading
				1000//fireing base delay
			};//en ms
			BaseDamage = 100;
			MinAttackRange = 2000;
			MaxAttackRange = 5000;
			AttackRadius = 150;
		}

		/// <summary>
		/// Calculates the damage based on the target type (door, siege, player)
		/// <summary>
		public override int CalcDamageToTarget(GameLiving target)
		{
			//Trebs are better against doors but lower damage against players/npcs/other objects
			if(target is GameKeepDoor || target is GameRelicDoor)
				return BaseDamage * 3;
			else
				return BaseDamage;
		}
	}
}
