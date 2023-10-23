using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS;

public class GuardMerchant : GameGuardMerchant
{
	public override bool AddToWorld()
	{
		switch (Realm)
		{
			case ERealm.Albion:
				TradeItems = new MerchantTradeItems("AlbRvRCraftingList");
				break;
			case ERealm.Midgard:
				TradeItems = new MerchantTradeItems("MidRvRCraftingList");
				break;
			case ERealm.Hibernia:
				TradeItems = new MerchantTradeItems("HibRvRCraftingList");
				break;
		}

		GuildName = "Merchant";
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
						Name = "Frida";
					else Name = "Frederic";
				}
				else
				{
					if (Gender == EGender.Female)
						Name = "Fabienne";
					else Name = "Francis";
				}
				GuildName = "Merchant";
				break;
			case ERealm.Midgard:
				if (IsPortalKeepGuard)
				{
					if (Gender == EGender.Female)
						Name = "Olga";
					else Name = "Odun";
				}
				else
				{
					if (Gender == EGender.Female)
						Name = "Rikke";
					else Name = "Rollo";
				}
				break;
			case ERealm.Hibernia:
				if (IsPortalKeepGuard)
				{
					if (Gender == EGender.Female)
						Name = "Alenja";
					else Name = "Airell";
				}
				else
				{
					if (Gender == EGender.Female)
						Name = "Arwen";
					else Name = "Aidan";
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