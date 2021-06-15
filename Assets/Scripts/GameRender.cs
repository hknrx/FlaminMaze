// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game
{
	// Camera
	private Camera cameraComponent;

	// Sprites
	public enum Sprites {BACKGROUND, SCORE, TIMER, GAME_BOARD, SLOT, BUTTON_TV, BUTTON_PODIUM}
	public GameObject spriteQuad;
	private Material [] spriteMaterials;
	private GameObject [] spriteObjects;

	// Rendering
	public float renderScaleMin = 1.414f;
	public float renderScaleMax = 4.0f;
	private float renderScaleTarget;
	private float renderScale = 0.0f;
	public bool renderBgLastTarget = true;
	private bool renderBgLast = false;
	public bool renderTAATarget = true;
	private bool renderTAA = false;
	private Vector2 renderSizeTarget;
	private Vector2 renderSize;
	public Shader renderMixShader;
	private Material renderMixMaterial;
	private RenderTexture renderTextureMain;
	private RenderTexture renderTextureOffsetted;
	private Vector2 renderGameBoardSizeTotal;
	private float renderGameBoardSizeBlock;
	private float renderGameBoardYHidden;
	private float renderGameBoardYDisplayed;
	private float renderGameBoardYCurrent;
	private float renderGameBoardDisplayCoefficient = 0.0f;
	public bool renderGameBoardDisplay = false;

	// Layout
	public float LAYOUT_SPACING = 0.01f;
	public float LAYOUT_LED_HALF_THICKNESS = 0.08f;
	public float LAYOUT_LED_RATIO = 0.6f;
	public float LAYOUT_LED_HEIGHT = 0.08f;
	public float LAYOUT_LED_COUNT_TIMER = 2.0f;
	public float LAYOUT_LED_COUNT_SCORE = 4.0f;
	public float LAYOUT_SLOT_HEIGHT = 0.12f;
	public float LAYOUT_BORDER_THICKNESS = 0.01f;

	// Frame rate measurement
	public UnityEngine.UI.Text frameRateLabel;
	public float frameRateMeasureDuration = 1.5f;
	public float frameRateAccuracy = 0.95f;
	private float frameTimer = 0.0f;
	private int frameCount = 0;

	// Initialize the rendering
	private void RenderInitialize ()
	{
		// Set the target frame rate
		Application.targetFrameRate = 60;

		// Disable screen dimming
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		// Get the camera
		cameraComponent = GetComponent <Camera> ();

		// Create the mix material
		renderMixMaterial = new Material (renderMixShader);

		// Create all the sprites
		Sprites [] sprites = (Sprites []) System.Enum.GetValues (typeof (Sprites));
		spriteMaterials = new Material [sprites.Length];
		spriteObjects = new GameObject [sprites.Length];
		foreach (Sprites sprite in sprites) {

			// Load and copy the material
			// Note: we do not check whether the material exists (we assume that it does)
			Material spriteMaterial = (Material) Resources.Load (sprite.ToString (), typeof (Material));
			spriteMaterial = Object.Instantiate <Material> (spriteMaterial);
			spriteMaterials [(int) sprite] = spriteMaterial;

			// Create the game object
			GameObject spriteObject = Object.Instantiate <GameObject> (spriteQuad);
			spriteObject.name = spriteMaterial.name;
			spriteObject.GetComponent <Renderer> ().material = spriteMaterial;
			spriteObjects [(int) sprite] = spriteObject;
		}
	}

	// Reset a sprite
	private void RenderResetSprite (Sprites sprite, Vector3 position, Vector2 size)
	{
		GameObject spriteObject = spriteObjects [(int) sprite];
		spriteObject.transform.localPosition = position;
		spriteObject.transform.localScale = new Vector3 (size.x, size.y, 1.0f);
		spriteMaterials [(int) sprite].SetVector ("quadInfo", new Vector4 (size.x / size.y, size.y / renderSize.y, 2.0f * position.x / renderSize.y, 2.0f * position.y / renderSize.y));
	}

	// Reset the rendering
	private void RenderReset ()
	{
		// Define the rendering resolution
		int width = (int) (renderSize.x / renderScale);
		int height = (int) (renderSize.y / renderScale);

		// Create new render textures (with bilinear filtering)
		renderTextureMain = renderTAA || renderScale != 1.0f ? new RenderTexture (width, height, 16) : null;
		renderTextureOffsetted = renderTAA ? new RenderTexture (width + 1, height + 1, 16) : null;

		// Update the mix shader
		renderMixMaterial.SetVector ("_MainTexResolution", new Vector2 (width, height));
		renderMixMaterial.SetTexture ("_OffsettedTex", renderTextureOffsetted);

		// Change the rendering order of the background
		if (renderBgLast) {

			// Display the background after the score, the timer and the game board
			spriteMaterials [(int) Sprites.BACKGROUND].renderQueue = 2001;
			spriteMaterials [(int) Sprites.SCORE].EnableKeyword ("TRANSPARENT_CUTOUT");
			spriteMaterials [(int) Sprites.TIMER].EnableKeyword ("TRANSPARENT_CUTOUT");
			spriteMaterials [(int) Sprites.GAME_BOARD].EnableKeyword ("TRANSPARENT_CUTOUT");
		} else {

			// Display the background first
			spriteMaterials [(int) Sprites.BACKGROUND].renderQueue = 1000;
			spriteMaterials [(int) Sprites.SCORE].DisableKeyword ("TRANSPARENT_CUTOUT");
			spriteMaterials [(int) Sprites.TIMER].DisableKeyword ("TRANSPARENT_CUTOUT");
			spriteMaterials [(int) Sprites.GAME_BOARD].DisableKeyword ("TRANSPARENT_CUTOUT");
		}

		// Compute the size of each sprite
		float spacingDefault = renderSize.y * LAYOUT_SPACING;

		float timerWidthRatio = LAYOUT_LED_HEIGHT * LAYOUT_LED_RATIO * LAYOUT_LED_COUNT_TIMER;
		float scoreWidthRatio = LAYOUT_LED_HEIGHT * LAYOUT_LED_RATIO * LAYOUT_LED_COUNT_SCORE;
		float topWidthRatio = timerWidthRatio + scoreWidthRatio + 2.0f * LAYOUT_LED_HEIGHT + LAYOUT_SLOT_HEIGHT;
		float topWidthFactor = Mathf.Min (renderSize.y, (renderSize.x - 6.0f * spacingDefault) / topWidthRatio);

		float timerWidth = topWidthFactor * timerWidthRatio;
		float scoreWidth = topWidthFactor * scoreWidthRatio;
		float buttonSize = topWidthFactor * LAYOUT_LED_HEIGHT;
		float slotSize = topWidthFactor * LAYOUT_SLOT_HEIGHT;

		float timerHeight = buttonSize;
		float scoreHeight = buttonSize;

		float topWidth = topWidthFactor * topWidthRatio + 6.0f * spacingDefault;
		float topHeight = Mathf.Max (buttonSize, slotSize);

		float gameBoardBorder = 2.0f * LAYOUT_BORDER_THICKNESS * renderSize.y;
		float blockCountWidth = 7.0f + 2.0f * LAYOUT_LED_HALF_THICKNESS;
		float blockCountHeight = 10.0f + 2.0f * LAYOUT_LED_HALF_THICKNESS;
		renderGameBoardSizeTotal.y = renderSize.y - 3.0f * spacingDefault - topHeight;
		renderGameBoardSizeBlock = (renderGameBoardSizeTotal.y - gameBoardBorder) / blockCountHeight;
		renderGameBoardSizeTotal.x = gameBoardBorder + renderGameBoardSizeBlock * blockCountWidth;
		float gameBoardWidthMax = renderSize.x - 2.0f * spacingDefault;
		if (renderGameBoardSizeTotal.x > gameBoardWidthMax) {
			renderGameBoardSizeTotal.x = gameBoardWidthMax;
			renderGameBoardSizeBlock = (renderGameBoardSizeTotal.x - gameBoardBorder) / blockCountWidth;
			renderGameBoardSizeTotal.y = gameBoardBorder + renderGameBoardSizeBlock * blockCountHeight;
		}

		float spacingVertical = (renderSize.y - topHeight - renderGameBoardSizeTotal.y) / 3.0f;

		// Compute the position of each sprite
		float buttonTvX = 0.5f * (buttonSize - topWidth) + spacingDefault;
		float buttonTvY = 0.5f * (renderSize.y - topHeight) - spacingVertical;

		float timerX = buttonTvX + 0.5f * (buttonSize + timerWidth) + spacingDefault;
		float timerY = buttonTvY;

		float slotX = timerX + 0.5f * (timerWidth + slotSize) + spacingDefault;
		float slotY = buttonTvY;

		float scoreX = slotX + 0.5f * (slotSize + scoreWidth) + spacingDefault;
		float scoreY = buttonTvY;

		float buttonPodiumX = scoreX + 0.5f * (scoreWidth + buttonSize) + spacingDefault;
		float buttonPodiumY = buttonTvY;

		renderGameBoardYDisplayed = 0.5f * (renderGameBoardSizeTotal.y - renderSize.y) + spacingVertical;
		renderGameBoardYHidden = 0.5f * (-renderGameBoardSizeTotal.y - renderSize.y) - 1.0f;

		// Reset all the sprites except the game board
		// Notes:
		// - Make sure the background is behind everything else
		// - Make sure the background covers the whole screen (and even more, because of the TAA!)
		RenderResetSprite (Sprites.BACKGROUND, new Vector3 (0.0f, 0.0f, 0.2f), new Vector2 (renderSize.x + 2.0f, renderSize.y + 2.0f));
		RenderResetSprite (Sprites.SCORE, new Vector2 (scoreX, scoreY), new Vector2 (scoreWidth, scoreHeight));
		RenderResetSprite (Sprites.TIMER, new Vector3 (timerX, timerY), new Vector2 (timerWidth, timerHeight));
		RenderResetSprite (Sprites.SLOT, new Vector2 (slotX, slotY), new Vector2 (slotSize, slotSize));
		RenderResetSprite (Sprites.BUTTON_TV, new Vector2 (buttonTvX, buttonTvY), new Vector2 (buttonSize, buttonSize));
		RenderResetSprite (Sprites.BUTTON_PODIUM, new Vector2 (buttonPodiumX, buttonPodiumY), new Vector2 (buttonSize, buttonSize));

		// Additional settings for specific sprites
		string [] properties = new string [] {"ledRatio", "ledHalfThickness", "borderThickness"};
		spriteMaterials [(int) Sprites.SCORE].SetFloat (properties [0], LAYOUT_LED_RATIO);
		spriteMaterials [(int) Sprites.TIMER].SetFloat (properties [0], LAYOUT_LED_RATIO);
		spriteMaterials [(int) Sprites.GAME_BOARD].SetFloat (properties [1], LAYOUT_LED_HALF_THICKNESS);
		spriteMaterials [(int) Sprites.GAME_BOARD].SetFloat (properties [2], LAYOUT_BORDER_THICKNESS);
		spriteMaterials [(int) Sprites.SLOT].SetFloat (properties [2], LAYOUT_BORDER_THICKNESS);
		spriteMaterials [(int) Sprites.BUTTON_TV].SetFloat (properties [2], LAYOUT_BORDER_THICKNESS);
		spriteMaterials [(int) Sprites.BUTTON_PODIUM].SetFloat (properties [2], LAYOUT_BORDER_THICKNESS);
	}

	// Update the rendering
	private void RenderUpdate ()
	{
		// Measure the frame rate
		if (frameTimer <= 0.0f) {
			float frameRate = frameCount / (frameRateMeasureDuration - frameTimer);

			// Display the frame rate and current rendering scale
			frameRateLabel.text = System.String.Format ("{0:F1} fps\n(x{1:F1})", frameRate, renderScale);

			// Adapt the rendering scale once the game is stable
			if (frameRate <= 0.0f) {
				renderScaleTarget = renderScaleMin;
			} else if (frameRate < Application.targetFrameRate * frameRateAccuracy || frameRate > Application.targetFrameRate * (2.0f - frameRateAccuracy)) {
				renderScaleTarget = Mathf.Clamp (renderScale * Mathf.Sqrt (Application.targetFrameRate / frameRate), renderScaleMin, renderScaleMax);
			}

			// Reset
			frameTimer = frameRateMeasureDuration;
			frameCount = 1;
		} else {
			frameTimer -= Time.unscaledDeltaTime;
			++frameCount;
		}

		// Monitor changes that would require to reset the rendering
		bool renderReset = renderScale != renderScaleTarget ||
			renderBgLast != renderBgLastTarget ||
			renderTAA != renderTAATarget ||
			renderSize != renderSizeTarget;
		if (renderReset) {

			// Update the rendering parameters
			renderScale = renderScaleTarget;
			renderBgLast = renderBgLastTarget;
			renderTAA = renderTAATarget;
			renderSize = renderSizeTarget;

			// Reset the rendering
			RenderReset ();
		}

		// Reset the game board sprite to update its position
		renderGameBoardDisplayCoefficient += ((renderGameBoardDisplay ? 1.0f : 0.0f) - renderGameBoardDisplayCoefficient) * 0.1f;
		renderGameBoardYCurrent = Mathf.Lerp (renderGameBoardYHidden, renderGameBoardYDisplayed, renderGameBoardDisplayCoefficient);
		RenderResetSprite (Sprites.GAME_BOARD, new Vector3 (0.0f, renderGameBoardYCurrent, 0.1f), renderGameBoardSizeTotal);

		// Select the appropriate render texture
		if (renderTAA && (Time.frameCount & 1) != 0) {
			cameraComponent.targetTexture = renderTextureOffsetted;
			cameraComponent.orthographicSize = renderSize.y * 0.5f * renderTextureOffsetted.height / renderTextureMain.height;
			cameraComponent.aspect = (float) renderTextureOffsetted.width / renderTextureOffsetted.height;
		} else {
			cameraComponent.targetTexture = renderTextureMain;
			cameraComponent.orthographicSize = renderSize.y * 0.5f;
			if (cameraComponent.targetTexture) {
				cameraComponent.aspect = (float) renderTextureMain.width / renderTextureMain.height;
			} else {
				cameraComponent.aspect = renderSize.x / renderSize.y;
			}
		}

		// Set the mix ratio
		float mixRatio;
		if (!renderReset) {
			mixRatio = 0.5f;
		} else if (cameraComponent.targetTexture == renderTextureMain) {
			mixRatio = 0.0f;
		} else {
			mixRatio = 1.0f;
		}
		renderMixMaterial.SetFloat ("mixRatio", mixRatio);
	}

	// Method to render the scene full screen (to be called from the "OnPostRender" of another camera)
	public void RenderFullScreen (RenderTexture renderTextureDest = null)
	{
		if (renderTAA) {
			Graphics.Blit (renderTextureMain, renderTextureDest, renderMixMaterial);
		} else if (cameraComponent.targetTexture) {
			Graphics.Blit (cameraComponent.targetTexture, renderTextureDest);
		}
	}
}
