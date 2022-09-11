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
            OpenDoor(kvp.Value);
            commandWasRun = true;
            break;
        case "login":
            LoginProcedure(true);
            commandWasRun = true;
            break;
    }
}

if (!commandWasRun) {
    Man();
}


// ~ app exits here ~
#region methods
void LoginProcedure(bool verboseWhenSuccessful = false) {
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

    try {
        client.Login(username, password);
    } catch (CantFindTokenElementException tokenEx) {
        System.Console.WriteLine($"Couldn't find token (\"{tokenEx.Element}\") from login page.");
        Environment.Exit(0);
    }

    if (!client.IsLoggedIn())
    {
        Console.WriteLine("Login failed.");
        Environment.Exit(0);
    } else {
        if (verboseWhenSuccessful) {
            System.Console.WriteLine("Login successful.");
        }
    }
}

void OpenDoor(string message = "") {
    try {
        client.OpenDoor(message);
    } catch (NotConnectedToKerdeWifiException) {
        System.Console.WriteLine("Not connected to Kerde wifi.");
        Environment.Exit(0);
    }
}

void Man() {
    var cmdInfos = new Dictionary<string, string>(); // title as key, description as value
    cmdInfos.Add("opendoor (or \"door\")", "Opens Kerde door. Requires Kerde wifi connection.");
    cmdInfos.Add("opendoor=\"my message here\"", "Opens the Kerde door with a message.");
    cmdInfos.Add("login", "Logs in with the credentials provided in conf files or with cli args.");

    System.Console.WriteLine("Astalo");
    System.Console.WriteLine("---");
    System.Console.WriteLine("Usage:");
    System.Console.WriteLine("astalo COMMANDHERE");
    System.Console.WriteLine("astalo COMMANDHERE=args");
    System.Console.WriteLine("---");
    System.Console.WriteLine();
    System.Console.WriteLine("Commands:");
    
    foreach (var cmdInfo in cmdInfos) {
        System.Console.WriteLine(cmdInfo.Key);
        System.Console.WriteLine(cmdInfo.Value);
        System.Console.WriteLine();
    }
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

#endregion