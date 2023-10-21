using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.GS.Commands;
using Core.GS.Enums;
using Core.GS.RealmAbilities;
using Core.GS.ServerProperties;
using log4net;

namespace Core.GS.Packets.Clients;

/// <summary>
/// handles Train clicks from Trainer Window
/// D4 is up to 1.104
/// </summary>
[PacketHandler(EPacketHandlerType.TCP, EClientPackets.TrainRequest, "Handles Player Train Requests", EClientStatus.PlayerInGame)]
public class PlayerTrainRequestHandler : IPacketHandler
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
        if (!Properties.ALLOW_TRAIN_ANYWHERE && client.Account.PrivLevel == (int)EPrivLevel.Player)
        {
            GameTrainer trainer = client.Player.TargetObject as GameTrainer;
            if (trainer == null || (trainer.CanTrain(client.Player) == false && trainer.CanTrainChampionLevels(client.Player) == false))
            {
                client.Out.SendMessage("You must select a valid trainer for your class.", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
                return;
            }
        }

		uint x = packet.ReadInt();
		uint y = packet.ReadInt();
		int idLine = packet.ReadByte();
		int unk = packet.ReadByte();
		int row = packet.ReadByte();
		int skillIndex = packet.ReadByte();

		// idline not null so this is a Champion level training window
		if (idLine > 0)
		{
			if (row > 0 && skillIndex > 0)
			{
				// Get Player CL Spec
				var clspec = client.Player.GetSpecList().Where(sp => sp is LiveChampionLevelsSpecialization).Cast<LiveChampionLevelsSpecialization>().FirstOrDefault();
				
				// check if the tree can be used
				List<Tuple<MiniLineSpecialization, List<Tuple<Skill, byte>>>> tree = null;
				if (clspec != null)
				{
					tree = clspec.GetTrainerTreeDisplay(client.Player, clspec.RetrieveTypeForIndex(idLine));
				}
				
				if (tree != null)
				{
					Tuple<byte, MiniLineSpecialization> skillstatus = clspec.GetSkillStatus(tree, row-1, skillIndex-1);

					if (skillstatus.Item1 == 1)
					{
						client.Out.SendMessage("You already have that ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return;
					}
					if (skillstatus.Item1 != 2)
					{
						client.Out.SendMessage("You do not meet the requirements for that ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return;
					}
					if (client.Player.ChampionSpecialtyPoints < 1)
					{
						client.Out.SendMessage("You do not have enough champion specialty points for that ability!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						return;
					}
					
					skillstatus.Item2.Level++;
					client.Player.AddSpecialization(skillstatus.Item2);
					client.Player.RefreshSpecDependantSkills(false);
					client.Player.Out.SendUpdatePlayer();
					client.Player.Out.SendUpdatePoints();
					client.Player.Out.SendUpdatePlayerSkills();
					client.Player.UpdatePlayerStatus();
					client.Player.Out.SendChampionTrainerWindow(idLine);

					return;
				}
				else
				{
					client.Out.SendMessage("Could not find Champion Spec!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					log.ErrorFormat("Could not find Champion Spec idline {0}, row {1}, skillindex {2}", idLine, row, skillIndex);
				}
			}
		}
		else
		{
			// Trainable Specs or RA's
			IList<Specialization> speclist = client.Player.GetSpecList().Where(e => e.Trainable).ToList();

			if (skillIndex < speclist.Count)
			{
				Specialization spec = (Specialization)speclist[skillIndex];
				if (spec.Level >= client.Player.BaseLevel)
				{
					client.Out.SendMessage("You can't train in this specialization again this level!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}

				// Graveen - autotrain 1.87 - allow players to train their AT specs even if no pts left
				int temp = client.Player.SkillSpecialtyPoints + client.Player.GetAutoTrainPoints(spec, 2);

				if (temp >= spec.Level + 1)
				{
					spec.Level++;
					client.Player.OnSkillTrained(spec);

					client.Out.SendUpdatePoints();
					client.Out.SendTrainerWindow();
					return;
				}
				else
				{
					client.Out.SendMessage("That specialization costs " + (spec.Level + 1) + " specialization points!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					client.Out.SendMessage("You don't have that many specialization points left for this level.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
			}
			else if (skillIndex >= 100)
			{
				// Realm Abilities
				var raList = SkillBase.GetClassRealmAbilities(client.Player.PlayerClass.ID).Where(ra => !(ra is Rr5RealmAbility));
				if (skillIndex < raList.Count() + 100)
				{
					RealmAbility ra = raList.ElementAtOrDefault(skillIndex - 100);
					if (ra != null)
					{
						ra.Level = client.Player.GetAbilityLevel(ra.KeyName);
						int cost = ra.CostForUpgrade(ra.Level);
						ra.Level++;
						
						if (client.Player.RealmSpecialtyPoints < cost)
						{
							client.Out.SendMessage(ra.Name + " costs " + (cost) + " realm ability points!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							client.Out.SendMessage("You don't have that many realm ability points left to get this.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						if (!ra.CheckRequirement(client.Player))
						{
							client.Out.SendMessage("You do not have required pre-requisites to train " + ra.Name + " now. Come back later.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						
						client.Player.AddRealmAbility(ra, true);
						client.Out.SendUpdatePoints();
						client.Out.SendUpdatePlayer();
						client.Out.SendUpdatePlayerSkills();
						client.Out.SendTrainerWindow();
					}
					else
					{
						client.Out.SendMessage("Unfortunately your training failed. Please report that to admins or game master. Thank you.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						log.Error("Realm Ability " + ra.Name + "(" + ra.KeyName + ") unexpected not found");
					}
				}
			}

			if (log.IsErrorEnabled)
				log.Error("Player <" + client.Player.Name + "> requested to train incorrect skill index");
		}
	}
}

/// <summary>
/// Handles Train clicks from Trainer Window
/// </summary>
[PacketHandler(EPacketHandlerType.TCP, EClientPackets.TrainHandler, "Handles Player Train", EClientStatus.PlayerInGame)]
public class PlayerTrainHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
        if (!Properties.ALLOW_TRAIN_ANYWHERE && client.Account.PrivLevel == (int)EPrivLevel.Player)
        {
            // A trainer of the appropriate class must be around (or global trainer, with TrainedClass = eCharacterClass.Unknow
            GameTrainer trainer = client.Player.TargetObject as GameTrainer;
            if (trainer == null || (trainer.CanTrain(client.Player) == false && trainer.CanTrainChampionLevels(client.Player) == false))
            {
                client.Out.SendMessage("You must select a valid trainer for your class.", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
                return;
            }
        }
		
		//Specializations - 8 trainable specs max
		uint size = 8;
		long position = packet.Position;
		IList<uint> skills = new List<uint>();
		Dictionary<uint, uint> amounts = new Dictionary<uint, uint>();
		bool stop = false;
		for (uint i = 0; i < size; i++)
		{
			uint code = packet.ReadInt();
			if (!stop)
			{
				if (code == 0xFFFFFFFF) stop = true;
				else
				{
					if (!skills.Contains(code))
						skills.Add(code);
				}
			}
		}

		foreach (uint code in skills)
		{
			uint val = packet.ReadInt();

			if (!amounts.ContainsKey(code) && val > 1)
				amounts.Add(code, val);
		}

		IList<Specialization> specs = client.Player.GetSpecList().Where(e => e.Trainable).ToList();
		uint skillcount = 0;
		IList<string> done = new List<string>();
		bool trained = false;
		
		// Graveen: the trainline command is called
		foreach (Specialization spec in specs)
		{
			if (amounts.ContainsKey(skillcount))
			{
				if (spec.Level < amounts[skillcount])
				{
					TrainCommand train = new TrainCommand(true);
					train.OnCommand(client, new string[] { "&trainline", spec.KeyName, amounts[skillcount].ToString() });
					trained = true;
				}
			}
			skillcount++;
		}

		//RealmAbilities
		packet.Seek(position + 64, System.IO.SeekOrigin.Begin);
		size = 50;//50 RA's max?
		amounts.Clear();
		for (uint i = 0; i < size; i++)
		{
			uint val = packet.ReadInt();

			if (val > 0 && !amounts.ContainsKey(i))
			{
				amounts.Add(i, val);
			}
		}

		if (amounts != null && amounts.Count > 0)
		{
			// Realm Abilities
			var raList = SkillBase.GetClassRealmAbilities(client.Player.PlayerClass.ID).Where(ra => !(ra is Rr5RealmAbility));
			foreach (var kv in amounts)
			{
				RealmAbility ra = raList.ElementAtOrDefault((int)kv.Key);
				if (ra != null)
				{
					RealmAbility playerRA = (RealmAbility)client.Player.GetAbility(ra.KeyName);
					
					if (playerRA != null && (playerRA.Level >= ra.MaxLevel || playerRA.Level >= kv.Value))
						continue;
					
					int cost = 0;
					for (int i = playerRA != null ? playerRA.Level : 0; i < kv.Value; i++)
						cost += ra.CostForUpgrade(i);
					
					if (client.Player.RealmSpecialtyPoints < cost)
					{
						client.Out.SendMessage(ra.Name + " costs " + (cost) + " realm ability points!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						client.Out.SendMessage("You don't have that many realm ability points left to get this.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						continue;
					}
					
					if (!ra.CheckRequirement(client.Player))
					{
						client.Out.SendMessage("You are not experienced enough to get " + ra.Name + " now. Come back later.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						continue;
					}
					
					bool valid = false;
					if (playerRA != null)
					{
						playerRA.Level = (int)kv.Value;
						valid = true;
					}
					else
					{
						ra.Level = (int)kv.Value;
						valid = true;
						client.Player.AddRealmAbility(ra, false);
					}
					
					if (valid)
					{
						client.Out.SendUpdatePoints();
						client.Out.SendUpdatePlayer();
						client.Out.SendCharResistsUpdate();
						client.Out.SendCharStatsUpdate();
						client.Out.SendUpdatePlayerSkills();
						client.Out.SendTrainerWindow();
						trained = true;
					}
					else
					{
						client.Out.SendMessage("Unfortunately your training failed. Please report that to admins or game master. Thank you.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
				}
				else
				{
					client.Out.SendMessage("Unfortunately your training failed. Please report that to admins or game master. Thank you.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
			}
		}
		if (trained)
			client.Player.SaveIntoDatabase();
	}
	
	/// <summary>
	/// Summon trainer window
	/// </summary>
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.TrainWindowHandler, "Call Player Train Window", EClientStatus.PlayerInGame)]
	public class PlayerTrainWindowHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			if (!Properties.ALLOW_TRAIN_ANYWHERE && client.Account.PrivLevel == (int)EPrivLevel.Player)
            {
                GameTrainer trainer = client.Player.TargetObject as GameTrainer;
                if (trainer == null || (trainer.CanTrain(client.Player) == false && trainer.CanTrainChampionLevels(client.Player) == false))
                {
                    client.Out.SendMessage("You must select a valid trainer for your class.", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
                    return;
                }
            }

			client.Out.SendTrainerWindow();
		}
	}
}