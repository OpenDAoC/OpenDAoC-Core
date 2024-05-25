namespace DOL.GS
{
    public class TheurgistAirPet : TheurgistPet
    {
        public override double MaxHealthScalingFactor => 0.4115;

        public TheurgistAirPet(INpcTemplate npcTemplate) : base(npcTemplate)
        {
            WeaponSkillScalingFactor = 11;

            foreach (Spell spell in Spells)
            {
                if (spell.IsInstantCast)
                    DisableSkill(spell, 0);
            }
        }

        public override void DisableSkill(Skill skill, int duration)
        {
            // Make air pet's instant stun a bit more random.
            // Should ideally be in its own class.
            if (skill is Spell spell && spell.IsInstantCast)
                duration += Util.Random((int)(spell.RecastDelay / 2.5));

            base.DisableSkill(skill, duration);
        }
    }
}
