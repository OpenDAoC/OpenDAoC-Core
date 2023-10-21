using Core.GS.Server;

namespace Core.GS.APIs;

public class ApiPasswordVerification
{
    public ApiPasswordVerification()
    {

    }

    public bool VerifyAPIPassword(string password)
    {
        var apiPassword = ServerProperty.API_PASSWORD;
        if (apiPassword is (null or "")) return false;
        if (password is (null or "")) return false;
        if (password != apiPassword) return false;
        return true;
    }
}
