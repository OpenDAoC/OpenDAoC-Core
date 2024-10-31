using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.GS.Styles;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.UseSkill, "Handles Player Use Skill Request.", eClientStatus.PlayerInGame)]
    public class UseSkillHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
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
            GamePlayer player = client.Player;

            // Commenting out. 'flagSpeedData' doesn't vary with movement speed, and this stops the player for a fraction of a second.
            //if ((flagSpeedData & 0x200) != 0)
            //    player.CurrentSpeed = (short)(-(flagSpeedData & 0x1ff)); // backward movement
            //else
            //    player.CurrentSpeed = (short)(flagSpeedData & 0x1ff); // forward movement

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
            if (sk == null)
            {
                player.Out.SendMessage("Skill is not implemented.", eChatType.CT_Advise, eChatLoc.CL_SystemWindow);
                return;
            }

            // Test if we can use it !
            int reuseTime = player.GetSkillDisabledDuration(sk);

            if (reuseTime > 60000)
            {
                player.Out.SendMessage(
                    string.Format("You must wait {0} minutes {1} seconds to use this ability!", reuseTime/60000, reuseTime%60000/1000),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (player.Client.Account.PrivLevel < 2)
                    return;
            }
            else if (reuseTime > 0)
            {
                // Allow Pulse Spells to be canceled while they are on reusetimer
                if (sk is Spell spell && spell.IsPulsing && player.ActivePulseSpells.ContainsKey(spell.SpellType))
                {
                    ECSPulseEffect effect = EffectListService.GetPulseEffectOnTarget(player, spell);
                    EffectService.RequestImmediateCancelConcEffect(effect);

                    if (spell.InstrumentRequirement == 0)
                        player.Out.SendMessage("You cancel your effect.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    else
                        player.Out.SendMessage("You stop playing your song.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }
                else
                    player.Out.SendMessage(string.Format("You must wait {0} seconds to use this ability!", reuseTime / 1000 + 1), eChatType.CT_System, eChatLoc.CL_SystemWindow);

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
                player.castingComponent.RequestStartUseAbility(ability);
            else if (sk is Spell spell)
            {
                if (sksib is SpellLine spellLine)
                    player.CastSpell(spell, spellLine);
            }
            else if (sk is Style style)
            {
                if (player.styleComponent.AwaitingBackupInput && Properties.ALLOW_AUTO_BACKUP_STYLES)
                {
                    player.styleComponent.AwaitingBackupInput = false;

                    if (!Properties.ALLOW_NON_ANYTIME_BACKUP_STYLES && (style.AttackResultRequirement != Style.eAttackResultRequirement.Any || style.OpeningRequirementType == Style.eOpening.Positional))
                    {
                        player.Out.SendMessage($"You must use an anytime style as your backup.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    player.Out.SendMessage($"You will now use {style.Name} as your backup.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    player.styleComponent.AutomaticBackupStyle = style;
                    return;
                }

                player.styleComponent.ExecuteWeaponStyle(style);
            }
        }
    }
}
