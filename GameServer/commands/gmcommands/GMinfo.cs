using System;
using System.Collections.Generic;
using System.Net;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Housing;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler.Client.v168;
using DOL.Language;
using static DOL.AI.Brain.StandardMobBrain;

namespace DOL.GS.Commands
{
	[Cmd("&GMinfo", ePrivLevel.GM, "Various Information", "'/GMinfo (select a target or not)")]
	public class GMInfoCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
			uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
			uint seconde = WorldMgr.GetCurrentGameTime() / 1000 % 60;
			IPHostEntry ip = Dns.GetHostByName(Dns.GetHostName());
			string myNIC = ip.AddressList[0].ToString();

			string myInternetIP = ip.AddressList[0].ToString();

			string name = "(NoName)";
			var info = new List<string>();
			info.Add("        Current Region : " + client.Player.CurrentRegionID );
			info.Add(" ");
			Type regionType = client.Player.CurrentRegion.GetType();
			info.Add("       Region ClassType: " + regionType.FullName);
			info.Add(" ");
			
			if (client.Player.TargetObject != null)
			{

				if (client.Player.TargetObject is GuardLord gl)
				{
					info.Add("--KEEP LORD--");
					info.Add(" ");
					info.Add("Name : " + client.Player.TargetObject.Name);
					info.Add($"RP Reward: {gl.RealmPointsValue}");
					info.Add($"BP Reward: {gl.BountyPointsValue}");
					info.Add($"XP Reward: {gl.ExperienceValue}");
				}
				
				#region Mob
				/********************* MOB ************************/
				if (client.Player.TargetObject is GameNPC)
				{
					var target = client.Player.TargetObject as GameNPC;
					name = target.Name;
					
					
					info.Add(" + Class: " + target.GetType().ToString());
					info.Add(" + Brain: " + (target.Brain == null ? "(null)" : target.Brain.GetType().ToString()));
					if (target.LoadedFromScript)
						info.Add(" + Loaded: from Script");
					else
						info.Add(" + Loaded: from Database");
					info.Add(" ");
					if (client.Player.TargetObject is GameMerchant)
					{
						var targetM = client.Player.TargetObject as GameMerchant;
						
                        info.Add(" + Is Merchant ");
						if (targetM.TradeItems != null)
						{
                            info.Add(" + Sell List: \n   " + targetM.TradeItems.ItemsListID);
						}
						else 
							info.Add(" + Sell List:  Not Present !\n");
						info.Add(" ");
					}

					if (target.Faction != null)
					{
						info.Add("Faction: " + target.Faction.Name);
						info.Add("ID:   " + target.Faction.Id);
						info.Add("Enemies: " + target.Faction.EnemyFactions.Count);
						info.Add("Friends: " + target.Faction.FriendFactions.Count);
					}
					if (client.Player.TargetObject is GameSummonedPet)
					{
						var targetP = client.Player.TargetObject as GameSummonedPet;
                        info.Add(" + Is Pet ");
						info.Add(" + Pet Owner:   " + targetP.Owner);
						info.Add(" ");
						info.Add(" + Pet target:   " + targetP.TargetObject?.Name);
					}
					
					if (client.Player.TargetObject is GameMovingObject)
					{
						var targetM = client.Player.TargetObject as GameMovingObject;
                        info.Add(" + Is GameMovingObject  ");
                        info.Add(" + ( Boats - Siege weapons - Custom Object");
						info.Add(" + Emblem:   " + targetM.Emblem);
						info.Add(" ");
					}
					
					info.Add(" + Name: " + name);
					if (target.GuildName != null && target.GuildName.Length > 0)
						info.Add(" + Guild: " + target.GuildName);
					info.Add(" + Level: " + target.Level);
					info.Add(" + Realm: " + GlobalConstants.RealmToName(target.Realm));
					info.Add(" + Model:  " + target.Model);
					info.Add(" + Size " + target.Size);
					info.Add(string.Format(" + Flags: {0} (0x{1})", ((GameNPC.eFlags)target.Flags).ToString("G"), target.Flags.ToString("X")));
					info.Add(" ");
					info.Add(" + Attacker count: " + target.attackComponent.AttackerTracker.Count);
					info.Add(" + Melee attacker count: " + target.attackComponent.AttackerTracker.MeleeCount);

					IOldAggressiveBrain aggroBrain = target.Brain as IOldAggressiveBrain;
					if (aggroBrain != null)
					{
						info.Add(" + Aggro level: " + aggroBrain.AggroLevel);
						info.Add(" + Aggro range: " + aggroBrain.AggroRange);

						if(aggroBrain is StandardMobBrain mobBrain)
							info.Add(" + ThinkInterval: " + mobBrain.ThinkInterval +"ms");
					}
					else
						info.Add(" + Not aggressive brain");

					if (target.NPCTemplate != null)
						info.Add(" + NPCTemplate: " + "[" + target.NPCTemplate.TemplateId + "] " + target.NPCTemplate.Name);

					info.Add(" + Roaming Range: " + target.RoamingRange);
					info.Add(" + Tether Range: " + target.TetherRange);

					TimeSpan respawn = TimeSpan.FromMilliseconds(target.RespawnInterval);
					if (target.RespawnInterval <= 0)
						info.Add(" + Respawn: NPC will not respawn");
					else
					{
						string days = string.Empty;
						string hours = string.Empty;
						if (respawn.Days > 0)
							days = respawn.Days + " days ";
						if (respawn.Hours > 0)
							hours = respawn.Hours + " hours ";
						info.Add(" + Respawn: " + days + hours + respawn.Minutes + " minutes " + respawn.Seconds + " seconds");
						info.Add(" + SpawnPoint:  " + target.SpawnPoint.X + ", " + target.SpawnPoint.Y + ", " + target.SpawnPoint.Z);
					}
					
					if (target.QuestListToGive.Count > 0)
						info.Add(" + Quests to give:  " + target.QuestListToGive.Count);
						
					if (target.PathID != null && target.PathID.Length > 0)
						info.Add(" + Path: " + target.PathID);
						
					if (target.OwnerID != null && target.OwnerID.Length > 0)
						info.Add(" + OwnerID: " + target.OwnerID);
						
					info.Add(" ");
					info.Add(" + Damage type: " + target.MeleeDamageType);

					if (target.DamageFactor > 0)
						info.Add(" + DamageFactor: " + target.DamageFactor);

					if (target.GetModified(eProperty.MeleeDamage) > 0)
						info.Add(" + MeleeDamage bonus %: " + target.GetModified(eProperty.MeleeDamage));

					if (target.Abilities != null && target.Abilities.Count > 0)
						info.Add(" + Abilities: " + target.Abilities.Count);

					if (target.Spells != null && target.Spells.Count > 0)
						info.Add(" + Spells: " + target.Spells.Count);
						
					if (target.Styles != null && target.Styles.Count > 0)
						info.Add(" + Styles: " + target.Styles.Count);

					info.Add(" ");
					if (target.Race > 0)
						info.Add(" + Race:  " + target.Race);

					if (target.BodyType > 0)
						info.Add(" + Body Type:  " + target.BodyType);

					info.Add(" ");
					info.Add(" + Active weapon slot: " + target.ActiveWeaponSlot);
					info.Add(" + Visible weapon slot: " + target.VisibleActiveWeaponSlots);
					
					if (target.EquipmentTemplateID != null && target.EquipmentTemplateID.Length > 0)
						info.Add(" + Equipment Template ID: " + target.EquipmentTemplateID);
						
					if (target.Inventory != null)
						info.Add(" + Inventory: " + target.Inventory.AllItems.Count + " items");
						
					info.Add(" ");
					info.Add(" + Mob_ID:  " + target.InternalID);
					info.Add(" + Position:  " + target.X + ", " + target.Y + ", " + target.Z + ", " + target.Heading);
					info.Add(" + OID: " + target.ObjectID);
					info.Add(" + Package ID:  " + target.PackageID);
					
				/*	if (target.Brain != null && target.Brain.IsActive)
					{
						info.Add(target.Brain.GetType().FullName);
						info.Add(target.Brain.ToString());
						info.Add("");
					}
				*/
					info.Add("");
					info.Add(" ------ State ------");
					if (target.IsReturningToSpawnPoint)
					{
						info.Add("IsReturningToSpawnPoint: " + target.IsReturningToSpawnPoint);
						info.Add("");
					}

					info.Add("InCombat: " + target.InCombat);
					info.Add("AttackState: " + target.attackComponent.AttackState);
					info.Add("LastCombatPVE: " + target.LastCombatTickPvE);
					info.Add("LastCombatPVP: " + target.LastCombatTickPvP);
					info.Add("AttackAction: " + target.attackComponent.attackAction);
					info.Add("WeaponAction: " + target.attackComponent.weaponAction);

					if (target.InCombat || target.attackComponent.AttackState)
					{
						info.Add("RegionTick: " + GameLoop.GameLoopTime);
						info.Add("AttackAction NextTick " + target.attackComponent.attackAction.NextTick);
						info.Add("AttackAction TimeUntilStart " + (target.attackComponent.attackAction.NextTick - GameLoop.GameLoopTime));
					}

					info.Add("");

					if (target.TargetObject != null)
					{
						info.Add("TargetObject: " + target.TargetObject.Name);
						info.Add("InView: " + target.TargetInView);
					}

					if (target.Brain is StandardMobBrain brain)
					{
						List<OrderedAggroListElement> aggroList = brain.GetOrderedAggroList();

						if (aggroList.Count > 0)
						{
							info.Add("");
							info.Add("Aggro List:");

							foreach (OrderedAggroListElement orderedAggroListElement in aggroList)
								info.Add($"{orderedAggroListElement.Living.Name}: {orderedAggroListElement.AggroAmount}");
						}
					}

					if (target.attackComponent.AttackerTracker.Count > 0)
					{
						info.Add("");
						info.Add("Attacker List:");

						foreach (GameObject attacker in target.attackComponent.AttackerTracker.Attackers)
							info.Add(attacker.Name);
					}

					if (target.EffectList.Count > 0)
					{
						info.Add("");
						info.Add("Effect List:");

						foreach (IGameEffect effect in target.EffectList)
							info.Add(effect.Name + " remaining " + effect.RemainingTime);
					}

					info.Add("");
					info.Add(" + Loot:");

					var template = DOLDB<DbLootTemplate>.SelectObjects(DB.Column("TemplateName").IsEqualTo(target.Name));
					foreach (DbLootTemplate loot in template)
					{
						DbItemTemplate drop = GameServer.Database.FindObjectByKey<DbItemTemplate>(loot.ItemTemplateID);

						string message = string.Empty;
						if (drop == null)
						{
							message += loot.ItemTemplateID + " (Template Not Found)";
						}
						else
						{
							message += drop.Name + " (" + drop.Id_nb + ")";
						}

						message += " Chance: " + loot.Chance.ToString();
						info.Add("- " + message);
					}
				}

				#endregion Mob

				#region Player
				/********************* PLAYER ************************/
				if (client.Player.TargetObject is GamePlayer)
				{
					var target = client.Player.TargetObject as GamePlayer;

					info.Add("PLAYER INFORMATION (Client # " + target.Client.SessionID + ")");
					info.Add("  - Name : " + target.Name);
					info.Add("  - Lastname : " + target.LastName);
					info.Add("  - Realm : " + GlobalConstants.RealmToName(target.Realm));
					info.Add("  - Level : " + target.Level);
					info.Add("  - Class : " + target.CharacterClass.Name);
					info.Add("  - Guild : " + target.GuildName);
					info.Add(" ");
					info.Add("  - Account Name : " + target.AccountName);
					info.Add("  - IP : " + target.Client.Account.LastLoginIP);
					info.Add("  - Local: " + target.Client.Socket.LocalEndPoint);
					info.Add("  - Remote: " + target.Client.Socket.RemoteEndPoint);
					info.Add("  - Priv. Level : " + target.Client.Account.PrivLevel);
					info.Add("  - Client Version: " + target.Client.Account.LastClientVersion);
					info.Add(" ");
					info.Add("  - Craftingskill : " + target.CraftingPrimarySkill + "");
					info.Add("  - Model ID : " + target.Model);
					info.Add("  - AFK Message: " + target.TempProperties.GetProperty<string>(GamePlayer.AFK_MESSAGE) + "");
					info.Add(" ");
                    info.Add("  - Money : " + Money.GetString(target.GetCurrentMoney()) + "\n");
					info.Add("  - XPs : " + target.Experience);
					info.Add("  - RPs : " + target.RealmPoints);
					info.Add("  - BPs : " + target.BountyPoints);
					info.Add(" ");
					info.Add("--CUSTOM PARAMS-- ");
					var customParams = target.Client.Account.CustomParams;
					if (customParams != null)
					{
						foreach (CustomParam param in customParams)
						{
							info.Add(param.KeyName + " " + param.Value);
						}
					}

					string sCurrent;
					string sTitle;
					int cnt = 0;
								
					info.Add(" ");
					info.Add("SPECCING INFORMATIONS ");
					info.Add("  - Remaining spec. points : " + target.SkillSpecialtyPoints);
					sTitle = "  - Player specialisations / level: \n";
					sCurrent = string.Empty;
                    foreach (Specialization spec in target.GetSpecList())
					{
						sCurrent += "  - " +spec.Name + " = " + spec.Level + " \n";
					}
					info.Add(sTitle + sCurrent);

					info.Add(" ");
					info.Add("  - Respecs dol : " + target.RespecAmountDOL);
					info.Add("  - Respecs single : " + target.RespecAmountSingleSkill);
					info.Add("  - Respecs full : " + target.RespecAmountAllSkill);
					
					info.Add(" ");
					info.Add(" ");
					info.Add("  --------------------------------------");
					info.Add("  -----  Inventory Equiped -----");
					info.Add("  --------------------------------------");
					////////////// Inventaire /////////////
					info.Add("  ----- Money:");
					info.Add(Money.GetShortString(target.GetCurrentMoney()));
					info.Add(" ");

					info.Add("  ----- Wearing:");
					foreach (DbInventoryItem item in target.Inventory.EquippedItems)
						info.Add(" [" + GlobalConstants.SlotToName(item.Item_Type) + "] " + item.Name);
					info.Add(" ");
				}

				#endregion Player

				#region StaticItem

				/********************* OBJECT ************************/
				if (client.Player.TargetObject is GameStaticItem)
				{
					var target = client.Player.TargetObject as GameStaticItem;
					
					if (!string.IsNullOrEmpty(target.Name))
						name = target.Name;
					info.Add("  ------- OBJECT ------\n");
					info.Add(" Name: " + name);
					info.Add(" Model: " + target.Model);
					info.Add(" Emblem: " + target.Emblem);
					info.Add(" Realm: " + target.Realm);

					if (target is GameStaticItemTimed staticItem && staticItem.Owners.Count > 0)
					{
						info.Add(" ");

						foreach (IGameStaticItemOwner owner in staticItem.Owners)
							info.Add($" Owner: {owner.Name}");
					}

					info.Add(" ");
					info.Add(" OID: " + target.ObjectID);
					info.Add (" Type: " + target.GetType());

					WorldInventoryItem invItem = target as WorldInventoryItem;
					if( invItem != null )
					{
						info.Add (" Count: " + invItem.Item.Count);
					}

					info.Add(" ");
					info.Add(" Location: X= " + target.X + " ,Y= " + target.Y + " ,Z= " + target.Z);
				}

				#endregion StaticItem

				#region Door

				/********************* DOOR ************************/
				if (client.Player.TargetObject is GameDoor)
				{
					var target = client.Player.TargetObject as GameDoor;
					
					string Realmname = string.Empty;
					string statut = string.Empty;
					
					name = target.Name;
					
					if (target.Realm == eRealm.None)
						Realmname = "None";
					else if (target.Realm == eRealm.Albion)
						Realmname = "Albion";
					else if (target.Realm == eRealm.Midgard)
						Realmname = "Midgard";
					else if (target.Realm == eRealm.Hibernia)
						Realmname = "Hibernia";
					else if (target.Realm == eRealm.Door)
						Realmname = "All";

					if (target.Locked)
						statut = " Locked";
					else
						statut = " Unlocked";

					info.Add("  ------- DOOR ------\n");
					info.Add(" ");
					info.Add( " + Name : " + target.Name );
					info.Add(" + ID : " + target.DoorId);
					info.Add( " + Realm : " + (int)target.Realm + " : " +Realmname );
					info.Add( " + Level : " + target.Level );
					info.Add( " + Guild : " + target.GuildName );
					info.Add( " + Health : " + target.Health +" / "+ target.MaxHealth);
					info.Add(" + Statut : " + statut);
					info.Add(" + Type : " + DoorRequestHandler.HandlerDoorId / 100000000);
					info.Add(" ");
					info.Add(" + X : " + target.X);  
					info.Add(" + Y : " + target.Y);
					info.Add(" + Z : " + target.Z);
					info.Add(" + Heading : " + target.Heading);
				}

				if(client.Player.TargetObject is GameKeepDoor)
                {
					var target = client.Player.TargetObject as GameKeepDoor;

					string Realmname = string.Empty;
					string statut = string.Empty;

					name = target.Name;

					if (target.Realm == eRealm.None)
						Realmname = "None";

					if (target.Realm == eRealm.Albion)
						Realmname = "Albion";

					if (target.Realm == eRealm.Midgard)
						Realmname = "Midgard";

					if (target.Realm == eRealm.Hibernia)
						Realmname = "Hibernia";

					if (target.Realm == eRealm.Door)
						Realmname = "All";

					info.Add("Component: " + target.Component);
					info.Add("Keep: " + target.Component?.Keep);

					info.Add("  ------- DOOR ------\n");
					info.Add(" ");
					info.Add(" + Name : " + target.Name);
					info.Add(" + ID : " + target.DoorId);
					info.Add(" + Realm : " + (int)target.Realm + " : " + Realmname);
					info.Add(" + Level : " + target.Level);
					info.Add(" + Guild : " + target.GuildName);
					info.Add(" + Health : " + target.Health + " / " + target.MaxHealth);
					info.Add(" + Statut : " + statut);
					info.Add(" + Type : " + DoorRequestHandler.HandlerDoorId / 100000000);
					info.Add(" ");
					info.Add(" + X : " + target.X);
					info.Add(" + Y : " + target.Y);
					info.Add(" + Z : " + target.Z);
					info.Add(" + Heading : " + target.Heading);
				}

				#endregion Door

				#region Keep

				/********************* KEEP ************************/
				if (client.Player.TargetObject is GameKeepComponent)
				{
					var target = client.Player.TargetObject as GameKeepComponent;
					
					name = target.Name;
					
					string realm = " other realm";
					if((byte)target.Realm == 0)
						realm = " Monster";
					if((byte)target.Realm == 1)
						realm = " Albion";
					if((byte)target.Realm == 2)
						realm = " Midgard";
					if((byte)target.Realm == 3)
						realm = " Hibernia";
						
					info.Add( "  ------- KEEP ------\n");
					info.Add( " + Name : " + target.Name);
					info.Add( " + KeepID : " + target.Keep.KeepID);
					info.Add( " + Level : " + target.Level);
					info.Add( " + BaseLevel : " + target.Keep.BaseLevel);
					info.Add( " + Realm : " + realm);
					info.Add( " ");
					info.Add( " + Model : " + target.Model);
					info.Add( " + Skin : " + target.Skin);
					info.Add( " + Height : " + target.Height);
					info.Add( " + ID : " + target.ID);
					info.Add( " ");
					info.Add( " + Health : " + target.Health);
					info.Add( " + IsRaized : " + target.IsRaized);
					info.Add( " + Status : " + target.Status);
					info.Add( " ");
					info.Add( " + Climbing : " + target.Climbing);
					info.Add( " ");
					info.Add( " + ComponentX : " + target.ComponentX);
					info.Add( " + ComponentY : " + target.ComponentY);
					info.Add( " + ComponentHeading : " + target.ComponentHeading);
					info.Add( " ");
					info.Add( " + HookPoints : " + target.HookPoints.Count);
					info.Add( " + Positions : " + target.Positions.Count);
					info.Add( " ");
					info.Add( " + RealmPointsValue : " + target.RealmPointsValue);
					info.Add( " + ExperienceValue : " + target.ExperienceValue);
					info.Add( " + AttackRange : " + target.attackComponent.AttackRange);
					info.Add(" ");
					if (GameServer.KeepManager.GetFrontierKeeps().Contains(target.Keep))
					{
						info.Add(" + Keep Manager : " + GameServer.KeepManager.GetType().FullName);
						info.Add(" + Frontiers");
					}
					else if (GameServer.KeepManager.GetBattleground(target.CurrentRegionID) != null)
					{
						info.Add(" + Keep Manager : " + GameServer.KeepManager.GetType().FullName);
						DbBattleground bg = GameServer.KeepManager.GetBattleground(client.Player.CurrentRegionID);
						info.Add(" + Battleground (" + bg.MinLevel + " to " + bg.MaxLevel + ", max RL: " + bg.MaxRealmLevel + ")");
					}
					else
					{
						info.Add(" + Keep Manager :  Not Managed");
					}
				}

				#endregion Keep

				#region Ram
				if(client.Player.TargetObject is GameSiegeRam)
				{
					var target = client.Player.TargetObject as GameSiegeRam;

						
					info.Add( "  ------- SIEGE RAM ------\n");
					info.Add( " + Max # Riders: " + target.Riders.Length);
					foreach (GamePlayer rider in target.Riders)
					{
						if(rider != null)
							info.Add( " + Rider slot: " + target.RiderSlot(rider) + " Player Name: " + rider.Name);
					}
					
				}

				#endregion Ram

				client.Out.SendCustomTextWindow("[ " + name + " ]", info);
				return;
			}
			
			if (client.Player.TargetObject == null)
			{
				/*********************** HOUSE *************************/
				if (client.Player.InHouse)
				{
					#region House

					House house = client.Player.CurrentHouse as House;
					
					name = house.Name;
		
					int level = house.Model - ((house.Model - 1)/4)*4;
					TimeSpan due = (house.LastPaid.AddDays(ServerProperties.Properties.RENT_DUE_DAYS).AddHours(1) - DateTime.Now);
					
					info.Add("  ------- HOUSE ------\n");
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Owner", name));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Lotnum", house.HouseNumber));
					info.Add("Unique ID: "+house.UniqueID);
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Level", level));
					info.Add(" ");
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Porch"));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.PorchEnabled", (house.Porch ? " Present" : " Not Present")));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.PorchRoofColor",  Color(house.PorchRoofColor)));
					info.Add(" ");
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.ExteriorMaterials"));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.RoofMaterial", MaterialWall(house.RoofMaterial)));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.WallMaterial", MaterialWall(house.WallMaterial)));
					
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.DoorMaterial", MaterialDoor(house.DoorMaterial)));
					
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.TrussMaterial", MaterialTruss(house.TrussMaterial)));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.PorchMaterial", MaterialTruss(house.PorchMaterial)));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.WindowMaterial", MaterialTruss(house.WindowMaterial)));
					
					info.Add(" ");
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.ExteriorUpgrades"));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.OutdoorGuildBanner", ((house.OutdoorGuildBanner) ? " Present" : " Not Present")));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.OutdoorGuildShield", ((house.OutdoorGuildShield) ? " Present" : " Not Present")));
					info.Add(" ");
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.InteriorUpgrades"));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.IndoorGuildBanner", ((house.IndoorGuildBanner) ? " Present" : " Not Present")));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.IndoorGuildShield",((house.IndoorGuildShield) ? " Present" : " Not Present")));
					info.Add(" ");
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.InteriorCarpets"));
					if (house.Rug1Color != 0)
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Rug1Color", Color(house.Rug1Color)));
					if (house.Rug2Color != 0)
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Rug2Color", Color(house.Rug2Color)));
					if (house.Rug3Color != 0)
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Rug3Color", Color(house.Rug3Color)));
					if (house.Rug4Color != 0)
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Rug4Color", Color(house.Rug4Color)));
					info.Add(" ");
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.Lockbox", Money.GetString(house.KeptMoney)));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.RentalPrice", Money.GetString(HouseMgr.GetRentByModel(house.Model))));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.MaxLockbox", Money.GetString(HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS)));
					info.Add(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.RentDueIn", due.Days, due.Hours));

					#endregion House

					client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "House.SendHouseInfo.HouseOwner", name), info);
				}
				else // No target and not in a house
				{
					string realm = " other realm";
					if(client.Player.CurrentZone.Realm == eRealm.Albion)
						realm = " Albion";
					if(client.Player.CurrentZone.Realm == eRealm.Midgard)
						realm = " Midgard";
					if(client.Player.CurrentZone.Realm == eRealm.Hibernia)
						realm = " Hibernia";
					
					info.Add(" Game Time: \t"+ hour.ToString() + ":" + minute.ToString());
                    info.Add(" ");
					info.Add(" Server Rules: " + GameServer.ServerRules.GetType().FullName);

					if (GameServer.KeepManager.FrontierRegionsList.Contains(client.Player.CurrentRegionID))
					{
						info.Add(" Keep Manager: " + GameServer.KeepManager.GetType().FullName);
						info.Add(" Frontiers");
					}
					else if (GameServer.KeepManager.GetBattleground(client.Player.CurrentRegionID) != null)
					{
						info.Add(" Keep Manager: " + GameServer.KeepManager.GetType().FullName);
						DbBattleground bg = GameServer.KeepManager.GetBattleground(client.Player.CurrentRegionID);
						info.Add(" Battleground (" + bg.MinLevel + " to " + bg.MaxLevel + ", max RL: " + bg.MaxRealmLevel + ")");
					}
					else
					{
						info.Add(" Keep Manager :  None for this region");
					}

					info.Add(" ");
					info.Add(" Server players: " + ClientService.ClientCount);
                    info.Add(" ");
                    info.Add(" Region Players:");
                    info.Add(" All players: " + ClientService.GetPlayersOfRegion(client.Player.CurrentRegion).Count);
                    info.Add(" Alb players: " + ClientService.GetPlayersOfRegionAndRealm(client.Player.CurrentRegion, eRealm.Albion).Count);
                    info.Add(" Hib players: " + ClientService.GetPlayersOfRegionAndRealm(client.Player.CurrentRegion, eRealm.Hibernia).Count);
                    info.Add(" Mid players: " + ClientService.GetPlayersOfRegionAndRealm(client.Player.CurrentRegion, eRealm.Midgard).Count);

					info.Add(" ");
					info.Add(" Total objects in region: " + client.Player.CurrentRegion.TotalNumberOfObjects);

                    info.Add(" ");
					info.Add(" NPC in zone:");
                    info.Add(" Alb : " + client.Player.CurrentZone.GetNPCsOfZone(eRealm.Albion).Count);
                    info.Add(" Hib : " + client.Player.CurrentZone.GetNPCsOfZone(eRealm.Hibernia).Count);
                    info.Add(" Mid: " + client.Player.CurrentZone.GetNPCsOfZone(eRealm.Midgard).Count);
                    info.Add(" None : " + client.Player.CurrentZone.GetNPCsOfZone(eRealm.None).Count);
                    info.Add(" ");
					info.Add(" Total objects in zone: " + client.Player.CurrentZone.ObjectCount);
					info.Add(" ");
					info.Add(" Zone Description: "+ client.Player.CurrentZone.Description);
					info.Add(" Zone Realm: "+ realm);
					info.Add(" Zone ID: "+ client.Player.CurrentZone.ID);
					info.Add(" Zone IsDungeon: "+ client.Player.CurrentZone.IsDungeon);
					info.Add(" Zone SkinID: "+ client.Player.CurrentZone.ZoneSkinID);
					info.Add(" Zone X: "+ client.Player.CurrentZone.XOffset);
					info.Add(" Zone Y: "+ client.Player.CurrentZone.YOffset);
					info.Add(" Zone Width: "+ client.Player.CurrentZone.Width);
					info.Add(" Zone Height: "+ client.Player.CurrentZone.Height);
					info.Add(" Zone DivingEnabled: " + client.Player.CurrentZone.IsDivingEnabled);
					info.Add(" Zone Waterlevel: " + client.Player.CurrentZone.Waterlevel);
					info.Add(" Zone Pathing: " + (PathingMgr.Instance.HasNavmesh(client.Player.CurrentZone) ? "enabled" : "disabled"));
					info.Add(" ");
					info.Add(" Region Name: "+ client.Player.CurrentRegion.Name);
                    info.Add(" Region Description: " + client.Player.CurrentRegion.Description);
                    info.Add(" Region Skin: " + client.Player.CurrentRegion.Skin);
					info.Add(" Region ID: "+ client.Player.CurrentRegion.ID);
                    info.Add(" Region Expansion: " + client.Player.CurrentRegion.Expansion);
					info.Add(" Region IsRvR: "+ client.Player.CurrentRegion.IsRvR);
					info.Add(" Region IsFrontier: " + client.Player.CurrentRegion.IsFrontier);
					info.Add(" Region IsDungeon: " + client.Player.CurrentRegion.IsDungeon);
					info.Add(" Region IsNight: "+ client.Player.CurrentRegion.IsNightTime);
					info.Add(" Zone in Region: " + client.Player.CurrentRegion.Zones.Count);
                    info.Add(" Region WaterLevel: " + client.Player.CurrentRegion.WaterLevel);
                    info.Add(" Region HousingEnabled: " + client.Player.CurrentRegion.HousingEnabled);
                    info.Add(" Region IsDisabled: " + client.Player.CurrentRegion.IsDisabled);
					info.Add(" ");
                    info.Add(" Region ServerIP: " + client.Player.CurrentRegion.ServerIP);
                    info.Add(" Region ServerPort: " + client.Player.CurrentRegion.ServerPort);
					
                    client.Out.SendCustomTextWindow("[ " + client.Player.CurrentRegion.Description + " ]", info);
				}
			}
		}

		private string Color(int color)
		{
			if (color == 0) return " White";
			if (color == 53) return " Royal Blue";
			if (color == 54) return " Dark Blue";
			if (color == 57) return " Royal Turquoise";
			if (color == 60) return " Royal Teal";
			if (color == 66) return " Royal Red";
			if (color == 84) return " Violet";
			if (color == 69) return " Green";
			if (color == 70) return " Royal Green";
			if (color == 62) return " Brown ";
			if (color == 72) return " Dark Grey";
			if (color == 74) return " Black";
			if (color == 77) return " Royal Orange";
			if (color == 83) return " Royal Yellow";

            return null;
		}
		
		private string MaterialWall(int material)
		{
			if (material == 0) return " Commoner";
			if (material == 1) return " Burgess";
			if (material == 2) return " Noble";
			
			return null;
		}
		
		private string MaterialDoor(int material)
		{
			if (material == 0) return " Wooden Double";
			if (material == 1) return " Wooden with Chain";
			if (material == 2) return " Iron";
			if (material == 3) return " Aged Wood";
			if (material == 4) return " New Wood";
			if (material == 5) return " Four Panel";
			if (material == 6) return " Iron with Knocker";
			if (material == 7) return " Fine Wooden";
			if (material == 8) return " Fine Paneled";
			if (material == 9) return " Embossed Iron";
			
			return null;
		}
		
		private string MaterialTruss(int material)
		{
			if (material == 0) return " Sand";
			if (material == 1) return " River Stone";
			if (material == 2) return " Driftwood";
			if (material == 3) return " Charcoal Grey";
			if (material == 4) return " Pearl Grey";
			if (material == 5) return " Aged Beige";
			if (material == 6) return " Winter Moss";
			if (material == 7) return " Northern Ivy";
			if (material == 8) return " White Oak";
			if (material == 9) return " Onyx";
			
			return null;
		}

		public string GetComputerName(string clientIP)
		{                        
			try
			{                
				var hostEntry = Dns.GetHostEntry(clientIP);
				return hostEntry.HostName;
			}
			catch (Exception ex)
			{
				return string.Empty;
			}            
		}
		private double GetTotalAFHelper(GameLiving living)
		{
			List<eArmorSlot> armorSlots = new List<eArmorSlot>();
			armorSlots.Add(eArmorSlot.HEAD);
			armorSlots.Add(eArmorSlot.TORSO);
			armorSlots.Add(eArmorSlot.LEGS);
			armorSlots.Add(eArmorSlot.HAND);
			armorSlots.Add(eArmorSlot.ARMS);
			armorSlots.Add(eArmorSlot.FEET);

			double totalArmor = 0;
			
			foreach (var slot in armorSlots)
			{
				totalArmor += living.GetArmorAF(slot);
			}

			return totalArmor;
		}
	}
}
