using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetworkUtil;
using System.Text.RegularExpressions;

namespace SnakeGame;
/// <summary>
/// This is the Game controller class. It communicates with the 
/// the view, & vice versa communicating back by events.
/// This controller resgister connection to the server,
/// Movement of the snake, & receives data from the
/// server to update the world/view.
/// 
/// Author - Monthon Paul
/// Version - December 7, 2022
/// </summary>
public class GameController {
	// Creates a World for Snakes, Walls, & Powerups
	private World theWorld;

	// State representing the connection with the server
	private SocketState? theServer = null;

	// A delegate and event to fire when the controller
	// has received and processed new info from the server
	public delegate void GameUpdateHandler();
	public event GameUpdateHandler UpdateArrived;

	//Event that will display an error to the user
	public delegate void ErrorHandler(string err);
	public event ErrorHandler Error;

	//Allows the view to display players ID
	public delegate void PlayerIDGive(int info);
	public event PlayerIDGive PlayerIDgive;

	//Allows the view to display Snake Explosion
	public delegate void PlayerDied(Snake s);
	public event PlayerDied SnakeDied;

	//Snake object being the player/User
	private Snake PlayerSnake;
	private string playerName;
	private int playerID;

	//bools to register W,A,S,D key strokes
	private bool upDir, leftDir, downDir, rightDir = false;

	/// <summary>
	/// Get the World easily
	/// </summary>
	/// <returns> the World for View</returns>
	public World GetWorld() {
		return theWorld;
	}

	/// <summary>
	/// Begins the process of connecting to the server,
	/// once the "conect" button is clicked
	/// </summary>
	/// <param name="addr"> IP Address as a string </param>
	/// <param name="name"> the User name of snake </param>
	public void Connect(string addr, string name) {
		playerName = name;
		Networking.ConnectToServer(OnConnect, addr, 11000);
	}

	/// <summary>
	/// Method to be invoked by the networking library when a connection is made
	/// </summary>
	/// <param name="state"> User Socket </param>
	private void OnConnect(SocketState state) {
		if (state.ErrorOccurred) {
			// inform the view
			Error(state.ErrorMessage!);
			return;
		}

		theServer = state;

		// Start an event loop to receive messages from the server
		state.OnNetworkAction = ReceiveStartup;

		//Send player name to the socket
		Networking.Send(state.TheSocket, playerName + '\n');

		// Start an event loop to receive messages from the server
		Networking.GetData(state);
	}

	/// <summary>
	/// This method receives the beginning data for the world 
	/// and sets up the World
	/// </summary>
	/// <param name="state"> User Socket </param>
	private void ReceiveStartup(SocketState state) {
		//Check for errors
		if (state.ErrorOccurred) {
			Error(state.ErrorMessage!);
			return;
		}

		//Change network action to receive Full message of data
		state.OnNetworkAction = ReceiveFullMessage;

		//Extract the ID & world data for setup
		var StartMess = state.GetData();
		string[] elements = StartMess.Split('\n');
		playerID = int.Parse(elements[0]);
		var worldSize = int.Parse(elements[1]);

		//Setup the world
		theWorld = new(worldSize);

		//Clear data of the ID/World,the +1 is the '\n'.
		state.RemoveData(0, elements[0].Length + 1);
		state.RemoveData(0, elements[1].Length + 1);

		// Continue the event loop
		// state.OnNetworkAction has not been changed, 
		// so this same method (ReceiveStartup) 
		// will be invoked when more data arrives
		Networking.GetData(state);
	}

	/// <summary>
	/// Method to be invoked by the networking library when 
	/// data is available
	/// </summary>
	/// <param name="state"> User Socket </param>
	private void ReceiveFullMessage(SocketState state) {
		//Check for error
		if (state.ErrorOccurred) {
			Error(state.ErrorMessage!);
			return;
		}

		//Process the Message
		ProcessMessages(state);

		// Continue the event loop
		// state.OnNetworkAction has not been changed, 
		// so this same method (ReceiveFullMessage) 
		// will be invoked when more data arrives
		Networking.GetData(state);
	}

	/// <summary>
	/// Process any Json buffered messages separated by '\n'
	/// Then inform the view with each specific Json object
	/// </summary>
	/// <param name="state"> User Socket</param>
	private void ProcessMessages(SocketState state) {
		// Get string JSON data from socket state 
		var Json = state.GetData();
		bool snakeUpdate = false;
		// Special Case: if the server is sending too many messages,
		// the data for one message is not guaranteed to arrive all at the same time, therefore
		// don't process message if '/n' is not included.
		string[] parsMessage = Regex.Split(Json, @"(?<=[\n])");
		JObject parseObj;
		JToken parseToken;
		// Loop through to processed all Json messages to update model.
		// Once Json type is found, pass the message along to deserailize to add
		// Json data to the World.
		foreach (string p in parsMessage) {
			// Ignore empty strings from split
			if (p is "") {
				continue;
			}
			// The regex splitter will include the last string even if it doesn't end with a '\n',
			// So we need to ignore it if this happens. 
			if (p[p.Length - 1] != '\n') {
				break;
			}
			//Parse the Json object and compare to other objects
			parseObj = JObject.Parse(p);

			// Check if Json object is Snake
			parseToken = parseObj["snake"]!;
			if (parseToken is not null) {
				UpdateWorld(p, "snake");
				snakeUpdate = true;
				goto Clear;
			}
			// Check if Json object is Wall
			parseToken = parseObj["wall"]!;
			if (parseToken is not null) {
				UpdateWorld(p, "wall");
				goto Clear;
			}
			// Check if Json object is Powerup
			parseToken = parseObj["power"]!;
			if (parseToken is not null) {
				UpdateWorld(p, "power");
				goto Clear;
			}
			//Then remove it from the SocketState's growable buffer
			Clear:
			state.RemoveData(0, p.Length);
		}

		// Send the player ID to view to display
		if (snakeUpdate) {
			PlayerIDgive(playerID);
		}

		// inform the view
		UpdateArrived?.Invoke();
	}

	/// <summary>
	/// The method takes the Json string from ProcessMessages and
	/// updates the model/view of the world.
	/// </summary>
	/// <param name="JsonString">Json string from ProcessMessages</param>
	/// <param name="JsonKey"> Json key for which JSON message to Deserailize </param>
	private void UpdateWorld(string JsonString, string JsonKey) {
		// Enter in a Switch Case, from the Server sending JSON messages
		// in order to update the model in the world
		lock (theWorld) {
			switch (JsonKey) {
				case "snake":
					// Convert Json message into Snake 
					Snake? curSnake = JsonConvert.DeserializeObject<Snake>(JsonString);
					// Check if the Snake is the player
					if (curSnake!.ID == playerID) {
						PlayerSnake = curSnake;
					}
					// Check if the Snake is dead for 1 frame.
					if (curSnake.died) {
						SnakeDied(curSnake);
					}
					// Check if the world contains the Snake already
					if (theWorld.Snakes.ContainsKey(curSnake.ID)) {
						// Remove Snake to update the World
						theWorld.Snakes.Remove(curSnake.ID);
					}
					// Add the Snake to the world
					theWorld.Snakes.Add(curSnake.ID, curSnake);
					break;
				case "wall":
					// Convert Json message into Wall
					Wall? curWall = JsonConvert.DeserializeObject<Wall>(JsonString);
					// Add the wall into the world 
					theWorld.Walls.Add(curWall!.ID, curWall);
					break;
				case "power":
					// Convert Json message into PowerUp
					Powerup? curPowerUp = JsonConvert.DeserializeObject<Powerup>(JsonString);
					// Check if the world contains Powerup already
					if (theWorld.Powerups.ContainsKey(curPowerUp!.ID)) {
						// Remove the PowerUp so that it can be updated
						theWorld.Powerups.Remove(curPowerUp.ID);
					}
					// Check if the powerup was collected
					if (!curPowerUp.died) {
						// Re-add it back
						theWorld.Powerups.Add(curPowerUp.ID, curPowerUp);
					}
					break;
			}
		}
	}

	/// <summary>
	/// This is called when Key of either W,A,S,D is press. It register
	/// each direction boolean to true in order to update the Snake
	/// </summary>
	/// <param name="keyPressed"> W,A,S,D key represent direction </param>
	public void Movement(string keyPressed) {
		switch (keyPressed) {
			case "left":
				leftDir = true;
				break;
			case "right":
				rightDir = true;
				break;
			case "up":
				upDir = true;
				break;
			case "down":
				downDir = true;
				break;
		}
		// Update the Snake for directions
		SendSnakeUpdate(PlayerSnake);
	}

	/// <summary>
	/// Send snake updates back to view
	/// </summary>
	/// <param name="s"> Snake User </param>
	private void SendSnakeUpdate(Snake s) {
		string dir = "none";

		//Check movement, then reset directions
		if (leftDir) {
			dir = "left";
			leftDir = false;
		} else if (downDir) {
			dir = "down";
			downDir = false;
		} else if (upDir) {
			dir = "up";
			upDir = false;
		} else if (rightDir) {
			dir = "right";
			rightDir = false;
		} else {
			dir = "none";
		}

		if (PlayerSnake is not null) {
			//Create the control command with direction,
			ControlCommand control = new(dir);

			//Send to server
			Networking.Send(theServer!.TheSocket, JsonConvert.SerializeObject(control) + '\n');
		}
	}
}