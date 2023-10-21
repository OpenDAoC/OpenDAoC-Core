using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //no shared timer
    
    [SpellHandler("PowerTrap")]
    public class PowerTrapSpell : MineSpellHandler
    {
        // constructor
        public PowerTrapSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            //Construct a new mine.
            mine = new GameMine();
            mine.Model = 2590;
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
            dbs.Icon = 7313;
            dbs.ClientEffect = 7313;
            dbs.Damage = spell.Damage;
            dbs.DamageType = (int)spell.DamageType;
            dbs.Target = "Enemy";
            dbs.Radius = 0;
            dbs.Type = ESpellType.PowerRend.ToString();
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