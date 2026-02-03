using System;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.AI.Brain;
using DOL.Logging;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class RelicLord : GameNPC
    {
        private static new readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        protected const byte LordLevel = 65;
        protected const int LordRespawnInterval = 900000; // 15 Minuten

        public RelicLord() : base() { }

        public override bool AddToWorld()
        {
            SetOwnBrain(new RelicLordBrain());
            return base.AddToWorld();
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (IsReturningToSpawnPoint)
            {
                if (source is GamePlayer player)
                {
                    player.Out.SendMessage(Name + " is currenctly immune to damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                }
                return; 
            }

            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        #region Spawn Logic
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            var spawnPoints = new[]
            {
                // Albion
                new { Name = "Lord", Guild = "Castle Excalibur", Model = (ushort)1008, Realm = eRealm.Albion, Region = (ushort)163, X = 673843, Y = 590553, Z = 8745, Heading = (ushort)2721, Equip = "relic_temple_lord_alb" },
                new { Name = "Lord", Guild = "Castle Myrddin", Model = (ushort)1008, Realm = eRealm.Albion, Region = (ushort)163, X = 578172, Y = 677151, Z = 8737, Heading = (ushort)2721, Equip = "relic_temple_lord_alb" },
                // Midgard
                new { Name = "Jarl", Guild = "Grallarhorn Faste", Model = (ushort)137, Realm = eRealm.Midgard, Region = (ushort)163, X = 713090, Y = 403741, Z = 8785, Heading = (ushort)2721, Equip = "relic_temple_lord_mid" },
                new { Name = "Jarl", Guild = "Mjollner Faste", Model = (ushort)137, Realm = eRealm.Midgard, Region = (ushort)163, X = 610908, Y = 303040, Z = 8497, Heading = (ushort)2721, Equip = "relic_temple_lord_mid" },
                // Hibernia
                new { Name = "Chieftain", Guild = "Dun Lamfhota", Model = (ushort)286, Realm = eRealm.Hibernia, Region = (ushort)163, X = 372731, Y = 591105, Z = 8737, Heading = (ushort)2721, Equip = "relic_temple_lord_hib" },
                new { Name = "Chieftain", Guild = "Dun Dagda", Model = (ushort)286, Realm = eRealm.Hibernia, Region = (ushort)163, X = 470205, Y = 677754, Z = 8113, Heading = (ushort)2721, Equip = "relic_temple_lord_hib" }
            };

            foreach (var sp in spawnPoints)
            {
                string customID = $"RelicLord_{sp.Realm}_{sp.Guild.Replace(" ", "_")}";
                if (WorldMgr.GetNPCsFromRegion(sp.Region).Any(n => n.InternalID == customID))
                    continue;

                RelicLord lord = new RelicLord();
                lord.InternalID = customID;
                lord.Name = sp.Name;
                lord.GuildName = sp.Guild;
                lord.Model = sp.Model;
                lord.Level = LordLevel;
                lord.Realm = sp.Realm;
                lord.X = sp.X;
                lord.Y = sp.Y;
                lord.Z = sp.Z;
                lord.Heading = sp.Heading;
                lord.CurrentRegionID = sp.Region;
                lord.RespawnInterval = LordRespawnInterval;

                if (!string.IsNullOrEmpty(sp.Equip))
                    lord.LoadEquipmentTemplateFromDatabase(sp.Equip);

                lord.AddToWorld();
            }
        }
        #endregion
    }
}

namespace DOL.AI.Brain
{
    public class RelicLordBrain : StandardMobBrain
    {
        protected const int MaxDistance = 1100; // perfekt distance for relic temple

        public RelicLordBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 1000;
            ThinkInterval = 1000;
        }

        public override void Think()
        {
            // Dont think when dead, or already running back to spawn
            if (Body == null || !Body.IsAlive || Body.IsReturningToSpawnPoint) return;

            double dist = Body.GetDistanceTo(Body.SpawnPoint);
            if (dist > MaxDistance)
            {
                ResetLord();
                return;
            }

            base.Think();
        }

        private void ResetLord()
        {
            Body.StopAttack();
            ClearAggroList();

            Body.Health = Body.MaxHealth;
            Body.Mana = Body.MaxMana;

            // Back to spawn fast
            Body.ReturnToSpawnPoint(Body.MaxSpeed);
        }

        public override bool CanAggroTarget(GameLiving target)
        {
            if (Body == null || target == null) return false;
            return GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
        }
    }
}