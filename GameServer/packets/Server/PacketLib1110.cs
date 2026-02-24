using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DOL.GS.PacketHandler.Client.v168;
using DOL.GS.RealmAbilities;
using DOL.GS.Spells;
using DOL.GS.Styles;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1110, GameClient.eClientVersion.Version1110)]
    public class PacketLib1110 : PacketLib1109
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.110
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1110(GameClient client)
            : base(client)
        {
        }

        /// <summary>
        /// Property to enable "forced" Tooltip send when Update are made to player skills, or player effects.
        /// This can be controlled through server propertiers !
        /// </summary>
		public virtual bool ForceTooltipUpdate {
			get { return ServerProperties.Properties.USE_NEW_TOOLTIP_FORCEDUPDATE; }
		}

        /// <summary>
		/// New system in v1.110+ for delve info. delve is cached by client in extra file, stored locally.
		/// </summary>
		/// <param name="info"></param>
		public override void SendDelveInfo(string info)
		{
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.DelveInfo)))
			{
				pak.WriteString(info, 2048);
				pak.WriteByte(0); // 0-terminated
				SendTCP(pak);
			}
		}

		public override void SendUpdateIcons(IList changedEffects, ref int lastUpdateEffectsCount)
		{
			if (m_gameClient.Player == null)
			{
				return;
			}

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.UpdateIcons)))
			{
				long initPos = pak.Position;

				int fxcount = 0;
				int entriesCount = 0;

				pak.WriteByte(0); // effects count set in the end0
				pak.WriteByte(0); // unknown
				pak.WriteByte(Icons); // unknown
				pak.WriteByte(0); // unknown

				foreach (ECSGameEffect effect in m_gameClient.Player.effectListComponent.GetEffects().Where(e => e.EffectType != eEffect.Pulse))
				{
					if (effect.Icon == 0)
						continue;

					fxcount++;
					if (changedEffects != null && !changedEffects.Contains(effect))
					{
						continue;
					}

					// store tooltip update for gamespelleffect.
					if (ForceTooltipUpdate && effect is ECSGameSpellEffect gameEffect)
					{
						ISpellHandler spellHandler = gameEffect.SpellHandler;

						if (spellHandler.Spell.IsDynamic || m_gameClient.CanSendTooltip(24, spellHandler.Spell.InternalID))
							SendDelveInfo(DetailDisplayHandler.DelveSpell(spellHandler));
					}

					// icon index
					pak.WriteByte((byte)(fxcount - 1));
					// Determines where to grab the icon from. Spell-based effect icons use a different source than Ability-based icons.
					pak.WriteByte((effect is ECSGameAbilityEffect && effect.Icon <= 5000) ? (byte)0xff : (byte)(fxcount - 1));
					//pak.WriteByte((effect is ECSGameSpellEffect || effect.Icon > 5000) ? (byte)(fxcount - 1) : (byte)0xff); // <- [Takii] previous version

					byte ImmunByte = 0;
					var gsp = effect as ECSGameEffect;
					if (gsp is ECSImmunityEffect || gsp.IsDisabled)
						ImmunByte = 1;
					//todo this should be the ImmunByte
					pak.WriteByte(ImmunByte); // new in 1.73; if non zero says "protected by" on right click

					// bit 0x08 adds "more..." to right click info
					pak.WriteShort(effect.Icon);
					pak.WriteShort((ushort)(effect.GetRemainingTimeForClient() / 1000));
					if (effect is ECSGameEffect || effect is ECSImmunityEffect)
						pak.WriteShort(effect.Icon); //v1.110+ send the spell ID for delve info in active icon
					else
						pak.WriteShort(0);//don't override existing tooltip ids

					byte flagNegativeEffect = 0;

					if (!effect.HasPositiveEffect)
					{
						flagNegativeEffect = 1;
					}

					pak.WriteByte(flagNegativeEffect);

					pak.WritePascalString(effect.Name);
					entriesCount++;
				}

				int oldCount = lastUpdateEffectsCount;
				lastUpdateEffectsCount = fxcount;

				while (oldCount > fxcount)
				{
					pak.WriteByte((byte)(fxcount++));
					pak.Fill(0, 10);
					entriesCount++;
				}

				if (changedEffects != null)
				{
					changedEffects.Clear();
				}

				if (entriesCount == 0)
				{
					pak.ReleasePooledObject();
					return; // nothing changed - no update is needed
				}

				pak.Position = initPos;
				pak.WriteByte((byte)entriesCount);
				pak.Seek(0, SeekOrigin.End);

				SendTCP(pak);
			}
		}

		/// <summary>
		/// Override for handling force tooltip update...
		/// </summary>
		public override void SendTrainerWindow()
		{
			base.SendTrainerWindow();

			// Send tooltips
			if (ForceTooltipUpdate && m_gameClient.TrainerSkillCache != null)
				SendForceTooltipUpdate(m_gameClient.TrainerSkillCache.SelectMany(e => e.Item2).Select(e => e.Item3));
		}

		/// <summary>
		/// Send Delve for Provided Collection of Skills that need forced Tooltip Update.
		/// </summary>
		/// <param name="skills"></param>
		protected virtual void SendForceTooltipUpdate(IEnumerable<Skill> skills)
		{
			foreach (Skill t in skills)
			{
				if (t is Specialization)
					continue;

				if (t is RealmAbility)
				{
					if (m_gameClient.CanSendTooltip(27, t.InternalID))
						SendDelveInfo(DetailDisplayHandler.DelveRealmAbility(m_gameClient, t.InternalID));
				}
				else if (t is Ability)
				{
					if (m_gameClient.CanSendTooltip(28, t.InternalID))
						SendDelveInfo(DetailDisplayHandler.DelveAbility(m_gameClient, t.InternalID));
				}
				else if (t is Style style)
				{
					if (m_gameClient.CanSendTooltip(25, t.InternalID))
					{
						if (style.Procs != null && style.Procs.Count > 0)
						{
							foreach (StyleProcInfo proc in style.Procs)
								SendDelveInfo(DetailDisplayHandler.DelveSpell(m_gameClient, proc.Spell));
						}

						SendDelveInfo(DetailDisplayHandler.DelveStyle(m_gameClient, t.InternalID));
					}
				}
				else if (t is Spell spell)
				{
					if (spell is Song || spell.NeedInstrument)
					{
						if (m_gameClient.CanSendTooltip(26, spell.InternalID))
							SendDelveInfo(DetailDisplayHandler.DelveSong(m_gameClient, spell.InternalID));
					}

					if (m_gameClient.CanSendTooltip(24, spell.InternalID))
					{
						SendDelveInfo(DetailDisplayHandler.DelveSpell(m_gameClient, spell));

						if (spell.HasSubSpell)
						{
							if (m_gameClient.CanSendTooltip(24, SkillBase.GetSpellByID(spell.SubSpellID).InternalID))
								SendDelveInfo(DetailDisplayHandler.DelveSpell(m_gameClient, SkillBase.GetSpellByID(spell.SubSpellID)));
						}

						if (spell.SpellType == eSpellType.DefensiveProc || spell.SpellType == eSpellType.OffensiveProc)
							SendDelveInfo(DetailDisplayHandler.DelveSpell(m_gameClient, SkillBase.GetSpellByID((int)spell.Value)));
					}
				}
			}
		}

		/// <summary>
		/// new siege weapon animation packet 1.110
		/// </summary>
		public override void SendSiegeWeaponAnimation(GameSiegeWeapon siegeWeapon)
        {
            if (siegeWeapon == null)
                return;
            using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.SiegeWeaponAnimation)))
            {
                bool isGroundTargetValid = siegeWeapon.GroundTarget.IsValid;

                pak.WriteInt(siegeWeapon.ObjectID);
                pak.WriteInt(
                    (uint)
                    (siegeWeapon.TargetObject == null
                     ? (!isGroundTargetValid ? 0 : siegeWeapon.GroundTarget.X)
                     : siegeWeapon.TargetObject.X));
                pak.WriteInt(
                    (uint)
                    (siegeWeapon.TargetObject == null
                     ? (!isGroundTargetValid ? 0 : siegeWeapon.GroundTarget.Y)
                     : siegeWeapon.TargetObject.Y));
                pak.WriteInt(
                    (uint)
                    (siegeWeapon.TargetObject == null
                     ? (!isGroundTargetValid ? 0 : siegeWeapon.GroundTarget.Z)
                     : siegeWeapon.TargetObject.Z));
                pak.WriteInt((uint)(siegeWeapon.TargetObject == null ? 0 : siegeWeapon.TargetObject.ObjectID));
                pak.WriteShort(siegeWeapon.Effect);
                pak.WriteShort((ushort)(siegeWeapon.SiegeWeaponTimer.TimeUntilElapsed)); // timer is no longer ( value / 100 )
                pak.WriteByte((byte)siegeWeapon.SiegeWeaponTimer.CurrentAction);
                pak.Fill(0, 3); // TODO : these bytes change depending on siege weapon action, to implement when different ammo types available.
                SendTCP(pak);
            }
        }

		/// <summary>
		/// new siege weapon fireanimation 1.110 // patch 0021
		/// </summary>
		/// <param name="siegeWeapon">The siege weapon</param>
		/// <param name="timer">How long the animation lasts for</param>
		public override void SendSiegeWeaponFireAnimation(GameSiegeWeapon siegeWeapon, int timer)
		{
			if (siegeWeapon == null)
				return;
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.SiegeWeaponAnimation)))
			{
				pak.WriteInt((uint) siegeWeapon.ObjectID);
				pak.WriteInt((uint) (siegeWeapon.TargetObject == null ? siegeWeapon.GroundTarget.X : siegeWeapon.TargetObject.X));
				pak.WriteInt((uint) (siegeWeapon.TargetObject == null ? siegeWeapon.GroundTarget.Y : siegeWeapon.TargetObject.Y));
				pak.WriteInt((uint) (siegeWeapon.TargetObject == null ? siegeWeapon.GroundTarget.Z + 50 : siegeWeapon.TargetObject.Z + 50));
				pak.WriteInt((uint) (siegeWeapon.TargetObject == null ? 0 : siegeWeapon.TargetObject.ObjectID));
				pak.WriteShort(siegeWeapon.Effect);
				pak.WriteShort((ushort) (timer)); // timer is no longer ( value / 100 )
				pak.WriteByte((byte) SiegeTimer.eAction.Fire);
				pak.WriteShort(0xE134); // default ammo type, the only type currently supported on DOL
				pak.WriteByte(0x08); // always this flag when firing
				SendTCP(pak);
			}
		}
    }
}
