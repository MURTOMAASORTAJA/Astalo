# Astalo

A crossplatform CLI-app for interacting with Entropy Tunkki. Currently compatible only with x64 Linux.

The application is self-contained, so running it *should* not need .NET 6.0 runtime. In case it does, install it to your computer. The rather large size of the executable file is because of the app being self-contained.

### How to install
1. Download the [latest release](https://github.com/MURTOMAASORTAJA/Astalo/releases/download/best-release-so-far/astalo) of Astalo.
2. Create a directory for configuration:
```bash
sudo mkdir /etc/astalo
```
3. Create and populate conf file with nano (or whatever editor you prefer):
```bash
sudo nano /etc/astalo/astalo.conf
```
4. Copy-paste this into the file, change the stuff to correct values, save the file and quit editor:
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
