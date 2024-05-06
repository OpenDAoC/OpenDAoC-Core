using DOL.Database.Attributes;

namespace DOL.Database
{
	[DataTable(TableName = "ItemUnique", PreCache = false)]
	public class DbItemUnique : DbItemTemplate
	{
		public const string UNIQUE_SEPARATOR = "#";

		public DbItemUnique() : base()
		{
			m_allowUpdate = true;
			m_id_nb = "Unique_" + UniqueID.IdGenerator.GenerateID();
			m_name = "(blank item)";
		}

		public DbItemUnique(DbItemTemplate template) : base()
		{
			m_allowUpdate = true;

			if (template is DbItemUnique)
			{
				m_id_nb = "Unique_" + UniqueID.IdGenerator.GenerateID();
			}
			else
			{
				m_id_nb = template.Id_nb + UNIQUE_SEPARATOR + UniqueID.IdGenerator.GenerateID();
			}

			Name = template.Name;
			Bonus = template.Bonus;
			Bonus1 = template.Bonus1;
			Bonus2 = template.Bonus2;
			Bonus3 = template.Bonus3;
			Bonus4 = template.Bonus4;
			Bonus5 = template.Bonus5;
			Bonus6 = template.Bonus6;
			Bonus7 = template.Bonus7;
			Bonus8 = template.Bonus8;
			Bonus9 = template.Bonus9;
			Bonus10 = template.Bonus10;
			Color = template.Color;
			Condition = template.Condition;
			DPS_AF = template.DPS_AF;
			Durability = template.Durability;
			Effect = template.Effect;
			Emblem = template.Emblem;
			ExtraBonus = template.ExtraBonus;
			Hand = template.Hand;
			IsDropable = template.IsDropable;
			IsPickable = template.IsPickable;
			IsTradable = template.IsTradable;
			CanDropAsLoot = template.CanDropAsLoot;
			MaxCount = template.MaxCount;
			PackSize = template.PackSize;
			Item_Type = template.Item_Type;
			Level = template.Level;
			MaxCondition = template.MaxCondition;
			MaxDurability = template.MaxDurability;
			Model = template.Model;
			Extension = template.Extension;
			Object_Type = template.Object_Type;
			Quality = template.Quality;
			SPD_ABS = template.SPD_ABS;
			Type_Damage = template.Type_Damage;
			Weight = template.Weight;
			Price = template.Price;
			Bonus1Type = template.Bonus1Type;
			Bonus2Type = template.Bonus2Type;
			Bonus3Type = template.Bonus3Type;
			Bonus4Type = template.Bonus4Type;
			Bonus5Type = template.Bonus5Type;
			Bonus6Type = template.Bonus6Type;
			Bonus7Type = template.Bonus7Type;
			Bonus8Type = template.Bonus8Type;
			Bonus9Type = template.Bonus9Type;
			Bonus10Type = template.Bonus10Type;
			ExtraBonusType = template.ExtraBonusType;
			Charges = template.Charges;
			MaxCharges = template.MaxCharges;
			Charges1 = template.Charges1;
			MaxCharges1 = template.MaxCharges1;
			SpellID = template.SpellID;
			SpellID1 = template.SpellID1;
			ProcSpellID = template.ProcSpellID;
			ProcSpellID1 = template.ProcSpellID1;
			ProcChance = template.ProcChance;
			PoisonSpellID = template.PoisonSpellID;
			PoisonCharges = template.PoisonCharges;
			PoisonMaxCharges = template.PoisonMaxCharges;
			Realm = template.Realm;
			AllowedClasses = template.AllowedClasses;
			CanUseEvery = template.CanUseEvery;
			Flags = template.Flags;
			BonusLevel = template.BonusLevel;
			LevelRequirement = template.LevelRequirement;
			Description = template.Description;
			IsIndestructible = template.IsIndestructible;
			IsNotLosingDur = template.IsNotLosingDur;
			PackageID = template.PackageID;
			ClassType = template.ClassType;
			SalvageYieldID = template.SalvageYieldID;
		}
	}
}
