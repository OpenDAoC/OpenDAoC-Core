/*
 * Script by dargon 
 * 
 * Npc with a more easy to control HP, set its Max hp with /mob cha (HP Amount), HP is not affected by CON
 * Please note CON is still needed for a Mobs Defense.
 */


using System.Reflection;
using System.Collections.Generic;
using DOL.AI;
using DOL.Language;
using DOL.GS.Effects;
using DOL.GS.Movement;
using DOL.GS.Quests;
using DOL.GS.Spells;
using DOL.GS.Utils;
using DOL.GS.Housing;
using DOL.GS.RealmAbilities;
using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using System.Threading;
using log4net;
using DOL.AI.Brain;

namespace DOL.GS.Scripts
{
    public class HPMob : GameNPC
    {
        
        public override bool AddToWorld()
        {
           
            Flags = 0;
            return base.AddToWorld();
        }

        
      
        public override int MaxHealth
        {
            get
            {
                return base.Charisma;
            }
        }

      
    }
}