// Nicolas Robert [Nrx]

public class GameData
{
	// States
	public IGameState stateCurrent;
	public IGameState stateNext;
	public float stateTimer;
	public int stateFrameCounter;

	// Background
	public int backgroundIndex;

	// Coin slot
	public float slotOpenedStateCurrent;
	public float slotOpenedStateTarget;

	// Maze
	public int mazeLevel;
	public float mazeTimer;
	public const float mazeTimerMax = 99.9f;
	public const float mazeTimerWarning = 33.3f;
	public float mazeLedBrightness;
	public const float mazeLedBrightnessFadeSpeed = 0.1f;
	public float mazeBlockHue;
	public const float mazeBlockHueChangeSpeed = 0.005f;

	// Difficulty
	public int difficultyLevel;
	public struct Difficulty
	{
		public float timerSpeed;
		public float blockHue;
		public int nextLevel;
	}
	public readonly Difficulty [] difficulty = {
		new Difficulty {timerSpeed =  5.0f, blockHue = 0.33f, nextLevel =  5},
		new Difficulty {timerSpeed =  7.5f, blockHue = 0.43f, nextLevel = 10},
		new Difficulty {timerSpeed = 10.0f, blockHue = 0.58f, nextLevel = 15},
		new Difficulty {timerSpeed = 12.0f, blockHue = 0.78f, nextLevel = 25},
		new Difficulty {timerSpeed = 14.0f, blockHue = 0.93f, nextLevel = 35},
		new Difficulty {timerSpeed = 16.0f, blockHue = 0.03f, nextLevel = System.Int32.MaxValue},
	};

	// Player
	public int playerScoreCurrent;
	public int playerScoreBest;
	public int playerGameCounter;

	// Leaderboards
	public const string leaderboardIdHighScores = "FlaminMaze.HighScores";
	public const string leaderboardIdHighLevels = "FlaminMaze.HighLevels";
	public const string leaderboardIdPlayedGames = "FlaminMaze.PlayedGames";

	// Achievements
	public const string achievementIdFirstSteps = "FlaminMaze.FirstSteps"; // Reached the level 10 (10 points)
	public const string achievementIdMazeMaster = "FlaminMaze.MazeMaster"; // Reached the level 25 (50 points)
	public const string achievementIdGodOfMaze = "FlaminMaze.GodOfMaze"; // Reached the level 50 (100 points)
	public const string achievementIdExplorer = "FlaminMaze.Explorer"; // Played 10 games (10 points)
	public const string achievementIdAdventurer = "FlaminMaze.Adventurer"; // Played 50 games (20 points)
	public const string achievementIdHero = "FlaminMaze.Hero"; // Played 100 games (50 points)
	public const string achievementIdLoser = "FlaminMaze.Loser"; // Failed at the 1st level (5 points)

	// Information banner (general)
	public float informationBannerAlpha;
	public string informationBannerText;
	public UnityEngine.CanvasRenderer informationBannerRenderer;
	public UnityEngine.CanvasRenderer informationBannerTextRenderer;

	// Information banner (tutorial)
	public enum InformationTutorialDisplayedFlags {
		COIN_SLOT = 1 << 0,
		MAZE_EXIT = 1 << 1,
		TV_BUTTON = 1 << 2,
	}
	public InformationTutorialDisplayedFlags informationTutorialDisplayedFlags;
	public const int informationTutorialDismissGameCount = 5;
	public const int informationTutorialMazeExitDisplayDifficulty = 2;
	public const string informationTutorialTextHowToPlay = "Welcome to Flamin Maze!\n\nHOW TO PLAY?";
	public const string informationTutorialTextCoinSlot = "Welcome to Flamin Maze!\n\nTap the coin slot to start the game!";
	public const string informationTutorialTextDrawPath = "Draw a path and find the exit!\nHurry up!";
	public const string informationTutorialTextMazeExit = "Hint! The exit of each maze is the spot the furthest away from its entrance!";
	public const string informationTutorialTextTvButton = "Hint! When the TV button is lit up, tap it to watch a video ad and get more time!\n(Up to 3 times per game!)";

	// Information banner (rating)
	public const int informationRatingDisplayPeriod = 20;
	public const string informationRatingText = "If you like the game, please rate it 5 stars!\n(You can do so from the podium menu!)\n\nYour kind support is greatly appreciated!";

	// Information banner (ranking)
	public int informationRankCurrent = int.MaxValue;
	public int informationRankDisplayed = int.MaxValue;
	public const string informationRankText = "You are now ranked #{0} worldwide!";

	// Advertising
	public bool advertisingEnabled;
	public int advertisingCounter;

	// Music
	public UnityEngine.AudioClip music;
	public const float musicFadeSpeed = 0.015f;
}
