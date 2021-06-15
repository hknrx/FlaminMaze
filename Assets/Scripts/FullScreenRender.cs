// Nicolas Robert [Nrx]

using UnityEngine;

public class FullScreenRender : MonoBehaviour
{
	// Reference to the instance that handles the rendering
	public Game game;

	// Method called by Unity after a camera finished rendering the scene
	private void OnPostRender ()
	{
		game.RenderFullScreen ();
	}
}
