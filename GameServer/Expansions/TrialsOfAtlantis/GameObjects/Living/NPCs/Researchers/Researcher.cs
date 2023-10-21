using System;
using System.Collections;
using Core.AI.Brain;
using Core.Language;

namespace Core.GS
{
    /// <summary>
    /// Base class for all Atlantis scholar type NPCs.
    /// </summary>
    public class Researcher : GameNpc
    {
        public Researcher()
            : base() { }

		/// <summary>
		/// Can trade untradable items
		/// </summary>
		public override bool CanTradeAnyItem
		{
			get
			{
				return true;
			}
		}

        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			switch (Realm)
			{
				case ERealm.Albion: 
					template.AddNPCEquipment(EInventorySlot.TorsoArmor, 2230); break;
				case ERealm.Midgard:
					template.AddNPCEquipment(EInventorySlot.TorsoArmor, 2232);
					template.AddNPCEquipment(EInventorySlot.ArmsArmor, 2233);
					template.AddNPCEquipment(EInventorySlot.LegsArmor, 2234);
					template.AddNPCEquipment(EInventorySlot.HandsArmor, 2235);
					template.AddNPCEquipment(EInventorySlot.FeetArmor, 2236);
					break;
				case ERealm.Hibernia:
					template.AddNPCEquipment(EInventorySlot.TorsoArmor, 2231); ; break;
			}

            Inventory = template.CloseTemplate();
            Flags = ENpcFlags.PEACE;	// Peace flag.
            return base.AddToWorld();
        }

        /// <summary>
		/// How friendly this NPC is to a player.
		/// </summary>
		/// <param name="player">GamePlayer that is examining this object</param>
		/// <param name="firstLetterUppercase"></param>
		/// <returns>Aggro state as a string.</returns>
        public override string GetAggroLevelString(GamePlayer player, bool firstLetterUppercase)
        {
            IOldAggressiveBrain aggroBrain = Brain as IOldAggressiveBrain;
            String aggroLevelString;

            if (GameServer.ServerRules.IsSameRealm(this, player, true))
            {
                if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "GameNPC.GetAggroLevelString.Friendly2");
                else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "GameNPC.GetAggroLevelString.Friendly1");
            }
            else if (aggroBrain != null && aggroBrain.AggroLevel > 0)
            {
                if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "GameNPC.GetAggroLevelString.Aggressive2");
                else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "GameNPC.GetAggroLevelString.Aggressive1");
            }
            else
            {
                if (firstLetterUppercase) aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "GameNPC.GetAggroLevelString.Neutral2");
                else aggroLevelString = LanguageMgr.GetTranslation(player.Client.Account.Language, 
                    "GameNPC.GetAggroLevelString.Neutral1");
            }

            return aggroLevelString;
        }

        /// <summary>
        /// Returns a list of examine messages.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override IList GetExamineMessages(GamePlayer player)
        {
            IList list = new ArrayList(4);
			list.Add(String.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "Researcher.GetExamineMessages.YouExamine",
									Name, (Name.EndsWith("a") || Name.EndsWith("le")) ?
									LanguageMgr.GetTranslation(player.Client.Account.Language, "Researcher.GetExamineMessages.She") :
									LanguageMgr.GetTranslation(player.Client.Account.Language, "Researcher.GetExamineMessages.He"),
									GetAggroLevelString(player, false))));
            return list;
        }

        /// <summary>
        /// Turn the researcher to face the player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            TurnTo(player, 10000);
            return true;
        }
    }
}
