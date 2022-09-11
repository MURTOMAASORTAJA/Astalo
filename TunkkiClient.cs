using System.Net;

namespace Astalo
{
    public class TunkkiClient
    {
        const string BaseUrl = "https://entropy.fi";
        const string LoginPath = "/login";
        const string DoorOpeningPath = "/profiili/ovi";
        const string XPathForCsrfTokenInputElement = "//input[@name=\"_csrf_token\"]";
        CookieContainer CookieContainer { get; set; }
        HttpClientHandler ClientHandler { get; set; }
        HttpClient Client { get; set; }

        private string CsrfToken { get; set; } = "";

        public TunkkiClient()
        {
            CookieContainer = new CookieContainer();
            ClientHandler = new HttpClientHandler() { CookieContainer = CookieContainer };
            Client = new HttpClient(ClientHandler);
        }

        public void Login(string username, string password)
        {
            var uri = new Uri(new Uri(BaseUrl), LoginPath);

            if (string.IsNullOrEmpty(CsrfToken))
            {
                GetCsrfTokenFromLoginPage();
            }

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("email", username),
                new KeyValuePair<string,string>("password", password),
                new KeyValuePair<string,string>("_csrf_token", CsrfToken)
            });

            var response = Client.PostAsync(uri, formContent).Result;
        }

        public bool IsLoggedIn()
        {
            // Here the client attempts to navigate to a page visible only for logged in users.
            // Tunkki tends to redirect unauthorized requests to login page, so if the final 
            // response.RequestMessage.RequestUri is the Uri of the login page, session is not logged in.

            var uri = new Uri(new Uri(BaseUrl), "/yleiskatsaus/");
            var response = Client.GetAsync(uri).Result;
            return response.RequestMessage?.RequestUri?.AbsoluteUri != "https://entropy.fi/login";
        }

        public HttpResponseMessage Get(string route)
        {
            var uri = new Uri(new Uri(BaseUrl), route);

            return Client.GetAsync(uri).Result;
        }

        private string GetOpenDoorToken() {
            var uri = new Uri(new Uri(BaseUrl), DoorOpeningPath);

            var response = Client.GetAsync(uri).Result;
            if (response.StatusCode == HttpStatusCode.Forbidden) {
                throw new NotConnectedToKerdeWifiException();
            }
            var responseDoc = new HtmlAgilityPack.HtmlDocument();
            responseDoc.Load(response.Content.ReadAsStream());
            var tokenNode = responseDoc.GetElementbyId("open_door__token");
            if (tokenNode == null) {
                throw new CantFindTokenElementException() { Element = "open_door__token"};
            }
            return tokenNode.Attributes["value"].Value;
        }

        public void OpenDoor(string message = "")
        {
            var token = GetOpenDoorToken();
            var uri = new Uri(new Uri(BaseUrl), DoorOpeningPath);

            var response = Client.PostAsync(uri, new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("open_door[message]", message),
                new KeyValuePair<string, string>("open_door[_token]", token)
            })).Result;

            if (response.StatusCode == HttpStatusCode.Forbidden) {
                throw new NotConnectedToKerdeWifiException();
            }
        }

        public string GetCsrfTokenFromLoginPage()
        {
            var uri = new Uri(new Uri(BaseUrl), LoginPath);
            var loginPageResponse = Client.GetAsync(uri).Result;
            loginPageResponse.EnsureSuccessStatusCode();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(loginPageResponse.Content.ReadAsStream());
            var csrfInput = doc.DocumentNode.SelectSingleNode(XPathForCsrfTokenInputElement);

            if (csrfInput != null)
            {
                CsrfToken = csrfInput.Attributes["value"].Value;
                return csrfInput.Attributes["value"].Value;
            } else
            { 
                throw new CantFindTokenElementException() { Element="XPathForCsrfTokenInputElement" };
            }
        }
    }

    public class NotConnectedToKerdeWifiException : Exception {
        public HttpResponseMessage? ResponseMessage { get; set; }

        public NotConnectedToKerdeWifiException() {
            ResponseMessage = null;
        }
    }
    public class CantFindTokenElementException : Exception {
        public string Element { get; set; } = "";
    }

}
