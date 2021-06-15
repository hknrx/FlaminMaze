// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game
{
	// Game data
	[System.NonSerialized]
	public GameData data = new GameData ();

	// Game icon
	#if UNITY_EDITOR
	public bool iconDisplay;
	private byte [] iconData = {
		96, 96, 96, 96, 96, 96, 96, 96,
		96, 96, 96, 96, 96, 96, 96, 96,
		96, 96, 96, 96, 96, 96, 96, 96,
		96, 96,  0, 64, 64, 96, 32, 96,
		96, 96, 32, 16, 64, 64, 32, 96,
		96, 96, 32, 64, 32, 32, 32, 96,
		96, 96,  0, 96, 64, 96, 32, 96,
		96, 96, 64, 64, 64, 64, 96, 96,
		96, 96, 96, 96, 96, 96, 96, 96,
		96, 96, 96, 96, 96, 96, 96, 96,
		96, 96, 96, 96, 96, 96, 96, 96,
	};
	#endif

	// Sounds
	public AudioSource audioSourceEffect;
	public AudioSource audioSourceMusic;
	public AudioClip audioEffectAlarm;
	public AudioClip audioEffectDing;
	public AudioClip audioEffectLose;
	public AudioClip audioMusicMenu;
	public AudioClip [] audioMusicPlay;

	// Information banner
	public GameObject informationBanner;
	public UnityEngine.UI.Text informationBannerText;

	// Tutorial screens
	public GameObject [] tutorialScreens;

	// Get a hash corresponding to all player data
	private int LogicPlayerDataHash ()
	{
		return Animator.StringToHash (string.Format ("Fl{1}aM{0}iN",
			data.playerScoreBest,
			data.playerGameCounter
		));
	}

	// Load all player data
	public void LogicPlayerDataLoad ()
	{
		data.playerScoreBest = PlayerPrefs.GetInt ("K1");
		data.playerGameCounter = PlayerPrefs.GetInt ("K2");
		if (PlayerPrefs.GetInt ("K3") != LogicPlayerDataHash ()) {

			// Delete all existing keys and values
			PlayerPrefs.DeleteAll ();

			// Reset the values
			data.playerScoreBest = 0;
			data.playerGameCounter = 0;
		}
	}

	// Save all player data
	public void LogicPlayerDataSave ()
	{
		PlayerPrefs.SetInt ("K1", data.playerScoreBest);
		PlayerPrefs.SetInt ("K2", data.playerGameCounter);
		PlayerPrefs.SetInt ("K3", LogicPlayerDataHash ());
	}

	// Update the music
	public void LogicMusicUpdate ()
	{
		data.music = audioMusicPlay [(data.playerGameCounter + data.difficultyLevel) % audioMusicPlay.Length];
	}

	// Initialize the game logic
	private void LogicInitialize ()
	{
		// Initialize the state machine
		data.stateCurrent = new GameStateLoading ();
		data.stateNext = new GameStateSplash ();
	}

	// Update the game logic
	private void LogicUpdate ()
	{
		// Check for taps on all the sprites
		SpriteCheckTap ();

		// Check whether there is a change of state
		if (data.stateCurrent != data.stateNext) {

			// Exit the current state
			data.stateCurrent.Exit (this);

			// Reset the state timer & state frame counter
			data.stateTimer = 0.0f;
			data.stateFrameCounter = 0;

			// Enter the new state
			data.stateCurrent = data.stateNext;
			data.stateCurrent.Enter (this);
		}

		// Execute the current state
		data.stateCurrent.Execute (this);

		// Update the state timer & state frame counter
		data.stateTimer += Time.fixedDeltaTime;
		++data.stateFrameCounter;

		// Check whether the icon display flag is set
		#if UNITY_EDITOR
		if (iconDisplay) {
			System.Array.Copy (iconData, maze, maze.Length);
			data.mazeLedBrightness = 1.0f;
			data.mazeBlockHue = data.difficulty [4].blockHue;
			data.informationBannerText = null;
		}
		#endif

		// Open/close the coin slot
		data.slotOpenedStateCurrent = Mathf.MoveTowards (data.slotOpenedStateCurrent, data.slotOpenedStateTarget, 0.1f);

		// Show/hide the information banner
		data.informationBannerAlpha = Mathf.MoveTowards (data.informationBannerAlpha, data.informationBannerText == informationBannerText.text && data.informationBannerText != null ? 1.0f : 0.0f, 0.05f);
		data.informationBannerRenderer.SetAlpha (data.informationBannerAlpha);
		data.informationBannerTextRenderer.SetAlpha (data.informationBannerAlpha);
		if (data.informationBannerAlpha == 0.0f) {
			informationBannerText.text = data.informationBannerText;
		}

		// Handle the music
		if (data.music == null) {
			if (audioSourceMusic.isPlaying) {
				audioSourceMusic.Stop ();
			}
		} else if (audioSourceMusic.clip == data.music) {
			if (!audioSourceMusic.isPlaying) {
				audioSourceMusic.Play ();
			} else if (audioSourceMusic.volume < 1.0f) {
				audioSourceMusic.volume += GameData.musicFadeSpeed;
			}
		} else if (audioSourceMusic.isPlaying && audioSourceMusic.volume > 0.0f) {
			audioSourceMusic.volume -= GameData.musicFadeSpeed;
		} else {
			audioSourceMusic.clip = data.music;
		}
	}

	// Method to call when the game is paused or resumed (to be called from "OnApplicationPause")
	private void LogicPause (bool pause)
	{
		// Check whether the game is being resumed
		if (!pause) {

			// Let's display the tutorial again
			data.informationTutorialDisplayedFlags = 0;

			// Let's display the ranking again
			data.informationRankCurrent = data.informationRankDisplayed = int.MaxValue;

			// Make sure the sprites have been created and the title is being displayed
			if (spriteMaterials != null && data.stateCurrent is GameStateTitle) {

				// Update the background
				SpriteUpdateBackground ();

				// Hide the game board
				renderGameBoardDisplayCoefficient = 0.0f;
				renderGameBoardDisplay = false;
			}

			// Adapt the rendering scale once again
			frameTimer = 0.0f;
			frameCount = 0;
		}
	}
}
