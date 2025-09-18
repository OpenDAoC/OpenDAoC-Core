using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PlayerClass;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Keeps
{
    public class GuardCaster : GameKeepGuard
    {
        private static Dictionary<eRealm, Spell> _spells;

        static GuardCaster()
        {
            Spell spell;
            _spells = new(3);

            spell = new Spell(CreateAlbionSpell(), 0);
            _spells[eRealm.Albion] = spell;
            AddScriptedSpell(spell);

            spell = new Spell(CreateMidgardSpell(), 0);
            _spells[eRealm.Midgard] = spell;
            AddScriptedSpell(spell);

            spell = new Spell(CreateHiberniaSpell(), 0);
            _spells[eRealm.Hibernia] = spell;
            AddScriptedSpell(spell);

            static DbSpell CreateAlbionSpell()
            {
                return new()
                {
                    AllowAdd = false,
                    CastTime = 3,
                    ClientEffect = 77,
                    Damage = 170,
                    Name = string.Empty,
                    Description = string.Empty,
                    Range = 1500,
                    Target = eSpellTarget.ENEMY.ToString(),
                    Type = eSpellType.DirectDamage.ToString(),
                    DamageType = (int) eDamageType.Matter
                };
            }

            static DbSpell CreateMidgardSpell()
            {
                return new()
                {
                    AllowAdd = false,
                    CastTime = 3,
                    ClientEffect = 2570,
                    Damage = 170,
                    Name = string.Empty,
                    Description = string.Empty,
                    Range = 1500,
                    Target = eSpellTarget.ENEMY.ToString(),
                    Type = eSpellType.DirectDamage.ToString(),
                    DamageType = (int) eDamageType.Cold
                };
            }

            static DbSpell CreateHiberniaSpell()
            {
                return new()
                {
                    AllowAdd = false,
                    CastTime = 3,
                    ClientEffect = 4269,
                    Damage = 170,
                    Name = string.Empty,
                    Description = string.Empty,
                    Range = 1500,
                    Target = eSpellTarget.ENEMY.ToString(),
                    Type = eSpellType.DirectDamage.ToString(),
                    DamageType = (int) eDamageType.Heat
                };
            }

            static void AddScriptedSpell(Spell spell)
            {
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, spell);
            }
        }

        public override byte Level
        {
            get => base.Level;
            set
            {
                base.Level = value;
                SetAndScaleSpell();
            }
        }

        public GuardCaster() : base() { }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            return base.GetArmorAbsorb(slot) - 0.05;
        }

        protected override ICharacterClass GetClass()
        {
            return ModelRealm switch
            {
                eRealm.Albion => new ClassWizard(),
                eRealm.Midgard => new ClassRunemaster(),
                eRealm.Hibernia => new ClassEldritch(),
                _ => new DefaultCharacterClass()
            };
        }

        protected override KeepGuardBrain GetBrain()
        {
            return new CasterBrain();
        }

        protected override void SetName()
        {
            switch (ModelRealm)
            {
                case eRealm.None:
                case eRealm.Albion:
                {
                    if (IsPortalKeepGuard)
                        Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.MasterWizard");
                    else
                        Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Wizard");

                    break;
                }
                case eRealm.Midgard:
                {
                    if (IsPortalKeepGuard)
                        Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.MasterRunes");
                    else
                        Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Runemaster");

                    break;
                }
                case eRealm.Hibernia:
                {
                    if (IsPortalKeepGuard)
                        Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.MasterEldritch");
                    else
                        Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Eldritch");

                    break;
                }
            }

            if (Realm is eRealm.None)
                Name = LanguageMgr.GetTranslation(Properties.SERV_LANGUAGE, "SetGuardName.Renegade", Name);
        }

        public override void SetSpells()
        {
            SetAndScaleSpell();
        }

        private void SetAndScaleSpell()
        {
            Spell spell;

            switch (ModelRealm)
            {
                case eRealm.Albion:
                case eRealm.Midgard:
                case eRealm.Hibernia:
                {
                    spell = _spells[ModelRealm];
                    break;
                }
                default:
                {
                    spell = _spells.Values.ToList()[Util.Random(_spells.Count - 1)];
                    break;
                }
            }

            Spells = [GetScaledSpell(spell)];
        }
    }
}
