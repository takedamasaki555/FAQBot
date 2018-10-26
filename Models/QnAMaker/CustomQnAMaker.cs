using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CustomQnAMaker
{
    public class GenerateAnswer
    {
        public static async Task<string> GetResultAsync(string messageText, string StrictFilters) 
        {
            string endpoint = ConfigurationManager.AppSettings["QnAEndpointHostName"] + "/knowledgebases/" + ConfigurationManager.AppSettings["QnAKnowledgebaseId"] + "/generateAnswer";
            string input_json = "{\"question\":\"" + messageText + "\",\"top\": \"10\""+ StrictFilters +"}";


            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("EndpointKey", ConfigurationManager.AppSettings["QnAAuthKey"]);
                    request.Content = new StringContent(input_json, Encoding.UTF8, "application/json");

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();
                            return json;
                        }
                        string failture = "failture";
                        return failture;
                    }
                }
            }
        }
    }
}