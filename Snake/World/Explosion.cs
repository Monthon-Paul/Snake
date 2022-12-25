namespace SnakeGame;
/// <summary>
/// This class will represent Explosion of the snake.
/// To use by the GameController in sending 
/// messages about the Snake death.
/// 
/// Author - Monthon Paul
/// Version - December 6, 2022
/// </summary>
public class Explosion {
	// Represent each explosion frame and size of image
	public int frame { get; set; } = 0;
	public int expand { get; set; } = 0;
	// the Snake position upon death
	public double S_X { get; set; }
	public double S_Y { get; set; }

	// Constructor
	public Explosion(double S_X, double S_Y) {
		this.S_X = S_X;
		this.S_Y = S_Y;
	}
}