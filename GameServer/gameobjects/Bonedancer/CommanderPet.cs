using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	public class CommanderPet : BdPet
	{
		private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public readonly Lock ControlledNpcListLock = new();

		public enum eCommanderType
		{
			ReturnedCommander,
			DecayedCommander,
			SkeletalCommander,
			BoneCommander,
			DreadCommander,
			DreadArcher,
			DreadGuardian,
			DreadLich,
			DreadLord,
			Unknown
		}

		public eCommanderType CommanderType { get; protected set; }

		/// <summary>
		/// True when commander is taunting.
		/// </summary>
		public bool Taunting { get; protected set; } = false;


		/// <summary>
		/// Are the minions assisting this commander?
		/// </summary>
		public bool MinionsAssisting { get; protected set; } = true;

		/// <summary>
		/// Create a commander.
		/// </summary>
		/// <param name="npcTemplate"></param>
		/// <param name="owner"></param>
		public CommanderPet(INpcTemplate npcTemplate)
			: base(npcTemplate)
		{
			// Set the Commander Type
			string upperName = Name.ToUpper();

			if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.ReturnedCommander").ToUpper())
				CommanderType = eCommanderType.ReturnedCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DecayedCommander").ToUpper())
				CommanderType = eCommanderType.DecayedCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.SkeletalCommander").ToUpper())
				CommanderType = eCommanderType.SkeletalCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.BoneCommander").ToUpper())
				CommanderType = eCommanderType.BoneCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadCommander").ToUpper())
				CommanderType = eCommanderType.DreadCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadArcher").ToUpper())
				CommanderType = eCommanderType.DreadArcher;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadLich").ToUpper())
				CommanderType = eCommanderType.DreadLich;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadGuardian").ToUpper())
				CommanderType = eCommanderType.DreadGuardian;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadLord").ToUpper())
				CommanderType = eCommanderType.DreadLord;
			else
			{
				CommanderType = eCommanderType.Unknown;
				log.Warn($"CommanderPet(): unrecognized commander name {Name} in npcTemplate {npcTemplate.TemplateId}, is Name in server default language?");
			}

			// Determine how many subpets we can have, using TetherRange when possible
			if (npcTemplate.TetherRange > 0)
				InitControlledBrainArray(npcTemplate.TetherRange);
			else
				switch (CommanderType)
				{
					case eCommanderType.SkeletalCommander:
						InitControlledBrainArray(1);
						break;
					case eCommanderType.BoneCommander:
						InitControlledBrainArray(2);
						break;
					case eCommanderType.DreadCommander:
					case eCommanderType.DreadArcher:
					case eCommanderType.DreadLich:
					case eCommanderType.DreadGuardian:
						InitControlledBrainArray(3);
						break;
					case eCommanderType.DreadLord:
						InitControlledBrainArray(5);
						break;
					default:
						log.Warn($"CommanderPet(): npcTemplate for unknown commander {Name} does not specify number of subpets, defaulting to 0");
						InitControlledBrainArray(0);
						break;
				}

			// Choose a melee weapon
			switch (CommanderType)
			{
				case eCommanderType.DreadGuardian:
				case eCommanderType.DreadLich:
					CommanderSwitchWeapon(eWeaponType.Staff, false);
					break;
				default:
					bool oneHand = CanUseWeaponSlot(eActiveWeaponSlot.Standard);
					bool twoHand = CanUseWeaponSlot(eActiveWeaponSlot.TwoHanded);
					if (oneHand && twoHand)
						CommanderSwitchWeapon((eWeaponType)Util.Random((int)eWeaponType.OneHandAxe, (int)eWeaponType.TwoHandSword));
					else if (oneHand)
						CommanderSwitchWeapon((eWeaponType)Util.Random((int)eWeaponType.OneHandAxe, (int)eWeaponType.OneHandSword));
					else if (twoHand)
						CommanderSwitchWeapon((eWeaponType)Util.Random((int)eWeaponType.TwoHandAxe, (int)eWeaponType.TwoHandSword));
					break;
			}

			// Get a bow if we can use one
			if (CanUseWeaponSlot(eActiveWeaponSlot.Distance))
				CommanderSwitchWeapon(eWeaponType.Bow);
		}

		static CommanderPet()
		{
			// Get weapon templates, creating them if necessary.
			for (int i = (int)eWeaponType.MIN; i <= (int)eWeaponType.MAX; i++)
			{
				DbItemTemplate itemTemp = GameServer.Database.FindObjectByKey<DbItemTemplate>(WEAPON_KEYS[i]);

				if (itemTemp == null)
				{
					itemTemp = CreateBdPetWeapon((eWeaponType)i);

					if (itemTemp == null)
						log.Error($"Unable to find item template: {WEAPON_KEYS[i]}, could not create a new template.");
					else
						log.Info($"Unable to find item template: {WEAPON_KEYS[i]}, using default template.");
				}

				WEAPON_TEMPLATES.Add(itemTemp);
			}
		}

		/// <summary>
		/// Called when owner sends a whisper to the pet
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		/// <returns>True, if string needs further processing.</returns>
		public override bool WhisperReceive(GameLiving source, string str)
		{
			// Everything below this comment is added in 1.83, and should not exist in a strict 1.65 level. Feel free to add it back in if desired.
			return false;

			if (source is not GamePlayer player || player != Owner)
				return false;

			string[] strargs = str.ToUpper().Split(new char[] { ' ', '-' });

			for (int i = 0; i < strargs.Length; i++)
			{
				string curStr = strargs[i];

				if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Commander").ToUpper())
				{
					switch (CommanderType)
					{
						case eCommanderType.DreadGuardian:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Harm"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								eChatType.CT_Say, eChatLoc.CL_PopupWindow);
							break;
						case eCommanderType.DreadLich:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spells"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								eChatType.CT_Say, eChatLoc.CL_PopupWindow);
							break;
						case eCommanderType.DreadArcher:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadArcher",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								eChatType.CT_Say, eChatLoc.CL_PopupWindow);
							break;
						default:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.XCommander",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Weapons"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								eChatType.CT_Say, eChatLoc.CL_PopupWindow);
							break;
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat").ToUpper())
				{
					if (ControlledNpcList == null || ControlledNpcList.Length < 1)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.CombatNoMinions",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Taunt")),
							eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					else
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Combat",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Assist"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Taunt")),
							eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				} 

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Assist").ToUpper())
				{
					if (ControlledNpcList != null && ControlledNpcList.Length > 0)
					{
						MinionsAssisting = !MinionsAssisting;

						if (MinionsAssisting)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Assist.On"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
						else
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Assist.Off"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);

						// Refresh minion aggression state
						if (Brain is IControlledBrain commBrain)
							foreach (IControlledBrain minBrain in ControlledNpcList)
								if (minBrain != null)
									minBrain.SetAggressionState(commBrain.AggressionState);
					}
				}
				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Taunt").ToUpper())
				{
					Taunting = !Taunting;

					if (Taunting)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.CommStartTaunt"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
					else
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.CommNoTaunt"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Weapons").ToUpper())
				{
					bool oneHand = CanUseWeaponSlot(eActiveWeaponSlot.Standard);
					bool twoHand = CanUseWeaponSlot(eActiveWeaponSlot.TwoHanded);
					if (oneHand && twoHand)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Weapons.All",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedSword"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedSword")),
							eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					else if (oneHand)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Weapons.Limited",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedSword")),
							eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					else if (twoHand)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Weapons.Limited",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedSword")),
							eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spells").ToUpper())
				{
					if (CommanderType == eCommanderType.DreadLich)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich2",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Snares"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Debilitating"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Damage")),
							eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower").ToUpper())
				{
					bool found = false;
					switch (CommanderType)
					{
						case eCommanderType.DreadGuardian:
						case eCommanderType.DreadLich:
						case eCommanderType.DreadArcher:
							// Cast the first spell we find named "empower" in the server's default language;
							// Likely pointless, as the mob should already be casting self-buffs automatically
							foreach (Spell spell in Spells)
							{
								if (spell.Name.ToUpper() == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.Empower").ToUpper())
								{
									GameObject oldTarget = TargetObject;
									TargetObject = this;
									CastSpell(spell, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									TargetObject = oldTarget;
									found = true;
									break;
								}
							}
							if (!found)
								log.Warn("WhisperReceive() could not locate spell to empower pet");
							break;
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Snares").ToUpper())
				{
					if (CommanderType == eCommanderType.DreadLich)
					{
						if (CommSpellOther == null)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						else
						{
							PreferredSpell = eCommanderPreferredSpell.Other;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich.Snare", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						}
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Debilitating").ToUpper())
				{
					if (CommanderType == eCommanderType.DreadLich)
					{
						if (CommSpellDebuff == null)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						else
						{
							PreferredSpell = eCommanderPreferredSpell.Debuff;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich.Debilitating", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						}
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Damage").ToUpper())
				{
					if (CommanderType == eCommanderType.DreadLich)
					{
						if (CommSpellDamageDebuff != null || CommSpellDamage != null)
						{
							PreferredSpell = eCommanderPreferredSpell.Damage;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich.Damage", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						}
						else
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.One").ToUpper())
				{
					i++;
					if (i + 1 >= strargs.Length)
						return false;

					CommanderSwitchWeapon(eActiveWeaponSlot.Standard, strargs[++i]);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Two").ToUpper())
				{
					i++;
					if (i + 1 >= strargs.Length)
						return false;

					CommanderSwitchWeapon(eActiveWeaponSlot.TwoHanded, strargs[++i]);
				}

				// German and possibly other languages use a single word for weapons, so we have to check for that
				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedAxe").ToUpper())
					CommanderSwitchWeapon(eWeaponType.OneHandAxe);
				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedHammer").ToUpper())
					CommanderSwitchWeapon(eWeaponType.OneHandHammer);
				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedSword").ToUpper())
					CommanderSwitchWeapon(eWeaponType.OneHandSword);
				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedAxe").ToUpper())
					CommanderSwitchWeapon(eWeaponType.TwoHandAxe);
				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedHammer").ToUpper())
					CommanderSwitchWeapon(eWeaponType.TwoHandHammer);
				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedSword").ToUpper())
					CommanderSwitchWeapon(eWeaponType.TwoHandSword);

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Harm").ToUpper())
				{
					if (CommanderType == eCommanderType.DreadGuardian)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian2",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Drain"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Suppress")),
							eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Drain").ToUpper())
				{
					if (CommanderType == eCommanderType.DreadGuardian)
					{
						if (CommSpellOther == null)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						else
						{
							PreferredSpell = eCommanderPreferredSpell.Other;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian.Drain", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						}
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Suppress").ToUpper())
				{
					if (CommanderType == eCommanderType.DreadGuardian)
					{
						if (CommSpellDamageDebuff != null || CommSpellDamage != null)
						{
							PreferredSpell = eCommanderPreferredSpell.Damage;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian.Suppress", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
						}
						else
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
					}
				}
			}
			return base.WhisperReceive(source, str);
		}

		public override bool Interact(GamePlayer player)
        {
			return WhisperReceive(player, "commander");
        }

		#region Inventory
		public enum eWeaponType
		{
			None = -1,
			MIN = 0,
			Bow = 0,
			OneHandAxe = 1,
			OneHandHammer = 2,
			OneHandSword = 3,
			TwoHandAxe = 4,
			TwoHandHammer = 5,
			TwoHandSword = 6,
			Staff = 7,
			MAX = 7
		}

		private static readonly string[] WEAPON_KEYS =
		{
			"BD_Archer_Distance_bow",
			"BD_Pet_1H_Axe",
			"BD_Pet_1H_Hammer",
			"BD_Pet_1H_Sword",
			"BD_Pet_2H_Axe",
			"BD_Pet_2H_Hammer",
			"BD_Pet_2H_Sword",
			"BD_Pet_Staff"
		};

		private static readonly List<DbItemTemplate> WEAPON_TEMPLATES = new List<DbItemTemplate>((int)eWeaponType.MAX);

		/// <summary>
		/// Get the item template for a BD pet
		/// </summary>
		public static DbItemTemplate GetWeaponTemplate(eWeaponType type)
		{
			if (type < eWeaponType.MIN || type > eWeaponType.MAX)
				return null;

			return WEAPON_TEMPLATES[(int)type];
		}

		/// <summary>
		/// Create a new BD pet weapon using server default language to generate key and name.
		/// </summary>
		/// <param name="weaponType">Weapon key from GetWeaponKey()</param>
		/// <returns>New weapon item template, null if template could not be created</returns>
		public static DbItemTemplate CreateBdPetWeapon(eWeaponType weaponType)
		{
			DbItemTemplate temp = new DbItemTemplate();

			string weaponName;

			if (weaponType == eWeaponType.Bow)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "SkillBase.RegisterPropertyNames.ShortBow");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3467;
				temp.Object_Type = (int)eObjectType.CompositeBow;
				temp.Type_Damage = (int)eDamageType.Thrust;
				temp.SPD_ABS = 45;
				temp.Item_Type = (int)eInventorySlot.DistanceWeapon;
				temp.Hand = (int)eActiveWeaponSlot.Distance;
			}
			else if (weaponType == eWeaponType.OneHandAxe)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.1HandedAxe");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3469;
				temp.Object_Type = (int)eObjectType.Axe;
				temp.Type_Damage = (int)eDamageType.Slash;
				temp.SPD_ABS = 37;
				temp.Item_Type = (int)eInventorySlot.RightHandWeapon;
				temp.Hand = (int)eActiveWeaponSlot.Standard;
			}
			else if (weaponType == eWeaponType.OneHandHammer)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.1HandedHammer");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3466;
				temp.Object_Type = (int)eObjectType.Hammer;
				temp.Type_Damage = (int)eDamageType.Crush;
				temp.SPD_ABS = 37;
				temp.Item_Type = (int)eInventorySlot.RightHandWeapon;
				temp.Hand = (int)eActiveWeaponSlot.Standard;
			}
			else if (weaponType == eWeaponType.OneHandSword)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.1HandedSword");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3463;
				temp.Object_Type = (int)eObjectType.Sword;
				temp.Type_Damage = (int)eDamageType.Slash;
				temp.SPD_ABS = 34;
				temp.Item_Type = (int)eInventorySlot.RightHandWeapon;
				temp.Hand = (int)eActiveWeaponSlot.Standard;
			}
			else if (weaponType == eWeaponType.TwoHandAxe)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.2HandedAxe");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3468;
				temp.Object_Type = (int)eObjectType.Axe;
				temp.Type_Damage = (int)eDamageType.Slash;
				temp.SPD_ABS = 50;
				temp.Item_Type = (int)eInventorySlot.TwoHandWeapon;
				temp.Hand = (int)eActiveWeaponSlot.TwoHanded;
			}
			else if (weaponType == eWeaponType.TwoHandHammer)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.2HandedHammer");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3465;
				temp.Object_Type = (int)eObjectType.Hammer;
				temp.Type_Damage = (int)eDamageType.Crush;
				temp.SPD_ABS = 50;
				temp.Item_Type = (int)eInventorySlot.TwoHandWeapon;
				temp.Hand = (int)eActiveWeaponSlot.TwoHanded;
			}
			else if (weaponType == eWeaponType.TwoHandSword)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.2HandedSword");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3462;
				temp.Object_Type = (int)eObjectType.Sword;
				temp.Type_Damage = (int)eDamageType.Slash;
				temp.SPD_ABS = 45;
				temp.Item_Type = (int)eInventorySlot.TwoHandWeapon;
				temp.Hand = (int)eActiveWeaponSlot.TwoHanded;
			}
			else if (weaponType == eWeaponType.Staff)
			{
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "SkillBase.RegisterPropertyNames.Staff");
				temp.Model = 3464;
				temp.Object_Type = (int)eObjectType.Staff;
				temp.Type_Damage = (int)eDamageType.Crush;
				temp.SPD_ABS = 50;
				temp.Item_Type = (int)eInventorySlot.TwoHandWeapon;
				temp.Hand = (int)eActiveWeaponSlot.TwoHanded;
			}
			else
				return null;

			string bonedancer = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "PlayerClass.Name.Bonedancer");
			temp.Name = $"{bonedancer} {weaponName}";
			temp.IsDropable = false;
			temp.IsPickable = false;

			return temp;
		}

		/// <summary>
		/// Can the pet swap this eActiveWeaponSlot?
		/// </summary>
		/// <param name="slot">Which slot to check</param>
		protected bool CanUseWeaponSlot(eActiveWeaponSlot slot)
		{
			switch (CommanderType)
			{
				case eCommanderType.ReturnedCommander:
				case eCommanderType.DecayedCommander:
					return slot == eActiveWeaponSlot.Standard;
				case eCommanderType.SkeletalCommander:
				case eCommanderType.BoneCommander:
				case eCommanderType.DreadCommander:
					return slot == eActiveWeaponSlot.Standard || slot == eActiveWeaponSlot.TwoHanded;
				case eCommanderType.DreadArcher:
					return slot == eActiveWeaponSlot.Standard || slot == eActiveWeaponSlot.Distance;
				case eCommanderType.DreadGuardian:
				case eCommanderType.DreadLich:
				case eCommanderType.DreadLord:
					// These pets can't change weapons at all
					return false;
				default:
					// Pets we don't recognize can swap weapons populated by their npcTemplate.EquipmentTemplateID
					if (Inventory != null)
						switch (slot)
						{
							case eActiveWeaponSlot.Distance:
								return Inventory.GetItem(eInventorySlot.DistanceWeapon) != null;
							case eActiveWeaponSlot.Standard:
								return Inventory.GetItem(eInventorySlot.RightHandWeapon) != null;
							case eActiveWeaponSlot.TwoHanded:
								return Inventory.GetItem(eInventorySlot.TwoHandWeapon) != null;
						}
					return false;
			}
		}

		/// <summary>
		/// Changes the commander's weapon to the specified weapon spec and slot
		/// </summary>
		/// <param name="slot">Weapon slot</param>
		/// <param name="weaponSpec">Weapon spec name</param>
		/// <returns></returns>
		public void CommanderSwitchWeapon(eActiveWeaponSlot slot, string weaponSpec)
		{
			if (!CanUseWeaponSlot(slot))
				return;

			if (slot == eActiveWeaponSlot.Distance)
				CommanderSwitchWeapon(eWeaponType.Bow);

			if (Owner is GamePlayer player)
			{
				string upperWeapon = weaponSpec.ToUpper();

				if (slot == eActiveWeaponSlot.Standard)
				{
					if (upperWeapon == LanguageMgr.GetTranslation(player.Client.Account.Language, "SkillBase.RegisterPropertyNames.Axe").ToUpper())
						CommanderSwitchWeapon(eWeaponType.OneHandAxe);

					if (upperWeapon == LanguageMgr.GetTranslation(player.Client.Account.Language, "SkillBase.RegisterPropertyNames.Hammer").ToUpper())
						CommanderSwitchWeapon(eWeaponType.OneHandHammer);

					if (upperWeapon == LanguageMgr.GetTranslation(player.Client.Account.Language, "SkillBase.RegisterPropertyNames.Sword").ToUpper())
						CommanderSwitchWeapon(eWeaponType.OneHandSword);
				}
				else
				{
					if (upperWeapon == LanguageMgr.GetTranslation(player.Client.Account.Language, "SkillBase.RegisterPropertyNames.Axe").ToUpper())
						CommanderSwitchWeapon(eWeaponType.TwoHandAxe);

					if (upperWeapon == LanguageMgr.GetTranslation(player.Client.Account.Language, "SkillBase.RegisterPropertyNames.Hammer").ToUpper())
						CommanderSwitchWeapon(eWeaponType.TwoHandHammer);

					if (upperWeapon == LanguageMgr.GetTranslation(player.Client.Account.Language, "SkillBase.RegisterPropertyNames.Sword").ToUpper())
						CommanderSwitchWeapon(eWeaponType.TwoHandSword);
				}
			}
		}

		/// <summary>
		/// Switch the commander to the specified weapon type
		/// </summary>
		/// <param name="checkCanUse">Make sure the commander can use this weapon type?</param>
		public void CommanderSwitchWeapon(eWeaponType weaponType, bool checkCanUse = true)
		{
			DbItemTemplate itemTemp = GetWeaponTemplate(weaponType);

			if (itemTemp == null || (checkCanUse && !CanUseWeaponSlot((eActiveWeaponSlot)itemTemp.Hand)))
				return;

			DbInventoryItem weapon;

			weapon = GameInventoryItem.Create(itemTemp);
			if (weapon != null)
			{
				if (Inventory == null)
					Inventory = new GameNPCInventory(new GameNpcInventoryTemplate());
				else
				{
					if (itemTemp.Hand == (int)eActiveWeaponSlot.Distance)
						Inventory.RemoveItem(Inventory.GetItem(eInventorySlot.DistanceWeapon));
					else if (CommanderType == eCommanderType.Unknown)
					{
						// Only empty the slot we're about to fill, as we check the other weapon slots
						//	to determine which weapon types unknown commanders can switch to
						Inventory.RemoveItem(Inventory.GetItem((eInventorySlot)weapon.Item_Type));
					}
					else
					{
						// Remove melee weapons
						Inventory.RemoveItem(Inventory.GetItem(eInventorySlot.RightHandWeapon));
						Inventory.RemoveItem(Inventory.GetItem(eInventorySlot.TwoHandWeapon));
					}

				}

				Inventory.AddItem((eInventorySlot)weapon.Item_Type, weapon);

				// If we've got a ranged weapon, keep using it
				if (ActiveWeaponSlot == eActiveWeaponSlot.Distance)
					BroadcastLivingEquipmentUpdate();
				else
					SwitchWeapon((eActiveWeaponSlot)weapon.Hand);
			}
		}
		#endregion

		#region Spells
		// Dread Guardian & Lich spell lists
		public Spell CommSpellDamage { protected set; get; } = null;
		public Spell CommSpellDamageDebuff { protected set; get; } = null;
		public Spell CommSpellDebuff { protected set; get; } = null;
		public Spell CommSpellDot { protected set; get; } = null;
		public Spell CommSpellOther { protected set; get; } = null;

		public enum eCommanderPreferredSpell
		{
			Damage,
			Debuff,
			Other, // Lifedrain for guardian, dd+snare for lich
			None // No selected spell, use base CheckSpells()
		}

		// Both guardian and lich default to debuff spells
		public eCommanderPreferredSpell PreferredSpell { protected set; get; } = eCommanderPreferredSpell.None;
		/// <summary>
		/// Sort damaging spells into specialized lists for Guardians and Liches
		/// </summary>
		public override void SortSpells()
		{
			if (Spells.Count < 1)
				return;

			base.SortSpells();

			if (HarmfulSpells != null && HarmfulSpells.Count < 1)
				return;

			if (CommanderType != eCommanderType.DreadGuardian && CommanderType != eCommanderType.DreadLich)
				return;

			CommSpellDamage = null;
			CommSpellDamageDebuff = null;
			CommSpellDebuff = null;
			CommSpellOther = null;

			foreach (Spell spell in HarmfulSpells)
			{
				if (spell != null)
				{
					switch (spell.SpellType)
					{
                        case eSpellType.DamageOverTime:
							CommSpellDot = spell;
							break;
                        case eSpellType.DirectDamage:
							CommSpellDamage = spell;
							break;
                        case eSpellType.DirectDamageWithDebuff:
							CommSpellDamageDebuff = spell;
							break;
                        case eSpellType.Disease:
							CommSpellDebuff = spell;
							break;
                        case eSpellType.Lifedrain:
                        case eSpellType.DamageSpeedDecrease:
							CommSpellOther = spell;
							break;
					}
				}
			}

			// Remove selected spells so the base method doesn't have to deal with them
			if (CommSpellDamage != null)
				HarmfulSpells.Remove(CommSpellDamage);
			if (CommSpellDamageDebuff != null)
				HarmfulSpells.Remove(CommSpellDamageDebuff);
			if (CommSpellDebuff != null)
				HarmfulSpells.Remove(CommSpellDebuff);
			if (CommSpellDot != null)
				HarmfulSpells.Remove(CommSpellDot);
			if (CommSpellOther != null)
				HarmfulSpells.Remove(CommSpellOther);
			HarmfulSpells.TrimExcess();

			// Make sure we have a valid preferred spell
			switch (PreferredSpell)
			{
				case eCommanderPreferredSpell.Damage:
					if (CommSpellDamage == null && CommSpellDamageDebuff == null && CommSpellDot == null)
						PreferredSpell = eCommanderPreferredSpell.None;
					break;
				case eCommanderPreferredSpell.Debuff:
					if (CommSpellDebuff == null)
						PreferredSpell = eCommanderPreferredSpell.None;
					break;
				case eCommanderPreferredSpell.Other:
					if (CommSpellOther == null)
						PreferredSpell = eCommanderPreferredSpell.None;
					break;
			}

			// Pick a valid default preferred spell
			if (PreferredSpell == eCommanderPreferredSpell.None)
			{
				if (CommSpellDebuff != null)
					PreferredSpell = eCommanderPreferredSpell.Debuff;
				else if (CommSpellDamage != null || CommSpellDamageDebuff != null || CommSpellDot != null)
					PreferredSpell = eCommanderPreferredSpell.Damage;
				else if (CommSpellOther != null)
					PreferredSpell = eCommanderPreferredSpell.Other;
			}
		}
		#endregion

		/// <summary>
		/// Adds a pet to the current array of pets
		/// </summary>
		/// <param name="controlledBrain">The brain to add to the list</param>
		/// <returns>Whether the pet was added or not</returns>
		public override bool AddControlledBrain(IControlledBrain controlledBrain)
		{
			lock (ControlledNpcListLock)
			{
				if (ControlledNpcList == null)
					return false;

				bool foundSpot = false;

				for (int i = 0; i < ControlledNpcList.Length; i++)
				{
					if (ControlledNpcList[i] == null)
					{
						foundSpot = true;
						ControlledNpcList[i] = controlledBrain;

						if (Brain is IControlledBrain commanderBrain)
							controlledBrain.SetAggressionState(commanderBrain.AggressionState);

						UpdatePetCount(controlledBrain.Body as GameSummonedPet, true);
						break;
					}
				}

				return foundSpot;
			}
		}

		/// <summary>
		/// Removes the brain from
		/// </summary>
		/// <param name="controlledBrain">The brain to find and remove</param>
		/// <returns>Whether the pet was removed</returns>
		public override bool RemoveControlledBrain(IControlledBrain controlledBrain)
		{
			bool foundBrain = false;

			lock (ControlledNpcListLock)
			{
				if (controlledBrain == null)
					return false;

				for (int i = 0; i < ControlledNpcList.Length; i++)
				{
					if (ControlledNpcList[i] == controlledBrain)
					{
						foundBrain = true;

						if (controlledBrain is ControlledMobBrain controlledNpcBrain)
							controlledNpcBrain.StripCastedBuffs();

						ControlledNpcList[i] = null;
						UpdatePetCount(controlledBrain.Body as GameSummonedPet, false);
						break;
					}
				}
			}

			return foundBrain;
		}
	}
}
