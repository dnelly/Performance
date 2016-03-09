#Prerequisites:
    * Install git command line client
    * Install curl
        - For Windows, edit "SetEnv.ps1" and set $global:toolsPath to the folder where curl.exe is
        - For Linux, curl should be in PATH
    * Install libuv (linux only: http://docs.asp.net/en/latest/getting-started/installing-on-linux.html#install-libuv)

********* Windows *****************

Installation (admin mode required)
.\InstallDotnet.ps1                 ## admin mode required

To build and prepare application only without test run:
.\SetEnv.ps1
.\SetupPerfApp.ps1

To run 1 iteration of test when application is already built
.\SetEnv.ps1
.\PerformTest.ps1


********* Linux *********************

Installation
sudo ./InstallDotnet.sh

To build and prepare application only without test run:
source ./SetEnv.sh
source ./SetupPerfApp.sh

To run 1 iteration of test when application is already built
source ./SetEnv.sh
source ./PerformTest.sh


******************************************************************************************************************
Cold Start Scenarios to track:

* Plain Text
Barebone kestrel application with a plaintext middle app. Output content follows the Techempower plain text view.
- Published application
- Currently no crossgen (the only native module is mscorlib.ni.dll)
- Packages are loaded in DOTNET_PACKAGES_CACHE

* HelloWorldMvc (WIP)
Simple MVC application with a plaintext mvc home view
- Published application
- Currently no crossgen (the only native module is mscorlib.ni.dll)
- Packages are loaded in DOTNET_PACKAGES_CACHE
- Views are precompiled

* HelloWorldMvc dynamic views
Simple MVC application with a plaintext mvc home view
- Published application
- Currently no crossgen (the only native module is mscorlib.ni.dll)
- Packages are loaded in DOTNET_PACKAGES_CACHE
- Views are compiled at first request

* MusicStore home page scenario (WIP)
Full fledged MVC application with database query (EF)
- Published application
- Currently no crossgen (the only native module is mscorlib.ni.dll)
- Packages are loaded in DOTNET_PACKAGES_CACHE
- Views are precompiled