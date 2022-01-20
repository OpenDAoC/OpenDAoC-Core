using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.AI;
using DOL.spells;
using System.Collections;
using DOL.Database;
using System.Reflection;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [Cmd("&spammob", //command to handle
         ePrivLevel.GM, //minimum privelege level
         "Mob creation and modification commands.", //command description
         "/spammob create [amount]"
        )]
    public class SpamMobCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length == 2)
            {
                //create one mob
                string theType = "DOL.GS.SpamMob.SpamMobNPC";
                byte realm = 0;

                //Create a new mob
                GameNPC mob = null;

                foreach (Assembly script in ScriptMgr.GameServerScripts)
                {
                    try
                    {
                        client.Out.SendDebugMessage(script.FullName);
                        mob = (GameNPC)script.CreateInstance(theType, false);

                        if (mob != null)
                            break;
                    }
                    catch (Exception e)
                    {
                        client.Out.SendMessage(e.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                    }
                }

                if (mob == null)
                {
                    client.Out.SendMessage("There was an error creating an instance of " + theType + "!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                //Fill the object variables
                mob.X = client.Player.X;
                mob.Y = client.Player.Y;
                mob.Z = client.Player.Z;
                mob.CurrentRegion = client.Player.CurrentRegion;
                mob.Heading = client.Player.Heading;
                mob.Level = 50;
                mob.Realm = (eRealm)realm;
                mob.Name = "Spam Mob";
                mob.Model = 34;

                //Fill the living variables
                mob.CurrentSpeed = 0;
                mob.MaxSpeedBase = 200;
                mob.GuildName = "Burn Baby Burn";
                mob.Size = 50;
                mob.Flags |= GameNPC.eFlags.PEACE;
                mob.Flags ^= GameNPC.eFlags.PEACE;
                mob.AddToWorld();
                //mob.LoadedFromScript = false; // allow saving
                //mob.SaveIntoDatabase();
                //client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                //client.Out.SendMessage("The mob has been created with the peace flag, so it can't be attacked, to remove type /mob peace", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            }
            else if (args.Length == 3)
            {
                if (args[1].Equals("create"))
                {
                    int temp = 0;
                    int.TryParse(args[2],out temp);
                    for (int i = 0; i < temp; i++)
                    {
                        string theType = "DOL.GS.SpamMob.SpamMobNPC";
                        byte realm = 0;

                        //Create a new mob
                        GameNPC mob = null;

                        foreach (Assembly script in ScriptMgr.GameServerScripts)
                        {
                            try
                            {
                                client.Out.SendDebugMessage(script.FullName);
                                mob = (GameNPC)script.CreateInstance(theType, false);

                                if (mob != null)
                                    break;
                            }
                            catch (Exception e)
                            {
                                client.Out.SendMessage(e.ToString(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            }
                        }

                        if (mob == null)
                        {
                            client.Out.SendMessage("There was an error creating an instance of " + theType + "!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        //Fill the object variables
                        mob.X = client.Player.X;
                        mob.Y = client.Player.Y;
                        mob.Z = client.Player.Z;
                        mob.CurrentRegion = client.Player.CurrentRegion;
                        mob.Heading = client.Player.Heading;
                        mob.Level = 50;
                        mob.Realm = (eRealm)realm;
                        mob.Name = "Spam Mob";
                        mob.Model = 34;

                        //Fill the living variables
                        mob.CurrentSpeed = 0;
                        mob.MaxSpeedBase = 200;
                        mob.GuildName = "Burn Baby Burn";
                        mob.Size = 50;
                        mob.Flags |= GameNPC.eFlags.PEACE;
                        mob.Flags ^= GameNPC.eFlags.PEACE;
                        mob.AddToWorld();
                        //mob.LoadedFromScript = false; // allow saving
                        //mob.SaveIntoDatabase();
                        //client.Out.SendMessage("Mob created: OID=" + mob.ObjectID, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        //client.Out.SendMessage("The mob has been created with the peace flag, so it can't be attacked, to remove type /mob peace", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    }
                    //create multiple mobs
                }
            }


        }
    }
    
}
namespace DOL.GS.SpamMob
{
    public class SpamMobBrain : StandardMobBrain
    {
        public SpamMobBrain()
        {
        }

        public override void Think()
        {
            base.Think();
        }
        public override bool CheckSpells(eCheckSpellType type)
        {
            if (Body.IsCasting)
                return true;

            bool casted = false;

            if (Body != null && Body.Spells != null && Body.Spells.Count > 0)
            {
                ArrayList spell_rec = new ArrayList();
                Spell spellToCast = null;

                if (type == eCheckSpellType.Defensive)
                {
                    foreach (Spell spell in Body.Spells)
                    {
                        if (Body.GetSkillDisabledDuration(spell) > 0) continue;
                        if (spell.Target.ToLower() == "enemy" || spell.Target.ToLower() == "area" || spell.Target.ToLower() == "cone") continue;

                        if (spell.Uninterruptible && CheckDefensiveSpells(spell))
                            casted = true;
                        else if (!Body.IsBeingInterrupted && CheckDefensiveSpells(spell))
                            casted = true;
                    }
                }
                else if (type == eCheckSpellType.Offensive)
                {
                    foreach (Spell spell in Body.Spells)
                    {

                        if (Body.GetSkillDisabledDuration(spell) == 0)
                        {
                            if (spell.CastTime > 0)
                            {
                                if (spell.Target.ToLower() == "enemy" || spell.Target.ToLower() == "area" || spell.Target.ToLower() == "cone")
                                    spell_rec.Add(spell);
                            }
                        }
                    }
                    if (spell_rec.Count > 0)
                    {
                        spellToCast = (Spell)spell_rec[Util.Random((spell_rec.Count - 1))];


                        if (spellToCast.Uninterruptible && CheckOffensiveSpells(spellToCast))
                            casted = true;
                        else
                            if (!Body.IsBeingInterrupted && CheckOffensiveSpells(spellToCast))
                            casted = true;
                    }
                }

                return casted;
            }
            return casted;
        }
    }

    public class SpamMobNPC : GameNPC
    {
        private Spell af;
        private Spell str;
        private Spell con;
        private Spell dex;

        public SpamMobNPC() : base(new SpamMobBrain())
        {
            if (af == null)
            {
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.Concentration = 0;
                spell.ClientEffect = 1467;
                spell.Icon = 1467;
                spell.Duration = 5;
                spell.Value = 20; //Effective buff 58
                spell.Name = "Armor of the Realm";
                spell.Description = "Adds to the recipient's Armor Factor (AF) resulting in better protection againts some forms of attack. It acts in addition to any armor the target is wearing.";
                spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                spell.SpellID = 88001;
                spell.Target = "Self";
                spell.Message1 = "Increases target's Base Armor Factor by 20.";
                spell.Type = eSpellType.ArmorFactorBuff.ToString();
                spell.EffectGroup = 1;

                af = new Spell(spell, 0);
            }
            if (str == null)
            {
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.Concentration = 0;
                spell.ClientEffect = 5004;
                spell.Icon = 5004;
                spell.Duration = 5;
                spell.Value = 20; //effective buff 55
                spell.Name = "Strength of the Realm";
                spell.Description = "Increases target's Strength.";
                spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                spell.SpellID = 88002;
                spell.Target = "Self";
                spell.Message1 = "Increases target's Strength by 20.";
                spell.Type = eSpellType.StrengthBuff.ToString();
                spell.EffectGroup = 4;

                str = new Spell(spell, 0);
            }
            if (con == null)
            {
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.Concentration = 0;
                spell.ClientEffect = 5034;
                spell.Icon = 5034;
                spell.Duration = 5;
                spell.Value = 20; //effective buff 55
                spell.Name = "Fortitude of the Realm";
                spell.Description = "Increases target's Constitution.";
                spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                spell.SpellID = 88003;
                spell.Target = "Self";
                spell.Message1 = "Increases target's Constitution by 20.";
                spell.Type = eSpellType.ConstitutionBuff.ToString();
                spell.EffectGroup = 201;

                con = new Spell(spell, 0);
            }
            if (dex == null)
            {
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.Concentration = 0;
                spell.ClientEffect = 5024;
                spell.Icon = 5024;
                spell.Duration = 5;
                spell.Value = 20; //effective buff 55
                spell.Name = "Dexterity of the Realm";
                spell.Description = "Increases Dexterity for a character.";
                spell.Range = WorldMgr.VISIBILITY_DISTANCE;
                spell.SpellID = 88004;
                spell.Target = "Self";
                spell.Message1 = "Increases target's Dexterity by 20.";
                spell.Type = eSpellType.DexterityBuff.ToString();
                spell.EffectGroup = 202;

                dex = new Spell(spell, 39);
            }

            Spells = new List<Spell>
            {
                SkillBase.GetSpellByID(10111),
                SkillBase.GetSpellByID(1311),
                af,
                str,
                con,
                dex
            };
        }
        public override bool CastSpell(Spell spell, SpellLine line)
        {
            return castingComponent.StartCastSpell(spell, line);
        }
        public override bool CastSpell(Spell spell, SpellLine line, bool checkLOS)
        {
            return castingComponent.StartCastSpell(spell, line); 
        }

    }
}
