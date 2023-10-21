using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
	public class CommanderPet : BonedancerPet
	{
		private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ECommanderType CommanderType { get; protected set; }

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
				CommanderType = ECommanderType.ReturnedCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DecayedCommander").ToUpper())
				CommanderType = ECommanderType.DecayedCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.SkeletalCommander").ToUpper())
				CommanderType = ECommanderType.SkeletalCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.BoneCommander").ToUpper())
				CommanderType = ECommanderType.BoneCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadCommander").ToUpper())
				CommanderType = ECommanderType.DreadCommander;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadArcher").ToUpper())
				CommanderType = ECommanderType.DreadArcher;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadLich").ToUpper())
				CommanderType = ECommanderType.DreadLich;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadGuardian").ToUpper())
				CommanderType = ECommanderType.DreadGuardian;
			else if (upperName == LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.DreadLord").ToUpper())
				CommanderType = ECommanderType.DreadLord;
			else
			{
				CommanderType = ECommanderType.Unknown;
				log.Warn($"CommanderPet(): unrecognized commander name {Name} in npcTemplate {npcTemplate.TemplateId}, is Name in server default language?");
			}

			// Determine how many subpets we can have, using TetherRange when possible
			if (npcTemplate.TetherRange > 0)
				InitControlledBrainArray(npcTemplate.TetherRange);
			else
				switch (CommanderType)
				{
					case ECommanderType.SkeletalCommander:
						InitControlledBrainArray(1);
						break;
					case ECommanderType.BoneCommander:
						InitControlledBrainArray(2);
						break;
					case ECommanderType.DreadCommander:
					case ECommanderType.DreadArcher:
					case ECommanderType.DreadLich:
					case ECommanderType.DreadGuardian:
						InitControlledBrainArray(3);
						break;
					case ECommanderType.DreadLord:
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
				case ECommanderType.DreadGuardian:
				case ECommanderType.DreadLich:
					CommanderSwitchWeapon(eWeaponType.Staff, false);
					break;
				default:
					bool oneHand = CanUseWeaponSlot(EActiveWeaponSlot.Standard);
					bool twoHand = CanUseWeaponSlot(EActiveWeaponSlot.TwoHanded);
					if (oneHand && twoHand)
						CommanderSwitchWeapon((eWeaponType)Util.Random((int)eWeaponType.OneHandAxe, (int)eWeaponType.TwoHandSword));
					else if (oneHand)
						CommanderSwitchWeapon((eWeaponType)Util.Random((int)eWeaponType.OneHandAxe, (int)eWeaponType.OneHandSword));
					else if (twoHand)
						CommanderSwitchWeapon((eWeaponType)Util.Random((int)eWeaponType.TwoHandAxe, (int)eWeaponType.TwoHandSword));
					break;
			}

			// Get a bow if we can use one
			if (CanUseWeaponSlot(EActiveWeaponSlot.Distance))
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
						case ECommanderType.DreadGuardian:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Harm"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								EChatType.CT_Say, EChatLoc.CL_PopupWindow);
							break;
						case ECommanderType.DreadLich:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spells"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								EChatType.CT_Say, EChatLoc.CL_PopupWindow);
							break;
						case ECommanderType.DreadArcher:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadArcher",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								EChatType.CT_Say, EChatLoc.CL_PopupWindow);
							break;
						default:
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.XCommander",
								Name,
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Weapons"),
								LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat")),
								EChatType.CT_Say, EChatLoc.CL_PopupWindow);
							break;
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Combat").ToUpper())
				{
					if (ControlledNpcList == null || ControlledNpcList.Length < 1)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.CombatNoMinions",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Taunt")),
							EChatType.CT_Say, EChatLoc.CL_PopupWindow);
					else
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Combat",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Assist"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Taunt")),
							EChatType.CT_Say, EChatLoc.CL_PopupWindow);
				} 

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Assist").ToUpper())
				{
					if (ControlledNpcList != null && ControlledNpcList.Length > 0)
					{
						MinionsAssisting = !MinionsAssisting;

						if (MinionsAssisting)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Assist.On"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
						else
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Assist.Off"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);

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
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.CommStartTaunt"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
					else
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.CommNoTaunt"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Weapons").ToUpper())
				{
					bool oneHand = CanUseWeaponSlot(EActiveWeaponSlot.Standard);
					bool twoHand = CanUseWeaponSlot(EActiveWeaponSlot.TwoHanded);
					if (oneHand && twoHand)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Weapons.All",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedSword"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedSword")),
							EChatType.CT_Say, EChatLoc.CL_PopupWindow);
					else if (oneHand)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Weapons.Limited",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.1HandedSword")),
							EChatType.CT_Say, EChatLoc.CL_PopupWindow);
					else if (twoHand)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Weapons.Limited",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedAxe"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedHammer"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.2HandedSword")),
							EChatType.CT_Say, EChatLoc.CL_PopupWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spells").ToUpper())
				{
					if (CommanderType == ECommanderType.DreadLich)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich2",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Snares"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Debilitating"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Damage")),
							EChatType.CT_Say, EChatLoc.CL_PopupWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Empower").ToUpper())
				{
					bool found = false;
					switch (CommanderType)
					{
						case ECommanderType.DreadGuardian:
						case ECommanderType.DreadLich:
						case ECommanderType.DreadArcher:
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
					if (CommanderType == ECommanderType.DreadLich)
					{
						if (CommSpellOther == null)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						else
						{
							PreferredSpell = eCommanderPreferredSpell.Other;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich.Snare", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						}
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Debilitating").ToUpper())
				{
					if (CommanderType == ECommanderType.DreadLich)
					{
						if (CommSpellDebuff == null)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						else
						{
							PreferredSpell = eCommanderPreferredSpell.Debuff;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich.Debilitating", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						}
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Damage").ToUpper())
				{
					if (CommanderType == ECommanderType.DreadLich)
					{
						if (CommSpellDamageDebuff != null || CommSpellDamage != null)
						{
							PreferredSpell = eCommanderPreferredSpell.Damage;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadLich.Damage", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						}
						else
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.One").ToUpper())
				{
					i++;
					if (i + 1 >= strargs.Length)
						return false;

					CommanderSwitchWeapon(EActiveWeaponSlot.Standard, strargs[++i]);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Two").ToUpper())
				{
					i++;
					if (i + 1 >= strargs.Length)
						return false;

					CommanderSwitchWeapon(EActiveWeaponSlot.TwoHanded, strargs[++i]);
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
					if (CommanderType == ECommanderType.DreadGuardian)
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian2",
							Name,
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Drain"),
							LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Suppress")),
							EChatType.CT_Say, EChatLoc.CL_PopupWindow);
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Drain").ToUpper())
				{
					if (CommanderType == ECommanderType.DreadGuardian)
					{
						if (CommSpellOther == null)
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						else
						{
							PreferredSpell = eCommanderPreferredSpell.Other;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian.Drain", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						}
					}
				}

				else if (curStr == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Suppress").ToUpper())
				{
					if (CommanderType == ECommanderType.DreadGuardian)
					{
						if (CommSpellDamageDebuff != null || CommSpellDamage != null)
						{
							PreferredSpell = eCommanderPreferredSpell.Damage;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.DreadGuardian.Suppress", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
						}
						else
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObjects.CommanderPet.WR.Const.Spell.NotAvailable", Name), EChatType.CT_Say, EChatLoc.CL_PopupWindow);
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
				temp.Object_Type = (int)EObjectType.CompositeBow;
				temp.Type_Damage = (int)EDamageType.Thrust;
				temp.SPD_ABS = 45;
				temp.Item_Type = (int)EInventorySlot.DistanceWeapon;
				temp.Hand = (int)EActiveWeaponSlot.Distance;
			}
			else if (weaponType == eWeaponType.OneHandAxe)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.1HandedAxe");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3469;
				temp.Object_Type = (int)EObjectType.Axe;
				temp.Type_Damage = (int)EDamageType.Slash;
				temp.SPD_ABS = 37;
				temp.Item_Type = (int)EInventorySlot.RightHandWeapon;
				temp.Hand = (int)EActiveWeaponSlot.Standard;
			}
			else if (weaponType == eWeaponType.OneHandHammer)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.1HandedHammer");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3466;
				temp.Object_Type = (int)EObjectType.Hammer;
				temp.Type_Damage = (int)EDamageType.Crush;
				temp.SPD_ABS = 37;
				temp.Item_Type = (int)EInventorySlot.RightHandWeapon;
				temp.Hand = (int)EActiveWeaponSlot.Standard;
			}
			else if (weaponType == eWeaponType.OneHandSword)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.1HandedSword");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3463;
				temp.Object_Type = (int)EObjectType.Sword;
				temp.Type_Damage = (int)EDamageType.Slash;
				temp.SPD_ABS = 34;
				temp.Item_Type = (int)EInventorySlot.RightHandWeapon;
				temp.Hand = (int)EActiveWeaponSlot.Standard;
			}
			else if (weaponType == eWeaponType.TwoHandAxe)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.2HandedAxe");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3468;
				temp.Object_Type = (int)EObjectType.Axe;
				temp.Type_Damage = (int)EDamageType.Slash;
				temp.SPD_ABS = 50;
				temp.Item_Type = (int)EInventorySlot.TwoHandWeapon;
				temp.Hand = (int)EActiveWeaponSlot.TwoHanded;
			}
			else if (weaponType == eWeaponType.TwoHandHammer)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.2HandedHammer");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3465;
				temp.Object_Type = (int)EObjectType.Hammer;
				temp.Type_Damage = (int)EDamageType.Crush;
				temp.SPD_ABS = 50;
				temp.Item_Type = (int)EInventorySlot.TwoHandWeapon;
				temp.Hand = (int)EActiveWeaponSlot.TwoHanded;
			}
			else if (weaponType == eWeaponType.TwoHandSword)
			{
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "GameObjects.CommanderPet.WR.Const.2HandedSword");
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				temp.Model = 3462;
				temp.Object_Type = (int)EObjectType.Sword;
				temp.Type_Damage = (int)EDamageType.Slash;
				temp.SPD_ABS = 45;
				temp.Item_Type = (int)EInventorySlot.TwoHandWeapon;
				temp.Hand = (int)EActiveWeaponSlot.TwoHanded;
			}
			else if (weaponType == eWeaponType.Staff)
			{
				temp.Id_nb = WEAPON_KEYS[(int)weaponType];
				weaponName = LanguageMgr.GetTranslation(LanguageMgr.DefaultLanguage, "SkillBase.RegisterPropertyNames.Staff");
				temp.Model = 3464;
				temp.Object_Type = (int)EObjectType.Staff;
				temp.Type_Damage = (int)EDamageType.Crush;
				temp.SPD_ABS = 50;
				temp.Item_Type = (int)EInventorySlot.TwoHandWeapon;
				temp.Hand = (int)EActiveWeaponSlot.TwoHanded;
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
		protected bool CanUseWeaponSlot(EActiveWeaponSlot slot)
		{
			switch (CommanderType)
			{
				case ECommanderType.ReturnedCommander:
				case ECommanderType.DecayedCommander:
					return slot == EActiveWeaponSlot.Standard;
				case ECommanderType.SkeletalCommander:
				case ECommanderType.BoneCommander:
				case ECommanderType.DreadCommander:
					return slot == EActiveWeaponSlot.Standard || slot == EActiveWeaponSlot.TwoHanded;
				case ECommanderType.DreadArcher:
					return slot == EActiveWeaponSlot.Standard || slot == EActiveWeaponSlot.Distance;
				case ECommanderType.DreadGuardian:
				case ECommanderType.DreadLich:
				case ECommanderType.DreadLord:
					// These pets can't change weapons at all
					return false;
				default:
					// Pets we don't recognize can swap weapons populated by their npcTemplate.EquipmentTemplateID
					if (Inventory != null)
						switch (slot)
						{
							case EActiveWeaponSlot.Distance:
								return Inventory.GetItem(EInventorySlot.DistanceWeapon) != null;
							case EActiveWeaponSlot.Standard:
								return Inventory.GetItem(EInventorySlot.RightHandWeapon) != null;
							case EActiveWeaponSlot.TwoHanded:
								return Inventory.GetItem(EInventorySlot.TwoHandWeapon) != null;
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
		public void CommanderSwitchWeapon(EActiveWeaponSlot slot, string weaponSpec)
		{
			if (!CanUseWeaponSlot(slot))
				return;

			if (slot == EActiveWeaponSlot.Distance)
				CommanderSwitchWeapon(eWeaponType.Bow);

			if (Owner is GamePlayer player)
			{
				string upperWeapon = weaponSpec.ToUpper();

				if (slot == EActiveWeaponSlot.Standard)
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

			if (itemTemp == null || (checkCanUse && !CanUseWeaponSlot((EActiveWeaponSlot)itemTemp.Hand)))
				return;

			DbInventoryItem weapon;

			weapon = GameInventoryItem.Create(itemTemp);
			if (weapon != null)
			{
				if (Inventory == null)
					Inventory = new GameNpcInventory(new GameNpcInventoryTemplate());
				else
				{
					if (itemTemp.Hand == (int)EActiveWeaponSlot.Distance)
						Inventory.RemoveItem(Inventory.GetItem(EInventorySlot.DistanceWeapon));
					else if (CommanderType == ECommanderType.Unknown)
					{
						// Only empty the slot we're about to fill, as we check the other weapon slots
						//	to determine which weapon types unknown commanders can switch to
						Inventory.RemoveItem(Inventory.GetItem((EInventorySlot)weapon.Item_Type));
					}
					else
					{
						// Remove melee weapons
						Inventory.RemoveItem(Inventory.GetItem(EInventorySlot.RightHandWeapon));
						Inventory.RemoveItem(Inventory.GetItem(EInventorySlot.TwoHandWeapon));
					}

				}

				Inventory.AddItem((EInventorySlot)weapon.Item_Type, weapon);

				// If we've got a ranged weapon, keep using it
				if (ActiveWeaponSlot == EActiveWeaponSlot.Distance)
					BroadcastLivingEquipmentUpdate();
				else
					SwitchWeapon((EActiveWeaponSlot)weapon.Hand);
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

			if (CommanderType != ECommanderType.DreadGuardian && CommanderType != ECommanderType.DreadLich)
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
                        case ESpellType.DamageOverTime:
							CommSpellDot = spell;
							break;
                        case ESpellType.DirectDamage:
							CommSpellDamage = spell;
							break;
                        case ESpellType.DirectDamageWithDebuff:
							CommSpellDamageDebuff = spell;
							break;
                        case ESpellType.Disease:
							CommSpellDebuff = spell;
							break;
                        case ESpellType.Lifedrain:
                        case ESpellType.DamageSpeedDecrease:
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
		/// <param name="controlledNpc">The brain to add to the list</param>
		/// <returns>Whether the pet was added or not</returns>
		public override bool AddControlledNpc(IControlledBrain controlledNpc)
		{
			IControlledBrain[] brainlist = ControlledNpcList;
			if (brainlist == null) return false;
			foreach (IControlledBrain icb in brainlist)
			{
				if (icb == controlledNpc)
					return false;
			}

			if (controlledNpc.Owner != this)
				throw new ArgumentException("ControlledNpc with wrong owner is set (player=" + Name + ", owner=" + controlledNpc.Owner.Name + ")", "controlledNpc");

			//Find the next spot for this new pet
			int i = 0;
			for (; i < brainlist.Length; i++)
			{
				if (brainlist[i] == null)
					break;
			}
			//If we didn't find a spot return false
			if (i >= m_controlledBrain.Length)
				return false;
			m_controlledBrain[i] = controlledNpc;
			UpdatePetCount(true);
			return base.AddControlledNpc(controlledNpc);
		}

		/// <summary>
		/// Removes the brain from
		/// </summary>
		/// <param name="controlledNpc">The brain to find and remove</param>
		/// <returns>Whether the pet was removed</returns>
		public override bool RemoveControlledNpc(IControlledBrain controlledNpc)
		{
			bool found = false;
			lock (ControlledNpcList)
			{
				if (controlledNpc == null) return false;
				IControlledBrain[] brainlist = ControlledNpcList;
				int i = 0;

				//Try to find the minion in the list
				for (; i < brainlist.Length; i++)
				{
					//Found it
					if (brainlist[i] == controlledNpc)
					{
						found = true;
						break;
					}
				}

				//Found it, lets remove it
				if (found)
				{
					if (controlledNpc.Body.Brain is ControlledNpcBrain controlledNpcBrain)
						controlledNpcBrain.StripCastedBuffs();

					m_controlledBrain[i] = null;
					UpdatePetCount(false);

					return base.RemoveControlledNpc(controlledNpc);
				}
			}

			return found;
		}
	}
}
