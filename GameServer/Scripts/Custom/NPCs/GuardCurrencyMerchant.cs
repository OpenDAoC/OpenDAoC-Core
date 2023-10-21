using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PlayerClass;

namespace Core.GS.Keeps
{
	public class GuardCurrencyMerchant : GameServerGuardMerchant
	{
		public override bool AddToWorld()
		{
			TradeItems = new MerchantTradeItems("summonmerchant_merchant");
			GuildName = "Orb Merchant";
			return base.AddToWorld();
		}

		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			return base.GetArmorAbsorb(slot) - 0.05;
		}

		protected override KeepGuardBrain GetBrain() => new KeepGuardBrain();
		
		protected override IPlayerClass GetClass()
		{
			if (ModelRealm == ERealm.Albion) return new ClassArmsman();
			else if (ModelRealm == ERealm.Midgard) return new ClassWarrior();
			else if (ModelRealm == ERealm.Hibernia) return new ClassHero();
			return new DefaultPlayerClass();
		}
		protected override void SetName()
		{
			switch (ModelRealm)
			{
				case ERealm.None:
				case ERealm.Albion:
					if (IsPortalKeepGuard)
					{
						if (Gender == EGender.Female)
							Name = "Johanna";
						else Name = "Johann";
					}
					else
					{
						if (Gender == EGender.Female)
							Name = "Ulrike";
						else Name = "Ulrich";
					}
					GuildName = "Merchant";
					break;
				case ERealm.Midgard:
					if (IsPortalKeepGuard)
					{
						if (Gender == EGender.Female)
							Name = "Sarina";
						else Name = "Sander";
					}
					else
					{
						if (Gender == EGender.Female)
							Name = "Kaira";
						else Name = "Kaj";
					}
					break;
				case ERealm.Hibernia:
					if (IsPortalKeepGuard)
					{
						if (Gender == EGender.Female)
							Name = "Daireann";
						else Name = "Drystan";
					}
					else
					{
						if (Gender == EGender.Female)
							Name = "Moja";
						else Name = "Maeron";
					}
					break;
			}

			if (Realm == ERealm.None)
			{
				if (Gender == EGender.Female)
					Name = "Finnja";
				else Name = "Fynn";
			}
		}
	}
}
