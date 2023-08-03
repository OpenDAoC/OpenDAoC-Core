using System;
using System.Collections;
using log4net;
using DOL.Database;

namespace DOL.GS.DatabaseConverters
{
	/// <summary>
	/// Converts the database format to the version 3
	/// </summary>
	[DatabaseConverter(3)]
	public class Version003 : IDatabaseConverter
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// we need to make use of the new poison fields
		/// </summary>
		public void ConvertDatabase()
		{
			log.Info("Database Version 3 Convert Started");

			if (GameServer.Instance.Configuration.DBType == DOL.Database.Connection.EConnectionType.DATABASE_XML)
			{
				log.Info("You have an XML database loaded, this converter will only work with MySQL, skipping");
				return;
			}

			var templates = CoreDb<DbItemTemplates>.SelectObjects(DB.Column("SpellID").IsEqualTo(0));

			int count = 0;
			foreach (DbItemTemplates template in templates)
			{
				SpellLine poisonLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mundane_Poisons);
				if (poisonLine != null)
				{
					IList spells = SkillBase.GetSpellList(poisonLine.KeyName);
					if (spells != null)
					{
						foreach (Spell spl in spells)
						{
							if (spl.ID == template.SpellID)
							{
								template.PoisonSpellID = template.SpellID;
								template.SpellID = 0;
								template.PoisonCharges = template.Charges;
								template.Charges = 0;
								template.PoisonMaxCharges = template.MaxCharges;
								template.MaxCharges = 0;
								GameServer.Database.SaveObject(template);
								count++;
								break;
							}
						}
					}
				}
			}

			log.Info("Converted " + count + " templates");

			var items = CoreDb<InventoryItem>.SelectObjects(DB.Column("SpellID").IsEqualTo(0));
			count = 0;
			foreach (InventoryItem item in items)
			{
				foreach (DbItemTemplates template in templates)
				{
					SpellLine poisonLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mundane_Poisons);
					if (poisonLine != null)
					{
						IList spells = SkillBase.GetSpellList(poisonLine.KeyName);
						if (spells != null)
						{
							foreach (Spell spl in spells)
							{
								if (spl.ID == template.SpellID)
								{
									template.PoisonSpellID = template.SpellID;
									template.SpellID = 0;
									template.PoisonCharges = template.Charges;
									template.Charges = 0;
									template.PoisonMaxCharges = template.MaxCharges;
									template.MaxCharges = 0;
									GameServer.Database.SaveObject(template);
									count++;
									break;
								}
							}
						}
					}
				}
			}

			log.Info("Converted " + count + " items");

			log.Info("Database Version 3 Convert Finished");
		}
	}
}