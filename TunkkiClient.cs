﻿using System.Net;

namespace Astalo
{
    public class TunkkiClient
    {
        const string BaseUrl = "https://entropy.fi";
        const string LoginPath = "/login";
        const string DoorOpeningPath = "/profiili/ovi";
        const string XPathForCsrfTokenInputElement = "//input[@name=\"_csrf_token\"]";
        const string FailedLoginCharacteristic = "Email could not be found.";
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
            // Tunkki redirects unauthorized users to login page, so if response.RequestMessage.RequestUri
            // is the Uri of the login page, session is not logged in.

            var uri = new Uri(new Uri(BaseUrl), "/yleiskatsaus/");
            var response = Client.GetAsync(uri).Result;
            return response.RequestMessage?.RequestUri?.AbsoluteUri != "https://entropy.fi/login";
        }

        public HttpResponseMessage Get(string route)
        {
            var uri = new Uri(new Uri(BaseUrl), route);

            return Client.GetAsync(uri).Result;
        }

        public void OpenDoor()
        {
            var uri = new Uri(new Uri(BaseUrl), DoorOpeningPath);
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
                throw new InvalidOperationException("Couldn't find <input> element that is assumed to contain the csrf token.");
            }
        }
    }
}
