Atlas Freeshard - Core
========

How To Build
----

We're targeting Linux+Net5 on prod.

Clone Git Repository to a <workingDirectory>.

`cp <workingDirectory>/Net5/DOLServer/config/serverconfig.example.xml cp <workingDirectory>/Net5/DOLServer/config/serverconfig.xml`
`cd Net5 && dotnet build DOLdotnetLinux.sln`
`dotnet run --project DOLServer`
