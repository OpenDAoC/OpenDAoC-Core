Atlas Freeshard - Core
========

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
