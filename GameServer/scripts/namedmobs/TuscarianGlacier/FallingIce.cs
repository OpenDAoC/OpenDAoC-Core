using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class FallingIce : GameNPC
    {
        public FallingIce() : base() { }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                    || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                    || damageType == eDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override eFlags Flags { get => base.Flags; set => base.Flags = (eFlags)12; }
        public override bool AddToWorld()
        {
            Model = 913;
            Name = "falling ice";
            Size = 100;
            Level = 70;
            MaxSpeedBase = 0;
            RespawnInterval = Util.Random(30000, 50000);

            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            FallingIceBrain adds = new FallingIceBrain();
            SetOwnBrain(adds);
            LoadedFromScript = false;//load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class FallingIceBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public FallingIceBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 500;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        private bool Announcetext = false;
        private bool isDisabled = false;
        private bool CanCast = false;
        
        public override void Think()
        {
            foreach(GamePlayer ppls in Body.GetPlayersInRadius(800))
            {
                if (ppls != null && ppls.IsAlive && ppls.Client.Account.PrivLevel == 1 && !AggroTable.ContainsKey(ppls) && !isDisabled)
                    AggroTable.Add(ppls,100);
            }
            foreach (GamePlayer player in Body.GetPlayersInRadius(200))
            {
                if (player != null)
                {
                    if (player.IsAlive)
                    {
                        if (player.Client.Account.PrivLevel == 1 && !Announcetext && !isDisabled)
                        {
                            BroadcastMessage("A terrifying cracking sound echoes in the caves! Falling ice slams into " + player.Name + "'s head!");                                                    
                            Announcetext = true;
                        }
                    }
                }
            }
            if(Announcetext && !CanCast)
            {
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DealIceDD), 200);
                CanCast = true;
            }
            base.Think();
        }
        private int DealIceDD(ECSGameTimer timer)
        {
            Body.CastSpell(FallingIceDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(KillIce), 500);//enable ice every 30-50s
            return 0;
        }
        private int KillIce(ECSGameTimer timer)
        {
            if (Body.IsAlive)
                Body.Die(Body);
            return 0;
        }
        private Spell m_FallingIceDD;
        private Spell FallingIceDD
        {
            get
            {
                if (m_FallingIceDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 159;
                    spell.Name = "Falling Ice";
                    spell.Icon = 159;
                    spell.TooltipId = 159;
                    spell.Damage = 400;
                    spell.Range = 0;
                    spell.Radius = 450;
                    spell.SpellID = 11871;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_FallingIceDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FallingIceDD);
                }
                return m_FallingIceDD;
            }
        }
    }
}
