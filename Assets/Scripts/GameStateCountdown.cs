// Nicolas Robert [Nrx]

public class GameStateCountdown : IGameState
{
	private int symbolIndex;

	public void Enter (Game game)
	{
		// Reset the difficulty
		game.data.difficultyLevel = 0;

		// Initialize the maze
		game.data.mazeLevel = 1;
		game.data.mazeTimer = GameData.mazeTimerMax;
		game.MazeInitalizeStartPoint ();

		// Initialize the score
		game.data.playerScoreCurrent = 0;

		// Initialize the advertising
		game.data.advertisingCounter = 0;

		// Initialize the countdown
		symbolIndex = 0;

		// Show the timer
		game.SpriteUpdateStateEnabled (Game.Sprites.TIMER, true);

		// Show the game board
		game.renderGameBoardDisplay = true;
	}

	public void Execute (Game game)
	{
		// Handle the countdown
		if (game.data.stateTimer >= symbolIndex) {
			if (symbolIndex < 4) {

				// Display the symbol
				game.MazeCreateWithSymbol (symbolIndex);
				if (symbolIndex < 3) {

					// Play a sound effect
					game.audioSourceEffect.PlayOneShot (game.audioEffectDing);
				} else {

					// Play the music
					game.LogicMusicUpdate ();
				}

				// Next symbol
				++symbolIndex;
			} else {

				// Let's play!
				game.data.stateNext = new GameStatePlay ();
			}
		}

		// Animate the game board LED brightness
		game.data.mazeLedBrightness = symbolIndex != 4 || (game.data.stateFrameCounter & 4) != 0 ? 1.0f : 0.0f;
	}

	public void Exit (Game game)
	{
	}
}
