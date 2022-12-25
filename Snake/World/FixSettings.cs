namespace SnakeGame;
/// <summary>
/// This is a class contains the basic information of
/// Fix Setting Config of the Snake game. These values
/// aren't changable but readable for the basis of Model
/// 
/// Author - Monthon Paul
/// Version - December 6, 2022
/// </summary>
public static class FixSettings {
	public static readonly int SnakeWidth = 10;

	public static readonly int SnakeLength = 120;

	public static readonly int MaxPowerUpNumber = 20;

	public static readonly int MaxPowerupDelay = 200;

	public static readonly float SnakeSpeed = 3f;

	public static readonly float GrowthFrames = 12f;

	public static readonly float WallWidth = 50f;

	// a collection of Fix Coordinate Direction for which Snake is looking
	public static readonly Dictionary<string, Vector2D> CoordinateDirections = new Dictionary<string, Vector2D> {
		{ "up", new Vector2D (0.0, -1.0) },
		{ "down", new Vector2D (0.0, 1.0) },
		{ "left", new Vector2D (-1.0, 0.0) },
		{ "right", new Vector2D (1.0, 0.0) } };
}