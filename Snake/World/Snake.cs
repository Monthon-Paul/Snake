using Newtonsoft.Json;

namespace SnakeGame;
[JsonObject(MemberSerialization.OptIn)]
/// <summary>
/// This is a class contains the basic information of a Snake object
/// Contains fields of ID, Name, body postion, Direction,
/// if it's connected or dc, & whether it's alive or dead.
/// 
/// Author - Monthon Paul
/// Version - December 8, 2022
/// </summary>
public class Snake {
	[JsonProperty(PropertyName = "snake")]
	public int ID { get; private set; }

	[JsonProperty]
	public string name { get; set; }

	[JsonProperty(PropertyName = "body")]
	public List<Vector2D> Position { get; set; }

	[JsonProperty(PropertyName = "dir")]
	public Vector2D Direction { get; set; }

	[JsonProperty]
	public int score = 0;

	[JsonProperty]
	public bool died { get; set; } = false;

	[JsonProperty]
	public bool alive { get; set; } = true;

	[JsonProperty]
	public bool dc { get; set; } = false;

	[JsonProperty]
	public bool join { get; set; } = true;

	private int NumofSeg => Position.Count - 1;

	private long FrameofDeath;

	private float speed { get; set; } = FixSettings.SnakeSpeed;

	private float growing { get; set; }

	// Get/Set the Head of the Snake
	public Vector2D Head {
		get {
			return Position.Last();
		}
		private set {
			Position.RemoveAt(NumofSeg);
			Position.Add(value);
		}
	}

	// Get/Set the Tail of the Snake
	public Vector2D Tail {
		get {
			return Position.First();
		}
		private set {
			Position.RemoveAt(0);
			Position.Insert(0, value);
		}
	}

	/// <summary>
	/// Constructor of Snake (i.e. Player/User)
	/// </summary>
	/// <param name="id"> Client/Server of what snake</param>
	/// <param name="Name"> Player name of Snake</param>
	/// <param name="pos"> Positions representing the entire body of the snake</param>
	/// <param name="dir"> Representing the snake's orientation </param>
	public Snake(int id, string Name, List<Vector2D> pos, Vector2D dir) {
		ID = id;
		name = Name;
		Position = pos;
		Direction = dir;
	}

	/// <summary>
	/// Get the Rectangle Segment of Snakes for each iterable
	/// </summary>
	/// <returns> an IEnumerable of two points for Snake body</returns>
	public IEnumerable<(Vector2D v1, Vector2D v2)> Segments() {
		Vector2D current = Tail;
		for (int i = 1; i <= NumofSeg; i++) {
			yield return (current, Position[i]);
			current = Position[i];
		}
	}

	/// <summary>
	/// When Snake collects a Powerup, increase in length & score
	/// </summary>
	public void Scored() {
		score++;
		growing += FixSettings.GrowthFrames;
	}

	/// <summary>
	/// Snake disconnect,set it to die at a specific frame & dc.
	/// </summary>
	/// <param name="frame"> Frame of the World</param>
	public void Disconnect(long frame) {
		if (alive) {
			Die(frame);
		}
		dc = true;
	}

	/// <summary>
	/// If Snake died, set to the approiate properties with frame of Death
	/// </summary>
	/// <param name="frame"> Frame of upon death</param>
	public void Die(long frame) {
		died = true;
		FrameofDeath = frame;
		alive = false;
	}

	/// <summary>
	/// When Snake respawn, set to the acording values 
	/// </summary>
	/// <param name="body"> Randomly generate body for Snake</param>
	/// <param name="dir"> Randomly Generate direction for snake</param>
	public void Respawn(List<Vector2D> body, Vector2D dir) {
		Position = body;
		Direction = dir;
		score = 0;
		alive = true;
	}

	/// <summary>
	/// Get time of frame death for Snake
	/// </summary>
	/// <returns> Frame of Snake death</returns>
	public long GetFrameofDeath() {
		return FrameofDeath;
	}

	/// <summary>
	/// Given a client send direction for Snake, make the appropiate change in look
	/// & direction.
	/// </summary>
	/// <param name="look"> Client sends a moving command string</param>
	/// <param name="w">the World to check for able to turn</param>
	public void ChangeDirection(string look, World w) {
		// Setup old direction in case for the same direction
		// or opposite direction (i.e example of can't turn from left to right)
		Vector2D prevDirec = new Vector2D(Direction);
		// Set to appropiate new Direction
		switch (look) {
			case "up":
				Direction = FixSettings.CoordinateDirections[look];
				break;
			case "down":
				Direction = FixSettings.CoordinateDirections[look];
				break;
			case "left":
				Direction = FixSettings.CoordinateDirections[look];
				break;
			case "right":
				Direction = FixSettings.CoordinateDirections[look];
				break;
			case "none":
				return;
		}
		// You can't turn in the opposite direction
		// So set back to old
		if (Direction.IsOppositeCardinalDirection(prevDirec)) {
			Direction = prevDirec;
			// Check that if the Snake can turn, if not set it back to old direction
		} else if (!CanTurn(Direction, w)) {
			Direction = prevDirec;
		}
	}

	/// <summary>
	/// Allow the Snake to see if it can turn in the World
	/// </summary>
	/// <param name="dir"> the Snake direction</param>
	/// <param name="w"> the World that is calculated</param>
	/// <returns>True if it's able to turn, Otherwise False</returns>
	public bool CanTurn(Vector2D dir, World w) {
		// Set the old head as need to do calculation on new head.
		Vector2D oldHead = new Vector2D(Head);
		// check that if able to move it's head
		bool move = MoveHead(dir, w.Size);
		if (CollidesWith(this, TurnAtempt: true)) {
			return false;
		}
		// Reset Move head for new turn
		ResetMoveHead(oldHead, move);
		return true;
	}

	/// <summary>
	/// Move the head accordinly to the direction, the old head will
	/// act as a catch up for the tail, with contiune to go along each frame.
	/// </summary>
	/// <param name="dir"> Direction of Snake for Head</param>
	/// <param name="worldSize"> the World size</param>
	/// <returns> true if the Head can move</returns>
	public bool MoveHead(Vector2D dir, int worldSize) {
		// get the difference in lenght for head to it's "neck" (second to last)
		Vector2D headtosecond = Head - Position[NumofSeg - 1];
		// Normalize vectors
		Vector2D norm = new Vector2D(headtosecond);
		norm.Normalize();
		bool result = false;
		// Check to see if the direc match to new direc. if it doesn't then change look.
		if (!norm.Equals(dir)) {
			Position.Add(new Vector2D(Head));
			result = true;
		}
		// add on speed with normalize vector
		Head += dir * speed;
		return result;
	}

	/// <summary>
	/// Reset Head move for multiply movements from Snake
	/// (for A.I client)
	/// </summary>
	/// <param name="oldHead"> old positon of Head</param>
	/// <param name="move"> bool to check if continue move</param>
	public void ResetMoveHead(Vector2D oldHead, bool move) {
		if (move) {
			Position.RemoveAt(NumofSeg);
		}
		Head = new Vector2D(oldHead);
	}

	/// <summary>
	/// Wrap the Snake so that it appears in other side
	/// of the World
	/// </summary>
	/// <param name="worldSize"> Use the World size for Wrap</param>
	public void WrapingAround(int worldSize) {
		// oldHead indicates hitting the edge
		Vector2D oldHead = new Vector2D(Head);
		// begin Wrap
		Wrap(oldHead, worldSize);
		Wrap(Tail, worldSize);
		// check that the Vectors are opposite in sign values
		// then a wrap-around occur, process in adding per point of wrap
		if (!oldHead.Equals(Head)) {
			Position.Add(oldHead);
			Position.Add(new Vector2D(oldHead));
		}
		// if the Tail reaches Second to tail,
		// remove the Tail
		if (Tail.Equals(Position[1])) {
			Position.RemoveAt(0);
		}
	}

	/// <summary>
	/// Assign the points across the World to it's opposite values.
	/// </summary>
	/// <param name="p"> Coordinate point</param>
	/// <param name="worldSize"> Use the World Size for edge</param>
	private void Wrap(Vector2D p, int worldSize) {
		// get the Worldsize by length & width (it's the same value)
		double edge = worldSize / 2;
		// Assign each point of X & Y to it's opposite value for each coordinate
		if (p.GetX() > edge) {
			p.X = -edge;
		} else if (p.GetX() < -edge) {
			p.X = edge;
		} else if (p.GetY() > edge) {
			p.Y = -edge;
		} else if (p.GetY() < -edge) {
			p.Y = edge;
		}
	}

	/// <summary>
	///  Update Snake to process if the Snake Head is moving
	///  growing the Snake, Tail catch up to second to Tail
	///  and does wrap-around
	/// </summary>
	/// <param name="worldSize"> Use the World Size for vectors</param>
	public void Update(int worldSize) {
		// continue to move Snake
		MoveHead(Direction, worldSize);
		// if Snake is not growing
		if (growing == 0) {
			// calculate the Difference of Tail to second to Tail
			Vector2D secondtoTail = Position[1] - Tail;
			secondtoTail.Normalize();
			// Move the tail to correct snake speed
			Tail += secondtoTail * speed;
		}
		// if a turn happen. the Tail will reach its second position in which
		// it get rid of tail to set up second posiotn to new tail.
		if (Tail.Equals(Position[1])) {
			Position.RemoveAt(0);
		}
		// Go to Wrap around to see if Snake reach the edge
		WrapingAround(worldSize);
		// Grow the Snake
		growing = Math.Max(0, growing - 1);
	}

	/// <summary>
	///  Collison Helper: Rectangle segments to represent collsion for both
	///  Snake & Wall collision.
	/// </summary>
	/// <param name="P1"> point one for rectangle segment</param>
	/// <param name="P2"> point two for rectangle segment</param>
	/// <param name="radius"> factor in a radus of either Wall or Snake</param>
	/// <returns></returns>
	private bool IntersectRectangle(Vector2D head, Vector2D P1, Vector2D P2, double radius) {
		// determine min & max of the Rectangle segment with the factor with radius
		double maxX = Math.Max(P1.GetX(), P2.GetX()) + radius;
		double minX = Math.Min(P1.GetX(), P2.GetX()) - radius;
		double maxY = Math.Max(P1.GetY(), P2.GetY()) + radius;
		double minY = Math.Min(P1.GetY(), P2.GetY()) - radius;

		// if Snake head collides with any part of the rectangle, it's true, otherwise false
		if (head.GetX() > minX && head.GetX() < maxX && head.GetY() > minY && head.GetY() < maxY) {
			return true;
		}
		return false;
	}

	/// <summary>
	/// Check if the Snake will collide with Walls,
	/// Being that Walls are rectangles, that the the Snake Head need to check
	/// for any collisions.
	/// </summary>
	/// <param name="w"> All the Walls in the World</param>
	/// <returns> True if collided, otherwise false</returns>
	public bool CollidesWith(Wall w) {
		// radius is wall (25) + snake (5)
		// use the Wall points for collison
		return IntersectRectangle(Head, w.P1, w.P2, 30.0);
	}

	/// <summary>
	/// Collison for other Snakes with itself, go through each segment body to see if colides.
	/// </summary>
	/// <param name="other"> Either can be other Snake or it'self</param>
	/// <param name="TurnAtempt"> Snake will turn to collide</param>
	/// <returns>True if collide with other Snakes or itself, false otherwise</returns>
	public bool CollidesWith(Snake other, bool TurnAtempt = false) {
		// Collection of Segments for either Snake or other Snakes
		IEnumerable<(Vector2D, Vector2D)> collection;
		// Check if the Snake collides if itself
		int noncollide;
		if (ID == other.ID) {
			noncollide = 2;
			int bounds = NumofSeg - 3;
			// since dealing with a List for index out of bounds
			if (bounds < 0) {
				goto Segments;
			}
			// get the point where collision could happen for itself
			Vector2D doesitcollide = Position[bounds];
			while (bounds > 0) {
				// Get the difference of vector length for it's two neck value points
				// it is good to get the direction for those difference
				Vector2D CollidetoThird = doesitcollide - Position[bounds + 1];
				Vector2D SecondtoFirst = Position[bounds + 2] - Position[bounds + 3];
				CollidetoThird.Normalize();
				SecondtoFirst.Normalize();
				// re-assign direction values to an angle
				float CtoT = CollidetoThird.ToAngle();
				float StoF = SecondtoFirst.ToAngle();
				// Check that if the Snake just turn in 180 degrees
				if (Math.Abs(CtoT - StoF) == 180f) {
					break;
				}
				// if no more points in the Snake body, break loop,
				bounds--;
				doesitcollide = Position[bounds];
				noncollide++;
			}
			// ignore the Head with its 2 'neck' bodies Segment for collision
			Segments:
			collection = other.Segments().SkipLast(noncollide);
		} else {
			// get each Segment for Snake body
			collection = other.Segments();
		}
		// loop through if collides with Snake body, true if it does
		noncollide = 0;
		foreach (var rectangle in collection) {
			// Check if any collisions happen with Snakes, with radius of both snake adding up.
			if ((!TurnAtempt || noncollide++ >= NumofSeg - 3) && IntersectRectangle(Head, rectangle.Item1, rectangle.Item2, 10.0)) {
				return true;
			}
		}
		return false;
	}
}
