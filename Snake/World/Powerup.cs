using Newtonsoft.Json;

namespace SnakeGame;
[JsonObject(MemberSerialization.OptIn)]
/// <summary>
/// This is a class contains the basic information for a Powerup object
/// Contains fields for the ID number of the Powerup, it's position,
/// & whether it exit in the World
/// 
/// Author - Monthon Paul
/// Version - December 6, 2022
/// </summary>
public class Powerup {
	[JsonProperty(PropertyName = "power")]
	public int ID { get; private set; }

	[JsonProperty(PropertyName = "loc")]
	public Vector2D Position { get; private set; }

	[JsonProperty]
	public bool died { get; set; } = false;

	/// <summary>
	/// Constructor of a Powerup
	/// </summary>
	/// <param name="id"> Specific Powerup</param>
	/// <param name="loc"> location of the Powerup</param>
	public Powerup(int id, Vector2D loc) {
		ID = id;
		Position = loc;
	}
}
