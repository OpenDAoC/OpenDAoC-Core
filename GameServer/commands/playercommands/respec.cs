using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&respec",
		ePrivLevel.Player,
		"Respecs the char",
		"/respec")]
	public class RespecCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		const string RA_RESPEC = "realm_respec";
		const string ALL_RESPEC = "all_respec";
		const string LINE_RESPEC = "line_respec";
		const string DOL_RESPEC = "dol_respec";
		const string BUY_RESPEC = "buy_respec";
		const string CHAMP_RESPEC = "champion_respec";
		
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				if (ServerProperties.Properties.FREE_RESPEC || client.Player.Level < 50)
				{
					DisplayMessage(client, "Target any trainer and use:");
					DisplayMessage(client, "/respec ALL to respec all skills");
					DisplayMessage(client, "/respec <line name> to respec a single skill line");
					DisplayMessage(client, "/respec REALM to respec realm abilities");
					//DisplayMessage(client, "/respec CHAMPION to respec champion abilities");
					return;
				}
				
				// Check for respecs.
				if (client.Player.RespecAmountAllSkill < 1
					&& client.Player.RespecAmountSingleSkill < 1
					&& client.Player.RespecAmountDOL <1
					&& client.Player.RespecAmountRealmSkill < 1)
				{
					DisplayMessage(client, "You don't seem to have any respecs available.");
					DisplayMessage(client, "Use /respec buy to buy an single-line respec.");
					return;
				}

				if (client.Player.RespecAmountAllSkill > 0)
				{
					DisplayMessage(client, "You have " + client.Player.RespecAmountAllSkill + " full skill respecs available.");
					DisplayMessage(client, "Target any trainer and use /respec ALL");
				}
				if (client.Player.RespecAmountSingleSkill > 0)
				{
					DisplayMessage(client, "You have " + client.Player.RespecAmountSingleSkill + " single-line respecs available.");
					DisplayMessage(client, "Target any trainer and use /respec <line name>");
				}
				if (client.Player.RespecAmountRealmSkill > 0)
				{
					DisplayMessage(client, "You have " + client.Player.RespecAmountRealmSkill + " realm skill respecs available.");
					DisplayMessage(client, "Target any trainer and use /respec REALM");
				}
				if (client.Player.RespecAmountDOL > 0)
				{
					DisplayMessage(client, "You have " + client.Player.RespecAmountDOL + " DOL ( full skill ) respecs available.");
					DisplayMessage(client, "Target any trainer and use /respec all");
				}
				DisplayMessage(client, "Use /respec buy to buy an single-line respec.");
				return;
			}

			GameTrainer trainer = client.Player.TargetObject as GameTrainer;
			// Player must be speaking with trainer to respec.  (Thus have trainer targeted.) Prevents losing points out in the wild.
			if (args[1].ToLower() != "buy" && (trainer == null || !trainer.CanTrain(client.Player)))
			{
				DisplayMessage(client, "You must be speaking with your trainer to respec.");
				return;
			}

			switch (args[1].ToLower())
			{
				//case "buy":
				//	{
				//		if (ServerProperties.Properties.FREE_RESPEC)
				//			return;

				//		// Buy respec
				//		if (client.Player.CanBuyRespec == false || client.Player.RespecCost < 0)
				//		{
				//			DisplayMessage(client, "You can't buy a respec on this level again.");
				//			return;
				//		}

				//		long mgold = client.Player.RespecCost;
				//		if ((client.Player.Gold + 1000 * client.Player.Platinum) < mgold)
				//		{
				//			DisplayMessage(client, "You don't have enough money! You need " + mgold + " gold!");
				//			return;
				//		}
				//		client.Out.SendCustomDialog("It costs " + mgold + " gold. Want you really buy?", new CustomDialogResponse(RespecDialogResponse));
				//		client.Player.TempProperties.setProperty(BUY_RESPEC, true);
				//		break;
				//	}
				case "all":
					{
                        if (/*client.Player.Level >= 50 || */TimeSpan.FromSeconds(client.Player.PlayedTimeSinceLevel).Hours > 24)
                        {
                            // Check for full respecs.
                            if ( client.Player.RespecAmountAllSkill < 1
                                && !ServerProperties.Properties.FREE_RESPEC)
                            {
                                DisplayMessage(client, "You don't seem to have any full skill respecs available.");
                                return;
                            }
                        }

                        client.Out.SendCustomDialog("CAUTION: All respec changes are final with no second chance. Proceed carefully!", new CustomDialogResponse(RespecDialogResponse));
                        client.Player.TempProperties.SetProperty(ALL_RESPEC, true);

						break;
					}
				//case "dol":
				//	{
				//		// Check for DOL respecs.
				//		if (client.Player.RespecAmountDOL < 1
				//			&& !ServerProperties.Properties.FREE_RESPEC)
				//		{
				//			DisplayMessage(client, "You don't seem to have any DOL respecs available.");
				//			return;
				//		}

				//		client.Out.SendCustomDialog("CAUTION: All respec changes are final with no second chance. Proceed carefully!", new CustomDialogResponse(RespecDialogResponse));
				//		client.Player.TempProperties.setProperty(DOL_RESPEC, true);
				//		break;
				//	}
				case "realm":
					{
                        if (/*client.Player.Level >= 50 || */TimeSpan.FromSeconds(client.Player.PlayedTimeSinceLevel).Hours > 24)
                        {
                            if (client.Player.RespecAmountRealmSkill < 1
                                && !ServerProperties.Properties.FREE_RESPEC)
                            {
                                DisplayMessage(client, "You don't seem to have any realm skill respecs available.");
                                return;
                            }
                        }
						client.Out.SendCustomDialog("CAUTION: All respec changes are final with no second chance. Proceed carefully!", new CustomDialogResponse(RespecDialogResponse));
						client.Player.TempProperties.SetProperty(RA_RESPEC, true);
						break;
					}
				//case "champion":
				//	{
				//		if (ServerProperties.Properties.FREE_RESPEC)
				//		{
				//			client.Out.SendCustomDialog("CAUTION: All respec changes are final with no second chance. Proceed carefully!", new CustomDialogResponse(RespecDialogResponse));
				//			client.Player.TempProperties.setProperty(CHAMP_RESPEC, true);
				//			break;
				//		}
				//		return;
				//	}
				default:
					{
						if (/*client.Player.Level >= 50 || */TimeSpan.FromSeconds(client.Player.PlayedTimeSinceLevel).Hours > 24)
						{
							// Check for single-line respecs.
							if (client.Player.RespecAmountSingleSkill < 1
							&& !ServerProperties.Properties.FREE_RESPEC)
							{
								DisplayMessage(client, "You don't seem to have any single-line respecs available.");
								return;
							}
						}

						string lineName = string.Join(" ", args, 1, args.Length - 1);
						Specialization specLine = client.Player.GetSpecializationByName(lineName);

						if (specLine == null)
						{
							DisplayMessage(client, "No line with name '" + lineName + "' found.");
							return;
						}
						if (specLine.Level < 2)
						{
							DisplayMessage(client, "Level of " + specLine.Name + " line is less than 2. ");
							return;
						}

						client.Out.SendCustomDialog("CAUTION: All respec changes are final with no second chance. Proceed carefully!", new CustomDialogResponse(RespecDialogResponse));
						client.Player.TempProperties.SetProperty(LINE_RESPEC, specLine);
						break;
					}
			}
		}
		

		protected void RespecDialogResponse(GamePlayer player, byte response)
		{

			if (response != 0x01) return; //declined

			int specPoints = player.SkillSpecialtyPoints;
			int realmSpecPoints = player.RealmSpecialtyPoints;

			if (player.TempProperties.GetProperty(ALL_RESPEC, false))
			{
				player.RespecAll();
				player.TempProperties.RemoveProperty(ALL_RESPEC);
			}
			if (player.TempProperties.GetProperty(DOL_RESPEC, false))
			{
				player.RespecDOL();
				player.TempProperties.RemoveProperty(DOL_RESPEC);
			}
			if (player.TempProperties.GetProperty(RA_RESPEC, false))
			{
				player.RespecRealm();
				player.TempProperties.RemoveProperty(RA_RESPEC);
			}
			if (player.TempProperties.GetProperty(CHAMP_RESPEC, false))
			{
				player.RespecChampionSkills();
				player.TempProperties.RemoveProperty(CHAMP_RESPEC);
			}
			Specialization specLine = player.TempProperties.GetProperty<Specialization>(LINE_RESPEC, null);
			if (specLine != null)
			{
				player.RespecSingle(specLine);
				player.TempProperties.RemoveProperty(LINE_RESPEC);
			}
			if (player.TempProperties.GetProperty(BUY_RESPEC, false))
			{
				player.TempProperties.RemoveProperty(BUY_RESPEC);
				if (player.RespecCost >= 0 && player.RemoveMoney(player.RespecCost * 10000))
				{
                    InventoryLogging.LogInventoryAction(player, "(respec)", eInventoryActionType.Merchant, player.RespecCost * 10000);
					player.RespecAmountSingleSkill++;
					player.RespecBought++;
					DisplayMessage(player, "You bought a single line respec!");
				}
				player.Out.SendUpdateMoney();
			}			
			// Assign full points returned
			if (player.SkillSpecialtyPoints > specPoints)
			{
				player.styleComponent.RemoveAllStyles(); // Kill styles
				DisplayMessage(player, "You regain " + (player.SkillSpecialtyPoints - specPoints) + " specialization points!");
			}
			if (player.RealmSpecialtyPoints > realmSpecPoints)
			{
				 DisplayMessage(player, "You regain " + (player.RealmSpecialtyPoints - realmSpecPoints) + " realm specialization points!");
			}
			player.RefreshSpecDependantSkills(false);
			// Notify Player of points
			player.Out.SendUpdatePlayerSkills();
			player.Out.SendUpdatePoints();
			player.Out.SendUpdatePlayer();
			player.SendTrainerWindow();
			player.SaveIntoDatabase();

			// Remove all self-cast buffs when respeccing to avoid exploits.
            DisplayMessage(player, "All self-cast buffs have been removed due to a respec.");
            if (player.effectListComponent != null)
            {
				foreach (ECSGameEffect e in player.effectListComponent.GetAllEffects())
                {
					if (e is ECSGameSpellEffect eSpell && eSpell.SpellHandler.Caster == player)
                    {
						EffectService.RequestCancelEffect(e);
					}
                }

				//Remove self-casted pulsing effects
				foreach (ECSGameEffect e in player.effectListComponent.GetAllPulseEffects())
                {
					if (e is ECSGameSpellEffect eSpell && eSpell.SpellHandler.Caster == player)
                    {
						EffectService.RequestCancelEffect(e);
					}
                }
            }
		}
	}
}
