using Astalo;

string[] RelativeConfFilePaths = new[] { "astalo.conf" };
string[] AbsoluteConfFilePaths = new[] { "/etc/astalo/astalo.conf" };
bool shouldExit = false;
var kvpArgs = args.AsKeyValuePairs();
var client = new TunkkiClient();
bool commandWasRun = false;
foreach (var kvp in kvpArgs) {
    switch (kvp.Key) {
        case "opendoor":
        case "door":
            LoginProcedure();
            client.OpenDoor(kvp.Value);
            commandWasRun = true;
            break;
    }
}

if (!commandWasRun) {
    Man();
}


// ~ app exits here ~

void LoginProcedure() {
    var username = "";
    var password = "";

    var fromFile = GetUserAndPassFromFirstConfFileFound();
    if (fromFile.Item1 != null) {
        username = fromFile.Item1;
    }
    if (fromFile.Item2 != null) {
        password = fromFile.Item2;
    }

    if (kvpArgs.Any(x => x.Key == "user")) {
        username = kvpArgs.FirstOrDefault(x => x.Key == "user").Value;
        password = kvpArgs.FirstOrDefault(x => x.Key == "pass").Value;
    }

    if (string.IsNullOrEmpty(username)) {
        System.Console.WriteLine("No username set.");
        shouldExit = true;
    }

    if (string.IsNullOrEmpty(password)) {
        System.Console.WriteLine("No password set.");
        shouldExit = true;
    }

    if (shouldExit) 
    { 
        Environment.Exit(0); 
    }

    client.Login(username, password);

    if (!client.IsLoggedIn())
    {
        Console.WriteLine("Login failed.");
        Environment.Exit(0);
    }
}

void Man() {
    System.Console.WriteLine("Astalo");
    System.Console.WriteLine("---");
    System.Console.WriteLine("Usage:");
    System.Console.WriteLine("astalo COMMANDHERE");
    System.Console.WriteLine("astalo COMMANDHERE=args");
    System.Console.WriteLine("---");
    System.Console.WriteLine();
    System.Console.WriteLine("Commands:");
    System.Console.WriteLine();
    System.Console.WriteLine("opendoor (or \"door\")\nOpens Kerde door. Requires Kerde wifi connection.");
    System.Console.WriteLine();
    System.Console.WriteLine("opendoor=\"my message here\"\nOpens the Kerde door with a message.");
}

(string?, string?) GetUserAndPassFromFirstConfFileFound() {
    foreach (var relativePath in RelativeConfFilePaths) {
        var absolutePath = Path.GetFullPath(relativePath);
        if (File.Exists(absolutePath)) {
            var result = GetUserAndPassFromConfFile(absolutePath);
            if (result.Item1 != null && result.Item2 != null) {
                return result;
            }
        }
    }

    foreach (var absolutePath in AbsoluteConfFilePaths) {
        if (File.Exists(absolutePath)) {
            var result = GetUserAndPassFromConfFile(absolutePath);
            if (result.Item1 != null && result.Item2 != null) {
                return result;
            }
        }
    }

    return (null, null);
}

(string?, string?) GetUserAndPassFromConfFile(string filePath) {
    var lines = File.ReadAllLines(filePath);
    var linesAsKvps = lines.AsKeyValuePairs();

    string? user = null;
    string? pass = null;

    foreach (var kvp in linesAsKvps) {
        switch (kvp.Key.ToLowerInvariant()) {
            case "user":
            case "username":
                user = kvp.Value;
                break;
            case "pass":
            case "password":
                pass = kvp.Value;
                break;
        }
    }

    return (user, pass);
}