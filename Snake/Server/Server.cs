using NetworkUtil;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace SnakeGame;
/// <summary>
/// This is the Server class that will handle all logic for SnakeGame
/// This class will keep track of the Networking with interacting
/// with Snake clients from this server
/// 
/// Author - Monthon Paul
/// Version - December 7, 2022 
/// </summary>
class Server {
	// Collection of SocketState map to connection ID
	// Server will create a World
	private Dictionary<long, SocketState> clients;
	private World theWorld;

	// settings from XML
	private int MSPerFrame { get; set; }
	private int FramesPerShot { get; set; } // does nothing
	private int RespawnRate { get; set; }

	public static void Main(string[] args) {
		// Creates new Server
		Server server = new();

		// Read the "settings.xml" file
		server.ReadSettingsXML("../../../settings.xml");

		//Start Server
		server.StartServer();

		// Start a new Stopwatch timer to control the frame rate
		System.Diagnostics.Stopwatch watch = new();
		watch.Start();
		while (true) {
			// wait until the next frame
			while (watch.ElapsedMilliseconds < server.MSPerFrame) { /* empty loop body */ }
			watch.Restart();
			server.UpdateWorld();
		}
	}

	/// <summary>
	/// Initialized the server's state
	/// </summary>
	public Server() {
		clients = new();
	}

	/// <summary>
	/// Start accepting Tcp sockets connections from clients
	/// </summary>
	public void StartServer() {
		// This begins an "event loop"
		Networking.StartServer(NewClientConnected, 11000);

		Console.WriteLine("Server is running. Accepting clients.");
	}

	/// <summary>
	/// Method to be invoked by the networking library
	/// when a new client connects
	/// </summary>
	/// <param name="state">The SocketState representing the new client</param>
	private void NewClientConnected(SocketState state) {
		if (state.ErrorOccurred) {
			Console.WriteLine(state.ErrorMessage);
			return;
		}

		// change the state's network action to the 
		// receive handler so we can process data when something
		// happens on the network
		state.OnNetworkAction = ReceivePlayerMessage;

		// Continue the event loop that receives messages from this client
		Networking.GetData(state);
	}

	/// <summary>
	/// Method to be invoked by the networking library
	/// when a network action occurs
	/// </summary>
	/// <param name="state">The SocketState representing the new client</param>
	private void ReceivePlayerMessage(SocketState state) {
		// Remove the client if they aren't still connected
		if (state.ErrorOccurred) {
			RemoveClient(state.ID);
			Console.WriteLine(state.ErrorMessage);
			return;
		}

		// change the state's network action to the 
		// receive handler so we can process data when something
		// happens on the network
		state.OnNetworkAction = ProcessActionMessage;

		// Client send in player name 
		string playerName = state.GetData().Trim('\n');

		Console.WriteLine("Player(" + state.ID + ") \"" + playerName + "\" has connected");

		// Save the client state
		// Generate a Snake for player with a Random location.
		// Need to lock here because clients can disconnect at any time
		Snake newSnake;
		lock (theWorld) {
			newSnake = theWorld.AddRandomSnake(playerName, (int) state.ID);
			lock (clients) {
				clients[state.ID] = state;
			}
		}

		//Send ID and worldsize to the client
		Networking.Send(state.TheSocket, newSnake.ID + "\n" + theWorld.Size + "\n");

		//Send all the walls to the client
		StringBuilder sb_WallInfo = new();
		foreach (Wall w in theWorld.Walls.Values) {
			sb_WallInfo.Append(JsonConvert.SerializeObject(w) + '\n');
		}
		//Send walls to the client
		Networking.Send(state.TheSocket, sb_WallInfo.ToString());

		//Empty the socket state of data, + 1 is the '\n'
		state.RemoveData(0, playerName.Length + 1);

		// Continue the event loop that receives messages from this client
		Networking.GetData(state);
	}

	/// <summary>
	/// Method to be invoked by the networking library
	/// when a network action occurs
	/// </summary>
	/// <param name="state">The SocketState representing the new client</param>
	private void ProcessActionMessage(SocketState state) {
		// Remove the client if they aren't still connected
		if (state.ErrorOccurred) {
			//Set Snake stats to disconnecting from Server
			Console.WriteLine(state.ErrorMessage);
			return;
		}
		//Get JSON information from the socket state
		string totalData = state.GetData();
		string[] moveUpdate = Regex.Split(totalData, @"(?<=[\n])");

		// Loop until the client message been processed.
		// Client may have received more than one.
		foreach (string move in moveUpdate) {
			// Ignore empty strings added by the regex splitter
			if (move is "") {
				continue;
			}
			// The regex splitter will include the last string even if it doesn't end with a '\n',
			// So ignore it if this happens. 
			if (move[move.Length - 1] != '\n') {
				break;
			}

			// Process message of movement
			lock (theWorld) {
				if (!theWorld.Snakes.ContainsKey((int) state.ID)) {
					continue;
				}
				theWorld.ProcessCommand(theWorld.Snakes[(int) state.ID], move);
				goto clear;
			}
			clear:
			state.RemoveData(0, move.Length);
		}
		// Continue the event loop that receives messages from this client
		Networking.GetData(state);
	}

	/// <summary>
	/// Update the World frame by frame in an infinite loop
	/// </summary>
	private void UpdateWorld() {
		//Build JSON and send to each client
		StringBuilder sb_WorldInfo = new();

		//Prepare JSON messages in order to sent to each client
		lock (theWorld) {
			theWorld.Update();
			//JSON Snakes
			foreach (Snake s in theWorld.Snakes.Values) {
				sb_WorldInfo.Append(JsonConvert.SerializeObject(s) + '\n');
			}
			//JSON Powerups
			foreach (Powerup p in theWorld.Powerups.Values) {
				sb_WorldInfo.Append(JsonConvert.SerializeObject(p) + '\n');
			}
			// Setup Snake & PowerUps according to Game rule
			theWorld.Setup();
		}

		//Send the JSON message out to every connected client
		lock (clients) {
			foreach (long ID in clients.Keys) {
				// Send only to connected  clients
				// Otherwise disconnected client should be remove.
				if (clients[ID].TheSocket.Connected) {
					Networking.Send(clients[ID].TheSocket, sb_WorldInfo.ToString());
				} else {
					//Console.WriteLine("Client (" + ID + ") disconnected");
					lock (theWorld) {
						if (theWorld.Snakes.ContainsKey((int) ID)) {
							theWorld.Snakes[(int) ID].Disconnect(theWorld.GetFrame());
							RemoveClient(ID);
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Reads the XML file with the tags correlating to the Snake Game.
	/// the Settings include frame rate, respawn rate, world size, Walls
	/// </summary>
	private void ReadSettingsXML(string text) {
		// First try to read from settings file
		try {
			// Create an XmlReader, then Dispose at end
			using (XmlReader reader = XmlReader.Create(text)) {
				while (reader.Read()) {
					//If the element is a start element, move foward
					if (reader.IsStartElement()) {
						switch (reader.Name) {
							case "GameSettings":
								continue;
							case "FramesPerShot":
								reader.Read();
								// support legacy settings (i.e does nothing) 🤔
								FramesPerShot = int.Parse(reader.Value);
								break;
							case "MSPerFrame":
								reader.Read();
								// Set the FPS
								MSPerFrame = int.Parse(reader.Value);
								break;
							case "RespawnRate":
								reader.Read();
								// Set the Respawn time
								RespawnRate = int.Parse(reader.Value);
								break;
							case "UniverseSize":
								reader.Read();
								//Create the world
								theWorld = new World(int.Parse(reader.Value), RespawnRate);
								break;
							case "Walls":
								ReadWalls(reader);
								break;
						}
					}
				}
				// if reach the end for the reader, close it
				reader.Close();
			}
		} catch (Exception) {
			Console.WriteLine("Error found in locating or Settings format Error");
		}
	}

	/// <summary>
	/// This Helper is for reading the amount of Walls in the settings.xml
	/// </summary>
	/// <param name="reader"> XML reader</param>
	private void ReadWalls(XmlReader reader) {
		// Get wall points
		// boolean clock for true match with p1, otherwise false for p2
		int x1 = 0, x2 = 0, y1 = 0, y2 = 0, Wall_ID = 0;
		bool clock = true;

		// Read the XML, since the rest is Wall
		while (reader.Read()) {
			//Read settings of Wall, it would have either a p1 or p2 with each respected x & y points
			if (reader.IsStartElement()) {
				switch (reader.Name) {
					case "ID":
						reader.Read();
						Wall_ID = int.Parse(reader.Value);
						break;
					case "p1":
						clock = true;
						break;
					case "p2":
						clock = false;
						break;
					case "x":
						reader.Read();
						// clock determine change from p1 & p2
						if (clock) {
							x1 = int.Parse(reader.Value);
						} else {
							x2 = int.Parse(reader.Value);
						}
						break;
					case "y":
						reader.Read();
						// clock determine change from p1 & p2
						if (clock) {
							y1 = int.Parse(reader.Value);
						} else {
							y2 = int.Parse(reader.Value);
						}
						break;
				}
			} else {
				// if it reach an end element of Wall, create a Wall with the given data
				if (reader.Name is "Wall") {
					//Build vector for P1 & P2
					Vector2D p1 = new(x1, y1);
					Vector2D p2 = new(x2, y2);

					//Add the walls to the world Dictionary
					lock (theWorld.Walls) {
						theWorld.Walls.Add(Wall_ID, new Wall(Wall_ID, p1, p2));
					}
				}
			}
		}
	}

	/// <summary>
	/// Removes a client from the clients dictionary
	/// </summary>
	/// <param name="id">The ID of the client</param>
	private void RemoveClient(long id) {
		Console.WriteLine("Client (" + id + ") has disconnected");
		lock (clients) {
			clients.Remove(id);
		}
	}
}