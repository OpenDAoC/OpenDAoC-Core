Atlas Freeshard - Core

# Setting Up an Atlas Dev Environment

The following instructions should act as a guide for properly setting up an Ubuntu environment to fetch and push file changes with GitLab as well as build and run a DoL server successfully.

Instructions currently exist regarding setups for both [Ubuntu](#setting-up-on-ubuntu) and [Windows](#setting-up-on-windows) operating systems.

**IMPORTANT:** Check the [Before You Start](#before-you-start) section first as you will not be able to complete certain sections without the proper resources or privileges.

The following sections outline the process of preparing your environment to build an Atlas server:

1. [Environment Requirements](#environment-requirements)
   1. [Before You Start](#before-you-start)
   2. [Considerations for IDEs](#considerations-for-ides)
2. [Setting Up on Ubuntu](#setting-up-on-ubuntu)
   1. [Installing .NET 5.0](#installing-net-50-ubuntu)
   2. [Installing MariaDB 10.5](#installing-mariadb-105-ubuntu)
      1. [Preparing Your Database](#preparing-your-database-ubuntu)
      2. [Configuring `My.cnf`](#configuring-mycnf-ubuntu)
      3. [Adding `atlasDB.sql`](#adding-atlasdbsql-ubuntu)
   3. [Encrypting File Transfers](#encrypting-file-transfers-ubuntu)
      1. [Adding a Personal Access Token](#setting-up-a-personal-access-token-ubuntu)
      2. [Setting Up SSH Tunneling](#setting-up-ssh-tunneling-ubuntu)
         1. [Enabling SSH](#enabling-ssh-ubuntu)
         2. [Configuring Your Router](#configuring-your-router-ubuntu)
         3. [Creating an SSH Key](#creating-an-ssh-key-ubuntu)
         4. [Adding the SSH Key to GitLab](#adding-the-ssh-key-to-gitlab-ubuntu)
   4. [Installing Git](#installing-git-ubuntu)
   5. [Cloning Atlas' Repos](#cloning-the-repository-ubuntu)
   6. [Altering `serverconfig.xml`](#altering-serverconfigxml-ubuntu)
3. [Setting Up on Windows](#setting-up-on-windows)
   1. [Installing .NET 5.0](#installing-net-50-win)
   2. [Installing MariaDB 10.5](#installing-mariadb-105-win)
      1. [Preparing Your Database](#preparing-your-database-win)
      2. [Configuring `My.ini`](#configuring-myini-win)
      3. [Adding `atlasDB.sql`](#adding-atlasdbsql-win)
   3. [Encrypting File Transfers](#encrypting-file-transfers-win)
      1. [Adding a Personal Access Token](#setting-up-a-personal-access-token-win)
      2. [Setting Up SSH Tunneling](#setting-up-ssh-tunneling-win)
         1. [Enabling SSH](#enabling-ssh-win)
         2. [Configuring Your Router](#configuring-your-router-win)
         3. [Creating an SSH Key](#creating-an-ssh-key-win)
         4. [Adding the SSH Key to Gitlab](#adding-the-ssh-key-to-gitlab-win)
   4. [Installing Git](#installing-git-win)
   5. [Cloning Atlas' Repos](#cloning-the-repository-win)
   6. [Altering `serverconfig.xml`](#altering-serverconfigxml-win)
4. [Building Your DoL Server Locally](#building-your-dol-server-locally)
5. [Accessing Local Servers](#accessing-local-servers)
6. [Accessing the World Builders' Server](#accessing-the-world-builders-server)
7. [Testing](#testing)
   1. [In-Game Testing](#in-game-testing)
   2. [Recommended Extensions for Testing](#recommended-extensions-for-testing)
8. [Logging](#logging)

## Environment Requirements

The following are main OS, tool, and version requirements to consider when setting up an environment:

* **Operating System:** Ubuntu or Windows
* **Source-Code Editor:** .NET IDE that supports C#, such as [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/) or [Jetbrain Rider](https://www.jetbrains.com/rider/) (if you have a student email address)
* **Source Control:** Git is recommended for tracking file changes, and [GitLab](http://gitlab.com) is the current file repository
* **RDBMS:** [MariaDB v.10.5.X](https://mariadb.com/kb/en/changes-improvements-in-mariadb-105/)

_**NOTE:** This walkthrough assumes that you already have an account created on GitLab and have been granted access to Atlas' repos by a senior developer or administrator. If you have not done so already, [create an account on Gitlab](http://gitlab.com/) and send your username to an administrator or lead developer._

### Before You Start

You will need the following files or permissions before you'll be able to complete this process in its entirety. For each of these, please message clait on Discord:

* Download the latest stable copy of `atlasDB.sql`.
* Receive the `Developer` role on Discord to access team channels.
* Obtain access to all team forums on the Atlas freeshard website.

### Considerations for IDEs

Atlas team members are not required to use the same developer tools when working on any DoL-related projects. If you already have access to or prefer a certain tool, we encourage you to use what you're most familiar with. However, we would like to recommend the following tools:

* [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/): Free.
* [Jetbrains Rider](https://www.jetbrains.com/rider/): Free with a student email account.

Whatever tool you decide to use, please check to see if it has any environment configurations or functionalities for **.NET** and **C#** that can be activated/installed.

## Setting Up on Ubuntu

This process assumes you do not already have a fully-configured environment with the specific tools or software installed previously. 

If you've' already completed a step previously, we recommend that you quickly review the steps outlined to ensure no special configurations are missed.

1. [Installing .NET 5.0](#installing-net-50-ubuntu)
2. [Installing MariaDB 10.5](#installing-mariadb-105-ubuntu)
   1. [Preparing Your Database](#preparing-your-database-ubuntu)
   2. [Configuring `My.cnf`](#configuring-mycnf-ubuntu)
   3. [Adding `atlasDB.sql`](#adding-atlasdbsql-ubuntu)
3. [Encrypting File Transfers](#encrypting-file-transfers-ubuntu)
   1. [Adding a Personal Access Token](#setting-up-a-personal-access-token-ubuntu)
   2. [Setting Up SSH Tunneling](#setting-up-ssh-tunneling-ubuntu)
      1. [Enabling SSH](#enabling-ssh-ubuntu)
      2. [Configuring Your Router](#configuring-your-router-ubuntu)
      3. [Creating an SSH Key](#creating-an-ssh-key-ubuntu)
      4. [Adding the SSH Key to GitLab](#adding-the-ssh-key-to-gitlab-ubuntu)
   3. [Installing Git](#installing-git-ubuntu)
      1. [Cloning Atlas' repos](#cloning-the-repository-ubuntu)
      2. [Altering `serverconfig.xml`](#altering-serverconfigxml-ubuntu)

### Installing .NET 5.0 (Ubuntu)

.NET is an open-source developer platform used for building applications. Atlas uses .NET 5.0.X specifically, which is only supported on Ubuntu 21.04, 20.04, 18.04, and 16.04.

Perform the following steps from the Terminal:

1. `wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb`
2. `sudo dpkg -i packages-microsoft-prod.deb`
3. `sudo apt update`
4. `sudo apt install apt-transport-https`
5. `sudo apt-get install -y dotnet-sdk-5.0`
6. `dotnet --list-sdks`
7. `dotnet --list-runtimes`

### Installing MariaDB 10.5 (Ubuntu)

MariaDB is an open-source relational database management system (RDBMS). Atlas specifically utilizes v10.5.

Perform the following steps from the Terminal:

1. `sudo apt update && sudo apt upgrade`
2. `sudo apt -y install software-properties-common`
3. `sudo apt-key adv --fetch-keys 'https://mariadb.org/mariadb_release_signing_key.asc'`
4. `sudo add-apt-repository 'deb [arch=amd64] http://mariadb.mirror.globo.tech/repo/10.5/ubuntu focal main'`
5. `sudo apt update`
6. `sudo apt install mariadb-server mariadb-client`
7. Type `y` to accept.

The RDBMS is installed, but needs a user and database for Atlas to access and use.

#### Preparing Your Database (Ubuntu)

The following steps walk you through the process of adding a user and database using MariaDB.

If you're already familiar with the process and wish to skip it, just remember that the following things are required to run `AtlasCore`:

* There must be a user named `atlas`.
* The user's password must be `atlas`.
* The `atlas` user must have sufficient privileges.
* A database must exist called `atlas`.

_**NOTE:** If you set values (user ID, user password, and database name) contrary to those specified here, `DOLServer` builds will fail._
1. `sudo mysql -u root` <!-- This allows you to access the MariaDB client-->
2. `CREATE DATABASE atlas;` <!-- The DB **MUST** be named "atlas" -->
3. `SHOW DATABASES;`</li> <!-- Verify that the DB exists -->
4. `CREATE USER 'atlas'@localhost IDENTIFIED BY 'atlas';` <!-- Both username and password must be "atlas" -->
5. `SELECT User FROM mysql.user;` <!-- This lists all existing users -->
6. To grant all privileges **for all databases** to the _atlas_ user, use the following command: `GRANT ALL PRIVILEGES ON *.* TO 'atlas'@localhost IDENTIFIED BY 'atlas';` <!-- The 'atlas' user may exercise ALL privileges on ALL of your databases -->
7. To grant all privileges **only to the _atlas_ DB**, use this command: `GRANT ALL PRIVILEGES ON atlas.* TO 'atlas'@localhost;` <!-- The 'atlas' user may only modify the 'atlas' DB -->
8. `FLUSH PRIVILEGES` <!-- Refreshes the privilege changes -->
9. `SHOW GRANTS FOR 'atlas'@localhost;` <!-- This lists all privileges granted to the `atlas` user -->

#### Configuring `My.cnf` (Ubuntu)

We recommend that you also make some changes to the `my.cnf` file to avoid potential issues when building and running your DoL server locally.

1. `sudo nano /etc/mysql/my.cnf`
2. Add and/or un-comment the following settings:

```
[client-server]
[client]
   port = 3306
   socket = /run/mysqld/mysqld.sock
   default-character-set = utf8
[mariadb]
   lower_case_table_names = 1
   !includedir /etc/mysql/conf.d/
   !includedir /etc/mysql/mariadb.conf.d 
```

3. Save and exit the file, but **do not change the file name**.

#### Adding `atlasDB.sql` (Ubuntu)

Prior to accomplishing this step, you will need to request a recent copy of the `atlasDB.sql` file. Without it, you cannot successfully build a local Atlas server. After installing MariaDB, you should also notice it installed a program called HeidiSQL, which you'll need to use for this section.

1. Launch the Terminal and type `sudo mysql -u root atlas < ~/path/to/atlasDB.sql`. This copies the file's contents to the `atlas` database.
2. To check that the import was successful, enter `sudo mysql -u root`. This launches the MariaDB Client.
3. `use atlas;`
4. `show tables`

This should list multiple tables, which indicates the import was successful.

### Encrypting File Transfers (Ubuntu)

GitLab requires encrypted communication between your repository and client machine. You may pick between two methods to accomplish this:

 1. [Personal Access Token](#setting-up-a-personal-access-token-ubuntu) (easiest)
 2. [SSH tunneling](#setting-up-ssh-tunneling-ubuntu)

#### Setting Up a Personal Access Token (Ubuntu)

Creating and using a Personal Access Token is the easiest method of gaining access to Atlas' repositories.

 1. Navigate to [GitLab](https://gitlab.com/users/sign_in) and sign in.
 2. Click on your profile picture at the top-right corner of the screen and then **Edit profile**.
 3. Select the _Access Tokens_ tab on the left side of the screen.
 4. Provide a **Token name** and, if desired, an expiration date.
 5. Under **Select scopes**, make sure that **read_repository** and 6. write_repository** are selected.
 6. Click **Create personal access token**. A string appears.
 7. Copy this and paste it somewhere safe for later reference.

You may now continue to the next step, [Installing Git](#installing-git-ubuntu).

#### Setting Up SSH Tunneling (Ubuntu)

Enabling Secure Shell (SSH) is a network protocol used to secure connections between client and server. Enabling this requires access to your router's administrator interface.

These are the steps associated with using SSH encryption:

1. [Enabling SSH](#enabling-ssh-ubuntu)
2. [Configuring Your Router](#configuring-your-router-ubuntu)
3. [Creating an SSH Key](#creating-an-ssh-key-ubuntu)
4. [Adding the SSH Key to GitLab](#adding-the-ssh-key-to-gitlab-ubuntu)

##### Enabling SSH (Ubuntu)

By default, SSH functionality is not installed/enabled on Ubuntu.

From the Terminal, enter the following commands:

1. `sudo apt update`
2. `sudo apt install openssh-server`
3. `sudo systemctl status ssh`

The output should indicate that the service is running and `active`. Now Ubuntu's firewall configuration tool, UFW, must have the SSH port opened:

4. `sudo ufw allow ssh`

##### Configuring Your Router (Ubuntu)

As part of SSH, your router must allow external communication between GitLab and your machine. This process varies depending on the type of router and the web interface used, but the following actions must be performed as part of  SSH tunneling:

1. Identify your device's internal IP address (typically starts with `192`).
2. Make a DHCP reservation for the IP address (so that you don't have to reconfigure SSH every time your IP updates).
3. Add a single port forwarding for that IP using port `22`.

##### Creating an SSH Key (Ubuntu)

From the Terminal app, enter the following commands:

1. `ssh username@public_ip_address` <!-- Replace 'username' with your root account username and 'ip_address' with your public IP address, e.g., linux@71.190.200.35 -->
   1. `username` is the credential you use to log in on GitLab (not the email address).
   2. If you don't know your public IP address, navigate to [https://api.ipify.org](https://api.ipify.org) and copy/paste the value found there.
2. Type `yes` and then you'll be prompted to enter your account password.
3. `ssh-keygen -t rsa -b 4096 -C "your_email@domain.com"`.
4. Press **Enter** to accept the default file location and file name (or if you already have multiple keys, enter a name specific to the project that you'll easily recall, such as `atlas`). Provide a passphrase for security. Remember what you enter here.
5. To ensure the SSH key was created, enter `ls ~/.ssh/`.

Put your private key (e.g., `id_rsa`) somewhere safe. Remember the name and location of your public key (e.g., `id_rsa.pub`). Also, don't forget the passphrase.

##### Adding the SSH Key to GitLab (Ubuntu)

1. Navigate to GitLab from a browser and click **Login**. Enter your credentials and click **Sign In**.
2. Click on your profile picture at the top-right corner of the screen and then select **Edit profile**.
3. Select the _SSH Keys_ tab on the left side of the screen.
4. From your machine, open the **Files** app and then the `id_rsa.pub` file in a text editor. Copy the file's contents.
5. Returning to GitLab, paste the string in the **Key** field. Provide a **Title** to identify the key.
6. Click **Add key**.

Each time you pull or push files using Git, you may be prompted to enter the passphrase (if one was set) for your SSH key.

### Installing Git (Ubuntu)

If you have not already installed Git on your machine, launch the Terminal app and type these commands:

1. `sudo apt install git`
2. `git config --global user.name "Your Name"`
3. `git config --global user.email "you@example.com"`

### Cloning the Repository (Ubuntu)

With Git ready, it's time to clone the `AtlasCore` repository.

1. From the Terminal, navigate to the desired directory to house the repos.
2. In a browser, navigate to [GitLab](https://www.gitlab.com) and open the desired repo.
3. Click the **Clone** button at the top-right corner.
4. If you're configured with a [Personal Access Token](#setting-up-a-personal-access-token-ubuntu), copy the HTTPS URL. If you configured with [SSH](#setting-up-ssh-tunneling-ubuntu), then copy the SSH address.
5. Returning to the Terminal, (and from your desired directory) type `git clone (PASTE THE ADDRESS)`. Enter your credentials when prompted, or the SSH key passphrase.

### Altering `serverconfig.xml` (Ubuntu)

With the repo on your local hard drive, you need to alter the `serverconfig.xml` file to avoid some errors when building Atlas locally.

1. Copy the file `/AtlasCore/Net5/DOLServer/config/serverconfig.example.xml` to `/AtlasCore/Net5/DOLServer/config/serverconfig.xml`.
2. Open the `serverconfig.xml` file.
3. Within the `RegionIP` tags, change the value `0.0.0.0` to one of these:
   1. To test locally, enter `127.0.0.1`.
   2. To test over LAN, enter your machine's IP address (use the Terminal command `ip a`, and it should start with `192`).
   3. To test outside your network, [enter your public IP address](https://api.ipify.org).
   
Now you're ready to [run your own instance of Atlas](#building-your-dol-server-locally)!



## Setting Up on Windows

This process assumes you do not already have a fully-configured environment with the specific tools or software installed previously.

If you've already completed a step previously, we recommend that you quickly review the steps outlined to ensure no special configurations are missed.

1. [Installing .NET 5.0](#installing-net-50-win)
2. [Installing MariaDB 10.5](#installing-mariadb-105-win)
   1. [Preparing Your Database](#preparing-your-database-win)
   2. [Configuring `My.ini`](#configuring-myini-win)
   3. [Adding `atlasDB.sql`](#adding-atlasdbsql-win)
3. [Encrypting File Transfers](#encrypting-file-transfers-win)
   1. [Adding a Personal Access Token](#setting-up-a-personal-access-token-win)
   2. [Setting Up SSH Tunneling](#setting-up-ssh-tunneling-win)
      1. [Enabling SSH](#enabling-ssh-win)
      2. [Configuring Your Router](#configuring-your-router-win)
      3. [Creating an SSH Key](#creating-an-ssh-key-win)
      4. [Adding the SSH Key to Gitlab](#adding-the-ssh-key-to-gitlab-win)
4. [Installing Git](#installing-git-win)
5. [Cloning Atlas' Repos](#cloning-the-repository-win)
6. [Altering `serverconfig.xml`](#altering-serverconfigxml-win)

### Installing .NET 5.0 (Win)

.NET is an open-source developer platform used for building applications. Atlas uses .NET 5.0.X specifically, which is supported on all recent versions of Windows.

1. Download the [.NET 5.0 installer](https://dotnet.microsoft.com/download/dotnet/5.0).
2. Install the tool and make any configurations as needed.

### Installing MariaDB 10.5 (Win)

MariaDB is an open-source relational database management system (RDBMS). Atlas specifically uses v10.5.

1. Download MariaDB:
   1. [32-bit](https://downloads.mariadb.org/interstitial/mariadb-10.5.4/win32-packages/mariadb-10.5.4-win32.zip/from/https%3A//archive.mariadb.org/)
   2. [64-bit](https://downloads.mariadb.org/interstitial/mariadb-10.5.4/winx64-packages/mariadb-10.5.4-winx64.zip/from/https%3A//archive.mariadb.org/)
2. Initiate the MariaDB installer.
3. Enable **Use UTF8 as default server's character set**. Make any changes to the following dialog windows as desired.
4. Complete the installation by clicking **Install** and then **Finish**.

The RDBMS is installed, but needs a user and database for Atlas to access and use.

#### Preparing Your Database (Win)

The following steps walk you through the process of adding a user and database using MariaDB.

If you're already familiar with the process and wish to skip it, just remember that the following things are required to run `AtlasCore`:

* There must be a user named `atlas`.
* The user's password must be `atlas`.
* The `atlas` user must have sufficient privileges.
* A database must exist called `atlas`.

_**NOTE:** If you set values (user ID, user password, and database name) contrary to those specified here, `DOLServer` builds will fail._

1. Launch the **MySQL Client (MariaDB 10.5)** option from the Start menu.
2. `CREATE DATABASE atlas;` <!-- The DB **MUST** be named "atlas" -->
3. `SHOW DATABASES;` <!-- Verify that the DB exists -->
4. `CREATE USER 'atlas'@localhost IDENTIFIED BY 'atlas';` <!-- Both username and password must be "atlas" -->
5. `SELECT User FROM mysql.user;` <!-- This lists all existing users -->
6. To grant all privileges **for all databases** to the _atlas_ user, use the following command: `GRANT ALL PRIVILEGES ON *.* TO 'atlas'@localhost IDENTIFIED BY 'atlas';` <!-- The 'atlas' user may exercise ALL privileges on ALL of your databases -->
7. To grant all privileges **only to the _atlas_ DB**, use this command: `GRANT ALL PRIVILEGES ON atlas.* TO 'atlas'@localhost;` <!-- The 'atlas' user may only modify the 'atlas' DB -->
8. `FLUSH PRIVILEGES` <!-- Refreshes the privilege changes -->
9. `SHOW GRANTS FOR 'atlas'@localhost;` <!-- This lists all privileges granted to the `atlas` user -->

#### Configuring `My.ini` (Win)

We recommend that you also make some changes to the `my.ini` file to avoid potential issues when building and running your DoL server locally.

1. Open the `my.ini` file located at `C:\Program Files\MariaDB 10.5\data\`.
2. Add the following lines:

```
[mariadb]
lower_case_table_names=1
```

3. Save and exit the file, but **do not change the file name**.

#### Adding `atlasDB.sql` (Win)

Prior to accomplishing this step, you will need a recent copy of the `atlasDB.sql` file. Without it, you cannot successfully build a local Atlas server. After installing MariaDB, you should also notice a program called HeidiSQL, which you'll need to use for this section.

1. Launch the HeidiSQL app.
2. At the bottom-left corner. click **New > Session in root folder**.
3. Enter a root user **Password** if you set one previously.
4. Click **Save**.
5. Now click **Open** to start a connection with MariaDB.
6. Select the `atlas` database you created previously.
7. Click **File > Load SQL file**.
8. Navigate to the `atlasDB.sql` file and click **Open**.
9. Select the **Run file(s) directly** option as loading the file will cause HeidiSQL to crash.

The application will process the entire SQL file. Once done, you should now see tables and data populating the `atlas` database.

### Encrypting File Transfers (Win)

GitLab requires encrypted communication between the repository and client machine. You may pick one of two methods to accomplish this:

1. [Personal Access Token](#setting-up-a-personal-access-token-win) (easiest)
2. [SSH tunneling](#setting-up-ssh-tunneling-win)

#### Setting Up a Personal Access Token (Win)

Creating and using a Personal Access Token is the easiest method of gaining access to Atlas' repositories.

1. Navigate to [GitLab](https://gitlab.com/users/sign_in) and sign in.
2. Click on your profile picture at the top-right corner of the screen and then **Edit profile**.
3. Select the _Access Tokens_ tab on the left side of the screen.
4. Provide a **Token name** and, if desired, an expiration date.
5. Under **Select scopes**, make sure that **read_repository** and 6. write_repository** are selected.
6. Click **Create personal access token**. A string appears.
7. Copy this and paste it somewhere safe for later reference.

You may now continue to the next step, [Installing Git](#installing-git-win).

#### Setting Up SSH Tunneling (Win)

Enabling Secure Shell (SSH) is a network protocol used to secure connections between client and server. Enabling this requires access to your router's administrator interface.

These are the steps associated with using SSH encryption:

1. [Enabling SSH](#enabling-ssh-win)
2. [Configuring Your Router](#configuring-your-router-win)
3. [Creating an SSH Key](#creating-an-ssh-key-win)
4. [Adding the SSH Key to GitLab](#adding-the-ssh-key-to-gitlab-win)

##### Enabling SSH (Win)

By default, SSH functionality is already installed and enabled on Windows through PowerShell.

However, you should first verify that the service is installed and running:

1. Right-click on the **Windows PowerShell** app and select **Run as administrator**.
2. First, check to make sure OpenSSH is installed: `Get-WindowsCapability -Online | ? Name -like 'OpenSSH*'`.
3. If `OpenSSH.Client` appears with the **State** of:
   1. `NotPresent`, then type: `Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0`.
   2. `Installed`, then continue to the next steps below.

If the SSH client is already installed:

1. Type `Get-Service -Name ssh-agent`. If the **Status** returns as:
   1. `Running`, continue to step 2.
   2. `Stopped`, then type: `Start-Service ssh-agent`.
2. `Set-Service ssh-agent -StartupType Automatic`
3. `New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22`

##### Configuring Your Router (Win)

As part of SSH, your router must allow external communication between GitLab and your machine. This process varies depending on the type of router and the web interface used, but the following actions must be performed as part of  SSH tunneling:

1. Identify your device's internal IP address (typically starts with `192`).
2. Make a DHCP reservation for the IP address (so that you don't have to reconfigure SSH every time your IP updates).
3. Add a single port forwarding for that IP using internal port `22`.

##### Creating an SSH Key (Win)

Launch **Windows PowerShell** as an administrator:

1. `~/.ssh/`
2. `ssh-keygen -t ed25519 -C "your_email@example.com"`
3. Enter a key name or leave it blank to retain the default name.
4. Enter a passphrase and then confirm it. Remember this value for later.
5. `ssh-add private_key_name`

##### Adding the SSH Key to GitLab (Win)

1. Navigate to GitLab from a browser and click **Login**. Enter your credentials and click **Sign In**.
2. Click on your profile picture at the top-right corner of the screen and then select **Edit profile**.
3. Select the _SSH Keys_ tab on the left side of the screen.
4. From your machine, open the public key file in a text editor. Copy the file's contents.
5. Returning to GitLab, paste the string in the **Key** field. Provide a **Title** to identify the key.
6. Click **Add key**.

Each time you pull or push files using Git, you may be prompted to enter the passphrase (if one was set) for your SSH key.

### Installing Git (Win)

If you are using Windows 10, Git functionality is already included as part of Windows PowerShell and thus does not require installation.

Additionally, tools like Visual Studio and JetBrains Rider are equipped with plugins that allow you to connect with remote repositories.

However, you should make sure that the following global configurations are in place:

1. `git config --global user.name "user_name"`
2. `git config --global user.email "email@example.com"`

### Cloning the Repository (Win)

With Git ready, it's time to clone the `AtlasScripts` and `AtlasCore` repositories.

1. From Windows PowerShell, navigate to the desired directory to house the repos.
2. In a browser, navigate to [GitLab](https://www.gitlab.com) and open the desired repo.
3. Click the **Clone** button at the top-right corner.
4. If you're configured with a [Personal Access Token](#setting-up-a-personal-access-token-win), copy the HTTPS URL. If you configured with [SSH](#setting-up-ssh-tunneling-win), then copy the SSH address.
5. Returning to the Terminal, (and from your desired directory) type `git clone (PASTE THE ADDRESS)`. Enter your credentials if prompted, or the SSH key passphrase.
6. Perform these steps for each repo.

### Altering `serverconfig.xml` (Win)

With the repos on your local hard drive, you need to alter the `serverconfig.xml` file to avoid some errors when building Atlas locally.

1. Copy the file `/AtlasCore/Net5/DOLServer/config/serverconfig.example.xml` to `/AtlasCore/Net5/DOLServer/config/serverconfig.xml`.
2. Open the `serverconfig.xml` file.
3. Within the `RegionIP` tags, change the value `0.0.0.0` to one of these:
   1. To test locally, enter `127.0.0.1`.
   2. To test over LAN, enter your machine's IP address (use the Terminal command `ipconfig`, and it should start with `192`).
   3. To test outside your network, [enter your public IP address](https://api.ipify.org).

Now you're ready to [run your own instance of Atlas](#building-your-dol-server-locally)!

## Building Your DoL Server Locally

This section provides the commands necessary for both building and running a DoL server locally.

1. Launch the Terminal or PowerShell, navigate to `/AtlasCore/Net5/` and type `dotnet build DOLdotnetLinux.sln`. This builds the DoL server on your machine. <!-- This may take around 1-2 minutes to complete. Don't panic if you see multiple warnings and errors. -->
2. Enter the command `dotnet run --project DOLServer` to launch the server, making it accessible to player logins. <!-- This may take several minutes to complete. -->

Congratulations! You're now running an instance of Atlas on your machine.

## Preparing the DAoC Client

In order to access either local builds of Atlas or the World Builders' server, you need to have the DAoC client installed and equipped with the right files.

1. Download the full [DAoC Client](https://darkageofcamelot.com/sites/daoc/files/downloads/DAoCSetup.exe).
2. Install it, launch the application, and then download all patches. 
3. Log in on the Atlas forums and download the [AtlasPack](https://www.atlasfreeshard.com/files/AtlasPack-0.2.zip) and [AtlasDF](https://www.atlasfreeshard.com/files/AtlasDF.zip).
Extract the files. 
4. Place everything from `AtlasPack` into the base directory (e.g., `C:\Program Files (x86)\Electronic Arts\Dark Age of Camelot\`). Replace any files affected when prompted. 
5. Place the `zones` folder from `AtlasDF` onto the `zones` folder in the same directory as was used for `AtlasPack`.
6. Download the [DAoCPortal](https://sourceforge.net/projects/dolserver/files/DAOC%20Portal/DAoCPortalSetup.msi/download) app and install it.

The client is ready for all Atlas servers.

## Accessing Local Servers

Once you've built your Atlas server and it's running locally, you can now access it using the DAoC client and DAoCPortal.

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

## Accessing the World Builders' Server

The World Builders all currently access a single live instance of the Atlas server to populate the regions with NPCs, mobs, and so on.

**TO GAIN ACCESS:** You must first request an account from clait to access this server. He will provide you with a regular (/plvl1) account. Should you require additional capabilities as a GameMaster (/plvl2) or World Builder/Administrator (/plvl3), an additional account will be provided.

**NOTE:** This is not intended as a "play" server, as using GM commands can negatively impact the work of the World Builders. So we would ask that perform tests with caution and respect for the work the Builders have done.

1. From the base directory of the DAoC client, execute the `AtlasLauncher` application.
2. Enter your login credentials and click **PLAY**.

This should launch the DAoC client and then the character selection screen.

**If you are unable to get past the loading screen**, check the `dol` channel on Discord to see if the server is online.

## Testing

`AtlasCore` uses [NUnit3](https://nunit.org/) for running tests.

### In-Game Testing

When testing Atlas in-game, special attention should be paid when utilizing the `/plvl` GM command. Changing this setting for an account is currently **permanent** in Atlas.

Also, testing components such as combat (PvP or PvE) cannot be done as a _Gamemaster_ or _Admin_ (creatures and players cannot attack these player types). You must have an account with `/plvl 1` status.

A full list of GM commands may be found [on the Atlas forums](https://www.atlasfreeshard.com/threads/wip-list-of-gm-commands.102/).

### Recommended Extensions for Testing

If you're using [Visual Studio](https://visualstudio.microsoft.com/vs/community/), these are recommended extensions to assist with testing:

* [.NET Core Test Explorer](https://marketplace.visualstudio.com/items?itemName=formulahendry.dotnet-test-explorer)

If you're using Jetbrain Rider, [explore plugins currently available](https://plugins.jetbrains.com/rider).

Should you have any tools or plugins you'd like to recommend, please let us know and we'll include them here.

_**NOTE:** Currently, not all tests run reliably during a full suite run. This is especially true in the `Integration` category, as several ephemeral failures may result due to race conditions with the database. Performing manual reruns of individual tests after a full-suite run should have the end result of clearing most previous failures._

### Logging

Logging is controlled by the `/AtlasCore/Net5/Debug/config/logconfig.xml` file. The default configuration may not be verbose enough for the purposes of development, so make any changes here as needed for logging.