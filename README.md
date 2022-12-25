# Snake Game - Multiplayer Networking Game
Authors: Monthon Paul

Current Version: 1.2

Last Updated: 12/24/2022

# Purpose: 
To learn of how Game Design work by this Snake game, with building the functionality of Networking, Snake Client, & Snake Server.

# Quick summary:
This is a Snake game, inspired by Snake game of 1976 arcade game Blockade developed by a British company called Gremlin Interactive. This Program takes a Spin by makeing it a full Online Multiplayer Game. Users are allow to connect to a Snake Server, on which the User can enter in its name to play on a Network with other players.

First Textbox allow the User to enter in a Server IP address. It only allows an IVP4 address in order to connect to the network.

Second Textbox is the the User name, the User can only enter in less than 16 characters in order to connect.

3 Buttons, 'Conect', 'Help', 'About'. The Connect button would allow the User to connect ot a Network with a valid IP address, Help would display the User of how to play the game with controls, About displays the information build of the Program.

In order to play, Connect to a valid Server with a valid name. You can move wit "W,A,S,D" to gather candy to grow your body. Dodge walls & other snakes or you will die. Try to out smart other snakes in order to acheive the highest score!

# How to run:

The Project was implemented in the .NET 6.0 Framwork & uses .NET MAUI for GUI, then require a compatible .NET SDK
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
dotnet build -t:Run -f net6.0-maccatalyst
```

For Windows
```
cd SnakeClient
dotnet build -t:Run -f net6.0-windows
```

#### Running the server

You should be able to just run the server in Visual Studio by clicking run.

An alternative: run the .EXE of the program when building the program

For MacOS
```
cd Server
dotnet build -t:Run -f net6.0-maccatalyst
```

For Windows
```
cd Server
dotnet build -t:Run -f net6.0-windows
```


# Settings:
There is a defult settings file that you can download and add it to the program in release.  
The setting file should be located in Server.
