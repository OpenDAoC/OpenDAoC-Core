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
using System.Collections;
using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS
{
	public abstract class CraftNPC : GameNPC
	{
		public abstract string GUILD_ORDER { get; }

		public abstract string ACCEPTED_BY_ORDER_NAME { get; }

		public abstract eCraftingSkill[] TrainedSkills { get; }

		public abstract eCraftingSkill TheCraftingSkill { get; }

		public abstract string InitialEntersentence { get; }

		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;
			if (player.CharacterClass == null)
				return false;

			TurnTo(player, 5000);

			var playerSkills = player.GetCraftingSkillValue(TheCraftingSkill);
			if (playerSkills >= 1)
			{
				player.CraftingPrimarySkill = TheCraftingSkill;
				SayTo(player, eChatLoc.CL_PopupWindow, "Hello, " + player.CraftTitle.GetDescription(player) + "! Because you are already a member of our order, you do not need to join. I have changed your primary crafting skill to " + player.CraftingPrimarySkill + ". Please speak to myself or another craft master if you wish to change your primary crafting skill again.");
				player.Out.SendUpdatePlayer();
				player.Out.SendUpdateCraftingSkills();
				return true;
			}
			
			// Dunnerholl : Basic Crafting Master does not give the option to rejoin this craft
			if (InitialEntersentence != null)
			{
				SayTo(player, eChatLoc.CL_PopupWindow, InitialEntersentence);
			}
            
            		
			return true;
		}

		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text))
				return false;
			if (source is GamePlayer == false)
				return true;

			GamePlayer player = (GamePlayer) source;
			var playerSkills = player.GetCraftingSkillValue(TheCraftingSkill);
			
			if (playerSkills >= 1 && text == GUILD_ORDER)
				return false;
            if(text == GUILD_ORDER)
			{
				player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.WhisperReceive.WishToJoin", ACCEPTED_BY_ORDER_NAME), new CustomDialogResponse(CraftNpcDialogResponse));
			}
			return true;
		}

		protected virtual void CraftNpcDialogResponse(GamePlayer player, byte response)
		{
			if (response != 0x01)
				return; //declined

			player.CraftingPrimarySkill = TheCraftingSkill;

			player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "CraftNPC.CraftNpcDialogResponse.Accepted", ACCEPTED_BY_ORDER_NAME), eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				
			foreach (eCraftingSkill skill in TrainedSkills)
			{
				player.AddCraftingSkill(skill, 1);
			}
			player.Out.SendUpdatePlayer();
			player.Out.SendUpdateCraftingSkills();
			
		}
	}
}