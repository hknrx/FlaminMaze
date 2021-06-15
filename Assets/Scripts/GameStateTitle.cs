// Nicolas Robert [Nrx]

public class GameStateTitle : IGameState
{
	private int titleIndex;

	public void Enter (Game game)
	{
		// Launch the menu music
		game.data.music = game.audioMusicMenu;

		// Update the background
		game.SpriteUpdateBackground ();

		// Enable the score
		game.SpriteUpdateStateEnabled (Game.Sprites.SCORE, true);

		// Open the coin slot
		game.data.slotOpenedStateTarget = 1.0f;

		// Enable the PODIUM button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_PODIUM, true);

		// Set the game board LED brightness
		game.data.mazeLedBrightness = 1.0f;

		// Initialize the title
		titleIndex = 0;
	}

	public void Execute (Game game)
	{
		// The coin slot is blinking
		game.SpriteUpdateStateEnabled (Game.Sprites.SLOT, (game.data.stateFrameCounter & 16) != 0);

		// Handle the information banner
		if (game.data.informationBannerText == null) {
			if ((game.data.informationTutorialDisplayedFlags & GameData.InformationTutorialDisplayedFlags.COIN_SLOT) == 0) {

				// Display the tutorial
				game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.COIN_SLOT;
				game.data.informationBannerText = GameData.informationTutorialTextCoinSlot;
			} else if (game.data.informationRankCurrent != game.data.informationRankDisplayed) {

				// Display the rank of the local user in the high scores leaderboard
				game.data.informationRankDisplayed = game.data.informationRankCurrent;
				game.data.informationBannerText = string.Format (GameData.informationRankText, game.data.informationRankCurrent);
			}
		}

		// Handle the taps
		if (game.SpriteCheckTap (Game.Sprites.BUTTON_PODIUM)) {

			// The PODIUM button is tapped, show the leaderboard
			UnityEngine.Social.ShowLeaderboardUI ();
		} else if (game.data.slotOpenedStateCurrent > 0.9f && game.SpriteCheckTap (Game.Sprites.SLOT)) {

			// The coin slot is opened and tapped, start the game
			game.data.stateNext = new GameStateCountdown ();
		} else if (game.SpriteCheckTapExclusive (Game.Sprites.BACKGROUND, Game.Sprites.GAME_BOARD)) {

			// The background or game board is tapped
			if (game.data.informationBannerText != null && (game.data.informationBannerText != GameData.informationTutorialTextCoinSlot || game.data.playerGameCounter >= GameData.informationTutorialDismissGameCount)) {

				// Hide the information banner, except when showing the 1st tutorial text
				game.data.informationBannerText = null;
			} else {

				// Toggle the display of the game board
				game.renderGameBoardDisplay = !game.renderGameBoardDisplay;
			}
		}

		// Display the title
		if (game.renderGameBoardDisplay && game.data.stateFrameCounter % 3 == 0) {
			game.MazeUpdateWithTitle (titleIndex++);
		}
	}

	public void Exit (Game game)
	{
		// Stop the menu music
		game.data.music = null;

		// Close and disable the coin slot
		game.SpriteUpdateStateEnabled (Game.Sprites.SLOT, false);
		game.data.slotOpenedStateTarget = 0.0f;

		// Disable the PODIUM button
		game.SpriteUpdateStateEnabled (Game.Sprites.BUTTON_PODIUM, false);

		// Hide the information banner (and take note that the tutorial does not need to be displayed again)
		game.data.informationTutorialDisplayedFlags |= GameData.InformationTutorialDisplayedFlags.COIN_SLOT;
		game.data.informationBannerText = null;
	}
}
