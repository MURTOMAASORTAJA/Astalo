using Astalo;

string[] Filenames = new[] { "astalo.conf" };
string[] ConfDirs = new[] 
{ 
    // linux:
    "/etc/astalo", 
    "~",
    
    // windows:
    "%HOMEDRIVE%",
    "%USERPROFILE%"
};

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
        case "testlogin":
            LoginProcedure(true);
            commandWasRun = true;
            break;
    }
    // Note for anyone wanting to add a new command/feature to Astalo that is for calling methods in TunkkiClient or elsewhere:
    //
    // 1: Add your new case to the switch case above
    // 2: Create the method you're going to use in this new feature to TunkkiClient or elsewhere where you think it belongs
    // 3: Add a method below into the "Methods and stuff" region, and write that method to call the method in the earlier step 2, as well as
    //    to write any Console stuff or anything else.
    //
    // The point in this is to keep the following order of things:
    // - The switch case above handles only the interpretation of the command line argument
    // - The "what happens when I give this command to Astalo"-magic and the so called "interface-part" of Astalo happens in the "Methods and stuff" region below
    // - Anything that doesn't belong into these two keypoints above should be in their own class file, like TunkkiClient is being right now...
    // - ...but if your new feature doesn't relate in any way to Tunkki, don't put it in TunkkiClient; make a new class instead.
}

if (!commandWasRun) {
    Man();
}

// ~ app exits here ~
#region Methods and stuff
void Man() {
    var cmdInfos = new Dictionary<string, string>(); // title as key, description as value
    cmdInfos.Add("opendoor (or \"door\")", "Opens Kerde door. Requires Kerde wifi connection.");
    cmdInfos.Add("opendoor=\"my message here\"", "Opens the Kerde door with a message. Requires Kerde wifi connection.");
    cmdInfos.Add("testlogin", "Does a test login with the credentials provided in conf files or with cli args, and states if login was successful.");
    cmdInfos.Add("user=USEREMAILHERE", "Sets username that will override any username possibly found from conf file.");
    cmdInfos.Add("pass=PASSWORDHERE", "Sets password that will override any username possibly found from conf file.");

    System.Console.WriteLine("Astalo");
    System.Console.WriteLine("---");
    Console.WriteLine();
    Console.WriteLine("Configuration:");
    Console.WriteLine("Create a file containing these two lines with your own username and password:");
    Console.WriteLine("user=YOUREMAILADDRESSHERE");
    Console.WriteLine("pass=YOURPASSWORDHERE");
    Console.WriteLine("...and save it as astalo.conf to one of the following directories:");
    foreach(var dirpath in ConfDirs)
    {
        Console.WriteLine(dirpath);
    }
    Console.WriteLine();
    System.Console.WriteLine("Usage:");
    System.Console.WriteLine("astalo COMMANDHERE");
    System.Console.WriteLine("astalo COMMANDHERE=args");
    System.Console.WriteLine("---");
    System.Console.WriteLine();
    System.Console.WriteLine("Commands:");
    
    foreach (var cmdInfo in cmdInfos) {
        System.Console.WriteLine(cmdInfo.Key);
        System.Console.WriteLine("\t" + cmdInfo.Value);
    }
}

/// <summary>
/// Reads username and password from a conf file and invokes <see cref="TunkkiClient.Login"/> 
/// while being verbose via Console standard output.
/// </summary>
void LoginProcedure(bool verboseWhenSuccessful = false)
{
    var username = "";
    var password = "";

    var fromFile = GetUserAndPassFromFirstConfFileFound();
    if (fromFile.Item1 != null)
    {
        username = fromFile.Item1;
    }
    if (fromFile.Item2 != null)
    {
        password = fromFile.Item2;
    }

    if (kvpArgs.Any(x => x.Key == "user"))
    {
        username = kvpArgs.FirstOrDefault(x => x.Key == "user").Value;
        password = kvpArgs.FirstOrDefault(x => x.Key == "pass").Value;
    }

    if (string.IsNullOrEmpty(username))
    {
        System.Console.WriteLine("No username set.");
        shouldExit = true;
    }

    if (string.IsNullOrEmpty(password))
    {
        System.Console.WriteLine("No password set.");
        shouldExit = true;
    }

    if (shouldExit)
    {
        Environment.Exit(0);
    }

    try
    {
        client.Login(username, password);
    }
    catch (CantFindTokenElementException tokenEx)
    {
        System.Console.WriteLine($"Couldn't find token (\"{tokenEx.Element}\") from login page.");
        Environment.Exit(0);
    }

    if (!client.IsLoggedIn())
    {
        Console.WriteLine("Login failed.");
        Environment.Exit(0);
    }
    else
    {
        if (verboseWhenSuccessful)
        {
            System.Console.WriteLine("Login successful.");
        }
    }
}

/// <summary>
/// Opens the Kerde door and writes to Console standard output about any errors.
/// </summary>
void OpenDoor(string message = "")
{
    try
    {
        client.OpenDoor(message);
    }
    catch (NotConnectedToKerdeWifiException)
    {
        System.Console.WriteLine("Not connected to Kerde wifi.");
        Environment.Exit(0);
    }
}

/// <summary>
/// Reads and returns username and password from the first found conf file.
/// </summary>
(string?, string?) GetUserAndPassFromFirstConfFileFound() {

    foreach (var dirpath in ConfDirs)
    {
        foreach (var filename in Filenames)
        {
            var path = Path.Combine(Environment.ExpandEnvironmentVariables(dirpath), filename);
            if (File.Exists(path))
            {
                var result = GetUserAndPassFromConfFile(path);
                if (result.Item1 != null && result.Item2 != null)
                {
                    return result;
                }
            }
        }
    }

    return (null, null);
}

/// <summary>
/// Reads and returns username and password from a file.
/// </summary>
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

/// <summary>
/// Calls <see cref="TunkkiClient.KerdeDoorIsOk"/> and writes either OK or NOT OK to
/// Console standard output, whether Kerde door status is OK.
/// </summary>
void KerdeDoorStatus()
{
    if (client.KerdeDoorIsOk())
    {
        Console.WriteLine("OK");
    } else
    {
        Console.WriteLine("NOT OK");
    }
}
#endregion