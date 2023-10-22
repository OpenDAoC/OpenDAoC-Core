using System.Net;
using System.Net.Http;

namespace Core.GS;

class CoreWebHook
{
    private HttpClient Client;
    private string Url;

    public string Name { get; set; }


    public CoreWebHook(string webhookUrl)
    {
        Client = new HttpClient();
        Url = webhookUrl;
    }

    public bool SendMessage(string content)
    {
        MultipartFormDataContent data = new MultipartFormDataContent();


        data.Add(new StringContent(content), "content");



        var resp = Client.PostAsync(Url, data).Result;

        return resp.StatusCode == HttpStatusCode.NoContent;
    }
}