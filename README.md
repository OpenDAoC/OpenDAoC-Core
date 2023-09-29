# ACTIVE WORK

FULLY WORKING:

Artifacts
- Alvarus' Leggings
- Belt of the Moon
- Belt of the Sun
- Aten's Shield
- Atlantis Tablet
- Band of Stars
- Flask

Master Levels

PARTIALLY WORKING:

Artifacts
- Bracer of Zo'Arkat

Master Levels

NOT YET LOOKED AT:

Artifacts

- Arms of the Winds
- Bane of Battler
- Braggart's Bow
- Bruiser
- Ceremonial Bracer
- Cloudsong
- Crocodile's Tears Ring
- Crocodile's Tooth Dagger
- Crown of Zahur
- Cyclops Eye Shield
- Dream Sphere
- Eerie Darkness Lighting Stone
- Egg of Youth
- Eirene's Hauberk
- Enyalio's Boots
- Erinys Charm
- Eternal Plant
- Flamedancer's Boots
- Fool's Bow
- Foppish Sleeves
- Guard of Valor
- Goddess' Necklace
- Golden Scarab Vest
- Golden Spear
- Harpy's Feather Cloak
- Healing Embrace Cloak
- Jacina's Sash
- Kalare's Necklace
- Maddening Scalars
- Malice Axe
- Sharkskin Gloves
- Nailah's Robes
- Night's Shroud Bracelet
- Oglidarsh's Belt
- Phoebus Harp
- Ring of Dances
- Ring of Fire
- Scepter of the Meritorious
- Scorpions Tail Ring
- Shades of Mist
- Shield of Khaos
- Snakecharmer's Weapon
- Snatcher's Tale
- Spear of Kings
- Staff of the Gods
- Stone of Atlantis
- Tartaro's Gift
- Traitor's Dagger
- Traldor's Oracle
- Winged Helm
- Wings Dive

Master Levels
- Banelord
- Battlemaster
- Convoker
- Perfecter
- Sojourner
- Stormlord
- Spymaster
- Warlord

# Docker
[![Build and Push Docker Image](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-docker-image.yml/badge.svg)](https://github.com/OpenDAoC/OpenDAoC-Core/actions/workflows/build-docker-image.yml)

The easiest way to get started with OpenDAoC is to use Docker. This will allow you to run a server without having to install any dependencies on your machine.

A Docker image is available on [Docker Hub](https://hub.docker.com/r/claitz/opendaoc).

# Setting Up an OpenDAoC Dev Environment

The following instructions should act as a guide for properly setting up a development environment to fetch and push file changes with GitHub as well as build and run a DAoC server successfully.

Instructions currently exist regarding setups for both [Ubuntu](#setting-up-on-ubuntu) and [Windows](#setting-up-on-windows) operating systems. These configurations may be likewise performed and a local server run on macOS, with some minor alterations--though you'll need either a Windows or an emulation setup to run the DAoC client.

**IMPORTANT:** Check the [Environment Requirements](#environment-requirements) section first as you will not be able to complete certain sections without the proper resources or privileges.

The following sections outline the process of preparing your machine to develop, build, and run an OpenDAoC server:

1. [Environment Requirements](#environment-requirements)
2. [Setting Up on Ubuntu](#setting-up-on-ubuntu)
   1. [Installing .NET 6.0](#installing-net-60-ubuntu)
   2. [Installing MariaDB 10.5](#installing-mariadb-105-ubuntu)
      1. [Preparing Your Database](#preparing-your-database-ubuntu)
      2. [Adding `DummyDB.sql`](#adding-dummydbsql-ubuntu)
      3. [Cloning OpenDAoC's Repos](#cloning-the-repository-ubuntu)
      4. [Altering `serverconfig.xml`](#altering-serverconfigxml-ubuntu)
3. [Setting Up on Windows](#setting-up-on-windows)
   1. [Installing .NET 6.0](#installing-net-60-win)
   2. [Installing MariaDB 10.5](#installing-mariadb-105-win)
      1. [Preparing Your Database](#preparing-your-database-win)
      2. [Configuring `My.ini`](#configuring-myini-win)
      3. [Adding `DummyDB.sql`](#adding-dummydbsql-win)
      4. [Cloning OpenDAoC's Repos](#cloning-the-repository-win)
      5. [Altering `serverconfig.xml`](#altering-serverconfigxml-win)
4. [Building Your OpenDAoC Server Locally](#building-your-dol-server-locally)
5. [Accessing Local Servers](#accessing-local-servers)
6. [Testing](#testing)
   1. [In-Game Testing](#in-game-testing)
   2. [Recommended Extensions for Testing](#recommended-extensions-for-testing)
7. [Logging](#logging)
8. [License](#license)

## Environment Requirements

The following are the main OS, tool, and version requirements to consider when setting up an environment:

* **Operating System:** Ubuntu or Windows (macOS usable as a server, but cannot run the DAoC game client without third-party apps)
* **Source-Code Editor:** .NET IDE that supports C#, such as [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/) or [Jetbrains Rider](https://www.jetbrains.com/rider/) (if you have a student email address)
* **Source Control:** Git is recommended for tracking file changes, and [GitHub](http://github.com) is the current source control service
* **RDBMS:** [MariaDB v.10.5.X+](https://mariadb.com/kb/en/changes-improvements-in-mariadb-105/)
* **.NET SDK Framework:** 6.0+ required

## Setting Up on Ubuntu

This process assumes you do not already have a fully-configured environment with the specific tools or software installed previously.

If you've already completed a step previously, we recommend that you quickly review the steps again to ensure no special configurations are missed.

1. [Installing .NET 6.0](#installing-net-60-ubuntu)
2. [Installing MariaDB 10.5](#installing-mariadb-105-ubuntu)
   1. [Preparing Your Database](#preparing-your-database-ubuntu)
   2. [Configuring `My.cnf`](#configuring-mycnf-ubuntu)
   3. [Adding `DummyDB.sql`](#adding-dummydbsql-ubuntu)
3. [Encrypting File Transfers](#encrypting-file-transfers-ubuntu)
   1. [Adding a Personal Access Token](#setting-up-a-personal-access-token-ubuntu)
   2. [Setting Up SSH Tunneling](#setting-up-ssh-tunneling-ubuntu)
      1. [Enabling SSH](#enabling-ssh-ubuntu)
      2. [Configuring Your Router](#configuring-your-router-ubuntu)
      3. [Creating an SSH Key](#creating-an-ssh-key-ubuntu)
      4. [Adding the SSH Key to GitHub](#adding-the-ssh-key-to-github-ubuntu)
   3. [Installing Git](#installing-git-ubuntu)
      1. [Cloning OpenDAoC's Repos](#cloning-the-repository-ubuntu)
      2. [Altering `serverconfig.xml`](#altering-serverconfigxml-ubuntu)

### Installing .NET 6.0 (Ubuntu)

.NET is an open-source developer platform used for building applications. OpenDAoC uses .NET 6.0.X specifically.

Perform the following steps from the Terminal:

1. `wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`
2. `sudo dpkg -i packages-microsoft-prod.deb`
3. `sudo apt update`
4. `sudo apt install apt-transport-https`
5. `sudo apt-get install -y dotnet-sdk-6.0`
6. `dotnet --list-sdks`
7. `dotnet --list-runtimes`

### Installing MariaDB 10.5 (Ubuntu)

MariaDB is an open-source relational database management system (RDBMS). OpenDAoC specifically utilizes v10.5.

Perform the following steps from the Terminal:

1. `sudo apt update && sudo apt upgrade`
2. `sudo apt -y install software-properties-common`
3. `sudo apt-key adv --fetch-keys 'https://mariadb.org/mariadb_release_signing_key.asc'`
4. `sudo add-apt-repository 'deb [arch=amd64] http://mariadb.mirror.globo.tech/repo/10.5/ubuntu focal main'`
5. `sudo apt update`
6. `sudo apt install mariadb-server mariadb-client`
7. Type `y` to accept.

The RDBMS is installed, but needs a user and database for OpenDAoC to access and use.

#### Preparing Your Database (Ubuntu)

The following steps walk you through the process of adding a user and database using MariaDB.

If you're already familiar with the process and wish to skip it, in the following steps we will use the following configurations:

* There must be a user named `opendaoc`.
* The user's password must be `opendaoc`.
* The `opendaoc` user must have sufficient privileges.
* A database must exist called `opendaoc`.

**NOTE:** If you set values (user ID, user password, and database name) contrary to those specified here, the build will fail.

1. `sudo mysql -u root` <!-- This allows you to access the MariaDB client-->
2. `CREATE DATABASE opendaoc;` <!-- The DB **MUST** be named "opendaoc" -->
3. `SHOW DATABASES;`</li> <!-- Verify that the DB exists -->
4. `CREATE USER 'opendaoc'@localhost IDENTIFIED BY 'opendaoc';` <!-- Both username and password must be "opendaoc" -->
5. `SELECT User FROM mysql.user;` <!-- This lists all existing users -->
6. To grant all privileges **for all databases** to the _opendaoc_ user, use the following command: `GRANT ALL PRIVILEGES ON *.* TO 'opendaoc'@localhost;` <!-- The 'opendaoc' user may exercise ALL privileges on ALL of your databases [RISKY] -->
7. To grant all privileges **only to the _opendaoc_ DB**, use this command: `GRANT ALL PRIVILEGES ON opendaoc.* TO 'opendaoc'@localhost;` <!-- The 'opendaoc' user may only modify the 'opendaoc' DB -->
8. `FLUSH PRIVILEGES;` <!-- Refreshes the privilege changes -->
9. `SHOW GRANTS FOR 'opendaoc'@localhost;` <!-- This lists all privileges granted to the `opendaoc` user -->

#### Adding `DummyDB.sql` (Ubuntu)

The most recent version of the required `DummyDB.sql` file is available as archive at `\OpenDAoC-Core\DummyDB.zip`. 
Without it, you cannot successfully build a local OpenDAoC server. 
After installing MariaDB, you should also notice it installed a program called HeidiSQL, which you'll need to use for this section.

1. Launch the Terminal and type `sudo mysql -u root opendaoc < ~/path/to/DummyDB.sql`. This copies the file's contents to the `opendaoc` database.
2. To check that the import was successful, enter `sudo mysql -u root -p`. This launches the MariaDB Client.
3. `USE opendaoc;`
4. `SHOW TABLES;`

This should list multiple tables, which indicates the import was successful.

### Cloning the Repository (Ubuntu)

With Git ready, it's time to clone the `OpenDAoC-Core` repository.

1. From the Terminal, navigate to the desired directory to house the repos.
2. In a browser, navigate to [GitHub](https://www.github.com) and open the desired repo.
3. Click the **Clone** button at the top-right corner.
4. Copy the clone address.
5. Returning to the Terminal, (and from your desired directory) type `git clone (PASTE THE ADDRESS)`. Enter your credentials when prompted, or the SSH key passphrase.

### Altering `serverconfig.xml` (Ubuntu)

With the repo on your local hard drive, you need to alter the `serverconfig.xml` file to avoid some errors when building OpenDAoC locally.

1. Copy the file `/OpenDAoC-Core/CoreServer/config/serverconfig.example.xml` to `/OpenDAoC-Core/CoreServer/config/serverconfig.xml`.
2. Open the `serverconfig.xml` file.
3. Within the `RegionIP` tags, change the value `0.0.0.0` to one of these:
   1. To test locally, enter `127.0.0.1`.
   2. To test over LAN, enter your machine's IP address (use the Terminal command `ip a`, and it should start with `192`).
   3. To test outside your network, [enter your public IP address](https://api.ipify.org).
4. Configure the database access as per your own configuration.
   
Now you're ready to [run your own instance of OpenDAoC](#building-your-dol-server-locally)!

## Setting Up on Windows

This process assumes you do not already have a fully-configured environment with the specific tools or software installed previously.

If you've already completed a step previously, we recommend that you quickly review the steps outlined to ensure no special configurations are missed.

1. [Installing .NET 6.0](#installing-net-60-win)
2. [Installing MariaDB 10.5](#installing-mariadb-105-win)
   1. [Preparing Your Database](#preparing-your-database-win)
   2. [Adding `DummyDB.sql`](#adding-dummydbsql-win)
3. [Installing Git](#installing-git-win)
4. [Cloning OpenDAoC' Repos](#cloning-the-repository-win)
5. [Altering `serverconfig.xml`](#altering-serverconfigxml-win)

### Installing .NET 6.0 (Win)

.NET is an open-source developer platform used for building applications. OpenDAoC uses .NET 6.0.X specifically, which is supported on all recent versions of Windows.

1. Download the [.NET 6.0 installer](https://dotnet.microsoft.com/download/dotnet/6.0).
2. Install the tool and make any configurations as needed.

### Installing MariaDB 10.5 (Win)

MariaDB is an open-source relational database management system (RDBMS). OpenDAoC specifically uses v10.5.

1. Download MariaDB:
   1. [32-bit](https://downloads.mariadb.org/interstitial/mariadb-10.5.4/win32-packages/mariadb-10.5.4-win32.zip/from/https%3A//archive.mariadb.org/)
   2. [64-bit](https://downloads.mariadb.org/interstitial/mariadb-10.5.4/winx64-packages/mariadb-10.5.4-winx64.zip/from/https%3A//archive.mariadb.org/)
2. Initiate the MariaDB installer.
3. Enable **Use UTF8 as default server's character set**. Make any changes to the following dialog windows as desired.
4. Complete the installation by clicking **Install** and then **Finish**.

The RDBMS is installed, but needs a user and database for OpenDAoC to access and use.

#### Preparing Your Database (Win)

The following steps walk you through the process of adding a user and database using MariaDB.

If you're already familiar with the process and wish to skip it, in the following steps we will use the following configurations:

* There must be a user named `opendaoc`.
* The user's password must be `opendaoc`.
* The `opendaoc` user must have sufficient privileges.
* A database must exist called `opendaoc`.

_**NOTE:** If you set values (user ID, user password, and database name) contrary to those specified here, the build will fail.

1. Launch the **MySQL Client (MariaDB 10.5)** option from the Start menu.
2. `CREATE DATABASE opendaoc;` <!-- The DB **MUST** be named "opendaoc" -->
3. `SHOW DATABASES;` <!-- Verify that the DB exists -->
4. `CREATE USER 'opendaoc'@localhost IDENTIFIED BY 'opendaoc';` <!-- Both username and password must be "opendaoc" -->
5. `SELECT User FROM mysql.user;` <!-- This lists all existing users -->
6. To grant all privileges **for all databases** to the _opendaoc_ user, use the following command: `GRANT ALL PRIVILEGES ON *.* TO 'opendaoc'@localhost IDENTIFIED BY 'opendaoc';` <!-- The 'opendaoc' user may exercise ALL privileges on ALL of your databases [RISKY]-->
7. To grant all privileges **only to the _opendaoc_ DB**, use this command: `GRANT ALL PRIVILEGES ON opendaoc.* TO 'opendaoc'@localhost;` <!-- The 'opendaoc' user may only modify the 'opendaoc' DB -->
8. `FLUSH PRIVILEGES;` <!-- Refreshes the privilege changes -->
9. `SHOW GRANTS FOR 'opendaoc'@localhost;` <!-- This lists all privileges granted to the `opendaoc` user -->

#### Adding `DummyDB.sql` (Win)

Prior to accomplishing this step, you will need a recent copy of the `DummyDB.sql` file. Without it, you cannot successfully build a local OpenDAoC server. After installing MariaDB, you should also notice a program called HeidiSQL, which you'll need to use for this section.

1. Launch the HeidiSQL app.
2. At the bottom-left corner. click **New > Session in root folder**.
3. Enter a root user **Password** if you set one previously.
4. Click **Save**.
5. Now click **Open** to start a connection with MariaDB.
6. Select the `opendaoc` database you created previously.
7. Click **File > Load SQL file**.
8. Navigate to the `DummyDB.sql` file and click **Open**.
9. Select the **Run file(s) directly** option as loading the file will cause HeidiSQL to crash.

The application will process the entire SQL file. Once done, you should now see tables and data populating the `opendaoc` database.

### Cloning the Repository (Win)

With Git ready, it's time to clone the `OpenDAoC-Core` repositories.

1. From Windows PowerShell, navigate to the desired directory to house the repos.
2. In a browser, navigate to [GitHub](https://www.github.com) and open the desired repo.
3. Click the **Clone** button at the top-right corner.
4. Copy the clone address.
5. Returning to the Terminal, (and from your desired directory) type `git clone (PASTE THE ADDRESS)`. Enter your credentials if prompted, or the SSH key passphrase.
6. Perform these steps for each repo.

### Altering `serverconfig.xml` (Win)

With the repos on your local hard drive, you need to alter the `serverconfig.xml` file to avoid some errors when building OpenDAoC locally.

1. Copy the file `/OpenDAoC-Core/CoreServer/config/serverconfig.example.xml` to `/OpenDAoC-Core/CoreServer/config/serverconfig.xml`.
2. Open the `serverconfig.xml` file.
3. Within the `RegionIP` tags, change the value `0.0.0.0` to one of these:
   1. To test locally, enter `127.0.0.1`.
   2. To test over LAN, enter your machine's IP address (use the Terminal command `ipconfig`, and it should start with `192`).
   3. To test outside your network, [enter your public IP address](https://api.ipify.org).
4. Configure the database access as per your own configuration.

Now you're ready to [run your own instance of OpenDAoC](#building-your-dol-server-locally)!

## Building Your OpenDAoC Server Locally

This section provides the commands necessary for both building and running an OpenDAoC server locally.

1. Launch the Terminal or PowerShell, navigate to the `/OpenDAoC-Core/` directory and type `dotnet build DOLLinux.sln`. This builds the OpenDAoC server on your machine. <!-- This may take several minutes to complete. -->
2. If the build was successful, now enter the command `dotnet run --project CoreServer` to launch the server, making it accessible to player logins. <!-- This may take several minutes to complete. -->

Congratulations! You're now running an instance of OpenDAoC on your machine.

## Preparing the DAoC Client

The best way to connect to your local instance, is to use the latest OpenDAoC DAoC client:

1. Download the installer and follow the instructions available on [OpenDAoC Website](https://www.opendaoc.com/how-to-connect).

The client is ready for all OpenDAoC servers.

## Accessing Local Servers

### Manually with a `.bat` file

1. Open the folder where you installed the client and create a new file called `local.bat`.
2. Open the file in a text editor and enter the following:
   `connect.exe game1127.dll 127.0.0.1 YOURUSERNAME YOURPASSWORD`

**Notes**:
- The IP address should match the IP of the machine running the server.
- With the standard configuration, the account will be created automatically at the first connection.

### Using DAoC Portal

Once you've built your OpenDAoC server and it's running locally, you can now access it using the DAoC client and DAoCPortal.

1. Launch DAoCPortal.
2. Navigate to the _Custom Shards_ tab.
3. Right-click in the app and click **Add Server...**.
4. Enter whatever values you want for **Name** and **Description**.
5. For **IP or Hostname**, enter `127.0.0.1` if you're accessing the server from the same machine.
6. Leave the port set to `10300`.
7. Click **OK**.
8. Click on the desired server.
9. Provide a value for **User** (Username) and **Pass** (Password).
10. Click **Play!**

The DAoC client launches and creates a new account based on the credentials you entered. You should be brought to the character selection screen now.

## Testing

### In-Game Testing

When testing OpenDAoC in-game, special attention should be paid when utilizing the `/plvl` GM command. Changing this setting for an account is currently **permanent** in OpenDAoC.

Also, testing components such as combat (PvP or PvE) cannot be done as a _Gamemaster_ or _Admin_ (creatures and players cannot attack these player types). You must have an account with `/plvl 1` status.

### Recommended Extensions for Testing

If you're using [Visual Studio](https://visualstudio.microsoft.com/vs/community/), these are recommended extensions to assist with testing:

* [.NET Core Test Explorer](https://marketplace.visualstudio.com/items?itemName=formulahendry.dotnet-test-explorer)

If you're using Jetbrains Rider, [explore plugins currently available](https://plugins.jetbrains.com/rider).

Should you have any tools or plugins you'd like to recommend, please let us know and we'll include them here.

_**NOTE:** Currently, not all tests run reliably during a full suite run. This is especially true in the `Integration` category, as several ephemeral failures may result due to race conditions with the database. Performing manual reruns of individual tests after a full-suite run should have the end result of clearing most previous failures._

### Logging

Logging is controlled by the `/OpenDAoC-Core/Debug/config/logconfig.xml` file. The default configuration may not be verbose enough for the purposes of development, so make any changes here as needed for logging.


## License

[GNU General Public License v3.0](https://choosealicense.com/licenses/gpl-3.0/)
