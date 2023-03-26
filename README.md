# Snake Game - Multiplayer Networking Game
Authors: Monthon Paul

Current Version: 1.2

Last Updated: 3/26/2023

# Purpose: 
To learn of how Game Design work by this Snake game, with building the functionality of Networking, Snake Client, & Snake Server.

# Quick summary:
This is a Snake game, inspired by Snake game of 1976 arcade game Blockade developed by a British company called Gremlin Interactive. This Program takes a Spin by makeing it a full Online Multiplayer Game. Users are allow to connect to a Snake Server, on which the User can enter in its name to play on a Network with other players.

First Textbox allow the User to enter in a Server IP address. It only allows an IVP4 address in order to connect to the network.

Second Textbox is the the User name, the User can only enter in less than 16 characters in order to connect.

3 Buttons, 'Conect', 'Help', 'About'. The Connect button would allow the User to connect ot a Network with a valid IP address, Help would display the User of how to play the game with controls, About displays the information build of the Program.

In order to play, Connect to a valid Server with a valid name. You can move wit "W,A,S,D" to gather candy to grow your body. Don't hit the walls or collide with other snakes or you will explode to death. Try to out-smart other snakes in order to acheive the highest score!

# Settings:
There is a defult settings file that you can download and use it with the Server Program.  
The setting file should be located in Server folder. Make sure you place in the right spot in order for the Program to use it.

# How to run:

The Project was implemented in the .NET 7.0 Framwork & uses .NET MAUI for GUI, then require a compatible .NET SDK
This Program can be run in the Visual Studio IDE, or can be build/run by the Command line.

#### Build 

First install .NET MAUI workload with the dotnet CLI 

```
dotnet workload install maui
```
Verify and install missing components with maui-check command line utility.
```
dotnet tool install -g redth.net.MAUI.check
maui-check
```

#### Run
Run the MAUI app either on Windows or MacOS (but first locate your directory for SnakeClient & build in Release mode)

For MacOS
```
cd SnakeClient
dotnet build -t:Run -f net7.0-maccatalyst
```

For Windows
```
cd SnakeClient
dotnet build -t:Run -f net7.0-windows
```

#### Running the server

You should be able to just run the server in Visual Studio by clicking run.

An alternative: run the .EXE of the program or run by .dll when building/running the program. 
(Make sure you place the **settings.xml** in the spot for Server to use)

For MacOS
```
cd Server
dotnet run -c Release 
/* or */
dotnet build -c Release
```
or

```
cd Server\bin\Release\net7.0
dotnet Server.dll
/* or */
./Server
```


For Windows
```
cd Server
dotnet run -c Release
```
or

```
cd Server\bin\Release\net7.0
dotnet Server.dll
/* or */
Server.exe
```
