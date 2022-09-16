using System.Net;

namespace Astalo
{
    public class TunkkiClient
    {
        const string BaseUrl = "https://entropy.fi";
        const string LoginPath = "/login";
        const string DoorOpeningPath = "/profiili/ovi";
        const string XPathForCsrfTokenInputElement = "//input[@name=\"_csrf_token\"]";
        HttpClientHandler ClientHandler { get; set; }
        HttpClient Client { get; set; }

        private const int RetryTimes = 3;

        private string CsrfToken { get; set; } = "";

        public double TimeoutSecondsPerRequest
        {
            get
            {
                return Client.Timeout.TotalSeconds;
            }

            set
            {
                Client.Timeout = new TimeSpan(0, 0, Convert.ToInt32(value));
            }
        }

        private async Task<HttpResponseMessage> SendRetryAsync(HttpMethod method, Uri uri, HttpContent? content = null, bool retryAfterNonSuccessStatusCode = true)
        {
            var retryTimes = 0;
            HttpResponseMessage? response = null;
            while (retryTimes < RetryTimes)
            {
                response = await Client.SendAsync(new HttpRequestMessage(method, uri) { Content = content });
                if (!response.IsSuccessStatusCode && !retryAfterNonSuccessStatusCode)
                {
                    break;
                }
                retryTimes++;
            }

            if (response == null)
            {
                throw new MaxRetriesException(uri);
            }

            return response;
        }

        public TunkkiClient()
        {
            ClientHandler = new HttpClientHandler();
            Client = new HttpClient(ClientHandler);
            TimeoutSecondsPerRequest = 3;
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

            var response = SendRetryAsync(HttpMethod.Post, uri, formContent).Result;
        }

        public bool IsLoggedIn()
        {
            // Here the client attempts to navigate to a page visible only for logged in users.
            // Tunkki tends to redirect unauthorized requests to login page, so if the final 
            // response.RequestMessage.RequestUri is the Uri of the login page, session is not logged in.

            var uri = new Uri(new Uri(BaseUrl), "/yleiskatsaus/");
            var response = SendRetryAsync(HttpMethod.Get, uri).Result;
            return response.RequestMessage?.RequestUri?.AbsoluteUri != "https://entropy.fi/login";
        }

        private string GetOpenDoorToken() {
            var uri = new Uri(new Uri(BaseUrl), DoorOpeningPath);

            var response = SendRetryAsync(HttpMethod.Get, uri, null, false).Result;
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

            var response = SendRetryAsync(HttpMethod.Post, uri, new FormUrlEncodedContent(new[] {
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
            var loginPageResponse = SendRetryAsync(HttpMethod.Get, uri).Result;
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
    public class MaxRetriesException : Exception
    {
        public Uri Uri { get; set; }
        public MaxRetriesException(Uri uri) : base()
        {
            Uri = uri;
        }
    }
}
