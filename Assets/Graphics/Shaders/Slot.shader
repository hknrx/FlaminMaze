// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragLocal = coordinates of the fragment in the quad space (from -1 to 1 on the Y axis)
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

// Notes:
// - labelRatio = labelTexture.width / labelTexture.height
// - Font used: Roboto, 64px, Gaussian blur (radius: 8, method: RLE)
// - ONE_DOLLAR: 138x76

Shader "Custom/Slot" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
		[HideInInspector] borderThickness ("Border thickness", Float) = 1.0
		[HideInInspector] stateEnabled ("State enabled", Float) = 0.0
		[HideInInspector] stateOpened ("State opened", Float) = 0.0
		labelTexture ("Label texture", 2D) = "white" {}
		labelRatio ("Label ratio", Float) = 1.0
		hue ("Hue", Float) = 0.0
	}
	SubShader {
		Tags {"Queue" = "Geometry+2"}
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

				// Constants & parameters (global)
				#define PI 3.14159265359
				#define BORDER_VALUE 0.4
				#define REFLECTION_SCALE 12.0
				#define SMOOTH_DISTANCE 0.01
				#define SMOOTH_DISTANCE_OUTSIDE 0.05

				// Constants & parameters (specific)
				#define LABEL_SCALE 0.6
				#define LABEL_POSITION vec2 (-0.03, 0.0)
				#define HOLE_HALF_THICKNESS 0.1

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform vec4 quadInfo;
				uniform float time;
				uniform float borderThickness;
				uniform float stateEnabled;
				uniform float stateOpened;
				uniform sampler2D labelTexture;
				uniform float labelRatio;
				uniform float hue;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragLocal;
				varying vec2 fragScreen;

				// HSV to RGB
				vec3 hsv2rgb (in vec3 hsv) {
					hsv.yz = clamp (hsv.yz, 0.0, 1.0);
					return hsv.z * (1.0 + hsv.y * clamp (abs (fract (hsv.x + vec3 (0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 2.0, -1.0, 0.0));
				}

				// Main function
				void main () {

					// Define the geometry of the coin slot
					float slotBorder = 2.0 * borderThickness / quadInfo.y;
					float slotRadius = 1.0 - slotBorder;
					float slotDist = length (fragLocal);
					vec3 color = vec3 (0.3) * (0.3 + 0.7 * cos (PI * 0.5 * slotDist)) ;
					float alpha = smoothstep (1.0, 1.0 - SMOOTH_DISTANCE_OUTSIDE, slotDist);

					// Transparency Cutout
					#ifdef TRANSPARENT_CUTOUT
					if (alpha < 0.5) {
						discard;
					}
					#endif

					// Add the label
					vec2 labelCoord = 0.5 * (fragLocal - LABEL_POSITION) / LABEL_SCALE;
					labelCoord.y *= labelRatio;
					labelCoord += 0.5;
					float labelValue = texture2D (labelTexture, labelCoord).r;
					float labelMask = smoothstep (0.0, 0.2, labelValue);
					labelValue *= 0.1 + 0.9 * stateEnabled * stateOpened;
					color = mix (color, hsv2rgb (vec3 (hue, 0.8 * (1.0 - labelValue), 0.2 + 0.8 * labelValue)), labelMask);

					// Add the border
					color *= smoothstep (slotRadius, slotRadius - SMOOTH_DISTANCE, slotDist);
					color += BORDER_VALUE * smoothstep (slotBorder * 0.5, slotBorder * 0.1, abs (1.0 - slotBorder * 0.5 - slotDist));

					// Add the actual slot
					float holeDist = length (vec2 (fragLocal.x, max (abs (fragLocal.y) - 0.5, 0.0)));
					float holeMask = smoothstep (HOLE_HALF_THICKNESS - SMOOTH_DISTANCE, HOLE_HALF_THICKNESS, holeDist);
					float holeState = smoothstep (1.0 + SMOOTH_DISTANCE, 1.0, fragLocal.x / HOLE_HALF_THICKNESS + 2.0 * stateOpened);
					color *= mix (holeMask, 0.8 + 0.2 * holeMask, holeState);

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
