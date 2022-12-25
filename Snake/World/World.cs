using Newtonsoft.Json;

namespace SnakeGame;
/// <summary>
/// This is a class that contains the World and all the
/// objects inside that zed World
/// 
/// Author - Monthon Paul
/// Version - December 8, 2022
/// </summary>
public class World {

	// Initialize Variables
	private int nextPowID = 0;

	private long FrameCounter = 0;

	private int RespawnRate;

	private int nextPow;

	private Random r = new();

	public Dictionary<int, Snake> Snakes;
	public Dictionary<int, Wall> Walls;
	public Dictionary<int, Powerup> Powerups;
	public int Size { get; private set; }


	/// <summary>
	/// Consstructor of World
	/// </summary>
	/// <param name="WorldSize"> the World Size </param>
	public World(int WorldSize) {
		Snakes = new();
		Walls = new();
		Powerups = new();
		Size = WorldSize;
	}

	/// <summary>
	/// Consstructor of World with Respawn time
	/// </summary>
	/// <param name="WorldSize"> the World Size </param>
	/// <param name="respawn"> Respawn time</param>
	public World(int WorldSize, int respawn) {
		Snakes = new();
		Walls = new();
		Powerups = new();
		Size = WorldSize;
		RespawnRate = respawn;
	}

	/// <summary>
	/// Get the frame of the World when any event happens
	/// </summary>
	/// <returns></returns>
	public long GetFrame() {
		return FrameCounter;
	}

	/// <summary>
	/// Add a Client Snake in the World
	/// </summary>
	/// <param name="name"> Name of the Client</param>
	/// <param name="ID"> Client ID</param>
	/// <returns> A New Snake </returns>
	public Snake AddRandomSnake(string name, int ID) {
		Vector2D dir = RandomDirection();
		List<Vector2D> bod = RandomBody(dir, FixSettings.SnakeLength, (FixSettings.SnakeWidth / 2));
		Snake snake = new Snake(ID, name, bod, dir);
		Snakes.Add(ID, snake);
		return snake;
	}

	/// <summary>
	/// Generate Random direction for the Snake
	/// </summary>
	/// <returns>Direction for where the Snake is looking</returns>
	private Vector2D RandomDirection() {
		//Generate a random Direction
		Random rDirec = new();
		Dictionary<string, Vector2D> dict = FixSettings.CoordinateDirections;
		int rand = rDirec.Next(dict.Count);
		return dict.ElementAt(rand).Value;
	}

	/// <summary>
	/// Generate Random body for Snake
	/// Either when joining Server or Respawning
	/// </summary>
	/// <returns>Snakes body in a data structure of Vectors</returns>
	private List<Vector2D> RandomBody(Vector2D dir, float tail_radius, float head_radius) {
		// Generate a randome location for body
		Random rLoc = new();
		double tail_X = 0.0, tail_Y = 0.0, head_X = 0.0, head_Y = 0.0;
		// Genrate infinite loop to check if generating will result in collision
		regen:
		head_X = (rLoc.NextDouble() * (Size - (-1 * Size)) + (-1 * Size)) / 2;
		head_Y = (rLoc.NextDouble() * (Size - (-1 * Size)) + (-1 * Size)) / 2;
		// place the snake body depending on which direction it's looking
		// Snakes body length is of 120 units long, which it's tail.
		switch (dir.GetX()) {
			case -1.0:
				tail_X = head_X + FixSettings.SnakeLength;
				tail_Y = head_Y;
				break;
			case 0.0:
				switch (dir.GetY()) {
					case -1.0:
						tail_Y = head_Y + FixSettings.SnakeLength;
						tail_X = head_X;
						break;
					case 1.0:
						tail_Y = head_Y - FixSettings.SnakeLength;
						tail_X = head_X;
						break;
				}
				break;
			case 1.0:
				tail_X = head_X - FixSettings.SnakeLength;
				tail_Y = head_Y;
				break;
		}
		// check each Wall for collsion at both points
		foreach (Wall w in Walls.Values) {
			if (w.Intersect(new Vector2D(tail_X, tail_Y), FixSettings.WallWidth + tail_radius)
				|| w.Intersect(new Vector2D(head_X, head_Y), FixSettings.WallWidth + head_radius)) {
				goto regen;
			}
		}
		// Head is the last position, while tail is Frist position
		return new List<Vector2D>() { new(tail_X, tail_Y), new(head_X, head_Y) };
	}

	/// <summary>
	/// Generate PoweUp to spawn in the World Randomly
	/// </summary>
	/// <returns>New PowerUp</returns>
	public Powerup AddRandomPowerup() {
		Powerup powerup = new(nextPowID, RandomLocation(5f));
		nextPowID++;
		Powerups.Add(powerup.ID, powerup);
		return powerup;
	}

	/// <summary>
	/// Generate a Random point in the World
	/// </summary>
	/// <returns> Return a random location in the World</returns>
	public Vector2D RandomLocation(float radius) {
		// Generate Random points
		Random r = new();
		Vector2D point = new(0, 0);
		regen:
		double x = (r.NextDouble() * (Size - (-1 * Size)) + (-1 * Size)) / 2;
		double y = (r.NextDouble() * (Size - (-1 * Size)) + (-1 * Size)) / 2;
		point = new Vector2D(x, y);
		foreach (Wall w in Walls.Values) {
			if (w.Intersect(point, FixSettings.WallWidth + radius)) {
				goto regen;
			}
		}
		return point;
	}

	/// <summary>
	/// Process movment from client message to be act upon the User Snake
	/// </summary>
	/// <param name="s"> User Snake</param>
	/// <param name="move"> Movement command</param>
	public void ProcessCommand(Snake s, string move) {
		if (s.alive) {
			ControlCommand? cc = JsonConvert.DeserializeObject<ControlCommand>(move);
			s.ChangeDirection(cc!.moving, this);
		}
	}

	/// <summary>
	/// Check if Snake collides with itself, other Snake, or Walls
	/// </summary>
	/// <param name="s"> Snake object </param>
	/// <returns>True if collide with any model, Otherwise false</returns>
	public bool SnakeCollide(Snake s) {
		// Check for Every Snake that is alive if it collides
		foreach (Snake other in Snakes.Values.Where((Snake x) => x.alive)) {
			if (s.CollidesWith(other)) {
				return true;
			}
		}
		// Check for every Wall that the Snake collides with Walls
		foreach (Wall w in Walls.Values) {
			if (s.CollidesWith(w)) {
				return true;
			}
		}
		// nothing collided
		return false;
	}

	/// <summary>
	/// General Setup for Snake & Powerup 
	/// </summary>
	public void Setup() {
		// for every snake, set that join is now false
		// snake is not dead, if dc, then remove from collection in World
		foreach (int sID in Snakes.Keys) {
			Snakes[sID].join = false;
			Snakes[sID].died = false;
			if (Snakes[sID].dc) {
				Snakes.Remove(sID);
			}
		}
		// for every Powerup, if the powerup been collected
		// remove from collection in World
		foreach (int pID in Powerups.Keys) {
			if (Powerups[pID].died) {
				Powerups.Remove(pID);
			}
		}
	}

	/// <summary>
	/// Update World according to spawning Powerups
	/// checking for collision of Snakes with Powerup, Walls, another Snake,
	/// or itself
	/// </summary>
	public void Update() {
		// Spawn in Powerup randomly by frame with a delay
		if (nextPow == 0) {
			nextPow = (int) FrameCounter + r.Next(FixSettings.MaxPowerupDelay);
		}
		if (nextPow <= FrameCounter) {
			// if Powerup doesn't reach it's max, keep spawning
			if (Powerups.Count < FixSettings.MaxPowerUpNumber) {
				AddRandomPowerup();
			}
			nextPow = (int) FrameCounter + r.Next(FixSettings.MaxPowerupDelay);
		}
		// loop through each Snake client to determine
		// collision on Walls, Snakes, Powerups, as well as status
		foreach (Snake s in Snakes.Values) {
			// if snake is dead, determine frame of dead with
			// Generating new location for Snake
			if (!s.alive) {
				if (FrameCounter - s.GetFrameofDeath() >= RespawnRate) {
					Vector2D dir = RandomDirection();
					s.Respawn(RandomBody(dir, FixSettings.SnakeLength, FixSettings.SnakeWidth / 2), dir);
				}
				continue;
			}
			// Update accordingly for Snake
			s.Update(Size);
			// Check collision on Snake with Powerup
			// If Powerup radius overlap both radius of head
			// it has been collected.
			foreach (Powerup p in Powerups.Values) { // 10 is both radius of Snake + Power
				if (!p.died && (p.Position - s.Head).Length() <= 10.0) {
					s.Scored();
					p.died = true;
				}
			}
			// Check if the Snake collides with itself, other Snakes, or Walls
			if (SnakeCollide(s)) {
				s.Die(FrameCounter);
			}
		}
		// add up frame
		FrameCounter++;
	}
}