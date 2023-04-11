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
        public const int DISABLE_DURATION = 15000;

        public override int MaxLevel => 1;
        public override ushort Icon => 4281;

        private GamePlayer _player;

        public static int GetMinAttackRange(eRealm realm)
        {
            double minAttackRange = 2000;

            if (realm == eRealm.Albion)
                minAttackRange = 2200;
            if (realm == eRealm.Hibernia)
                minAttackRange = 2100;
            if (realm == eRealm.Midgard)
                minAttackRange = 2000;

            return (int) (minAttackRange * 0.66);
        }

        public static int GetMaxAttackRange(eRealm realm)
        {
            double maxAttackRange = 4000;

            if (realm == eRealm.Albion)
                maxAttackRange = 4400;
            if (realm == eRealm.Hibernia)
                maxAttackRange = 4300;
            if (realm == eRealm.Midgard)
                maxAttackRange = 4200;

            return (int) maxAttackRange;
        }

        public override int CostForUpgrade(int level)
        {
            return 8;
        }

        public override int GetReUseDelay(int level)
        {
            return 15;
        }

        public override bool CheckRequirement(GamePlayer player)
        {
            return AtlasRAHelpers.GetLongshotLevel(player) >= 1;
        }

        public override void Execute(GameLiving living)
        {
            _player = living as GamePlayer;

            if (_player == null)
                return;

            if (CheckPreconditions(_player, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (_player.CurrentRegion.IsDungeon)
            {
                _player.Out.SendMessage("You can't use Volley in dungeons!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.ActiveWeaponSlot != eActiveWeaponSlot.Distance || _player.ActiveWeapon == null)
            {
                _player.Out.SendMessage("You need to be equipped with a bow to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.rangeAttackComponent.UpdateAmmo(_player.ActiveWeapon) == null)
            {
                _player.Out.SendMessage("You need arrows to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            Region region = WorldMgr.GetRegion(_player.CurrentRegion.ID);

            if (region == null || region.GetZone(_player.GroundTarget.X, _player.GroundTarget.Y) == null)
            {
                _player.Out.SendMessage("You must have a ground target to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (_player.IsWithinRadius(_player.GroundTarget, GetMinAttackRange(_player.Realm)))
            {
                _player.Out.SendMessage("You ground target is too close to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (!_player.IsWithinRadius(_player.GroundTarget, GetMaxAttackRange(_player.Realm)))
            {
                _player.Out.SendMessage("You ground target is too far away to use Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            ECSGameEffect volley = EffectListService.GetEffectOnTarget(_player, eEffect.Volley);

            if (volley != null)
            {
                _player.Out.SendMessage("You are already using Volley!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            AtlasOF_VolleyECSEffect Volley = (AtlasOF_VolleyECSEffect)_player.EffectList.GetOfType(typeof(AtlasOF_VolleyECSEffect));

            if (Volley != null)
               return;

            TrueshotEffect trueShot = (TrueshotEffect)_player.EffectList.GetOfType(typeof(TrueshotEffect));

            if (trueShot != null)
            {
                trueShot.Cancel(false);
                return;
            }

            RapidFireEffect rapidFire = (RapidFireEffect)_player.EffectList.GetOfType(typeof(RapidFireEffect));

            if (rapidFire != null)
            {
                rapidFire.Cancel(false);
                return;
            }

            if (_player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Critical)
            {
                _player.Out.SendMessage("You can't use Volley while Critical-Shot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            else if (_player.rangeAttackComponent.RangedAttackType == eRangedAttackType.Long)
            {
                _player.Out.SendMessage("You can't use Volley while Longshot is active!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            // Can't use Volley inside portal and border keeps on a RvR server.
            if (GameServer.Instance.Configuration.ServerType == eGameServerType.GST_Normal)
            {
                foreach (AbstractArea area in region.GetAreasOfSpot(_player.GroundTarget).Where(x => x is AbstractArea))
                {
                    if (area is Area.Circle)
                    {
                        if (_player.Realm == eRealm.Albion)
                        {
                            if (area.Description is "Druim Ligen" or "Druim Cain" or "Svasud Faste" or "Vindsaul Faste")
                            {
                                _player.Out.SendMessage("You can't use Volley inside enemy Border Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        else if (_player.Realm == eRealm.Hibernia)
                        {
                            if (area.Description is "Svasud Faste" or "Vindsaul Faste" or "Castle Sauvage" or "Snowdonia Fortress")
                            {
                                _player.Out.SendMessage("You can't use Volley inside enemy Border Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        else if (_player.Realm == eRealm.Midgard)
                        {
                            if (area.Description is "Druim Ligen" or "Druim Cain" or "Castle Sauvage" or "Snowdonia Fortress")
                            {
                                _player.Out.SendMessage("You can't use Volley inside enemy Border Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                    }
                    else if (area is KeepArea)
                    {
                        if (_player.Realm == eRealm.Albion)
                        {
                            if (area.Description is "Hibernia Portal Keep" or "Midgard Portal Keep")
                            {
                                _player.Out.SendMessage("You can't use Volley inside enemy Portal Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        else if (_player.Realm == eRealm.Hibernia)
                        {
                            if (area.Description is "Midgard Portal Keep" or "Albion Portal Keep")
                            {
                                _player.Out.SendMessage("You can't use Volley inside enemy Portal Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                        else if (_player.Realm == eRealm.Midgard)
                        {
                            if (area.Description is "Albion Portal Keep" or "Hibernia Portal Keep")
                            {
                                _player.Out.SendMessage("You can't use Volley inside enemy Portal Keep!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }
                        }
                    }
                }
            }

            if (_player.attackComponent.Attackers.Count > 0 && _player.IsBeingInterrupted)
            {
                GameObject attacker = _player.attackComponent.Attackers.Last();

                if (attacker is GameNPC npcAttacker)
                    _player.Out.SendMessage(LanguageMgr.GetTranslation(_player.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true, _player.Client.Account.Language, npcAttacker), "volley"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                else
                    _player.Out.SendMessage(LanguageMgr.GetTranslation(_player.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true), "volley"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                return;
            }

            new AtlasOF_VolleyECSEffect(new ECSGameEffectInitParams(_player, 0, 1));
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Ground-targetted archery attack that fires successive arrows at a random target in a given area. To use this ability, choose a ground target. This target must be at least 66% of your bow's normal max range away from you.");
            list.Add("Once you are ready to fire, you can fire up to 5 arrows by hitting your bow button in succession.");
            list.Add("\nTarget: Ground");
            list.Add("Range: Minimum 66% of player range.");
        }

        public AtlasOF_Volley(DBAbility ability, int level) : base(ability, level) { }
    }
}