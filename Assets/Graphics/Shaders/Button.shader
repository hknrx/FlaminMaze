// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragLocal = coordinates of the fragment in the quad space (from -1 to 1 on the Y axis)
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

Shader "Custom/Button" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
		[HideInInspector] borderThickness ("Border thickness", Float) = 1.0
		[HideInInspector] stateEnabled ("State enabled", Float) = 0.0
		[HideInInspector] statePushed ("State pushed", Float) = 0.0
		labelTexture ("Label texture", 2D) = "white" {}
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

				// Constants (global)
				#define PI 3.14159265359
				#define BORDER_VALUE 0.4
				#define REFLECTION_SCALE 12.0
				#define SMOOTH_DISTANCE 0.01
				#define SMOOTH_DISTANCE_OUTSIDE 0.05

				// Parameters (specific)
				#define PUSH_RATIO 0.15
				#define LABEL_SCALE 0.75
				#define BLOCK_SCALE 24.0

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform vec4 quadInfo;
				uniform float time;
				uniform float borderThickness;
				uniform float stateEnabled;
				uniform float statePushed;
				uniform sampler2D labelTexture;
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

					// Define the geometry of the button
					float buttonBorder = 2.0 * borderThickness / quadInfo.y;
					float buttonRadius = 1.0 - buttonBorder;
					float buttonScale = 1.0 - PUSH_RATIO * statePushed;
					float buttonDist = length (vec2 (max (abs (fragLocal.x) - max (0.0, quadInfo.x - 1.0), 0.0), fragLocal.y));
					float alpha = smoothstep (1.0, 1.0 - SMOOTH_DISTANCE_OUTSIDE, buttonDist);

					// Transparency Cutout
					#ifdef TRANSPARENT_CUTOUT
					if (alpha < 0.5) {
						discard;
					}
					#endif

					// Prepare the label
					vec2 labelCoord = 0.5 * fragLocal / (LABEL_SCALE * buttonScale * quadInfo.x) + 0.5;
					float labelValue = texture2D (labelTexture, labelCoord).r;
					labelValue *= 0.1 + 0.9 * stateEnabled;

					// Prepare the background
					vec2 blockCoord = BLOCK_SCALE * fragLocal * quadInfo.y / buttonScale;
					float blockValue = 0.9 + 0.1 * cos (2.0 * time + blockCoord.x - blockCoord.y + PI * (floor (blockCoord.x) + floor (blockCoord.y)));
					blockValue *= 0.4 + 0.6 * smoothstep (buttonRadius, buttonRadius * 0.5, buttonDist / buttonScale);
					blockValue *= 0.2 + 0.6 * stateEnabled;

					// Define the button
					vec3 color = hsv2rgb (vec3 (hue, 0.8 * (1.0 - labelValue), labelValue + blockValue));

					// Add the border
					color *= smoothstep (buttonRadius, buttonRadius - SMOOTH_DISTANCE, buttonDist);
					color += BORDER_VALUE * smoothstep (buttonBorder * 0.5, buttonBorder * 0.1, abs (1.0 - buttonBorder * 0.5 - buttonDist));

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
