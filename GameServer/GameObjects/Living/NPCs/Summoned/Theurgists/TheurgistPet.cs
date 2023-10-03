using DOL.Database;

namespace DOL.GS
{
	public class TheurgistPet : GameSummonedPet
	{
		public TheurgistPet(INpcTemplate npcTemplate) : base(npcTemplate) { }

		protected override void BuildAmbientTexts()
		{
			base.BuildAmbientTexts();

			// Not each summoned pet will fire ambient sentences.
			if (ambientTexts.Count > 0)
			{
				foreach (DbMobXAmbientBehavior ambientText in ambientTexts)
					ambientText.Chance /= 10;
			}
		}
	}
}
