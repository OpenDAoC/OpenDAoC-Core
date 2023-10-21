using Core.Database;
using Core.Database.Tables;

namespace Core.GS.Spells
{
    //shared timer 1
    
    [SpellHandler("TangleSnare")]
    public class TanglesnareSpell : MineSpellHandler
    {
        // constructor
        public TanglesnareSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            Unstealth = false;

            //Construct a new mine.
            mine = new GameMine();
            mine.Model = 2592;
            mine.Name = spell.Name;
            mine.Realm = caster.Realm;
            mine.X = caster.X;
            mine.Y = caster.Y;
            mine.Z = caster.Z;
            mine.CurrentRegionID = caster.CurrentRegionID;
            mine.Heading = caster.Heading;
            mine.Owner = (GamePlayer)caster;

            // Construct the mine spell
            dbs = new DbSpell();
            dbs.Name = spell.Name;
            dbs.Icon = 7220;
            dbs.ClientEffect = 7220;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.SpeedDecrease.ToString();
            dbs.Value = spell.Value;
            dbs.Duration = spell.ResurrectHealth;
            dbs.Frequency = spell.ResurrectMana;
            dbs.Pulse = 0;
            dbs.PulsePower = 0;
            dbs.LifeDrainReturn = spell.LifeDrainReturn;
            dbs.Power = 0;
            dbs.CastTime = 0;
            dbs.Range = WorldMgr.VISIBILITY_DISTANCE;
            sRadius = 350;
            s = new Spell(dbs, 1);
            sl = SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells);
            trap = ScriptMgr.CreateSpellHandler(m_caster, s, sl);
        }
    }
}