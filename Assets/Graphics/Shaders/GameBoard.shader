// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragLocal = coordinates of the fragment in the quad space (from -1 to 1 on the Y axis)
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

Shader "Custom/GameBoard" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
		[HideInInspector] borderThickness ("Border thickness", Float) = 1.0
		[HideInInspector] ledHalfThickness ("LED half thickness", Float) = 0.0
		[HideInInspector] ledBrightness ("LED brightness", Float) = 0.0
		[HideInInspector] dataBlock ("Data (blocks)", Vector) = (0.0, 0.0, 0.0, 0.0)
		[HideInInspector] dataLedH ("Data (horizontal LEDs)", Vector) = (0.0, 0.0, 0.0, 0.0)
		[HideInInspector] dataLedV ("Data (vertical LEDs)", Vector) = (0.0, 0.0, 0.0, 0.0)
		blockHue ("Block hue", Float) = 0.0
		ledHue ("LED hue", Float) = 0.0
	}
	SubShader {
		Tags {"Queue" = "Geometry"}
		Pass {
			Lighting Off
			Fog {Mode Off}
			Cull Off
			ZWrite On
			ZTest LEqual
			Blend SrcAlpha OneMinusSrcAlpha

			GLSLPROGRAM

			// Define multiple shader program variants:
			// - (default): process all fragments
			// - TRANSPARENT_CUTOUT: discard fragments which the alpha is lower than 0.5
			#pragma multi_compile __ TRANSPARENT_CUTOUT

			// Vertex shader: begin
			#ifdef VERTEX

				// Variables shared between the OpenGL ES environment and vertex shader
				uniform vec4 quadInfo;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragLocal;
				varying vec2 fragScreen;

				// Main function
				void main () {

					// Set the coordinates
					vec2 fragTexture = gl_MultiTexCoord0.st;
					fragLocal = (2.0 * fragTexture - 1.0) * vec2 (quadInfo.x, 1.0);
					fragScreen = fragLocal * quadInfo.y + quadInfo.zw;

					// Set the vertex position
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				}

			// Vertex shader: end
			#endif

			// Fragment shader: begin
			#ifdef FRAGMENT

				// Constants (global)
				#define PI 3.14159265359
				#define BORDER_VALUE 0.4
				#define REFLECTION_SCALE 12.0
				#define SMOOTH_DISTANCE 0.01
				#define SMOOTH_DISTANCE_OUTSIDE 0.05

				// Parameters (specific)
				#define LED_HALF_GAP 0.02

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform vec4 quadInfo;
				uniform float time;
				uniform float borderThickness;
				uniform float ledHalfThickness;
				uniform float ledBrightness;
				uniform vec4 dataBlock;
				uniform vec4 dataLedH;
				uniform vec4 dataLedV;
				uniform float blockHue;
				uniform float ledHue;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragLocal;
				varying vec2 fragScreen;

				// HSV to RGB
				vec3 hsv2rgb (in vec3 hsv) {
					hsv.yz = clamp (hsv.yz, 0.0, 1.0);
					return hsv.z * (1.0 + hsv.y * clamp (abs (fract (hsv.x + vec3 (0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 2.0, -1.0, 0.0));
				}

				// PRNG
				float rand (in vec2 seed) {
					return fract (sin (dot (seed, vec2 (12.9898, 78.233))) * 137.5453);
				}

				// Distance to a signed rounded box
				float boxDistSigned (in vec2 frag, in vec3 box) {
					frag = abs (frag) - box.xy + box.z;
					return length (max (frag, 0.0)) + min (max (frag.x, frag.y), 0.0) - box.z;
				}

				// Distance to a horizontal lozenge shape
				float ledDist (in vec2 frag, in float block, in float blockMax) {
					if (abs (block - blockMax + 0.5) > blockMax) {
						return 1.0;
					}
					return max (frag.x + ledHalfThickness + LED_HALF_GAP, 0.5) - frag.y;
				}

				// Main function
				void main () {

					// Define the geometry of the game board
					vec3 gameBoardHalfSize = vec3 (3.5, 5.0, ledHalfThickness);
					float gameBoardBorder = 2.0 * borderThickness / quadInfo.y;
					float gameBoardScale = (gameBoardHalfSize.y + ledHalfThickness) / (1.0 - gameBoardBorder);
					vec2 gameBoardFrag = fragLocal * gameBoardScale;
					gameBoardBorder *= gameBoardScale;
					float gameBoardDist = boxDistSigned (gameBoardFrag, gameBoardHalfSize + ledHalfThickness + gameBoardBorder);
					float alpha = smoothstep (0.0, -SMOOTH_DISTANCE_OUTSIDE, gameBoardDist);

					// Transparency Cutout
					#ifdef TRANSPARENT_CUTOUT
					if (alpha < 0.5) {
						discard;
					}
					#endif

					// Blocks
					gameBoardFrag += gameBoardHalfSize.xy;
					vec2 blockCoord = floor (gameBoardFrag);
					vec2 blockFrag = abs (fract (gameBoardFrag) - 0.5);
					float blockArea = floor (blockCoord.y / 3.0);
					float blockData = dot (dataBlock, floor (mod (vec4 (1, 2, 4, 8) / exp2 (blockArea), 2.0)));
					float blockId = blockCoord.x + 7.0 * mod (blockCoord.y, 3.0);
					float blockPhase = PI * rand (blockCoord);
					float blockState = floor (mod (blockData / exp2 (blockId), 2.0));
					float blockHueChange = blockState * 0.02 * cos (time + blockPhase);
					float blockValue = 0.2 + 0.8 * blockState * (0.8 + 0.2 * cos (2.5 * time + blockPhase));
					blockValue *= 0.1 + 0.9 * smoothstep (1.0, 0.0, length (blockFrag));
					blockValue *= smoothstep (0.0, SMOOTH_DISTANCE, 0.5 - ledHalfThickness - max (blockFrag.x, blockFrag.y));

					// LEDs
					float ledDistH = ledDist (blockFrag.xy, blockCoord.x, gameBoardHalfSize.x);
					float ledDistV = ledDist (blockFrag.yx, blockCoord.y, gameBoardHalfSize.y);
					float ledDist;
					float ledCoord;
					float ledData;
					float ledId;
					if (ledDistH < ledDistV) {
						ledDist = ledDistH;
						ledCoord = floor (gameBoardFrag.y + 0.5);
						float ledArea = floor (ledCoord / 3.0);
						ledData = dot (dataLedH, floor (mod (vec4 (1, 2, 4, 8) / exp2 (ledArea), 2.0)));
						ledId = blockCoord.x + 7.0 * mod (ledCoord, 3.0);
					} else {
						ledDist = ledDistV;
						ledCoord = floor (gameBoardFrag.x + 0.5);
						ledData = dot (dataLedV, floor (mod (vec4 (1, 2, 4, 8) / exp2 (blockArea), 2.0)));
						ledId = ledCoord + 8.0 * mod (blockCoord.y, 3.0);
					}
					float ledState = floor (mod (ledData / exp2 (ledId), 2.0)) * ledBrightness;
					float ledHueChange = ledState * 0.04 * cos (time + PI * rand (vec2 (ledCoord, ledId)));
					float ledSaturation = 1.0 - 0.5 * ledState * smoothstep (ledHalfThickness, 0.0, ledDist);
					float ledValue = 0.1 + 0.9 * ledState;
					ledValue *= smoothstep (0.0, SMOOTH_DISTANCE, ledHalfThickness - ledDist);

					// Define the color
					vec3 color = hsv2rgb (vec3 (blockHue + blockHueChange, 1.0, blockValue));
					color += hsv2rgb (vec3 (ledHue + ledHueChange, ledSaturation, ledValue));

					// Add the border
					color *= smoothstep (0.0, -SMOOTH_DISTANCE, gameBoardDist + gameBoardBorder);
					color += BORDER_VALUE * smoothstep (gameBoardBorder * 0.5, gameBoardBorder * 0.1, abs (gameBoardDist + gameBoardBorder * 0.5));

					// Screen reflection
					color *= 1.1 + 0.3 * cos (REFLECTION_SCALE * (fragScreen.x + fragScreen.y) + time);

					// Set the fragment color
					gl_FragColor = vec4 (color, alpha);
				}

			// Fragment shader: end
			#endif

			ENDGLSL
		}
	}
}
