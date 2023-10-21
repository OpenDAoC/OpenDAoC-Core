using System;
using System.Collections.Generic;
using System.Reflection;
using Core.GS.ECS;
using Core.GS.ServerProperties;
using Core.GS.Styles;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.UseSkill, "Handles Player Use Skill Request.", EClientStatus.PlayerInGame)]
	public class UseSkillHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			if (client.Version >= GameClient.eClientVersion.Version1124)
			{
				client.Player.X = (int)packet.ReadFloatLowEndian();
				client.Player.Y = (int)packet.ReadFloatLowEndian();
				client.Player.Z = (int)packet.ReadFloatLowEndian();
				client.Player.CurrentSpeed = (short)packet.ReadFloatLowEndian();
				client.Player.Heading = packet.ReadShort();
			}
			int flagSpeedData = packet.ReadShort();
			int index = packet.ReadByte();
			int type = packet.ReadByte();

			// new UseSkillAction(client.Player, flagSpeedData, index, type).Start(1);

			ProcessPacket(client.Player, flagSpeedData, index, type);
		}


		public void ProcessPacket(GamePlayer player, int flagSpeedData, int index, int type)
		{
			
			if (player == null)
					return;

				// Commenting out. 'flagSpeedData' doesn't vary with movement speed, and this stops the player for a fraction of a second.
				//if ((flagSpeedData & 0x200) != 0)
				//{
				//	player.CurrentSpeed = (short)(-(flagSpeedData & 0x1ff)); // backward movement
				//}
				//else
				//{
				//	player.CurrentSpeed = (short)(flagSpeedData & 0x1ff); // forwardmovement
				//}

				player.IsStrafing = (flagSpeedData & 0x4000) != 0;
				player.TargetInView = (flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
				player.GroundTargetInView = ((flagSpeedData & 0x1000) != 0);

				List<Tuple<Skill, Skill>> snap = player.GetAllUsableSkills();
				
				Skill sk = null;
				Skill sksib = null;
				
				// we're not using a spec !
				if (type > 0)
				{
					
					// find the first non-specialization index.
					int begin = Math.Max(0, snap.FindIndex(it => (it.Item1 is Specialization) == false));
					
					// are we in list ?
					if (index + begin < snap.Count)
					{
						sk = snap[index + begin].Item1;
						sksib = snap[index + begin].Item2;
					}
					
				}
				else
				{
					// mostly a spec !
					if (index < snap.Count)
					{
						sk = snap[index].Item1;
						sksib = snap[index].Item2;
					}
				}

				// we really got a skill !
				if (sk != null)
				{
					// Test if we can use it !
					int reuseTime = player.GetSkillDisabledDuration(sk);

					if (reuseTime > 60000)
					{
						player.Out.SendMessage(
							string.Format("You must wait {0} minutes {1} seconds to use this ability!", reuseTime/60000, reuseTime%60000/1000),
							EChatType.CT_System, EChatLoc.CL_SystemWindow);
						
						if (player.Client.Account.PrivLevel < 2)
							return;
					}
					else if (reuseTime > 0)
					{
						// Allow Pulse Spells to be canceled while they are on reusetimer
						if (sk is Spell spell && spell.IsPulsing && player.ActivePulseSpells.ContainsKey(spell.SpellType))
						{
							EcsPulseEffect effect = EffectListService.GetPulseEffectOnTarget(player, spell);
							EffectService.RequestImmediateCancelConcEffect(effect);

							if (spell.InstrumentRequirement == 0)
								player.Out.SendMessage("You cancel your effect.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
							else
								player.Out.SendMessage("You stop playing your song.", EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
						}
						else
							player.Out.SendMessage(string.Format("You must wait {0} seconds to use this ability!", reuseTime / 1000 + 1), EChatType.CT_System, EChatLoc.CL_SystemWindow);

						if (player.Client.Account.PrivLevel < 2)
							return;
					}

					// See what we should do depending on skill type !
					if (sk is Specialization specialization)
					{
						ISpecActionHandler handler = SkillBase.GetSpecActionHandler(specialization.KeyName);
						handler?.Execute(specialization, player);
					}
					else if (sk is Ability ability)
					{
						IAbilityActionHandler handler = SkillBase.GetAbilityActionHandler(ability.KeyName);

						if (handler != null)
						{
							handler.Execute(ability, player);
							return;
						}

						ability.Execute(player);
					}
					else if (sk is Spell spell)
					{
						if (sksib is SpellLine spellLine)
							player.castingComponent.RequestStartCastSpell(spell, spellLine);
					}
					else if (sk is Style style)
					{
						if (player.styleComponent.AwaitingBackupInput && Properties.ALLOW_AUTO_BACKUP_STYLES)
						{
							player.styleComponent.AwaitingBackupInput = false;
							
							if (!Properties.ALLOW_NON_ANYTIME_BACKUP_STYLES && (style.AttackResultRequirement != Style.eAttackResultRequirement.Any || style.OpeningRequirementType == Style.eOpening.Positional))
							{
								player.Out.SendMessage($"You must use an anytime style as your backup.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
								return;
							}

							player.Out.SendMessage($"You will now use {style.Name} as your backup.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							player.styleComponent.AutomaticBackupStyle = style;
							return;

						}
						player.styleComponent.ExecuteWeaponStyle(style);
					}
				}

				if (sk == null)
					player.Out.SendMessage("Skill is not implemented.", EChatType.CT_Advise, EChatLoc.CL_SystemWindow);
		}

		/// <summary>
		/// Handles player use skill actions
		/// </summary>
		protected class UseSkillAction : EcsGameTimerWrapperBase
		{
			/// <summary>
			/// The speed and flags data
			/// </summary>
			protected readonly int m_flagSpeedData;

			/// <summary>
			/// The skill index
			/// </summary>
			protected readonly int m_index;

			/// <summary>
			/// The skill type
			/// </summary>
			protected readonly int m_type;

			/// <summary>
			/// Constructs a new UseSkillAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="flagSpeedData">The skill type</param>
			/// <param name="index">The skill index</param>
			/// <param name="type">The skill type</param>
			public UseSkillAction(GamePlayer actionSource, int flagSpeedData, int index, int type) : base(actionSource)
			{
				m_flagSpeedData = flagSpeedData;
				m_index = index;
				m_type = type;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(EcsGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;

				if (player == null)
					return 0;

				if ((m_flagSpeedData & 0x200) != 0)
				{
					player.CurrentSpeed = (short)(-(m_flagSpeedData & 0x1ff)); // backward movement
				}
				else
				{
					player.CurrentSpeed = (short)(m_flagSpeedData & 0x1ff); // forwardmovement
				}

				player.IsStrafing = (m_flagSpeedData & 0x4000) != 0;
				player.TargetInView = (m_flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
				player.GroundTargetInView = ((m_flagSpeedData & 0x1000) != 0);

				List<Tuple<Skill, Skill>> snap = player.GetAllUsableSkills();

				Skill sk = null;
				Skill sksib = null;

				// we're not using a spec !
				if (m_type > 0)
				{
					
					// find the first non-specialization index.
					int begin = Math.Max(0, snap.FindIndex(it => (it.Item1 is Specialization) == false));
					
					// are we in list ?
					if (m_index + begin < snap.Count)
					{
						sk = snap[m_index + begin].Item1;
						sksib = snap[m_index + begin].Item2;
					}
					
				}
				else
				{
					// mostly a spec !
					if (m_index < snap.Count)
					{
						sk = snap[m_index].Item1;
						sksib = snap[m_index].Item2;
					}
				}

				// we really got a skill !
				if (sk != null)
				{
					// Test if we can use it !
					int reuseTime = player.GetSkillDisabledDuration(sk);
					if (reuseTime > 60000)
					{
						player.Out.SendMessage(
							string.Format("You must wait {0} minutes {1} seconds to use this ability!", reuseTime/60000, reuseTime%60000/1000),
							EChatType.CT_System, EChatLoc.CL_SystemWindow);
						
						if (player.Client.Account.PrivLevel < 2)
							return 0;
					}
					else if (reuseTime > 0)
					{
						player.Out.SendMessage(string.Format("You must wait {0} seconds to use this ability!", reuseTime/1000 + 1),
						                       EChatType.CT_System, EChatLoc.CL_SystemWindow);
						
						if (player.Client.Account.PrivLevel < 2) 
							return 0;
					}

					// See what we should do depending on skill type !

					
					if (sk is Specialization)
					{
						Specialization spec = (Specialization)sk;
						ISpecActionHandler handler = SkillBase.GetSpecActionHandler(spec.KeyName);
						if (handler != null)
						{
							handler.Execute(spec, player);
						}
					}
					else if (sk is Ability)
					{
						Ability ab = (Ability)sk;
						IAbilityActionHandler handler = SkillBase.GetAbilityActionHandler(ab.KeyName);
						if (handler != null)
						{
							handler.Execute(ab, player);
							return 0;
						}
						
						ab.Execute(player);
					}
					else if (sk is Spell)
					{
						if(sksib != null && sksib is SpellLine)
							player.CastSpell((Spell)sk, (SpellLine)sksib);
					}
					else if (sk is Style)
					{
						player.styleComponent.ExecuteWeaponStyle((Style)sk);
					}
				}

				if (sk == null)
				{
					player.Out.SendMessage("Skill is not implemented.", EChatType.CT_Advise, EChatLoc.CL_SystemWindow);
				}

				return 0;
			}
		}
	}
}
