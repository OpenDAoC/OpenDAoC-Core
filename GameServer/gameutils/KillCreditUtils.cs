using DOL.Database;

namespace DOL.GS;

public class KillCreditUtils
{
    public static string GetRequiredKillMob(string id_nb)
    {
        var mobToKill = DOLDB<ItemXKillCredit>.SelectObject(DB.Column("ItemTemplate")
            .IsEqualTo(id_nb));

        return mobToKill?.m_mobName;
    }
}