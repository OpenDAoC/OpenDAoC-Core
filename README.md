Atlas Freeshard - Core
========

[![.Net5 Tests on Linux](https://github.com/claitz/AtlasCore/actions/workflows/test_dotnet5_linux.yml/badge.svg)](https://github.com/claitz/AtlasCore/actions/workflows/test_dotnet5_linux.yml)
[![.Net5 Tests on Windows](https://github.com/claitz/AtlasCore/actions/workflows/test_dotnet5_windows.yml/badge.svg)](https://github.com/claitz/AtlasCore/actions/workflows/test_dotnet5_windows.yml)


How To Build
----

We're targeting Linux+Net5 on prod.

Clone Git Repository to a $workingDirectory.
Clone the [AtlasScript repository](https://github.com/claitz/AtlasCore) in the same root, you should have this folder structure:

/AtlasCore/  
/AtlasScripts/

`cp $wokingDirectory/Net5/DOLServer/config/serverconfig.example.xml $wokingDirectory/Net5/DOLServer/config/serverconfig.xml`

`cd Net5 && dotnet build DOLdotnetLinux.sln`

`dotnet run --project DOLServer`


Running Tests
----
/AtlasCore/ uses nunit3 for testing.

Suggested VSCode extensions that make test running easier:
  - .NET Core Test Explorer

> Currently not all tests run reliably during a full suite run. Especially in the Integration category, there can be several ephemeral failures due to race conditions with the database. Manual reruns of individual tests after a full suite run should see most or all failures clear. Obviously this is not optimal, and at some point someone should be deleting this note because it's been fixed...


Logging
----

The default logging configuration may not be verbose enough for development. You can change the level by editing /Net5/Debug/config/logconfig.xml (or similar). 
