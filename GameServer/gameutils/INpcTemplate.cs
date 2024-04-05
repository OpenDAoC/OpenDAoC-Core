using System.Collections;

namespace DOL.GS
{
	/// <summary>
	/// Interface for all NPC templates
	/// </summary>
	public interface INpcTemplate
	{
		/// <summary>
		/// Gets the npc template ID
		/// </summary>
		int TemplateId { get; }

        /// <summary>
        /// Gets the translation id.
        /// </summary>
        string TranslationId { get; }
		
		/// <summary>
		/// Do we want the npctemplate replace db mob's values ?
		/// </summary>
		bool ReplaceMobValues { get; }
		
		/// <summary>
		/// Gets the template 'physical' attributes
		/// </summary>
		string Name { get; }

        string Suffix { get; }
		string GuildName { get; }
        string ExamineArticle { get; }
        string MessageArticle { get; }
		string Model { get; }
		ushort Gender {get; }
		string Size { get; }
		string Level { get;}
		short MaxSpeed { get; }
		ushort Flags { get; }
		ushort Race { get; }
		ushort BodyType { get;}
		byte VisibleActiveWeaponSlot { get;}

		/// <summary>
		/// Gets the template npc inventory
		/// </summary>
		string Inventory { get; }

		/// <summary>
		/// List of items sold by this npc
		/// </summary>
		string ItemsListTemplateID { get; }

		/// <summary>
		/// Gets the template combat stats
		/// </summary>
		eDamageType MeleeDamageType { get; }
		byte ParryChance { get; }
		byte EvadeChance { get; }
		byte BlockChance { get; }
		byte LeftHandSwingChance { get; }

		/// <summary>
		/// Gets the template npc abilities
		/// </summary>
		IList Spells { get; }
		IList Styles { get; }
		IList SpellLines { get; }
		IList Abilities { get; }

		/// <summary>
		/// Gets the template npc stats
		///</summary>
		short Strength { get; }
		short Constitution { get; }
		short Dexterity { get; }
		short Quickness { get; }
		short Piety { get; }
		short Intelligence { get; }
		short Empathy { get; }
		short Charisma { get; }

		/// <summary>
		/// Gets the template npc aggro values
		/// </summary>
		byte AggroLevel { get;}
		int AggroRange { get;}

		/// <summary>
		/// The mob's tether range; if mob is pulled farther than this distance
		/// it will return to its spawn point.
		/// if TetherRange > 0 ... the amount is the normal value
		/// if TetherRange less or equal 0 ... no tether check
		/// </summary>
		int TetherRange { get; }

		/// <summary>
		/// What object ClassType should this template use?
		/// </summary>
		string ClassType { get; }
		
		int FactionId { get; }
	}
}
