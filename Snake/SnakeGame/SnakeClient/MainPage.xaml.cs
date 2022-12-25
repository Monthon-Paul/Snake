using System;

namespace SnakeGame;
/// <summary>
/// This is the View of the Snake Client,
/// This Display the GUI aspect of the SnakeGame,
/// including Textbox & buttons, the action would
/// trigger a Controller event for communicating wiht MVC format
/// the User can join a Server with their player name
/// in order to play Snake Online multiplayer.
///
/// Author - Monthon Paul
/// Version - November 26, 2022
/// </summary>
public partial class MainPage : ContentPage {

	// The controller handles updates from the "server"
	// and notifies by event
	private GameController theController = new();

	public MainPage() {
		InitializeComponent();
		graphicsView.Invalidate();
		theController.UpdateArrived += OnFrame;
		theController.PlayerIDgive += PlayerID;
		theController.Error += NetworkErrorHandler;
		theController.SnakeDied += SnakeExplode;
	}

	private void OnTapped(object sender, EventArgs args) {
		keyboardHack.Focus();
	}

	/// <summary>
	/// Handler for the controller's PlayerIDgive event
	/// </summary>
	/// <param name="info">Snake ID number</param>
	private void PlayerID(int info) {
		//Set up Panel with the created world and player ID
		worldPanel.SetWorld(theController.GetWorld());
		worldPanel.SetPlayerId(info);
	}

	/// <summary>
	/// handle for the controller to display Explosion in World Panel
	/// </summary>
	/// <param name="s">the Snake that died</param>
	private void SnakeExplode(Snake s) {
		worldPanel.SnakeExplode(s);
	}

	/// <summary>
	/// Use this method as an event handler for when the controller has updated the world
	/// </summary>
	private void OnFrame() {
		Dispatcher.Dispatch(() => graphicsView.Invalidate());
	}

	/// <summary>
	/// Handler for the controller's Error event
	/// </summary>
	/// <param name="err">Error Message</param>
	private void NetworkErrorHandler(string err) {
		// Show the error
		Dispatcher.Dispatch(() => DisplayAlert("Error", err, "OK"));

		// Then re-enable the controlls so the user can reconnect
		Dispatcher.Dispatch(
		  () => {
			  connectButton.IsEnabled = true;
			  serverText.IsEnabled = true;

			  //Maybe allow user to change Player name as well
			  nameText.IsEnabled = true;
		  });
	}

	/// <summary>
	/// Keyboard Keys, for W,A,S,D that are press should send it's information
	/// to the Game Controller.
	/// </summary>
	/// <param name="sender">Pointer to Textbox</param>
	/// <param name="args">triggle an event</param>
	private void OnTextChanged(object sender, TextChangedEventArgs args) {
		Entry entry = (Entry) sender;
		String text = entry.Text.ToLower();
		if (text == "w") {
			// Move up
			theController.Movement("up");
		} else if (text == "a") {
			// Move left
			theController.Movement("left");
		} else if (text == "s") {
			// Move down
			theController.Movement("down");
		} else if (text == "d") {
			// Move right
			theController.Movement("right");
		}
		entry.Text = "";
	}

	/// <summary>
	/// Event handler for the Connect button
	/// We will put the connection attempt interface here in the view.
	/// </summary>
	/// <param name="sender">Pointer to the Button</param>
	/// <param name="e">triggle an event</param>
	private void ConnectClick(object sender, EventArgs e) {
		if (serverText.Text == "") {
			DisplayAlert("Error", "Please enter a server address", "OK");
			return;
		}
		if (nameText.Text == "") {
			DisplayAlert("Error", "Please enter a name", "OK");
			return;
		}
		if (nameText.Text.Length > 16) {
			DisplayAlert("Error", "Name must be less than 16 characters", "OK");
			return;
		}

		keyboardHack.Focus();

		// Disable the buttons/textbox controls and try to connect
		serverText.IsEnabled = false;
		nameText.IsEnabled = false;
		connectButton.IsEnabled = false;
		theController.Connect(serverText.Text, nameText.Text);
	}

	/// <summary>
	/// Help button to display the User how to move the Snake
	/// </summary>
	/// <param name="sender">Pointer to the Button</param>
	/// <param name="e">triggle an event</param>
	private void ControlsButton_Clicked(object sender, EventArgs e) {
		DisplayAlert("Controls",
					 "W:\t Move up\n" +
					 "A:\t Move left\n" +
					 "S:\t Move down\n" +
					 "D:\t Move right\n",
					 "OK");
	}

	/// <summary>
	/// About menu to display the details about the Pogram
	/// </summary>
	/// <param name="sender">Pointer to the Button</param>
	/// <param name="e">triggle an event</param>
	private void AboutButton_Clicked(object sender, EventArgs e) {
		DisplayAlert("About",
	  "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
	  "Implementation by Monthon Paul & Hong Chen\n" +
		"CS 3500 Fall 2022, University of Utah", "OK");
	}

	/// <summary>
	/// Continue on with directional input.
	/// </summary>
	/// <param name="sender">Pointer to the textbox</param>
	/// <param name="e">triggle an event</param>
	private void ContentPage_Focused(object sender, FocusEventArgs e) {
		if (!connectButton.IsEnabled)
			keyboardHack.Focus();
	}
}