// See https://aka.ms/new-console-template for more information
using Astalo;

var username = "something@somewhere.com";
var password = "totallyMadeUpPassword123";

var client = new TunkkiClient();
client.Login(username, password);

if (client.IsLoggedIn())
{
    Console.WriteLine("Login successful.");
} else
{
    Console.WriteLine("Login failed.");
}

