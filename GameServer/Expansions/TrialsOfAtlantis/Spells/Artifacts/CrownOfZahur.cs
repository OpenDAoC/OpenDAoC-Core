using System;
using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Languages;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("EfficientHealing")]
public class EfficientHealing : SpellHandler
{
    #region Devle Info
    public override IList<string> DelveInfo
    {
        get
        {
            var list = new List<string>(16);

            //Name
            list.Add("Name: " + Spell.Name);
            list.Add("");

            //Description
            list.Add("Description: " + Spell.Description);
            list.Add("");

            //SpellType
            list.Add("Type: " + Spell.SpellType);

            //Value
            if (Spell.Value != 0)
                list.Add("Efficient Heal: " + Spell.Value + "%");

            //Target
            list.Add("Target: " + Spell.Target);

            //Duration
            if (Spell.Duration >= ushort.MaxValue * 1000)
                list.Add("Duration: Permanent.");
            else if (Spell.Duration > 60000)
                list.Add(string.Format("Duration: {0}:{1} min", Spell.Duration / 60000, (Spell.Duration % 60000 / 1000).ToString("00")));
            //Cost
            list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));

            //Cast
            list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
            return list;
        }
    }
    #endregion
    public EfficientHealing(GameLiving caster, Spell spell, SpellLine line)
        : base(caster, spell, line)
    {
    }
}
//handled in HealSpellHandler
[SpellHandler("EfficientEndurance")]
public class EfficientEndurance : SpellHandler
{
    #region Devle Info
    public override IList<string> DelveInfo
    {
        get
        {
            var list = new List<string>(16);

            //Name
            list.Add("Name: " + Spell.Name);
            list.Add("");

            //Description
            list.Add("Description: " + Spell.Description);
            list.Add("");

            //SpellType
            list.Add("Type: " + Spell.SpellType);

            //Value
            if (Spell.Value != 0)
                list.Add("Efficient Endurance: " + Spell.Value + "%");

            //Target
            list.Add("Target: " + Spell.Target);

            //Duration
            if (Spell.Duration >= ushort.MaxValue * 1000)
                list.Add("Duration: Permanent.");
            else if (Spell.Duration > 60000)
                list.Add(string.Format("Duration: {0}:{1} min", Spell.Duration / 60000, (Spell.Duration % 60000 / 1000).ToString("00")));
            //Cost
            list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
            
            //Cast
            list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
            return list;
        }
    }
    #endregion
    public EfficientEndurance(GameLiving caster, Spell spell, SpellLine line)
        : base(caster, spell, line)
    {
    }
}
[SpellHandler("Powershield")]
public class Powershield : SpellHandler
{
    public void AttackedEvent(CoreEvent e, object sender, EventArgs arguments)
    {
        GamePlayer player = (GamePlayer)sender;
        AttackedByEnemyEventArgs args = arguments as AttackedByEnemyEventArgs;
        int HalfHP = player.MaxHealth / 2;
        double ManaHeal = player.MaxMana * 0.25;
        if (args == null || args.AttackData == null)
        {
            return;
        }
        if (player.Health <= HalfHP)
        {
            if (player.Mana < ManaHeal)
            {
                player.Health += player.Mana;
                player.Mana -= (int)ManaHeal;
                player.Out.SendMessage("You convert "+player.Mana+" power into hit points!", EChatType.CT_Spell, EChatLoc.CL_ChatWindow);
                return;
            }
            player.Health += (int)ManaHeal;
            player.Mana -= (int)ManaHeal;
            player.Out.SendMessage("You convert " + ManaHeal + " power into hit points!", EChatType.CT_Spell, EChatLoc.CL_ChatWindow);
            return;
        }
    }
    public override void OnEffectStart(GameSpellEffect effect)
    {
        GameEventMgr.AddHandler(m_caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(AttackedEvent));
    }
    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        GameEventMgr.RemoveHandler(m_caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(AttackedEvent));
        return 0;
    }
    #region Devle Info
    public override IList<string> DelveInfo
    {
        get
        {
            var list = new List<string>(16);

            //Name
            list.Add("Name: " + Spell.Name);
            list.Add("");

            //Description
            list.Add("Description: " + Spell.Description);
            list.Add("");

            //SpellType
            list.Add("Type: " + Spell.SpellType);

            //Value
            if (Spell.Value != 0)
                list.Add("Power Converted: " + Spell.Value + "%");

            //Target
            list.Add("Target: " + Spell.Target);

            //Duration
            if (Spell.Duration >= ushort.MaxValue * 1000)
                list.Add("Duration: Permanent.");
            else if (Spell.Duration > 60000)
                list.Add(string.Format("Duration: {0}:{1} min", Spell.Duration / 60000, (Spell.Duration % 60000 / 1000).ToString("00")));
            //Cost
            list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));

            //Cast
            list.Add(LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
            return list;
        }
    }
    #endregion
    public Powershield(GameLiving caster, Spell spell, SpellLine line)
        : base(caster, spell, line)
    {
    }
}