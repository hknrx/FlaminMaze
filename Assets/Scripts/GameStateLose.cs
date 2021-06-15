// Nicolas Robert [Nrx]

public class GameStateLose : IGameState
{
	public void Enter (Game game)
	{
		// Display the skull
		game.MazeCreateWithSymbol (4);

		// Play a sound effect
		game.audioSourceEffect.PlayOneShot (game.audioEffectLose);

		// Stop the music
		game.data.music = null;
	}

	public void Execute (Game game)
	{
		// Handle the timer
		if (game.data.stateTimer > 4.0f) {

			// Go back to the title screen
			game.data.stateNext = new GameStateTitle ();
		}

		// Animate the game board LED brightness
		game.data.mazeLedBrightness = game.data.stateTimer > 1.0f || (game.data.stateFrameCounter & 4) != 0 ? 1.0f : 0.0f;

		// The timer and the score are blinking
		bool blinking = (game.data.stateFrameCounter & 16) != 0;
		game.SpriteUpdateStateEnabled (Game.Sprites.TIMER, blinking);
		game.SpriteUpdateStateEnabled (Game.Sprites.SCORE, blinking);
	}

	public void Exit (Game game)
	{
		// Disable the timer
		game.SpriteUpdateStateEnabled (Game.Sprites.TIMER, false);

		// Update the high score
		if (game.data.playerScoreCurrent > game.data.playerScoreBest) {
			game.data.playerScoreBest = game.data.playerScoreCurrent;
		}

		// Increment the number of played games
		++game.data.playerGameCounter;

		// Update the high scores leaderboard
		UnityEngine.Social.ReportScore (game.data.playerScoreBest, GameData.leaderboardIdHighScores, firstSuccess => {

			// Get the rank of the local user in the high scores leaderboard
			UnityEngine.SocialPlatforms.ILeaderboard leaderboard = UnityEngine.Social.CreateLeaderboard ();
			leaderboard.id = GameData.leaderboardIdHighScores;
			leaderboard.LoadScores (secondSuccess => {
				if (secondSuccess) {
					if (leaderboard.localUserScore.rank > 0) {
						game.data.informationRankCurrent = leaderboard.localUserScore.rank;
					}
					if (leaderboard.localUserScore.value > game.data.playerScoreBest) {
						game.data.playerScoreBest = (int) leaderboard.localUserScore.value;
						game.LogicPlayerDataSave ();
					}
				}

				// Update the played games leaderboard
				UnityEngine.Social.ReportScore (game.data.playerGameCounter, GameData.leaderboardIdPlayedGames, thirdSuccess => {

					// Get the actual number of played games from the played games leaderboard
					leaderboard = UnityEngine.Social.CreateLeaderboard ();
					leaderboard.id = GameData.leaderboardIdPlayedGames;
					leaderboard.LoadScores (fourthSuccess => {
						if (fourthSuccess && leaderboard.localUserScore.value > game.data.playerGameCounter) {
							game.data.playerGameCounter = (int) leaderboard.localUserScore.value;
							game.LogicPlayerDataSave ();
						}
					});
				});
			});
		});

		// Handle achievements related to the number of played games
		UnityEngine.Social.ReportProgress (GameData.achievementIdExplorer, 100.0 * game.data.playerGameCounter / 10, success => {});
		UnityEngine.Social.ReportProgress (GameData.achievementIdAdventurer, 100.0 * game.data.playerGameCounter / 50, success => {});
		UnityEngine.Social.ReportProgress (GameData.achievementIdHero, 100.0 * game.data.playerGameCounter / 100, success => {});

		// Handle achievements related to the level
		if (game.data.mazeLevel == 1) {
			UnityEngine.Social.ReportProgress (GameData.achievementIdLoser, 100.0, success => {});
		}

		// Display the rating text when it is appropriate
		if (game.data.playerGameCounter % GameData.informationRatingDisplayPeriod == 0) {
			game.data.informationBannerText = GameData.informationRatingText;
		} else {

			// Handle the tutorial
			if ((game.data.informationTutorialDisplayedFlags & GameData.InformationTutorialDisplayedFlags.MAZE_EXIT) == 0 && game.data.difficultyLevel <= GameData.informationTutorialMazeExitDisplayDifficulty) {
				game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.MAZE_EXIT;
				game.data.informationBannerText = GameData.informationTutorialTextMazeExit;
			} else if ((game.data.informationTutorialDisplayedFlags & GameData.InformationTutorialDisplayedFlags.TV_BUTTON) == 0) {
				game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.TV_BUTTON;
				game.data.informationBannerText = GameData.informationTutorialTextTvButton;
			}
		}

		// Save all player data
		game.LogicPlayerDataSave ();
	}
}
