// Nicolas Robert [Nrx]

public class GameStateLoading : IGameState
{
	public void Enter (Game game)
	{
	}

	public void Execute (Game game)
	{
	}

	public void Exit (Game game)
	{
		// Load all player data
		game.LogicPlayerDataLoad ();

		// Initialize the game board (difficulty & block hue)
		game.data.difficultyLevel = 4;
		game.data.mazeBlockHue = game.data.difficulty [game.data.difficultyLevel].blockHue;

		// Display the high score
		game.data.playerScoreCurrent = game.data.playerScoreBest;

		// Clear the game board
		game.MazeClean ();

		// Initialize the information banner
		game.data.informationBannerRenderer = game.informationBanner.GetComponent <UnityEngine.CanvasRenderer> ();
		game.data.informationBannerTextRenderer = game.informationBannerText.GetComponent <UnityEngine.CanvasRenderer> ();

		// Initialize the Game Center
		UnityEngine.SocialPlatforms.GameCenter.GameCenterPlatform.ShowDefaultAchievementCompletionBanner (true);
		UnityEngine.Social.localUser.Authenticate (success => {});
	}
}
