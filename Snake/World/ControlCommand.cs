using Newtonsoft.Json;

namespace SnakeGame;
[JsonObject(MemberSerialization.OptIn)]
/// <summary>
/// This class will represent movement of the snake.
/// To use by the GameController in sending 
/// messages about movement.
/// 
/// Author - Monthon Paul
/// Version - December 6, 2022
/// </summary>
public class ControlCommand {

	// Representing the direction of the Player request
	[JsonProperty]
	public string moving { get; set; }

	/// <summary>
	/// Contsructor of moving the Snake
	/// </summary>
	/// <param name="move"> Direction of the User input </param>
	public ControlCommand(string move) {
		moving = move;
	}
}