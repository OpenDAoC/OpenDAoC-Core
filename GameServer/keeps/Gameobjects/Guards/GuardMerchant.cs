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
 */
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;
using DOL.GS.ServerProperties;
using DOL.Language;


namespace DOL.GS.Keeps
{
	public class GuardMerchant : GameGuardMerchant
	{
		public const int INTERVAL = 360 * 1000;

		protected virtual int Timer(ECSGameTimer callingTimer)
		{
			return INTERVAL;
		}
		public override bool AddToWorld()
		{
			switch (Realm)
			{
				case eRealm.Albion:
					TradeItems = new MerchantTradeItems("AlbRvRCraftingList");
					break;
				case eRealm.Midgard:
					TradeItems = new MerchantTradeItems("MidRvRCraftingList");
					break;
				case eRealm.Hibernia:
					TradeItems = new MerchantTradeItems("HibRvRCraftingList");
					break;
			}

			GuildName = "Merchant";
			bool success = base.AddToWorld();
			if (success) new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Timer), INTERVAL);
			return success;
		}

		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			return base.GetArmorAbsorb(slot) - 0.05;
		}

		protected override KeepGuardBrain GetBrain() => new KeepGuardBrain();
		
		protected override ICharacterClass GetClass()
		{
			if (ModelRealm == eRealm.Albion) return new ClassArmsman();
			else if (ModelRealm == eRealm.Midgard) return new ClassWarrior();
			else if (ModelRealm == eRealm.Hibernia) return new ClassHero();
			return new DefaultCharacterClass();
		}
		protected override void SetName()
		{
			switch (ModelRealm)
			{
				case eRealm.None:
				case eRealm.Albion:
					if (IsPortalKeepGuard)
					{
						if (Gender == eGender.Female)
							Name = "Frida";
						else Name = "Frederic";
					}
					else
					{
						if (Gender == eGender.Female)
							Name = "Fabienne";
						else Name = "Francis";
					}
					GuildName = "Merchant";
					break;
				case eRealm.Midgard:
					if (IsPortalKeepGuard)
					{
						if (Gender == eGender.Female)
							Name = "Olga";
						else Name = "Odun";
					}
					else
					{
						if (Gender == eGender.Female)
							Name = "Rikke";
						else Name = "Rollo";
					}
					break;
				case eRealm.Hibernia:
					if (IsPortalKeepGuard)
					{
						if (Gender == eGender.Female)
							Name = "Alenja";
						else Name = "Airell";
					}
					else
					{
						if (Gender == eGender.Female)
							Name = "Arwen";
						else Name = "Aidan";
					}
					break;
			}

			if (Realm == eRealm.None)
			{
				if (Gender == eGender.Female)
					Name = "Finnja";
				else Name = "Fynn";
			}
		}
	}
}
