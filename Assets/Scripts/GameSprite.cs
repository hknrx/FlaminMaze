// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game
{
	// Tap flags
	private int spriteTapFlagsPrevious;
	private int spriteTapFlagsCurrent;

	// Check for taps on all the sprites
	private bool SpriteCheckTap ()
	{
		// Reset all the tap flags
		spriteTapFlagsPrevious = spriteTapFlagsCurrent;
		spriteTapFlagsCurrent = 0;

		// Check whether there is a tap
		if (!Input.GetMouseButton (0)) {
			return false;
		}

		// Check whether some sprites are tapped
		Vector2 tapPosition = (Vector2) Input.mousePosition - 0.5f * new Vector2 (Screen.width, Screen.height);
		for (int spriteIndex = (int) Sprites.BUTTON_PODIUM; spriteIndex >= 0; --spriteIndex) {
			Transform spriteTransform = spriteObjects [spriteIndex].transform;
			Vector2 tapDistance = 2.0f * (tapPosition - (Vector2) spriteTransform.localPosition);
			if (Mathf.Abs (tapDistance.x) < spriteTransform.localScale.x && Mathf.Abs (tapDistance.y) < spriteTransform.localScale.y) {
				spriteTapFlagsCurrent |= 1 << spriteIndex;
			}
		}
		return true;
	}

	// Check for a tap on a sprite
	public bool SpriteCheckTap (Sprites sprite)
	{
		int tapFlag = 1 << (int) sprite;
		return (spriteTapFlagsCurrent & tapFlag) != 0 && (spriteTapFlagsPrevious & tapFlag) == 0;
	}

	// Check for a tap on several sprites at once, without other sprites being tapped
	public bool SpriteCheckTapExclusive (params Sprites [] sprites)
	{
		int tapFlags = 0;
		foreach (Sprites sprite in sprites) {
			tapFlags |= 1 << (int) sprite;
		}
		tapFlags &= spriteTapFlagsCurrent ^ spriteTapFlagsPrevious;
		return tapFlags != 0 && (spriteTapFlagsCurrent ^ tapFlags) == 0;
	}

	// Update the pushed state of a sprite
	public void SpriteUpdateStatePushed (Sprites sprite)
	{
		int index = (int) sprite;
		float statePushed = (spriteTapFlagsCurrent & (1 << index)) != 0 ? 1.0f : 0.0f;
		spriteMaterials [index].SetFloat ("statePushed", statePushed);
	}

	// Update the enabled state of a sprite
	public void SpriteUpdateStateEnabled (Sprites sprite, bool enabled)
	{
		spriteMaterials [(int) sprite].SetFloat ("stateEnabled", enabled ? 1.0f : 0.0f);
	}

	// Update the opened state of a sprite
	public void SpriteUpdateStateOpened (Sprites sprite, float stateOpened)
	{
		spriteMaterials [(int) sprite].SetFloat ("stateOpened", stateOpened);
	}

	// Update a LED number
	public void SpriteUpdateDisplayedNumber (Sprites sprite, int number)
	{
		spriteMaterials [(int) sprite].SetInt ("displayedNumber", number);
	}

	// Update the game board
	private void SpriteUpdateGameBoard ()
	{
		// Update the data
		Vector4 dataBlock = new Vector4 ();
		Vector4 dataLedH = new Vector4 ();
		Vector4 dataLedV = new Vector4 ();
		int dataRow = 0;
		int dataGroup = 1;
		int dataBit7 = 1;
		int dataBit8 = 1;

		int mazeIndex = 0;
		for (int y = 0; y < mazePathCountHeight; ++y) {
			for (int x = 0; x < mazePathCountWidth; ++x) {
				byte mazeValue = maze [mazeIndex];
				if ((mazeValue & (byte) MazeFlags.PATH) != 0) {
					dataBlock [dataRow] += dataBit7;
				}
				if ((mazeValue & (byte) MazeFlags.DOOR_OPENED_TOP) == 0) {
					dataLedH [dataRow] += dataBit7;
				}
				if ((mazeValue & (byte) MazeFlags.DOOR_OPENED_LEFT) == 0) {
					dataLedV [dataRow] += dataBit8;
				}
				++mazeIndex;
				dataBit7 <<= 1;
				dataBit8 <<= 1;
			}
			if ((maze [mazeIndex] & (byte) MazeFlags.DOOR_OPENED_LEFT) == 0) {
				dataLedV [dataRow] += dataBit8;
			}
			++mazeIndex;
			dataBit8 <<= 1;
			if (dataGroup < 3) {
				++dataGroup;
			} else {
				++dataRow;
				dataGroup = 1;
				dataBit7 = 1;
				dataBit8 = 1;
			}
		}
		for (int x = 0; x < mazePathCountWidth; ++x) {
			if ((maze [mazeIndex] & (byte) MazeFlags.DOOR_OPENED_TOP) == 0) {
				dataLedH [dataRow] += dataBit7;
			}
			++mazeIndex;
			dataBit7 <<= 1;
		}

		// Update the block hue
		float mazeBlockHueTarget = data.difficulty [data.difficultyLevel].blockHue;
		mazeBlockHueTarget -= Mathf.Floor (mazeBlockHueTarget);

		data.mazeBlockHue -= Mathf.Floor (data.mazeBlockHue);

		float blockHueError = mazeBlockHueTarget - data.mazeBlockHue;
		if (blockHueError > 0.5f) {
			blockHueError -= 1.0f;
		} else if (blockHueError < -0.5f) {
			blockHueError += 1.0f;
		}
		#if UNITY_EDITOR
		if (!iconDisplay)
		#endif
		data.mazeBlockHue += Mathf.Clamp (blockHueError, -GameData.mazeBlockHueChangeSpeed, GameData.mazeBlockHueChangeSpeed);

		// Update the shader
		Material spriteMaterial = spriteMaterials [(int) Sprites.GAME_BOARD];
		spriteMaterial.SetFloat ("ledBrightness", data.mazeLedBrightness);
		spriteMaterial.SetVector ("dataBlock", dataBlock);
		spriteMaterial.SetVector ("dataLedH", dataLedH);
		spriteMaterial.SetVector ("dataLedV", dataLedV);
		spriteMaterial.SetFloat ("blockHue", data.mazeBlockHue);
	}

	// Update the background
	public void SpriteUpdateBackground ()
	{
		Material spriteMaterial = spriteMaterials [(int) Sprites.BACKGROUND];
		if ((data.backgroundIndex & 1) != 0) {
			spriteMaterial.EnableKeyword ("SCENE_BIT0");
		} else {
			spriteMaterial.DisableKeyword ("SCENE_BIT0");
		}
		if ((data.backgroundIndex & 2) != 0) {
			spriteMaterial.EnableKeyword ("SCENE_BIT1");
		} else {
			spriteMaterial.DisableKeyword ("SCENE_BIT1");
		}
		if ((data.backgroundIndex & 4) != 0) {
			spriteMaterial.EnableKeyword ("SCENE_BIT2");
		} else {
			spriteMaterial.DisableKeyword ("SCENE_BIT2");
		}
		++data.backgroundIndex;
	}

	// Update the time of all the sprites
	private void SpriteUpdateTime (float time)
	{
		foreach (Material spriteMaterial in spriteMaterials) {
			spriteMaterial.SetFloat ("time", time);
		}
	}

	// Update all the sprites
	private void SpriteUpdate (float time)
	{
		// Update the coin slot
		SpriteUpdateStateOpened (Sprites.SLOT, data.slotOpenedStateCurrent);

		// Update the TV button
		SpriteUpdateStateEnabled (Sprites.BUTTON_TV, data.advertisingEnabled);

		// Update the LED numbers (maze timer and score)
		SpriteUpdateDisplayedNumber (Game.Sprites.TIMER, Mathf.FloorToInt (data.mazeTimer));
		SpriteUpdateDisplayedNumber (Game.Sprites.SCORE, data.playerScoreCurrent);

		// Update the game board (blocks & LEDs)
		SpriteUpdateGameBoard ();

		// Update the pushed state of all the buttons
		SpriteUpdateStatePushed (Sprites.BUTTON_TV);
		SpriteUpdateStatePushed (Sprites.BUTTON_PODIUM);

		// Update the time of all the sprites
		SpriteUpdateTime (time);
	}
}
