using Core.GS.ServerProperties;

namespace Core.GS.API;

public class ApiPasswordVerification
{
    public ApiPasswordVerification()
    {

    }

    public bool VerifyAPIPassword(string password)
    {
        var apiPassword = Properties.API_PASSWORD;
        if (apiPassword is (null or "")) return false;
        if (password is (null or "")) return false;
        if (password != apiPassword) return false;
        return true;
    }
}
