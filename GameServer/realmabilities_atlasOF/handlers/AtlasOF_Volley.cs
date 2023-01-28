using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_Volley : TimedRealmAbility
    {
        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 8; }
        public override int GetReUseDelay(int level) { return 15; } // 15 seconds
        public override bool CheckRequirement(GamePlayer player)
        {
            return AtlasRAHelpers.GetLongshotLevel(player) >= 1;
        }
        public override ushort Icon => 4281;
        
        private GamePlayer m_player;
        public const int DISABLE_DURATION = 15000; //15s to use again ability
        public override void Execute(GameLiving living)
		{
            m_player = living as GamePlayer;
            double attackrangeMin = 2000 * 0.66;//minimum attack range
            double attackrangeMax = 4000;//maximum attack range
            if (m_player.Realm == eRealm.Albion)
            {
                attackrangeMin = 2200 * 0.66;//minimum attack range
                attackrangeMax = 4400;//maximum attack range
            }
            if (m_player.Realm == eRealm.Hibernia)
            {
                attackrangeMin = 2100 * 0.66;//minimum attack range
                attackrangeMax = 4300;//maximum attack range
            }
            if (m_player.Realm == eRealm.Midgard)
            {
                attackrangeMin = 2000 * 0.66;//minimum attack range
                attackrangeMax = 4200;//maximum attack range
            }

            Region rgn = WorldMgr.GetRegion(m_player.CurrentRegion.ID);

            if (CheckPreconditions(m_player, DEAD | SITTING | MEZZED | STUNNED))
                return;
            if(m_player.CurrentRegion.IsDungeon)
            {
                m_player.Out.SendMessage("You can't use Volley in dungeons!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (m_player.ActiveWeaponSlot != eActiveWeaponSlot.Distance || m_player.ActiveWeapon == null)
            {
                m_player.Out.SendMessage("You need to be equipped with a bow to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (m_player.rangeAttackComponent.UpdateAmmo(m_player.ActiveWeapon) == null)
            {
                m_player.Out.SendMessage("You need arrows to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (rgn == null || rgn.GetZone(m_player.GroundTarget.X, m_player.GroundTarget.Y) == null)
            {
                m_player.Out.SendMessage("You must have a ground target to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);     
                return;
            }
            if (m_player.IsWithinRadius(m_player.GroundTarget, (int)attackrangeMin))
            {
                m_player.Out.SendMessage("You ground target is too close to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (!m_player.IsWithinRadius(m_player.GroundTarget, (int)attackrangeMax))
            {
                m_player.Out.SendMessage("You ground target is too far away to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            ECSGameEffect volley = EffectListService.GetEffectOnTarget(m_player, eEffect.Volley);
            if (volley != null)
            {
                m_player.Out.SendMessage("You are already using Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            AtlasOF_VolleyECSEffect Volley = (AtlasOF_VolleyECSEffect)m_player.EffectList.GetOfType(typeof(AtlasOF_VolleyECSEffect));
            if (Volley != null)
            {
               return;
            }
            TrueshotEffect trueShot = (TrueshotEffect)m_player.EffectList.GetOfType(typeof(TrueshotEffect));
            if (trueShot != null)
            {
                trueShot.Cancel(false);
                return;
            }
            RapidFireEffect rapidFire = (RapidFireEffect)m_player.EffectList.GetOfType(typeof(RapidFireEffect));
            if (rapidFire != null)
            {
                rapidFire.Cancel(false);
                return;
            }
            if (m_player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Critical)
            {
                m_player.Out.SendMessage("You can't use Volley while Critical-Shot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (m_player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Long)
            {
                m_player.Out.SendMessage("You can't use Volley while Longshot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            Region region = WorldMgr.GetRegion(m_player.CurrentRegionID);
            foreach(AbstractArea area in region.GetAreasOfSpot(m_player.GroundTarget))//can't use volley inside border keep
            {
                if (area != null)
                {
                    if (area is Area.Circle)
                    {
                        if (m_player.Realm == eRealm.Albion)
                        {
                            if (area.Description == "Druim Ligen" || area.Description == "Druim Cain" || area.Description == "Svasud Faste" || area.Description == "Vindsaul Faste")
                            {
                                m_player.Out.SendMessage("You can't use Volley inside enemy Border Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        if (m_player.Realm == eRealm.Hibernia)
                        {
                            if (area.Description == "Svasud Faste" || area.Description == "Vindsaul Faste" || area.Description == "Castle Sauvage" || area.Description == "Snowdonia Fortress")
                            {
                                m_player.Out.SendMessage("You can't use Volley inside enemy Border Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        if (m_player.Realm == eRealm.Midgard)
                        {
                            if (area.Description == "Druim Ligen" || area.Description == "Druim Cain" || area.Description == "Castle Sauvage" || area.Description == "Snowdonia Fortress")
                            {
                                m_player.Out.SendMessage("You can't use Volley inside enemy Border Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                    }
                    if(area is KeepArea)
                    {
                        if (m_player.Realm == eRealm.Albion)
                        {
                            if (area.Description == "Hibernia Portal Keep" || area.Description == "Midgard Portal Keep")
                            {
                                m_player.Out.SendMessage("You can't use Volley inside enemy Portal Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        if (m_player.Realm == eRealm.Hibernia)
                        {
                            if (area.Description == "Midgard Portal Keep" || area.Description == "Albion Portal Keep")
                            {
                                m_player.Out.SendMessage("You can't use Volley inside enemy Portal Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        if (m_player.Realm == eRealm.Midgard)
                        {
                            if (area.Description == "Albion Portal Keep" || area.Description == "Hibernia Portal Keep")
                            {
                                m_player.Out.SendMessage("You can't use Volley inside enemy Portal Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                    }
                }
            }

            if (m_player.attackComponent.Attackers.Count > 0 && m_player.IsBeingInterrupted)
            {
                string attackTypeMsg;
                GameObject attacker = m_player.attackComponent.Attackers.Last();

                if (attacker is GameNPC npcAttacker)
                    m_player.Out.SendMessage(LanguageMgr.GetTranslation(m_player.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true, m_player.Client.Account.Language, npcAttacker), "volley"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                else
                    m_player.Out.SendMessage(LanguageMgr.GetTranslation(m_player.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true), "volley"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                return;
            }

            new AtlasOF_VolleyECSEffect(new ECSGameEffectInitParams(m_player, 0, 1));
		}
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Ground-targetted archery attack that fires successive arrows at a random target in a given area. To use this ability, choose a ground target. This target must be at least 66% of your bow's normal max range away from you.");
            list.Add("Once you are ready to fire, you can fire up to 5 arrows by hitting your bow button in succession.");
            list.Add("\nTarget: Ground");
            list.Add("Range: Minimum 66% of player range.");
        }
        public AtlasOF_Volley(DBAbility ability, int level) : base(ability, level)
        {
        }
    }
}