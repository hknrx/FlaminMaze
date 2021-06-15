// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragLocal = coordinates of the fragment in the quad space (from -1 to 1 on the Y axis)
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

Shader "Custom/LED" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
		[HideInInspector] ledRatio ("LED ratio", Float) = 0.0
		[HideInInspector] stateEnabled ("State enabled", Float) = 0.0
		[HideInInspector] displayedNumber ("Displayed number", Int) = 0
		hue ("Hue", Float) = 0.0
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
				#define REFLECTION_SCALE 12.0
				#define SMOOTH_DISTANCE 0.01
				#define SMOOTH_DISTANCE_OUTSIDE 0.05

				// Parameters (specific)
				#define LED_DIGIT_GAP 0.1
				#define LED_SEGMENT_HALF_GAP 0.02
				#define LED_SEGMENT_HALF_THICKNESS 0.14

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform vec4 quadInfo;
				uniform float time;
				uniform float ledRatio;
				uniform float stateEnabled;
				uniform float displayedNumber;
				uniform float hue;

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

				// Distance to a horizontal lozenge shape (LED segment)
				void ledSegDist (in vec2 frag, in vec3 segment, inout float distMin, in float digit, in float mask, inout float state, inout float maskSelected) {
					frag -= vec2 (clamp (frag.x, segment.x + LED_SEGMENT_HALF_THICKNESS + LED_SEGMENT_HALF_GAP, segment.y - LED_SEGMENT_HALF_THICKNESS - LED_SEGMENT_HALF_GAP), segment.z);
					float dist = abs (frag.x) + abs (frag.y);
					if (dist < distMin) {
						distMin = dist;
						state = fract (mask / digit);
						maskSelected = mask;
					}
				}

				// Main function
				void main () {

					// Extract the digit
					float ledWidth = 2.0 * ledRatio;
					float ledIndex = fragLocal.x - quadInfo.x;
					vec2 fragLED = vec2 (mod (ledIndex, ledWidth) - ledRatio, fragLocal.y);

					ledIndex = -ceil (ledIndex / ledWidth);
					float ledValue = floor (abs (displayedNumber) / pow (10.0, ledIndex));
					ledValue = ledValue > 0.0 || ledIndex == 0.0 ? mod (ledValue, 10.0) : 10.0;
					ledValue = exp2 (1.0 + ledValue);

					// Compute the distance to the nearest LED segment
					float top = 1.0 - LED_SEGMENT_HALF_THICKNESS;
					float bottom = -top;
					float right = ledRatio - LED_DIGIT_GAP * 0.5 - LED_SEGMENT_HALF_THICKNESS;
					float left = -right;

					float ledDist = 1.0;
					float ledState = 0.0;
					float maskSelected = 0.0;
					ledSegDist (fragLED, vec3 (left, right, top), ledDist, ledValue, 1005.0, ledState, maskSelected);
					ledSegDist (fragLED, vec3 (left, right, 0.0), ledDist, ledValue, 892.0, ledState, maskSelected);
					ledSegDist (fragLED, vec3 (left, right, bottom), ledDist, ledValue, 877.0, ledState, maskSelected);
					ledSegDist (fragLED.yx, vec3 (0.0, top, left), ledDist, ledValue, 881.0, ledState, maskSelected);
					ledSegDist (fragLED.yx, vec3 (bottom, 0.0, left), ledDist, ledValue, 325.0, ledState, maskSelected);
					ledSegDist (fragLED.yx, vec3 (0.0, top, right), ledDist, ledValue, 927.0, ledState, maskSelected);
					ledSegDist (fragLED.yx, vec3 (bottom, 0.0, right), ledDist, ledValue, 1019.0, ledState, maskSelected);
					ledState = step (0.5, ledState) * stateEnabled;

					// Compute the alpha value
					float alpha = smoothstep (0.0, SMOOTH_DISTANCE_OUTSIDE, LED_SEGMENT_HALF_THICKNESS * (0.8 + 0.2 * ledState) - ledDist);

					// Transparency Cutout
					#ifdef TRANSPARENT_CUTOUT
					if (alpha < 0.5) {
						discard;
					}
					#endif

					// Define the LED segment color
					float ledHue = hue + ledState * 0.04 * cos (time + PI * rand (vec2 (ledIndex, maskSelected)));
					float ledSaturation = 1.0 - ledState * smoothstep (LED_SEGMENT_HALF_THICKNESS, 0.0, ledDist);
					ledValue = 0.2 + 0.8 * ledState;
					vec3 color = hsv2rgb (vec3 (ledHue, ledSaturation, ledValue));

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
