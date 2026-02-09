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
        protected const int LordMaxHealth = 100000; // Hitpoints from the lord
        public override int MaxHealth { get { return LordMaxHealth; } }

        public RelicLord() : base() { }


        #region Spawn Logic
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            var spawnPoints = new[]
            {
                // Albion
                new { Name = "Lord Castle Excalibur", Guild = "", Model = new ushort[] { 1008, 1020 }, Realm = eRealm.Albion, Region = (ushort)163, X = 673843, Y = 590553, Z = 8745, Heading = (ushort)4094, Equip = "relic_temple_lord_alb" },
                new { Name = "Lord Castle Myrddin", Guild = "", Model = new ushort[] { 1008, 1020 }, Realm = eRealm.Albion, Region = (ushort)163, X = 578172, Y = 677151, Z = 8737, Heading = (ushort)2721, Equip = "relic_temple_lord_alb" },
                // Midgard
                new { Name = "Jarl Grallarhorn Faste", Guild = "", Model = new ushort[] { 137, 185, 145, 193 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 713090, Y = 403741, Z = 8785, Heading = (ushort)1357, Equip = "relic_temple_lord_mid" },
                new { Name = "Jarl Mjollner Faste", Guild = "", Model = new ushort[] { 137, 185, 145, 193 }, Realm = eRealm.Midgard, Region = (ushort)163, X = 610908, Y = 303040, Z = 8497, Heading = (ushort)2721, Equip = "relic_temple_lord_mid" },
                // Hibernia
                new { Name = "Chieftain Dun Lamfhota", Guild = "", Model = new ushort[] { 286, 294 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 372731, Y = 591105, Z = 8737, Heading = (ushort)2721, Equip = "relic_temple_lord_hib" },
                new { Name = "Chieftain Dun Dagda", Guild = "", Model = new ushort[] { 286, 294 }, Realm = eRealm.Hibernia, Region = (ushort)163, X = 470205, Y = 677754, Z = 8113, Heading = (ushort)1349, Equip = "relic_temple_lord_hib" }
            };

            foreach (var sp in spawnPoints)
            {
                string customID = $"RelicLord_{sp.Realm}_{sp.Name.Replace(" ", "_")}";
                if (WorldMgr.GetNPCsFromRegion(sp.Region).Any(n => n.InternalID == customID))
                    continue;
                ushort randomModel = sp.Model[Util.Random(0, sp.Model.Length - 1)];

                RelicLord lord = new RelicLord();
                lord.InternalID = customID;
                lord.Name = sp.Name;
                lord.GuildName = sp.Guild;
                lord.Model = randomModel;
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
        public override bool AddToWorld()
        {
            SetOwnBrain(new RelicLordBrain());
            return base.AddToWorld();
        }
        #endregion
        #region Combat

        /// <summary>
		/// We have to make sure, Lord wont gets any damage during reset
		/// </summary>
		/// <param name=""></param>
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

        /// <summary>
		/// When Lord dies, we do TempleSpam
		/// </summary>
		/// <param name="killer"></param>
		public override void Die(GameObject killer)
        {
            int count = TempleRelicPadsLoader.GetEnemiesNearby(this);
            TempleRelicPadsLoader.SendTempleMessage($"{Name} has been killed with {count} enemies in the area.");

            base.Die(killer);
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