// Nicolas Robert [Nrx]

Shader "Custom/Mix" {
	Properties {
		[HideInInspector] _MainTex ("Main texture", 2D) = "white" {}
		[HideInInspector] _MainTexResolution ("Main texture resolution", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] _OffsettedTex ("Offsetted texture", 2D) = "white" {}
		[HideInInspector] mixRatio ("mixRatio", Float) = 0.5
	}
	SubShader {
		Pass {
			Lighting Off
			Fog {Mode Off}
			Cull Off
			ZWrite Off
			ZTest Always
			Blend Off

			GLSLPROGRAM

			// Vertex shader: begin
			#ifdef VERTEX

				// Variables shared between the OpenGL ES environment and vertex shader
				uniform vec2 _MainTexResolution;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 uv0;
				varying vec2 uv1;

				// Main function
				void main () {

					// Set the texture coordinates
					uv0 = gl_MultiTexCoord0.st;
					uv1 = (uv0 * _MainTexResolution + 0.5) / (_MainTexResolution + 1.0);

					// Set the vertex position
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				}

			// Vertex shader: end
			#endif

			// Fragment shader: begin
			#ifdef FRAGMENT

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform sampler2D _MainTex;
				uniform sampler2D _OffsettedTex;
				uniform float mixRatio;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 uv0;
				varying vec2 uv1;

				// Main function
				void main () {

					// Set the fragment color
					gl_FragColor = mix (texture2D (_MainTex, uv0), texture2D (_OffsettedTex, uv1), mixRatio);
				}

			// Fragment shader: end
			#endif

			ENDGLSL
		}
	}
}
