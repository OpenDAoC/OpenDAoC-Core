using System;
using System.Collections.Generic;
using System.Reflection;
using Core.AI;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.Commands
{
    [Command("&xmob", //command to handle
         EPrivLevel.GM, //minimum privelege level
         "/xmob get <radius> (max5000)",
         "/xmob view",
         "/xmob spawn <radius> (max5000)",
         "/xmob clear",
         "/xmob copy <num> (max 20)",
         "/xmob setlevels <radius> <value> or <min> <max> (max5000)",
         "/xmob randomspawn <radius> <newradius> (max5000)",
         "/xmob npcsetupcreate <alb | mid | hib>",
         "/xmob setrealm <radius> <realm>",
         "/xmob remove <radius> (max5000)")]

    public class XMobCommand : ACommandHandler, ICommandHandler
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region spot cache

        private IDictionary<GameClient, IList<GameNpc>> m_spots = new Dictionary<GameClient, IList<GameNpc>>();

        private IList<GameNpc> GetClientSpot(GameClient client)
        {
            IList<GameNpc> spot = new List<GameNpc>();
            foreach (KeyValuePair<GameClient, IList<GameNpc>> pair in m_spots)
                if (pair.Key == client) spot = pair.Value;

            return spot;
        }

        private void AddClientSpot(GameClient client, IList<GameNpc> spot)
        {
            foreach (KeyValuePair<GameClient, IList<GameNpc>> pair in m_spots)
                if (pair.Key == client)
                {
                    foreach (GameNpc m in spot)
                        pair.Value.Add(m);

                    return;
                }
        }

        private void ClearClientSpot(GameClient client)
        {
            foreach (KeyValuePair<GameClient, IList<GameNpc>> pair in m_spots)
                if (pair.Key == client) pair.Value.Clear();
        }

        #endregion spot cache

        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {

                case "get":
                    {
                        #region get
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        ushort radius;

                        if (ushort.TryParse(args[2], out radius))
                        {
                            if (GetClientSpot(client).Count != 0)                                               
                                ClearClientSpot(client);

                            if (radius < 0) radius = 0;
                            if (radius > 5000) radius = 5000;

                            IList<GameNpc> spot = new List<GameNpc>();
                            if (m_spots.TryGetValue(client, out spot))
                            {
                                spot = new List<GameNpc>();
                                foreach (GameNpc npc in player.GetNPCsInRadius(radius))
                                    if (npc.Realm == ERealm.None)
                                        spot.Add(npc);

                                AddClientSpot(client, spot);
                            }
                            else
                            {
                                spot = new List<GameNpc>();
                                foreach (GameNpc npc in player.GetNPCsInRadius(radius))
                                    if (npc.Realm == ERealm.None)
                                        spot.Add(npc);
                                m_spots.Add(client, spot);
                            }



                            if (GetClientSpot(client).Count != 0)
                                DisplayMessage(player, "Your cache has " + GetClientSpot(client).Count + " mobs Loaded!");
                            else
                                DisplayMessage(player, "No mobs found in this radius!");
                        }
                        else
                            DisplayMessage(player, "Radius not valid");
                        #endregion
                    }
                    break;

                case "view":
                    {
                        #region view
                        if (GetClientSpot(client).Count != 0)
                        {
                            DisplayMessage(player, "There are " + GetClientSpot(client).Count + " mobs loaded : ");
                            foreach (GameNpc npc in GetClientSpot(client))
                                DisplayMessage(player, npc.Name);
                        }
                        else
                            DisplayMessage(player, "There are no mobs in the list!");
                        #endregion
                    }
                    break;
                
                case "clear":
                    {
                        #region clear
                        ClearClientSpot(client);
                        DisplayMessage(player, "Spot cleared!");
                        #endregion
                    }
                    break;

                case "spawn":
                    {
                        #region spawn
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (GetClientSpot(client).Count != 0)
                        {
                            ushort radius;

                            if (ushort.TryParse(args[2], out radius))
                            {
                                if (radius < 0) radius = 0;
                                if (radius > 5000) radius = 5000;

                                foreach (GameNpc npc in GetClientSpot(client))
                                {
                                    if (npc is GameSummonedPet) continue;
                                    
                                    copy(client, npc, radius);
                                }
                            }
                            else
                                DisplayMessage(player, "Radius not valid");

                            DisplayMessage(player, GetClientSpot(client).Count + " added in the new spot!");
                        }
                        else
                            DisplayMessage(player, "There are no spots loaded to release!");
                        #endregion
                    }
                    break;

                case "npcsetupcreate":
                    {
                        #region create
                        if(args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        ERealm realm = ERealm.None;

                        switch(args[2])
                        {
                            case "alb" : realm= ERealm.Albion; break;
                            case "mid" : realm= ERealm.Midgard; break;
                            case "hib" : realm= ERealm.Hibernia; break;
                        }

                        IList<GameNpc> setupmobs = new List<GameNpc>();
                        GameNpc mob;
                        GameNpc merchant;

                        #region load list
                        //base 
                    
                        mob = new GameHealer();
                        mob.GuildName = "Healer";
                        setupmobs.Add(mob);

                        //guild/name
                        mob=new GuildRegistrar();
                        mob.GuildName = "Guild Registrar";
                        setupmobs.Add(mob);
                        mob = new GuildEmblemeer();
                        mob.GuildName = "Emblem NPC";
                        setupmobs.Add(mob);
                        mob = new NameRegistrar();
                        mob.GuildName = "Name Registrar";
                        setupmobs.Add(mob);

                        //items
                    
                        mob = new GameVaultKeeper();
                        mob.GuildName = "VaultKeeper";
                        setupmobs.Add(mob);
                    
                        #endregion load list

                        // spawn in  2 circle
                        for (int i = 0; i < setupmobs.Count/2; i++)
                        {
                            ushort h = (ushort)(4096 / (setupmobs.Count/2) * i);
                            Point2D loc = client.Player.GetPointFromHeading(h, 150);
                            spawnsetupmob(client, setupmobs[i], realm,loc.X, loc.Y, h);
                        
                        }
                        for (int i = setupmobs.Count/2; i < setupmobs.Count; i++)
                        {
                            ushort h = (ushort)(4096 / (setupmobs.Count-setupmobs.Count/2) * i);
                            Point2D loc = client.Player.GetPointFromHeading(h, 250);
                            spawnsetupmob(client, setupmobs[i], realm, loc.X, loc.Y, h);
                        }
                        client.Out.SendMessage(setupmobs.Count + " setup mob spawned!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);

                        #endregion create
                    }
                    break;
                case "copy":
                    {
                        #region copy
                        GameNpc targetMob = null;

                        
                        if (client.Player.TargetObject != null && client.Player.TargetObject is GameNpc)
                            targetMob = (GameNpc)client.Player.TargetObject;

                        int num;

                        if (int.TryParse(args[2], out num))
                        {
                            if (num <= 0) num = 1;
                            if (num > 20) num = 20;
                        }
                        else
                            num = 1;

                        for (int i = 0; i < num; i++)
                        {
                            copy(client, targetMob, 150);
                            #region old
                            GameNpc mob = null;

                            if (targetMob == null)
                            {
                                client.Out.SendMessage("You must have a mob targeted to copy.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                return;
                            }


                            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                mob = (GameNpc)assembly.CreateInstance(targetMob.GetType().FullName, true);
                                if (mob != null)
                                    break;
                            }

                            if (mob == null)
                            {
                                client.Out.SendMessage("There was an error creating an instance of " + targetMob.GetType().FullName + "!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                return;
                            }
                            // Load NPCTemplate before overriding NPCTemplate's variables
                            mob.LoadTemplate(targetMob.NPCTemplate);

                            //Fill the object variables
                            mob.X = Util.Random(client.Player.X - 250 / 2, client.Player.X + 250 / 2);
                            mob.Y = Util.Random(client.Player.Y - 250 / 2, client.Player.Y + 250 / 2);
                            mob.Z = client.Player.Z;
                            mob.CurrentRegion = client.Player.CurrentRegion;
                            mob.Heading = (ushort)Util.Random(1, 4100);
                            mob.Level = targetMob.Level;
                            mob.Realm = targetMob.Realm;
                            mob.Name = targetMob.Name;
                            mob.Model = targetMob.Model;
                            mob.Flags = targetMob.Flags;
                            mob.MeleeDamageType = targetMob.MeleeDamageType;
                            mob.RespawnInterval = targetMob.RespawnInterval;
                            mob.RoamingRange = targetMob.RoamingRange;
                            mob.MaxDistance = targetMob.MaxDistance;
                            mob.BodyType = targetMob.BodyType;

                            // also copies the stats

                            mob.Strength = targetMob.Strength;
                            mob.Constitution = targetMob.Constitution;
                            mob.Dexterity = targetMob.Dexterity;
                            mob.Quickness = targetMob.Quickness;
                            mob.Intelligence = targetMob.Intelligence;
                            mob.Empathy = targetMob.Empathy;
                            mob.Piety = targetMob.Piety;
                            mob.Charisma = targetMob.Charisma;

                            //Fill the living variables
                            mob.CurrentSpeed = 0;
                            mob.MaxSpeedBase = targetMob.MaxSpeedBase;
                            mob.GuildName = targetMob.GuildName;
                            mob.Size = targetMob.Size;
                            mob.Race = targetMob.Race;

                            mob.Inventory = targetMob.Inventory;
                            if (mob.Inventory != null)
                                mob.SwitchWeapon(targetMob.ActiveWeaponSlot);

                            mob.EquipmentTemplateID = targetMob.EquipmentTemplateID;

                            if (mob is GameMerchant)
                            {
                                ((GameMerchant)mob).TradeItems = ((GameMerchant)targetMob).TradeItems;
                            }

                            ABrain brain = null;
                            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                brain = (ABrain)assembly.CreateInstance(targetMob.Brain.GetType().FullName, true);
                                if (brain != null)
                                    break;
                            }

                            if (brain == null)
                            {
                                client.Out.SendMessage("Cannot create brain, standard brain being applied", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                mob.SetOwnBrain(new StandardMobBrain());
                            }
                            else if (brain is StandardMobBrain)
                            {
                                StandardMobBrain sbrain = (StandardMobBrain)brain;
                                StandardMobBrain tsbrain = (StandardMobBrain)targetMob.Brain;
                                sbrain.AggroLevel = tsbrain.AggroLevel;
                                sbrain.AggroRange = tsbrain.AggroRange;
                                mob.SetOwnBrain(sbrain);
                            }

                            mob.PackageID = targetMob.PackageID;
                            mob.OwnerID = targetMob.OwnerID;

                            mob.AddToWorld();
                            mob.LoadedFromScript = false;
                            mob.SaveIntoDatabase();
                            client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            if ((mob.Flags & ENpcFlags.PEACE) != 0)
                            {
                                // because copying 100 mobs with their peace flag set is not fun
                                client.Out.SendMessage("This mobs PEACE flag is set!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                            }
                            #endregion old
                        }
                        #endregion copy
                    }
                    break;
                case "randomspawn":
                    {
                        #region randomspawn
                        

                        if (args.Length < 4)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        ushort radius, newradius;

                        if (ushort.TryParse(args[2], out radius) && ushort.TryParse(args[3], out newradius))
                        {
                            if (radius < 0) radius = 0;
                            if (radius > 5000) radius = 5000;
                            if (newradius < 0) newradius = 0;
                            if (newradius > 5000) newradius = 5000;

                            foreach (GameNpc npc in player.GetNPCsInRadius(radius))
                                if (npc.Realm == ERealm.None)
                                    move(client, npc, newradius);
                        }
                        else
                            DisplayMessage(player, "Radius not valid");

                        DisplayMessage(player, "Spot respawned! ");

                        #endregion randomspawn
                    }
                    break;

                case "setlevels":
                    {
                        #region setlevels
                        if (args.Length < 4)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        ushort radius;

                        if (ushort.TryParse(args[2], out radius))
                        {
                            if (radius < 0) radius = 0;
                            if (radius > 5000) radius = 5000;

                            if (args.Length > 4)
                            {
                                byte min, max;
                                if (byte.TryParse(args[3], out min) && byte.TryParse(args[4], out max))
                                {
                                    if (min <= 0) min = 1;
                                    if (min > 254) min = 254;
                                    if (max <= 1) max = 2;
                                    if (max > 255) min = 255;

                                    foreach (GameNpc npc in player.GetNPCsInRadius(radius))
                                        if (npc.Realm == ERealm.None)
                                        {
                                            npc.Level = (byte)Util.Random(min, max);
                                            npc.AutoSetStats();
                                            npc.SaveIntoDatabase();
                                        }
                                }
                            }
                            else
                            {
                                byte nlvl;
                                if (byte.TryParse(args[3], out nlvl))
                                {
                                    if (nlvl < 0) nlvl = 1;
                                    if (nlvl > 255) nlvl = 255;

                                    foreach (GameNpc npc in player.GetNPCsInRadius(radius))
                                    {
                                        if (npc.Realm == ERealm.None)
                                        {
                                            npc.Level = nlvl;
                                            npc.AutoSetStats();
                                            npc.SaveIntoDatabase();
                                        }
                                    }
                                }
                            }
                        }
                        else
                            DisplayMessage(player, "Radius not valid");
                        #endregion setlevels
                    }
                    break;

                case "setrealm":
                    {
                        #region setrealm
                        if (args.Length < 4)
                        {
                            DisplaySyntax(client);
                            return;
                        }
                        ushort radius;
                        byte realm;

                        if (ushort.TryParse(args[2], out radius) && byte.TryParse(args[3],out realm))
                        {
                            //adjustements
                            if (radius < 0) radius = 0;
                            if (radius > 5000) radius = 5000;
                            if (realm < 0 && realm > 3) realm = 0;

                            foreach (GameNpc npc in player.GetNPCsInRadius(radius))
                            {
                                npc.Realm = (ERealm)realm;
                                npc.SaveIntoDatabase();
                            }
                        }
                        else
                            DisplayMessage(player, "Invalid Radius or realm value");
                        #endregion setlevels
                    }
                    break;
                case "remove":
                    {
                        #region remove
                        ushort radius;
                        if (ushort.TryParse(args[2], out radius))
                        {
                            if (radius < 0) radius = 0;
                            if (radius > 5000) radius = 5000;

                            foreach (GameNpc npc in player.GetNPCsInRadius(radius))
                                if (npc.Realm == ERealm.None && !(npc is GameTrainingDummy))
                                    remove(npc);
                        }
                        else
                            DisplayMessage(player, "Radius not valid");
                        #endregion remove
                    }
                    break;
            }
        }

        private void spawnsetupmob(GameClient client, GameNpc mob, ERealm realm,int x,int y, ushort h)
        {
            //Fill the object variables
            mob.X = x;
            mob.Y = y;
            mob.Z = client.Player.Z;
            mob.Heading = h;

            mob.CurrentRegion = client.Player.CurrentRegion;
            mob.Level = 50;
            if (mob is GameTrainingDummy)
                mob.Realm = ERealm.None;
            else
            {
                mob.Realm = realm;
                mob.Flags |= ENpcFlags.PEACE;
            }
            mob.CurrentSpeed = 0;
            mob.AddToWorld();
            mob.LoadedFromScript = false; // allow saving
            mob.SaveIntoDatabase();
            client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, EChatType.CT_System, EChatLoc.CL_SystemWindow);
        }

        private void copy(GameClient client,GameNpc targetMob,ushort radius)
        {
                GameNpc mob = null;

                if (targetMob == null)
                {
                    client.Out.SendMessage("You must have a mob targeted to copy.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return;
                }


                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    mob = (GameNpc)assembly.CreateInstance(targetMob.GetType().FullName, true);
                    if (mob != null)
                        break;
                }

                if (mob == null)
                {
                    client.Out.SendMessage("There was an error creating an instance of " + targetMob.GetType().FullName + "!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return;
                }
                // Load NPCTemplate before overriding NPCTemplate's variables
                mob.LoadTemplate(targetMob.NPCTemplate);

                //Fill the object variables
                mob.X = Util.Random(client.Player.X - radius, client.Player.X + radius);
                mob.Y = Util.Random(client.Player.Y - radius, client.Player.Y + radius);
                mob.Z = client.Player.Z;
                mob.CurrentRegion = client.Player.CurrentRegion;
                mob.Heading = (ushort)Util.Random(1, 4100);
                mob.Level = targetMob.Level;
                mob.Realm = targetMob.Realm;
                mob.Name = targetMob.Name;
                mob.Model = targetMob.Model;
                mob.Flags = targetMob.Flags;
                mob.MeleeDamageType = targetMob.MeleeDamageType;
                mob.RespawnInterval = targetMob.RespawnInterval;
                mob.RoamingRange = targetMob.RoamingRange;
                mob.MaxDistance = targetMob.MaxDistance;
                mob.BodyType = targetMob.BodyType;

                // also copies the stats

                mob.Strength = targetMob.Strength;
                mob.Constitution = targetMob.Constitution;
                mob.Dexterity = targetMob.Dexterity;
                mob.Quickness = targetMob.Quickness;
                mob.Intelligence = targetMob.Intelligence;
                mob.Empathy = targetMob.Empathy;
                mob.Piety = targetMob.Piety;
                mob.Charisma = targetMob.Charisma;

                //Fill the living variables
                mob.CurrentSpeed = 0;
                mob.MaxSpeedBase = targetMob.MaxSpeedBase;
                mob.GuildName = targetMob.GuildName;
                mob.Size = targetMob.Size;
                mob.Race = targetMob.Race;

                mob.Inventory = targetMob.Inventory;
                if (mob.Inventory != null)
                    mob.SwitchWeapon(targetMob.ActiveWeaponSlot);

                mob.EquipmentTemplateID = targetMob.EquipmentTemplateID;

                if (mob is GameMerchant)
                {
                    ((GameMerchant)mob).TradeItems = ((GameMerchant)targetMob).TradeItems;
                }

                ABrain brain = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    brain = (ABrain)assembly.CreateInstance(targetMob.Brain.GetType().FullName, true);
                    if (brain != null)
                        break;
                }

                if (brain == null)
                {
                    client.Out.SendMessage("Cannot create brain, standard brain being applied", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    mob.SetOwnBrain(new StandardMobBrain());
                }
                else if (brain is StandardMobBrain)
                {
                    StandardMobBrain sbrain = (StandardMobBrain)brain;
                    StandardMobBrain tsbrain = (StandardMobBrain)targetMob.Brain;
                    sbrain.AggroLevel = tsbrain.AggroLevel;
                    sbrain.AggroRange = tsbrain.AggroRange;
                    mob.SetOwnBrain(sbrain);
                }

                mob.PackageID = targetMob.PackageID;
                mob.OwnerID = targetMob.OwnerID;

                mob.AddToWorld();
                mob.LoadedFromScript = false;
                mob.SaveIntoDatabase();
                client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, EChatType.CT_System, EChatLoc.CL_SystemWindow);
                if ((mob.Flags & ENpcFlags.PEACE) != 0)
                {
                    // because copying 100 mobs with their peace flag set is not fun
                    client.Out.SendMessage("This mobs PEACE flag is set!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
                }
        }

        private void remove(GameNpc targetMob)
        {
            targetMob.StopAttack();
            targetMob.StopCurrentSpellcast();
            targetMob.DeleteFromDatabase();
            targetMob.Delete();
        }

        private void move(GameClient client, GameNpc targetMob, ushort radius)
        {

            int X = Util.Random(client.Player.X - radius / 2, client.Player.X + radius / 2);
            int Y = Util.Random(client.Player.Y - radius / 2, client.Player.Y + radius / 2);
            targetMob.MoveTo(client.Player.CurrentRegionID, X, Y, client.Player.Z, (ushort)Util.Random(1, 4100));
            targetMob.SaveIntoDatabase();
        }
        
    }
}