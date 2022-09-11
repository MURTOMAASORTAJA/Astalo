# Astalo

A crossplatform CLI-app for interacting with Entropy Tunkki. Currently compatible only with x64 Linux.

The application is self-contained, so running it *should* not require to have .NET 6.0 runtime installed. In case it seems to require, [install the runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) to your computer. The rather large size of the executable file is because of the app being self-contained.

### How to install
1. Download the [latest release](https://github.com/MURTOMAASORTAJA/Astalo/releases/download/best-release-so-far/astalo) of Astalo.
2. Enable execute permission on the astalo executable:
```bash
chmod +x astalo
```
3. Create a directory for configuration:
```bash
sudo mkdir /etc/astalo
```
4. Create a conf file with nano (or whatever editor you prefer):
```bash
sudo nano /etc/astalo/astalo.conf
```
5. Copy-paste this into the file, change the stuff to correct values, save the file and quit editor:
```
user=youremail@here.com
pass=SomethingSomething
```

### How to use
The application is meant to be run in terminal.
Running the application with no arguments will print out quick guide listing all subcommands.

Example for testing login credentials:
`astalo testlogin`

Example for opening Kerde door (requires Kerde wifi connection):
`astalo door`
