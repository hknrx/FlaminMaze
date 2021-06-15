// Nicolas Robert [Nrx]

public class GameStatePlay : IGameState
{
	private float stateTimerPrevious;
	private bool timerEnabled;

	public void Enter (Game game)
	{
		// Handle achievements related to the level
		if (game.data.mazeLevel >= 10) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdFirstSteps, 100.0, success => {});
			if (game.data.mazeLevel >= 25) {
				UnityEngine.Social.ReportProgress (GameData.achievementIdMazeMaster, 100.0, success => {});
				if (game.data.mazeLevel >= 50) {
					UnityEngine.Social.ReportProgress (GameData.achievementIdGodOfMaze, 100.0, success => {});
				}
			}
		}

		// Define the width of the maze
		int width = game.data.mazeLevel <= 7 ? game.data.mazeLevel : UnityEngine.Random.Range (2, 8);

		// Create the maze
		game.MazeCreateRandomly (width);

		// Handle the tutorial
		if (game.data.mazeLevel == 1) {
			game.data.informationBannerText = GameData.informationTutorialTextDrawPath;
		}

		// Take note of the time
		stateTimerPrevious = game.data.stateTimer;

		// Enable the timer
		timerEnabled = true;
	}

	public void Execute (Game game)
	{
		// Take note of the time
		float stateTimerDelta = game.data.stateTimer - stateTimerPrevious;
		stateTimerPrevious = game.data.stateTimer;

		// Check whether an ad is being shown
		if (UnityEngine.Advertisements.Advertisement.isShowing) {
			return;
		}

		// Check for taps, and update the maze accordingly
		if (game.MazeUpdateWithTap ()) {

			// Level completed!
			game.data.stateNext = new GameStateLevelCompleted ();
		}

		// Update the maze timer
		float mazeTimerSpeed = game.data.difficulty [game.data.difficultyLevel].timerSpeed;
		if (timerEnabled) {
			game.data.mazeTimer -= stateTimerDelta * mazeTimerSpeed;
		}
		if (game.data.mazeTimer <= GameData.mazeTimerWarning) {

			// Animate the game board LED brightness (warning animation)
			game.data.mazeLedBrightness = 0.6f + 0.4f * UnityEngine.Mathf.Cos ((game.data.mazeTimer - GameData.mazeTimerWarning) * 10.0f / mazeTimerSpeed);

			// Check whether the game is over
			if (game.data.mazeTimer <= 0.0f) {

				// Make sure the timer is not lower than 0
				game.data.mazeTimer = 0.0f;

				// Game over!
				game.data.stateNext = new GameStateLose ();
			} else {

				// Launch the alarm sound effect
				if (!game.audioSourceEffect.isPlaying) {
					game.audioSourceEffect.clip = game.audioEffectAlarm;
					game.audioSourceEffect.Play ();
				}

				// Handle the advertising
				if (!game.data.advertisingEnabled) {
					game.data.advertisingEnabled = (game.data.advertisingCounter < 3) && timerEnabled && UnityEngine.Advertisements.Advertisement.IsReady ();
				} else if (game.SpriteCheckTap (Game.Sprites.BUTTON_TV)) {

					// Disable the timer
					timerEnabled = false;

					// Disable the advertising
					game.data.advertisingEnabled = false;

					// Show an ad
					UnityEngine.Advertisements.ShowOptions showOptions = new UnityEngine.Advertisements.ShowOptions ();
					showOptions.resultCallback = (result) => {
						if (result == UnityEngine.Advertisements.ShowResult.Finished) {

							// Reset the timer to its maximum value
							game.data.mazeTimer = GameData.mazeTimerMax;

							// Update the advertising counter
							++game.data.advertisingCounter;

							// Stop the alarm sound effect
							game.audioSourceEffect.Stop ();
						}

						// Enable the timer
						timerEnabled = true;
					};
					UnityEngine.Advertisements.Advertisement.Show (null, showOptions);
				}
			}
		} else {

			// Fade-in
			game.data.mazeLedBrightness = UnityEngine.Mathf.Min (1.0f, game.data.mazeLedBrightness + GameData.mazeLedBrightnessFadeSpeed);
		}
	}

	public void Exit (Game game)
	{
		// Stop the alarm sound effect (in case it is playing)
		game.audioSourceEffect.Stop ();

		// Hide the information banner
		game.data.informationBannerText = null;

		// Disable the advertising
		game.data.advertisingEnabled = false;
	}
}
