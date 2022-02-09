namespace DOL.GS.Scripts
{
	public class WarlordDorinakka : GameNPC
	{
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 1000;
		}

		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.85;
		}

		public override short MaxSpeedBase
		{
			get => (short)(191 + (Level * 2));
			set => m_maxSpeedBase = value;
		}
		public override int MaxHealth => 20000;

		public override int AttackRange
		{
			get => 180;
			set { }
		}

		public override bool AddToWorld()
		{
			Name = "Warlord Dorinakka";
			Model = 927;
			Size = 200;
			Level = 81;
			Gender = eGender.Neutral;
			BodyType = 5; // giant
			MaxDistance = 1500;
			TetherRange = 2000;
			RoamingRange = 400;
			Realm = eRealm.None;
			ParryChance = 100; // 100% parry chance
			base.AddToWorld();
			return true;
		}
	}
}