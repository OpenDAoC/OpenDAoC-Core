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

 using DOL.Language;

 namespace DOL.GS
{
	[NPCGuildScript("Alchemists Master")]
	public class AlchemistsMaster : CraftNPC
	{
		private static readonly ECraftingSkill[] m_trainedSkills = 
		{
			ECraftingSkill.SpellCrafting,
			ECraftingSkill.Alchemy,
			ECraftingSkill.GemCutting,
			ECraftingSkill.HerbalCrafting,
			ECraftingSkill.SiegeCrafting,
		};

		public override ECraftingSkill[] TrainedSkills
		{
			get { return m_trainedSkills; }
		}

		public override string GUILD_ORDER
		{
			get
			{
                return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AlchemistsMaster.GuildOrder");
            }
		}

		public override string ACCEPTED_BY_ORDER_NAME
		{
			get
			{
                return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AlchemistsMaster.AcceptedByOrderName");
            }
		}
		public override ECraftingSkill TheCraftingSkill
		{
			get { return ECraftingSkill.Alchemy; }
		}
		public override string InitialEntersentence
		{
			get
			{
                return LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "AlchemistsMaster.InitialEntersentence");
            }
		}
	}
}