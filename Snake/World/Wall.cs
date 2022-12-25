using Newtonsoft.Json;

namespace SnakeGame;
[JsonObject(MemberSerialization.OptIn)]
/// <summary>
/// This is a class contains the basic information for a wall object
/// Contains fields for the ID number of the wall, and 2 location points
/// 
/// Author - Monthon Paul
/// Version - December 8, 2022
/// </summary>
public class Wall {
	[JsonProperty(PropertyName = "wall")]
	public int ID { get; private set; }

	[JsonProperty(PropertyName = "p1")]
	public Vector2D P1 { get; private set; }

	[JsonProperty(PropertyName = "p2")]
	public Vector2D P2 { get; private set; }

	/// <summary>
	/// Constructor of Wall
	/// </summary>
	/// <param name="id"> Representing the wall's unique ID </param>
	/// <param name="start"> Representing one endpoint of the wall </param>
	/// <param name="end"> Representing the other endpoint of the wall </param>
	public Wall(int id, Vector2D start, Vector2D end) {
		ID = id;
		P1 = start;
		P2 = end;
	}

	/// <summary>
	/// Check for if the coordinate point on the World collide
	/// with the specific Wall area
	/// </summary>
	/// <param name="point"> Coordinate point</param>
	/// <param name="radius"> factor in radius of point</param>
	/// <returns> Return true if collide, Otherwise false</returns>
	public bool Intersect(Vector2D point, double radius) {
		//get the min & max of each point for spawn wall with factoring with radius
		double maxX = Math.Max(P1.GetX(), P2.GetX()) + radius;
		double minX = Math.Min(P1.GetX(), P2.GetX()) - radius;
		double maxY = Math.Max(P1.GetY(), P2.GetY()) + radius;
		double minY = Math.Min(P1.GetY(), P2.GetY()) - radius;

		//checks whether a point's X and Y values are within a given range relative to the
		//min & max of X and Y values of the wall, true if colliding, otherwise false
		if (point.GetX() >= minX && point.GetX() <= maxX && point.GetY() >= minY && point.GetY() <= maxY + radius) {
			return true;
		}
		return false;
	}
}
