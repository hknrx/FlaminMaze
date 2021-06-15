// Nicolas Robert [Nrx]

public class GameStateTutorial : IGameState
{
	private int step;
	private float stepTimerStart;

	public void Enter (Game game)
	{
		// Initialize the tutorial
		game.data.informationBannerText = GameData.informationTutorialTextHowToPlay;
		foreach (UnityEngine.GameObject tutorialScreen in game.tutorialScreens) {
			tutorialScreen.SetActive (true);
		}
		step = -1;
		stepTimerStart = game.data.stateTimer;
	}

	public void Execute (Game game)
	{
		// Animate the tutorial screens
		float stepTimer = UnityEngine.Mathf.Min ((game.data.stateTimer - stepTimerStart) * 1.5f, 1.0f);
		float angleControl = game.data.stateTimer * 0.2f;
		float angleControlStep = 2.0f * UnityEngine.Mathf.PI / game.tutorialScreens.Length;
		for (int tutorialScreenIndex = 0; tutorialScreenIndex < game.tutorialScreens.Length; ++tutorialScreenIndex) {
			UnityEngine.GameObject tutorialScreen = game.tutorialScreens [tutorialScreenIndex];
			float alpha;
			float scale;
			if (step > game.tutorialScreens.Length) {

				// All tutorial screens disappear
				alpha = 1.0f - stepTimer;
				scale = 0.4f;
			} else if (tutorialScreenIndex == step) {

				// The tutorial screen appears
				alpha = stepTimer;
				scale = 1.0f - 0.3f * stepTimer;
			} else if (tutorialScreenIndex == step - 1) {

				// The tutorial screen moves to its final position
				alpha = 1.0f;
				scale = 0.7f - 0.3f * stepTimer;
				UnityEngine.Vector2 radius = new UnityEngine.Vector2 (100.0f, 120.0f) * stepTimer;
				float angle = 0.5f * UnityEngine.Mathf.PI + angleControlStep * (0.5f - tutorialScreenIndex);
				tutorialScreen.transform.localPosition = new UnityEngine.Vector3 (radius.x * UnityEngine.Mathf.Cos (angle), radius.y * UnityEngine.Mathf.Sin (angle), 0.0f);
			} else if (tutorialScreenIndex < step) {

				// The tutorial screen is already where it shall be
				alpha = 1.0f;
				scale = 0.4f;
			} else {

				// The tutorial screen has not appeared yet
				alpha = 0.0f;
				scale = 1.0f;
			}
			tutorialScreen.GetComponent <UnityEngine.CanvasGroup> ().alpha = alpha;
			tutorialScreen.transform.localScale = new UnityEngine.Vector3 (scale, scale, 1.0f);

			// Rotate all tutorial screens
			tutorialScreen.transform.localEulerAngles = new UnityEngine.Vector3 (0.0f, 0.0f, 2.0f * UnityEngine.Mathf.Sin (angleControl));
			angleControl += angleControlStep;
		}

		// Check whether we can proceed to the next step
		if (stepTimer >= 1.0f) {
			if (step > game.tutorialScreens.Length) {

				// Show the title screen
				game.data.stateNext = new GameStateTitle ();
			} else if (UnityEngine.Input.GetMouseButton (0) || step == game.tutorialScreens.Length) {

				// Hide the information banner (in case it is not hidden already)
				game.data.informationBannerText = null;

				// Next step...
				++step;
				stepTimerStart = game.data.stateTimer;

				// Play a sound effect when appropriate
				if (step < game.tutorialScreens.Length) {
					game.audioSourceEffect.PlayOneShot (game.audioEffectDing);
				} else if (step == game.tutorialScreens.Length) {
					game.audioSourceEffect.PlayOneShot (game.audioEffectLose);
				}
			}
		}
	}

	public void Exit (Game game)
	{
		// Hide all tutorial screens
		foreach (UnityEngine.GameObject tutorialScreen in game.tutorialScreens) {
			tutorialScreen.SetActive (false);
		}

		// Show the game board
		game.renderGameBoardDisplay = true;
	}
}
