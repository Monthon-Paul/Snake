using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;


namespace SnakeGame;
/// <summary>
/// This is the View of the World Panel Drawing class.
/// This communicates back-n-forth with the View
/// in order to update the Panel by drawing any given
/// data from the Controller. This handles drawing
/// of Snake, Walls, Powerups, and Death Animation.
/// 
/// Author - Monthon Paul
/// Version - December 7, 2022
/// </summary>
public class WorldPanel : IDrawable {
	private IImage wall, background, power;
	private IImage[] expl = new IImage[12];

	private bool initializedForDrawing = false;

	/// <summary>
	/// Loads an image using either Mac or Windows image loading API
	/// </summary>
	/// <param name="name"> Image name </param>
	/// <returns>loaded image</returns>
#if MACCATALYST
	private IImage loadImage(string name) {
		Assembly assembly = GetType().GetTypeInfo().Assembly;
		string path = "SnakeGame.Resources.Images";
		return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
	}
#else
  private IImage loadImage( string name )
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream( assembly.GetManifestResourceStream( $"{path}.{name}" ) );
    }
#endif

	// A delegate for DrawObjectWithTransform
	// Methods matching this delegate can draw whatever they want onto the canvas  
	public delegate void ObjectDrawer(object o, ICanvas canvas);

	//set up Drawing
	private GraphicsView graphicsView = new();

	//Set up background image
	private int viewSize = 900;

	// Initalize Variables
	private World theWorld;
	private int SnakeID;
	private int frame, expand = 0;
	private double DeadX, DeadY;
	private Dictionary<int, Explosion> exp;

	//Constructor
	public WorldPanel() {
		graphicsView.Drawable = this;
		exp = new();
	}

	/// <summary>
	/// Sets the Panel grab the World
	/// </summary>
	/// <param name="w"> The World</param>
	public void SetWorld(World w) {
		theWorld = w;
	}

	/// <summary>
	/// Sets the Panel to have Player ID number
	/// </summary>
	/// <param name="IDnum"> Snake ID number</param>
	public void SetPlayerId(int IDnum) {
		SnakeID = IDnum;
	}

	/// <summary>
	/// When snake is died, get the positon & assign each died snake to an Explosion
	/// </summary>
	/// <param name="deadsnake"> Snake dead </param>
	public void SnakeExplode(Snake deadsnake) {
		DeadX = deadsnake.Position.Last().GetX();
		DeadY = deadsnake.Position.Last().GetY();
		// if the snake died again, remove to update to new location.
		if (exp.ContainsKey(deadsnake.ID)) {
			exp.Remove(deadsnake.ID);
		}
		exp.Add(deadsnake.ID, new(DeadX, DeadY));
	}

	/// <summary>
	/// Load Images for the Panel from View to display
	/// </summary>
	private void InitializeDrawing() {
		wall = loadImage("WallSprite.png");
		background = loadImage("Background.png");
		power = loadImage("candy.png");
		for (int i = 0; i <= expl.Length - 1; i++) {
			expl[i] = loadImage("exp" + i + ".png");
		}
		initializedForDrawing = true;
	}

	/// <summary>
	/// This method performs a translation and rotation to draw an object.
	/// </summary>
	/// <param name="canvas">The canvas object for drawing onto</param>
	/// <param name="o">The object to draw</param>
	/// <param name="worldX">The X component of the object's position in world space</param>
	/// <param name="worldY">The Y component of the object's position in world space</param>
	/// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
	/// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
	private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer) {
		// "push" the current transform
		canvas.SaveState();

		canvas.Translate((float) worldX, (float) worldY);
		canvas.Rotate((float) angle);
		drawer(o, canvas);

		// "pop" the transform
		canvas.RestoreState();
	}

	/// <summary>
	/// This runs whenever the drawing panel is invalidated and draws the game
	/// </summary>
	/// <param name="canvas">The canvas object for drawing onto</param>
	/// <param name="dirtyRect">Apple draw canvas ...</param>
	public void Draw(ICanvas canvas, RectF dirtyRect) {
		// We have to wait until Draw is called at least once 
		// before loading the images
		if (!initializedForDrawing) {
			InitializeDrawing();
		}

		// If the World doesn't exit, don't draw anything
		if (theWorld is null) {
			return;
		}

		// A Global lock for the World for race-conditions
		lock (theWorld) {
			// undo any leftover transformations from last frame
			canvas.ResetState();

			// center the view on the middle of the world
			if (theWorld.Snakes.ContainsKey(SnakeID)) {
				float playerX = (float) theWorld.Snakes[SnakeID].Position.Last().GetX(); //(the player's world-space X coordinate)
				float playerY = (float) theWorld.Snakes[SnakeID].Position.Last().GetY(); //(the player's world-space Y coordinate)
				canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));
			}

			//Draw the world
			canvas.DrawImage(background, (-theWorld.Size / 2), (-theWorld.Size / 2), theWorld.Size, theWorld.Size);

			//draw the Powerups in the world
			foreach (var p in theWorld.Powerups.Values) {
				DrawObjectWithTransform(canvas, p,
				  p.Position.GetX(), p.Position.GetY(), 0, PowerupDrawer);
			}

			//draw the Walls in the world
			foreach (var w in theWorld.Walls.Values) {
				DrawObjectWithTransform(canvas, w, 0, 0, 0, WallDrawer);
			}
			// draw the Snakes in the world
			foreach (var s in theWorld.Snakes.Values) {
				// If the the Snake is connected, begin to draw
				if (!s.dc) {
					if (s.alive) {
						DrawObjectWithTransform(canvas, s, 0, 0, 0, SnakeDrawer);
						DrawObjectWithTransform(canvas, s, 0, 0, 0, NameDrawer);
					} else {
						// Draw for each snake specifc location upon death explosion
						if (exp.ContainsKey(s.ID)) {
							DrawObjectWithTransform(canvas, s, exp[s.ID].S_X, exp[s.ID].S_Y, 0, DeathDrawer);
						} else {
							// Special Case: when entering the server,
							// if one snakes died just draw the death at that location
							DrawObjectWithTransform(canvas, s, DeadX, DeadY, 0, DeathDrawer);
						}
					}
				} else {
					// free up explode space
					if (exp.ContainsKey(s.ID)) {
						exp.Remove(s.ID);
					}
				}
			}
		}
	}

	/// <summary>
	/// A method that can be used as an ObjectDrawer delegate
	/// To draw a Snake
	/// </summary>
	/// <param name="o">The Snake to draw</param>
	/// <param name="canvas">The canvas object for drawing onto</param>
	private void SnakeDrawer(object o, ICanvas canvas) {
		Snake s = o as Snake;
		// determine color base of ID
		switch (s.ID % 8) {
			case 0:
				canvas.StrokeColor = Color.FromRgb(255, 0, 0); // Red
				break;
			case 1:
				canvas.StrokeColor = Color.FromRgb(0, 255, 0); // Green
				break;
			case 2:
				canvas.StrokeColor = Color.FromRgb(0, 0, 255); // Blue
				break;
			case 3:
				canvas.StrokeColor = Color.FromRgb(0, 0, 0); // Black
				break;
			case 4:
				canvas.StrokeColor = Color.FromRgb(255, 0, 255); // Magenta
				break;
			case 5:
				canvas.StrokeColor = Color.FromRgb(0, 255, 255); // Cyan
				break;
			case 6:
				canvas.StrokeColor = Color.FromRgb(255, 255, 0); // Yellow
				break;
			case 7:
				canvas.StrokeColor = Color.FromRgb(255, 165, 0); // Orange
				break;
		}
		canvas.StrokeSize = FixSettings.SnakeWidth;
		canvas.StrokeLineCap = LineCap.Round;
		// get the World size by WidthxHeight
		int size = theWorld.Size / 2;
		// Draw the snake base on segment, Head is the last index, while Tail is first index
		// Loop through snake segments, calculate segment length
		for (int i = s.Position.Count - 1; i > 0; i--) {
			// First index is the head so no wrap-around the head.
			if (i == s.Position.Count - 1) {
				goto Carryon;
			}
			// Wrap-around drawing Case:
			// check if any point has coordinates more or less than world size
			if (s.Position[i].GetX() >= size || s.Position[i].GetY() >= size ||
				s.Position[i].GetX() <= -size || s.Position[i].GetY() <= -size) {
				//draw first segment 
				canvas.DrawLine((float) s.Position[i + 1].GetX(), (float) s.Position[i + 1].GetY(),
			(float) s.Position[i].GetX(), (float) s.Position[i].GetY());
				//draw second segment
				canvas.DrawLine((float) s.Position[i - 1].GetX(), (float) s.Position[i - 1].GetY(),
			(float) s.Position[i - 2].GetX(), (float) s.Position[i - 2].GetY());
				// Ignore the extra element with coordinates more or less than world size
				i--;
				continue;
			}
			// draw each segment of snake
			Carryon:
			canvas.DrawLine((float) s.Position[i].GetX(), (float) s.Position[i].GetY(),
			(float) s.Position[i - 1].GetX(), (float) s.Position[i - 1].GetY());
		}
	}

	/// <summary>
	/// A method that can be used as an ObjectDrawer delegate
	/// To draw the explosion on death
	/// </summary>
	/// <param name="o">The Snake to draw</param>
	/// <param name="canvas">Panel display</param>
	private void DeathDrawer(object o, ICanvas canvas) {
		Snake s = o as Snake;
		int size = 32; // set size for explode
		IImage[] explosion = expl; // an Array of explode images

		// Each snake has it own fram for explosion
		// for each snake set it's own seperate fram.
		if (exp.ContainsKey(s.ID)) {
			frame = exp[s.ID].frame;
			expand = exp[s.ID].expand;
		}

		// Draw per frame for explosion when DeathDrawer is Invoke
		// increase the size of explosion as well.
		// Reason to not use Explosion Frame is due to Time Complexity
		if (frame <= explosion.Length - 1) {
			canvas.DrawImage(explosion[frame], -((size + expand) / 2), -((size + expand) / 2),
				size + expand, size + expand);
			frame++;
			expand += 5;

			// Once draw is done, update for each Snake specifc Explosion
			if (exp.ContainsKey(s.ID)) {
				exp[s.ID].frame = frame;
				exp[s.ID].expand = expand;
			}
		}
	}

	/// <summary>
	/// A method that can be used as an ObjectDrawer delegate
	/// To draw the Name with score for Snake
	/// </summary>
	/// <param name="o">The Snake to draw</param>
	/// <param name="canvas">Panel display </param>
	private void NameDrawer(object o, ICanvas canvas) {
		Snake s = o as Snake;
		// Center the player name in the world by String size
		string display = s.name + ": " + s.score.ToString();
		SizeF center = canvas.GetStringSize(display, new Font("Arial"), 12);
		// Sets up string format
		canvas.FontColor = Colors.White;
		canvas.FontSize = 12;
		canvas.Font = new Font("Arial");
		//Draw the player name with score for Snake
		canvas.DrawString(display, (float) s.Position.Last().GetX() - (center.Width / 2), (float) s.Position.Last().GetY() + 10,
		150, 15, HorizontalAlignment.Left, VerticalAlignment.Top);
	}

	/// <summary>
	/// A method that can be used as an ObjectDrawer delegate
	/// To draw the Walls
	/// </summary>
	/// <param name="o">The Wall to draw</param>
	/// <param name="canvas">Panel display </param>
	private void WallDrawer(object o, ICanvas canvas) {
		Wall w = o as Wall;
		int start;
		int end;
		// Check if it's Vertical Wall first
		// Otherwise, it's in Horizontal Position
		if (w.P1.GetX() == w.P2.GetX()) {
			// set offset from center when positioning wall
			int Horizontal = (int) w.P1.GetX() - 25;
			// Determine wall coordinates of start postion & end position
			if (w.P1.GetY() > w.P2.GetY()) {
				start = (int) w.P2.GetY();
				end = (int) w.P1.GetY();
			} else {
				start = (int) w.P1.GetY();
				end = (int) w.P2.GetY();
			}
			//Draw vertical walls
			for (int i = start; i <= end; i += 50) {
				canvas.DrawImage(wall, Horizontal, i - 25, FixSettings.WallWidth, FixSettings.WallWidth);
			}
		} else {
			//Determine vertical offset from center of map
			int Vertical = (int) w.P1.GetY() - 25;
			// Determine wall coordinates of start postion & end position
			if (w.P1.GetX() > w.P2.GetX()) {
				start = (int) w.P2.GetX();
				end = (int) w.P1.GetX();
			} else {
				start = (int) w.P1.GetX();
				end = (int) w.P2.GetX();
			}
			//Draw horizontal wall segments
			for (int i = start; i <= end; i += 50) {
				canvas.DrawImage(wall, i - 25, Vertical, FixSettings.WallWidth, FixSettings.WallWidth);
			}
		}
	}

	/// <summary>
	/// A method that can be used as an ObjectDrawer delegate
	/// To draw Powerup
	/// </summary>
	/// <param name="o">The powerup to draw</param>
	/// <param name="canvas">Panel display </param>
	private void PowerupDrawer(object o, ICanvas canvas) {
		Powerup p = o as Powerup;
		int size = 24; // should be 16px, but image sourc is 48px

		// Images are drawn starting from the top-left corner.
		// So if we want the image centered on the player's location, we have to offset it
		// by half its size to the left (-width/2) and up (-height/2)
		canvas.DrawImage(power, -(size / 2), -(size / 2), size, size);
	}
}