
using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.Commands;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
	/// <summary>
	/// Handles spell cast requests from client
	/// </summary>
	[PacketHandlerAttribute(EPacketHandlerType.TCP, EClientPackets.UseSpell, "Handles Player Use Spell Request.", eClientStatus.PlayerInGame)]
	public class UseSpellHandler : AbstractCommandHandler, IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			int flagSpeedData;
			int spellLevel;
			int spellLineIndex;
			if (client.Version >= GameClient.EClientVersion.Version1124)
			{
				client.Player.X = (int)packet.ReadFloatLowEndian();
				client.Player.Y = (int)packet.ReadFloatLowEndian();
				client.Player.Z = (int)packet.ReadFloatLowEndian();
				client.Player.CurrentSpeed = (short)packet.ReadFloatLowEndian();
				client.Player.Heading = packet.ReadShort();
				flagSpeedData = packet.ReadShort(); // target visible ? 0xA000 : 0x0000
				spellLevel = packet.ReadByte();
				spellLineIndex = packet.ReadByte();
				// two bytes at end, not sure what for
			}
			else
			{
				flagSpeedData = packet.ReadShort();
				int heading = packet.ReadShort();

				if (client.Version > GameClient.EClientVersion.Version171)
				{
					int xOffsetInZone = packet.ReadShort();
					int yOffsetInZone = packet.ReadShort();
					int currentZoneID = packet.ReadShort();
					int realZ = packet.ReadShort();

					Zone newZone = WorldMgr.GetZone((ushort)currentZoneID);
					if (newZone == null)
					{
						Log.Warn($"Unknown zone in UseSpellHandler: {currentZoneID} player: {client.Player.Name}");
					}
					else
					{
						client.Player.X = newZone.XOffset + xOffsetInZone;
						client.Player.Y = newZone.YOffset + yOffsetInZone;
						client.Player.Z = realZ;
						client.Player.MovementStartTick = GameLoop.GetCurrentTime();
					}
				}

				spellLevel = packet.ReadByte();
				spellLineIndex = packet.ReadByte();

				client.Player.Heading = (ushort)(heading & 0xfff);
			}

			GamePlayer player = client.Player;

			// Commenting out. 'flagSpeedData' doesn't vary with movement speed, and this stops the player for a fraction of a second.
			//if ((flagSpeedData & 0x200) != 0)
			//{
			//	player.CurrentSpeed = (short)(-(flagSpeedData & 0x1ff)); // backward movement
			//}
			//else
			//{
			//	player.CurrentSpeed = (short)(flagSpeedData & 0x1ff); // forward movement
			//}

			player.IsStrafing = (flagSpeedData & 0x4000) != 0;
			if(!player.IsCasting) player.TargetInView = (flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
			player.GroundTargetInView = ((flagSpeedData & 0x1000) != 0);

			List<Tuple<SpellLine, List<Skill>>> snap = player.GetAllUsableListSpells();
			Skill sk = null;
			SpellLine sl = null;
			
			// is spelline in index ?
			if (spellLineIndex < snap.Count)
			{
				int index = snap[spellLineIndex].Item2.FindIndex(s => s is Spell ? s.Level == spellLevel :
																(s is Styles.Style style ? style.SpecLevelRequirement == spellLevel :
																(s is AbilityUtil ability ? ability.SpecLevelRequirement == spellLevel :
																false)));
				
				if (index > -1)
				{
					sk = snap[spellLineIndex].Item2[index];
				}
				
				sl = snap[spellLineIndex].Item1;
			}
			
			if (sk is Spell && sl != null)
			{
				if (GameLoop.GameLoopTime > player.TempProperties.getProperty<long>(sk.Name) + GameLoop.TICK_RATE)
				{
					//todo How to attach a spell to a player? Casting Service should in theory create spellHandler and add to the player -- not the component
					//player.CastSpell((Spell)sk, sl);
					player.castingComponent.RequestStartCastSpell((Spell)sk, sl);
				}

				player.TempProperties.setProperty(sk.Name, 75);
			}
			else if (sk is Styles.Style)
			{
				player.styleComponent.ExecuteWeaponStyle((Styles.Style)sk);
			}
			else if (sk is AbilityUtil)
			{
				AbilityUtil ab = (AbilityUtil)sk;
				IAbilityActionHandler handler = SkillBase.GetAbilityActionHandler(ab.KeyName);
				if (handler != null)
				{
					handler.Execute(ab, player);
				}
				
				ab.Execute(player);
			}
			else
			{
				if (Log.IsWarnEnabled)
					Log.Warn("Client <" + player.Client.Account.Name + "> requested incorrect spell at level " + spellLevel +
						" in spell-line " + ((sl == null || sl.Name == null) ? "unkown" : sl.Name));
				
				player.Out.SendMessage(string.Format("Error : Spell (Line {0}, Level {1}) can't be resolved...", spellLineIndex, spellLevel), EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
			}
			

			//new UseSpellAction(client.Player, flagSpeedData, spellLevel, spellLineIndex).Start(1);
		}

		/// <summary>
		/// Handles player use spell actions
		/// </summary>
		protected class UseSpellAction : RegionAction
		{
			/// <summary>
			/// Defines a logger for this class.
			/// </summary>
			private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

			/// <summary>
			/// The speed and flags data
			/// </summary>
			protected readonly int m_flagSpeedData;

			/// <summary>
			/// The used spell level
			/// </summary>
			protected readonly int m_spellLevel;

			/// <summary>
			/// The used spell line index
			/// </summary>
			protected readonly int m_spellLineIndex;

			/// <summary>
			/// Constructs a new UseSpellAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="flagSpeedData">The speed and flags data</param>
			/// <param name="spellLevel">The used spell level</param>
			/// <param name="spellLineIndex">The used spell line index</param>
			public UseSpellAction(GamePlayer actionSource, int flagSpeedData, int spellLevel, int spellLineIndex)
				: base(actionSource)
			{
				m_flagSpeedData = flagSpeedData;
				m_spellLevel = spellLevel;
				m_spellLineIndex = spellLineIndex;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer)m_actionSource;

				if ((m_flagSpeedData & 0x200) != 0)
				{
					player.CurrentSpeed = (short)(-(m_flagSpeedData & 0x1ff)); // backward movement
				}
				else
				{
					player.CurrentSpeed = (short)(m_flagSpeedData & 0x1ff); // forward movement
				}
				player.IsStrafing = (m_flagSpeedData & 0x4000) != 0;
				if(!player.IsCasting)player.TargetInView = (m_flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
				player.GroundTargetInView = ((m_flagSpeedData & 0x1000) != 0);

				List<Tuple<SpellLine, List<Skill>>> snap = player.GetAllUsableListSpells();
				Skill sk = null;
				SpellLine sl = null;
				
				// is spelline in index ?
				if (m_spellLineIndex < snap.Count)
				{
					int index = snap[m_spellLineIndex].Item2.FindIndex(s => s is Spell ? 
																	   s.Level == m_spellLevel 
																	   : (s is Styles.Style ? ((Styles.Style)s).SpecLevelRequirement == m_spellLevel
																		  : (s is AbilityUtil ? ((AbilityUtil)s).SpecLevelRequirement == m_spellLevel : false)));
					
					if (index > -1)
					{
						sk = snap[m_spellLineIndex].Item2[index];
					}
					
					sl = snap[m_spellLineIndex].Item1;
				}
				
				if (sk is Spell && sl != null)
				{
					//todo How to attach a spell to a player? Casting Service should in theory create spellHandler and add to the player -- not the component
					player.CastSpell((Spell)sk, sl);
				}
				else if (sk is Styles.Style)
				{
					player.styleComponent.ExecuteWeaponStyle((Styles.Style)sk);
				}
				else if (sk is AbilityUtil)
				{
					AbilityUtil ab = (AbilityUtil)sk;
					IAbilityActionHandler handler = SkillBase.GetAbilityActionHandler(ab.KeyName);
					if (handler != null)
					{
						handler.Execute(ab, player);
					}
					
					ab.Execute(player);
				}
				else
				{
					if (Log.IsWarnEnabled)
						Log.Warn("Client <" + player.Client.Account.Name + "> requested incorrect spell at level " + m_spellLevel +
							" in spell-line " + ((sl == null || sl.Name == null) ? "unkown" : sl.Name));
					
					player.Out.SendMessage(string.Format("Error : Spell (Line {0}, Level {1}) can't be resolved...", m_spellLineIndex, m_spellLevel), EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				}

				return 0;
			}
		}
	}
}