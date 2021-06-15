// Nicolas Robert [Nrx]

public class GameStateLevelCompleted : IGameState
{
	private int scoreIncrement;

	public void Enter (Game game)
	{
		// Update the high levels leaderboard
		UnityEngine.Social.ReportScore (game.data.mazeLevel, GameData.leaderboardIdHighLevels, success => {});

		// Go to the next level
		++game.data.mazeLevel;

		// Define the score increment
		scoreIncrement = game.data.difficultyLevel + 1;

		// Handle the difficulty level
		if (game.data.mazeLevel >= game.data.difficulty [game.data.difficultyLevel].nextLevel) {
			++game.data.difficultyLevel;

			// Change the music
			game.LogicMusicUpdate ();
		}

		// Clear the path
		game.MazePathClear ();
	}

	public void Execute (Game game)
	{
		// Check whether the path has been cleaned
		if (!game.MazePathCleaned ()) {

			// Animate the game board LED brightness
			game.data.mazeLedBrightness = game.data.stateTimer > 0.5f || (game.data.stateFrameCounter & 4) != 0 ? 1.0f : 0.0f;

			// Play some sound effects
			if ((game.data.stateFrameCounter & 7) == 0) {
				game.audioSourceEffect.PlayOneShot (game.audioEffectDing);
			}

			// Don't go too fast...
			if ((game.data.stateFrameCounter & 3) == 0) {

				// Clean the path
				game.MazePathClean ();

				// Handle the maze timer
				game.data.mazeTimer = UnityEngine.Mathf.Min (GameData.mazeTimerMax, game.data.mazeTimer + 1.0f);

				// Increment the score
				game.data.playerScoreCurrent += scoreIncrement;
			}
		} else if (game.data.mazeLedBrightness > 0.0f) {

			// Fade-out
			game.data.mazeLedBrightness = UnityEngine.Mathf.Max (0.0f, game.data.mazeLedBrightness - GameData.mazeLedBrightnessFadeSpeed);
		} else {

			// Next level!
			game.data.stateNext = new GameStatePlay ();
		}
	}

	public void Exit (Game game)
	{
	}
}
