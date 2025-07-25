using System;
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// Represents an in-game Healer NPC, which can remove resurrection illness and restore lost Constitution points.
	/// </summary>
	[NPCGuildScript("Healer")]
	public class GameHealer : GameNPC
	{
		private const string CURED_SPELL_TYPE = "PveResurrectionIllness";

        private const string COST_BY_PTS = "cost";

		/// <summary>
		/// Constructor
		/// </summary>
		public GameHealer()
			: base()
		{
		}

		#region Examine Messages
		public override IList GetExamineMessages(GamePlayer player)
        {
			IList list = new ArrayList();
			// Message: You target [{0}].
			list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.Target.YouTarget.Object", GetName(0, false, player.Client.Account.Language, this)));
			// Message: You examine {0}. {1} is {2} and is a healer.
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Examine.YouExamine.Healer", GetName(0, false, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language), GetAggroLevelString(player, false)));
            // Message: [Right-click to restore lost Constitution]
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Examine.GiveDonation.Healer", null));
            return list;
		}
		#endregion Examine Messages

		#region Interact Messages
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false; // Prevent interact

			TurnTo(player, 5000); // Face the player upon interact
			
			// Check for ambient trigger messages for the NPC in the 'MobXAmbientBehaviour' table
			var triggers = GameServer.Instance.NpcManager.AmbientBehaviour[base.Name];
			// If the NPC has no ambient trigger message assigned, then return this message upon interact
			if (triggers == null || triggers.Length == 0)
				// Message: {0} says, "Greetings, {1}. What can I do for you today?"
				ChatUtil.SendSayMessage(player, "GameNPC.Dialogue.Greetings.Healer", GetName(0, true), player.CharacterClass.Name);

			//GameSpellEffect effect = SpellHandler.FindEffectOnTarget(player, CURED_SPELL_TYPE);
            ECSGameEffect effect = EffectListService.GetEffectOnTarget(player, eEffect.ResurrectionIllness); // Identify effect to remove
			if (effect != null) // If PvE sickness is active
			{
				effect.Stop(); // Cancel sickness
				// Message: {0} cures your resurrection sickness.
                ChatUtil.SendSystemMessage(player, "GameNPC.Interact.CuresRS.Healer", GetName(0, true, player.Client.Account.Language, this));
            }
            ECSGameEffect rvrEffect = EffectListService.GetEffectOnTarget(player, eEffect.RvrResurrectionIllness); // Identify effect to remove
            if (rvrEffect != null) // If RvR sickness is active
            {
	            rvrEffect.Stop(); // Cancel sickness
	            // Message: {0} cures your resurrection sickness.
	            ChatUtil.SendSystemMessage(player, "GameNPC.Interact.CuresRS.Healer", GetName(0, true, player.Client.Account.Language, this));
            }

            // Trigger if player has lost any Constitution from deaths
            if (player.TotalConstitutionLostAtDeath > 0)
			{
				int oneConCost = GamePlayer.prcRestore[player.Level < GamePlayer.prcRestore.Length ? player.Level : GamePlayer.prcRestore.Length - 1];
				player.TempProperties.SetProperty(COST_BY_PTS, (long)oneConCost);
				
				// Trigger custom ACCEPT/DECLINE dialog
				// Message: It will cost {0} to have your lost constitution restored. Do you accept?
				player.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Dialog.AcceptDecline.Healer", WalletHelper.ToString(player.TotalConstitutionLostAtDeath * (long)oneConCost)), new CustomDialogResponse(HealerDialogResponse));
            }
			else // No Con is missing
			{
				// Message: Your constitution is already fully restored!
				ChatUtil.SendSystemMessage(player, "GameNPC.Interact.AlreadyRestored.Healer", null);
			}
			return true; // Trigger interact
		}

		protected void HealerDialogResponse(GamePlayer player, byte response)
        {
	        // Prevent dialog trigger if player is not within range
            if (!IsWithinRadius(player, WorldMgr.INTERACT_DISTANCE))
            {
	            // Message: You are too far away to interact with {0}.
	            ChatUtil.SendSystemMessage(player, "GameNPC.Interact.TooFarAway", GetName(0, false, player.Client.Account.Language, this));
                return;
            }

            // Player selects 'DECLINE' dialog option
            if (response != 0x01) // Declined value
            {
	            // Message: You decline to have your constitution restored.
	            ChatUtil.SendSystemMessage(player, "GameNPC.Interact.Decline.Healer", null);
                return;
            }

            long cost = player.TempProperties.GetProperty<long>(COST_BY_PTS);
            player.TempProperties.RemoveProperty(COST_BY_PTS);
            if (cost <= 0) cost = 1;
            int restorePoints = (int)Math.Min(player.TotalConstitutionLostAtDeath, player.Wallet.GetMoney() / cost);
            if (restorePoints < 1)
                restorePoints = 1; // Constitution reduced by 1 at minimum
            long totalCost = restorePoints * cost; // Total cost to restore full Con lost
            
            // Trigger if player has sufficient money to "donate"
            if (player.Wallet.RemoveMoney(totalCost))
            {
                InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, totalCost); // Deduct the cost from the player
                if (restorePoints == 1)
	                // Message: {0} restores {1} point of your lost constitution.
	                ChatUtil.SendErrorMessage(player, "GameNPC.Interact.RestoresOneCon.Healer", Name, restorePoints);
                else
	                // Message: {0} restores {1} points of your lost constitution.
	                ChatUtil.SendErrorMessage(player, "GameNPC.Interact.RestoresMoreCon.Healer", Name, restorePoints);
                // Message: You give {0} a donation of {1}.
                ChatUtil.SendSystemMessage(player, "GameNPC.Interact.YouGiveDonation.Healer", GetPronoun(2, false, player.Client.Account.Language), WalletHelper.ToString(totalCost));
                player.TotalConstitutionLostAtDeath -= restorePoints; // Restore lost Con
                player.Out.SendCharStatsUpdate(); // Update the character with the change
            }
            else // If insufficient funds available, throw "error"
            {
	            // Message: {0} says, "It costs {1} to restore {2} lost constitution. You don't have that much."
	            ChatUtil.SendSayMessage(player, "GameNPC.Interact.NeedWalletHelper.Healer", GetName(0, true, player.Client.Account.Language, this), WalletHelper.ToString(totalCost), restorePoints);
            }
            return;
        }
		#endregion Interact Messages
	}
}
