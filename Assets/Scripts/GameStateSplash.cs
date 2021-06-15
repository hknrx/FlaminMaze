// Nicolas Robert [Nrx]

public class GameStateSplash : IGameState
{
	public void Enter (Game game)
	{
		// Launch the menu music
		game.data.music = game.audioMusicMenu;
	}

	public void Execute (Game game)
	{
		// Make sure the Unity slashscreen is gone
		if (UnityEngine.Rendering.SplashScreen.isFinished) {

			// Show the tutorial
			game.data.stateNext = new GameStateTutorial ();
		}
	}

	public void Exit (Game game)
	{
	}
}
