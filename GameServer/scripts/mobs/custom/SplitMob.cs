using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DOL.AI;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS.PacketHandler;

using log4net;
namespace DOL.GS.Scripts
{
    public class SplitMob : GameNPC
    {
        public bool m_First = true;
        public SplitMob()
        {
        }
        public static bool AnyMinions(GameNPC checker)
        {
            foreach (GameNPC npc in checker.GetNPCsInRadius(10000))
            {
                if (npc.Name.Contains("Minion"))
                {
                    return true;
                }
            }
            if (checker.Name.Contains("Minion"))
            {
                return true;
            }
            return false;
        }
        public void Split(GamePlayer player)
        {
            bool check = false;
            if (this.Level < 45)
            {
                if (!AnyMinions(this))
                {
                    check = true;
                }
            }
            if (check == true)
                return;
            this.Level -= 2;
            this.Health = this.MaxHealth;
            this.Size = ((byte)Math.Max(this.Size - 5, 20));
            SplitMob mob = new SplitMob();
            SetVariables(mob);
            mob.AddToWorld();

            mob.StartAttack(player);


        }
        public void SetVariables(GameNPC mob)
        {
            mob.X = this.X + 10;
            mob.Y = this.Y + 10;
			mob.Z = this.Z;
			mob.CurrentRegion = this.CurrentRegion;
			mob.Heading = this.Heading;
			mob.Level = this.Level;
			mob.Realm = this.Realm;
			mob.Name = "Split's Minion";
			mob.Model = this.Model;
			mob.Flags = this.Flags;
			mob.MeleeDamageType = this.MeleeDamageType;
            mob.RespawnInterval = -1; // dont respawn
			mob.RoamingRange = this.RoamingRange;
            mob.MaxDistance = 4000;

			// also copies the stats

			mob.Strength = this.Strength;
			mob.Constitution = this.Constitution;
			mob.Dexterity = this.Dexterity;
			mob.Quickness = this.Quickness;
			mob.Intelligence = this.Intelligence;
			mob.Empathy = this.Empathy;
			mob.Piety = this.Piety;
			mob.Charisma = this.Charisma;

			//Fill the living variables
			mob.CurrentSpeed = 0;

			mob.MaxSpeedBase = this.MaxSpeedBase;
			mob.Size = this.Size;
			mob.NPCTemplate = this.NPCTemplate;
			mob.Inventory = this.Inventory;
			mob.EquipmentTemplateID = this.EquipmentTemplateID;
			if (mob.Inventory != null)
				mob.SwitchWeapon(this.ActiveWeaponSlot);

			ABrain brain = null;
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				brain = (ABrain)assembly.CreateInstance(this.Brain.GetType().FullName, true);
				if (brain != null)
					break;
			}

			if (brain == null)
			{
				mob.SetOwnBrain(new StandardMobBrain());
			}
			else if (brain is StandardMobBrain)
			{
				StandardMobBrain sbrain = (StandardMobBrain)brain;
				StandardMobBrain tsbrain = (StandardMobBrain)this.Brain;
				sbrain.AggroLevel = tsbrain.AggroLevel;
				sbrain.AggroRange = tsbrain.AggroRange;
                mob.SetOwnBrain(sbrain);
            }
        }
        public void ResetToOriginal(GameNPC npc)
        {
            npc.Level = 70;
            npc.Health = this.MaxHealth;
        }
        public override void Die(GameObject killer)
        {
            this.Level = 60;
            this.Size = 100;
            base.Die(killer);
            if (this.Name == "Split")
            {
                foreach (GamePlayer player in this.GetPlayersInRadius(3000))
                {
                    SendReply(player, "You have defeated " + this.Name + " and you gain 5000 bounty points");
                    player.GainBountyPoints(5000, false);

                }
                foreach (GameNPC npc in this.GetNPCsInRadius(5000))
                {
                    if (npc.Name.Contains("Minion"))
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            GamePlayer player = source as GamePlayer;
            if (player != null)
            {
                if (this.HealthPercent < 50)
                {
                    Split(player);
                }
            }
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }
        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }
    }


}