// Nicolas Robert [Nrx]

using UnityEngine;

public partial class Game : MonoBehaviour
{
	// Screenshots
	#if UNITY_EDITOR
	public bool screenshotTrigger;
	private int screenshotTimer;
	private int screenshotIndex;
	private readonly Vector2 [] screenshotSize = {
		new Vector2 (640, 960),
		new Vector2 (640, 1136),
		new Vector2 (750, 1334),
		new Vector2 (1242, 2208),
		new Vector2 (1536, 2048),
		new Vector2 (2048, 2732),
	};
	#endif

	// Method called by Unity after that the GameObject has been instantiated
	private void Start ()
	{
		// Initialize the rendering
		RenderInitialize ();

		// Initialize the game logic
		LogicInitialize ();
	}

	// Method called by Unity every fixed framerate frame
	private void FixedUpdate ()
	{
		// Update the game logic (unless a screenshot is in progress)
		#if UNITY_EDITOR
		if (screenshotTimer == 0)
		#endif
		LogicUpdate ();
	}

	// Method called by Unity when the player pauses/resumes
	private void OnApplicationPause (bool pause)
	{
		// Inform the game logic
		LogicPause (pause);
	}

	// Method called by Unity before any camera starts rendering
	private void OnPreRender ()
	{
		// Check whether the screenshot trigger is set
		#if UNITY_EDITOR
		if (screenshotTrigger) {

			// Prepare to take screenshots
			screenshotTrigger = false;
			screenshotTimer = 3;
			screenshotIndex = 0;

			// Force the rendering scale factor to ensure consistent results
			// Note: it is actually important to have a scale different from 1 (or make sure that TAA is enabled), to
			// guarantee that the game is rendered to a texture, allowing the screenshot system to work properly
			frameTimer = float.MaxValue;
			renderScaleTarget = 1.414f;
		}

		// Monitor possible changes of the rendering size
		if (screenshotTimer == 3) {
			renderSizeTarget = screenshotSize [screenshotIndex];
		} else if (screenshotTimer == 0)
		#endif
		renderSizeTarget = new Vector2 (Screen.width, Screen.height);

		// Update the rendering
		RenderUpdate ();

		// Update all the sprites
		SpriteUpdate (Time.time);
	}

	// Method called by Unity after a camera finished rendering the scene
	#if UNITY_EDITOR
	private void OnPostRender ()
	{
		// Check whether it is time to take a screenshot
		if (screenshotTimer > 0 && --screenshotTimer == 0) {

			// Take a screenshot
			int screenshotWidth = (int) renderSizeTarget.x;
			int screenshotHeight = (int) renderSizeTarget.y;
			RenderTexture renderTexture = new RenderTexture (screenshotWidth, screenshotHeight, 0);
			RenderFullScreen (renderTexture);
			Texture2D texture = new Texture2D (screenshotWidth, screenshotHeight, TextureFormat.RGB24, false);
			texture.ReadPixels (new Rect (0, 0, screenshotWidth, screenshotHeight), 0, 0);
			System.IO.File.WriteAllBytes (string.Format ("Screenshots/{0}.{1}x{2}.{3}.png", UnityEditor.PlayerSettings.productName, screenshotWidth, screenshotHeight, System.DateTime.Now.ToString ("yyMMdd-HHmmss")), texture.EncodeToPNG ());

			// Make sure to take a screenshot for each defined resolution
			if (++screenshotIndex < screenshotSize.Length) {
				screenshotTimer = 3;
			}
		}
	}
	#endif
}
