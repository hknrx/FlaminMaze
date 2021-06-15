// Nicolas Robert [Nrx]

// Notes:
// - quadInfo.x = quad.width / quad.height
// - quadInfo.y = quad.height / screen.height
// - quadInfo.zw = position of the quad (center point) in the screen space = (2 * Qc - screen.size) / screen.height
// - fragScreen = coordinates of the fragment in the screen space (assuming that the screen height goes from -1 to 1)

Shader "Custom/Background" {
	Properties {
		[HideInInspector] quadInfo ("Quad information", Vector) = (1.0, 1.0, 0.0, 0.0)
		[HideInInspector] time ("Time", Float) = 0.0
	}
	SubShader {
		Tags {"Queue" = "Background"}
		Pass {
			Lighting Off
			Fog {Mode Off}
			Cull Off
			ZWrite Off
			ZTest LEqual
			Blend Off

			GLSLPROGRAM

			// Define multiple shader program variants (1 per scene)
			#pragma multi_compile __ SCENE_BIT0
			#pragma multi_compile __ SCENE_BIT1
			#pragma multi_compile __ SCENE_BIT2

			// Vertex shader: begin
			#ifdef VERTEX

				// Variables shared between the OpenGL ES environment and vertex shader
				uniform vec4 quadInfo;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragScreen;

				// Main function
				void main () {

					// Set the coordinates
					vec2 fragTexture = gl_MultiTexCoord0.st;
					fragScreen = (2.0 * fragTexture - 1.0) * vec2 (quadInfo.x, 1.0) * quadInfo.y + quadInfo.zw;

					// Set the vertex position
					gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
				}

			// Vertex shader: end
			#endif

			// Fragment shader: begin
			#ifdef FRAGMENT

				// Constants
				#define PI		3.14159265359
				#define SQRT2	1.41421356237

				// Variables shared between the OpenGL ES environment and the fragment shader
				uniform float time;

				// Variables shared between the vertex shader and the fragment shader
				varying vec2 fragScreen;

				// PRNG
				float rand (in vec2 seed) {
					return fract (sin (dot (seed, vec2 (12.9898, 78.233))) * 137.5453);
				}

				// HSV to RGB
				vec3 hsv2rgb (in vec3 hsv) {
					hsv.yz = clamp (hsv.yz, 0.0, 1.0);
					return hsv.z * (1.0 + hsv.y * clamp (abs (fract (hsv.x + vec3 (0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 2.0, -1.0, 0.0));
				}

/////////////////////////////
// SCENE 0 - "Psycho dots" //
/////////////////////////////
#if !defined (SCENE_BIT0) && !defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define PIXEL_SIZE 40.0

				// Main function
				void main () {

					// Prepare the pixelisation
					vec2 fragPixel = fragScreen * PIXEL_SIZE;
					vec2 pixelCoord = (floor (fragPixel) + 0.5) / PIXEL_SIZE;
					fragPixel = fract (fragPixel) - 0.5;

					// Background
					float radius = max (length (pixelCoord), 0.3);
					pixelCoord *= 1.0 + 0.1 * cos (radius * 3.0 - time * 7.0) / radius;
					float light = smoothstep (-0.7, 0.7, cos (time * 0.4));
					vec3 colorBackground = hsv2rgb (vec3 (radius * 0.4 - time * 1.5, 0.7, 0.8 * light));

					// Shapes
					float angle = 2.0 * PI * cos (0.5 * PI * cos (time * 0.2));
					float c = cos (angle);
					float s = sin (angle);
					pixelCoord = (2.75 + 0.75 * cos (time)) * mat2 (c, s, -s, c) * pixelCoord;

					float random = 2.0 * PI * rand (floor (pixelCoord));
					pixelCoord = fract (pixelCoord) - 0.5;
					angle = atan (pixelCoord.y, pixelCoord.x) + 0.25 * PI * cos (random + time * 5.0);
					radius = length (pixelCoord);
					radius *= 1.0 + (0.3 + 0.3 * cos (angle * 5.0)) * smoothstep (-0.5, 0.5, cos (random + time * 2.0));

					vec3 colorShape = hsv2rgb (vec3 (radius * 0.6 + random - time, 0.7, 0.8 * (1.0 - light)));

					// Mix
					float display = smoothstep (0.5, 0.4, radius);
					display *= smoothstep (-0.5, 0.5, cos (random + time * 1.5));
					vec3 color = vec3 (mix (colorBackground, colorShape, display));

					// Pixelisation
					color *= 1.0 - dot (fragPixel, fragPixel);

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

////////////////////////////////////
// SCENE 1 - "Light dot cylinder" //
////////////////////////////////////
#elif defined (SCENE_BIT0) && !defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define CAMERA_FOCAL_LENGTH	1.2
				#define DOT_COUNT			100.0

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 ray = vec3 (fragScreen, CAMERA_FOCAL_LENGTH);

					// Simulate the music info
					float soundBass = 0.6 + 0.4 * cos (time * 0.2);
					float soundTreble = 0.5 + 0.5 * cos (time * 1.2);

					// Define the number of rows
					float dotRowCount = floor (20.0 + 60.0 * soundTreble * soundBass) * 2.0;

					// Compute the orientation of the camera
					float yawAngle = cos (time * 2.0);
					float pitchAngle = 2.0 * PI * cos (time * 0.2 + soundTreble * 0.4);

					float cosYaw = cos (yawAngle);
					float sinYaw = sin (yawAngle);
					float cosPitch = cos (pitchAngle);
					float sinPitch = sin (pitchAngle);

					mat3 cameraOrientation;
					cameraOrientation [0] = vec3 (cosYaw, 0.0, -sinYaw);
					cameraOrientation [1] = vec3 (sinYaw * sinPitch, cosPitch, cosYaw * sinPitch);
					cameraOrientation [2] = vec3 (sinYaw * cosPitch, -sinPitch, cosYaw * cosPitch);

					ray = cameraOrientation * ray;

					// Compute the position of the camera
					float cameraDist = -3.0 * (cos (time * 0.3) * cos (time * 0.7) + soundBass);
					vec3 cameraPosition = cameraOrientation [2] * cameraDist;

					// Compute the intersection point (ray / cylinder)
					float a = dot (ray.xz, ray.xz);
					float b = dot (cameraPosition.xz, ray.xz);
					float c = dot (cameraPosition.xz, cameraPosition.xz) - 1.0;
					float ok = 1.0 - step (0.0, b) * step (0.0, c);
					c = sqrt (b * b - a * c);
					vec3 hit;
					if (b < -c) {
						hit = cameraPosition - ray * (b + c) / a;
						if (abs (hit.y * DOT_COUNT / PI + 1.0) > dotRowCount) {
							hit = cameraPosition - ray * (b - c) / a;
						}
					} else {
						hit = cameraPosition - ray * (b - c) / a;
					}
					vec2 frag = vec2 ((atan (hit.z, hit.x) + PI) * DOT_COUNT, hit.y * DOT_COUNT + PI) / (2.0 * PI);

					// Compute the fragment color
					vec2 id = floor (frag);
					float random = rand (id);
					vec3 color = hsv2rgb (vec3 (time * 0.05 + id.y * 0.005, 1.0, 1.0));
					color += 0.5 * cos (random * vec3 (1.0, 2.0, 3.0));
					color *= smoothstep (0.5, 0.1, length (fract (frag) - 0.5));
					color *= 0.5 + 1.5 * step (0.9, cos (random * time * 5.0));
					color *= 0.5 + 0.5 * cos (random * time + PI * 0.5 * soundTreble);
					color *= smoothstep (dotRowCount, 0.0, (abs (id.y + 0.5) - 1.0) * 2.0);
					color *= ok;

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

/////////////////////////
// SCENE 2 - "Dragoon" //
/////////////////////////
#elif !defined (SCENE_BIT0) && defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define DELTA			0.01
				#define RAY_LENGTH_MAX	300.0
				#define RAY_STEP_MAX	100

				float fixDistance (in float d, in float correction, in float k) {
					return min (d, max ((d - DELTA) * k + DELTA, d - correction));
				}

				float getDistance (in vec3 p) {
					p += vec3 (3.0 * sin (p.z * 0.2 + time * 2.0), sin (p.z * 0.3 + time), 0.0);
					return fixDistance (length (p.xy) - 4.0 + 0.8 * sin (abs (p.x * p.y) + p.z * 4.0) * sin (p.z), 2.5, 0.2);
				}

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 direction = normalize (vec3 (fragScreen, 2.0));

					// Set the camera
					vec3 origin = vec3 ((17.0 + 5.0 * sin (time)) * cos (time * 0.2), 12.0 * sin (time * 0.2), 0.0);
					vec3 forward = vec3 (-origin.x, -origin.y, 22.0 + 6.0 * cos (time * 0.2));
					vec3 up = vec3 (0.0, 1.0, 0.0);
					mat3 rotation;
					rotation [2] = normalize (forward);
					rotation [0] = normalize (cross (up, forward));
					rotation [1] = cross (rotation [2], rotation [0]);
					direction = rotation * direction;

					// Ray marching
					vec3 p = origin;
					float dist = RAY_LENGTH_MAX;
					float rayLength = 0.0;
					float stepCount = 0.0;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {
						dist = getDistance (p);
						rayLength += dist;
						if (dist < DELTA || rayLength > RAY_LENGTH_MAX) {
							break;
						}
						p = origin + direction * rayLength;
						++stepCount;
					}

					// Compute the fragment color
					vec3 color = vec3 (1.0, 0.5, 0.0) * 2.0 * stepCount / float (RAY_STEP_MAX);
					vec3 LIGHT = normalize (vec3 (1.0, -3.0, -1.0));
					if (dist < DELTA) {
						vec2 h = vec2 (DELTA, -DELTA);
						vec3 normal = normalize (
							h.xxx * getDistance (p + h.xxx) +
							h.xyy * getDistance (p + h.xyy) +
							h.yxy * getDistance (p + h.yxy) +
							h.yyx * getDistance (p + h.yyx));
						color.rg += 0.5 * max (0.0, dot (normal, LIGHT));
					}
					else {
						color.b += 0.1 + 0.5 * max (0.0, dot (-direction, LIGHT));
					}

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

//////////////////////////////
// SCENE 3 - "Voxel land 2" //
//////////////////////////////
#elif defined (SCENE_BIT0) && defined (SCENE_BIT1) && !defined (SCENE_BIT2)

				// Parameters
				#define CAMERA_FOCAL_LENGTH	1.5
				#define VOXEL_STEP			50

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 ray = normalize (vec3 (fragScreen, CAMERA_FOCAL_LENGTH));

					// Simulate the music info
					float soundBass = 0.6 + 0.4 * cos (time * 0.2);
					float soundTreble = 0.5 + 0.5 * cos (time * 1.2);

					// Set the camera
					vec3 origin = vec3 (0.0, 6.0 - 3.0 * cos (time * 0.3), time * 2.0 + 200.0 * (0.5 + 0.5 * sin (time * 0.1)));
					float cameraAngle = time * 0.1;
					vec3 cameraForward = vec3 (cos (cameraAngle), cos (time * 0.3) - 1.5, sin (cameraAngle));
					vec3 cameraUp = vec3 (0.2 * cos (time * 0.7), 1.0, 0.0);
					mat3 cameraRotation;
					cameraRotation [2] = normalize (cameraForward);
					cameraRotation [0] = normalize (cross (cameraUp, cameraForward));
					cameraRotation [1] = cross (cameraRotation [2], cameraRotation [0]);
					ray = cameraRotation * ray;

					// Voxel
					vec3 color = vec3 (0.0);

					vec2 voxelSign = sign (ray.xz);
					vec2 voxelIncrement = voxelSign / ray.xz;
					float voxelTimeCurrent = 0.0;
					vec2 voxelTimeNext = (0.5 + voxelSign * (0.5 - fract (origin.xz + 0.5))) * voxelIncrement;
					vec2 voxelPosition = floor (origin.xz + 0.5);
					float voxelHeight = 0.0;
					bool voxelDone = false;
					vec3 voxelNormal = vec3 (0.0);
					for (int voxelStep = 0; voxelStep < VOXEL_STEP; ++voxelStep) {

						// Compute the height of this column
						voxelHeight = 2.0 * rand (voxelPosition) * smoothstep (0.2, 0.5, soundBass) * sin (soundBass * 8.0 + voxelPosition.x * voxelPosition.y) - 5.0 * (0.5 + 0.5 * cos (voxelPosition.y * 0.15));

						// Check whether we hit the side of the column
						if (voxelDone = voxelHeight > origin.y + voxelTimeCurrent * ray.y) {
							break;
						}

						// Check whether we hit the top of the column
						float timeNext = min (voxelTimeNext.x, voxelTimeNext.y);
						float timeIntersect = (voxelHeight - origin.y) / ray.y;
						if (voxelDone = timeIntersect > voxelTimeCurrent && timeIntersect < timeNext) {
							voxelTimeCurrent = timeIntersect;
							voxelNormal = vec3 (0.0, 1.0, 0.0);
							break;
						}

						// Next voxel...
						voxelTimeCurrent = timeNext;
						voxelNormal.xz = step (voxelTimeNext.xy, voxelTimeNext.yx);
						voxelTimeNext += voxelNormal.xz * voxelIncrement;
						voxelPosition += voxelNormal.xz * voxelSign;
					}
					if (voxelDone) {
						origin += voxelTimeCurrent * ray;

						// Compute the local color
						vec3 mapping = origin;
						mapping.y -= voxelHeight + 0.5;
						mapping *= 1.0 - voxelNormal;
						mapping += 0.5;
						float id = rand (voxelPosition);
						color = hsv2rgb (vec3 ((time + floor (mapping.y)) * 0.05 + voxelPosition.x * 0.01, smoothstep (0.2, 0.4, soundBass), 0.7 + 0.3 * cos (id * time + PI * soundTreble)));
						color *= smoothstep (0.8 - 0.6 * cos (soundBass * PI), 0.1, length (fract (mapping) - 0.5));
						color *= 0.5 + smoothstep (0.90, 0.95, cos (id * 100.0 + soundTreble * PI * 0.5 + time * 0.5));
						color *= 1.0 - voxelTimeCurrent / float (VOXEL_STEP) * SQRT2;
					}

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

/////////////////////////////
// SCENE 4 - "Love Tunnel" //
/////////////////////////////
#elif !defined (SCENE_BIT0) && !defined (SCENE_BIT1) && defined (SCENE_BIT2)

				// Rendering parameters
				#define CAMERA_FOCAL_LENGTH	3.0
				#define RAY_STEP_MAX		50
				#define RAY_LENGTH_MAX		50.0
				#define LIGHT				vec3 (0.5, 0.0, -2.0)
				#define AMBIENT				0.5
				#define SPECULAR_POWER		4.0
				#define SPECULAR_INTENSITY	0.2
				#define FADE_POWER			3.0
				#define GAMMA				(1.0 / 2.2)
				#define DELTA				0.01

				// Rotation on the Z axis
				vec3 vRotateZ (in vec3 p, in float angle) {
					float c = cos (angle);
					float s = sin (angle);
					return vec3 (c * p.x + s * p.y, c * p.y - s * p.x, p.z);
				}

				// Fix the distance
				float fixDistance (in float d, in float correction, in float k) {
					correction = max (correction, 0.0);
					k = clamp (k, 0.0, 1.0);
					return min (d, max ((d - DELTA) * k + DELTA, d - correction));
				}

				// Distance to the scene
				float getDistance (in vec3 p, out vec3 q) {

					// Global deformation
					p += vec3 (2.0 * sin (p.z * 0.2 + time * 2.0), sin (p.z * 0.1 + time), 0.0);

					// Cylinder
					q = p;
					float d = 4.0 - length (q.xy) + 0.5 * sin (atan (q.y, q.x) * 6.0) * sin (q.z);
					d = fixDistance (d, 0.4, 0.8);

					// Twisted boxes
					vec3 q_ = vec3 (mod (p.xy, 5.0) - 0.5 * 5.0, mod (p.z, 12.0) - 0.5 * 12.0);
					q_ = vRotateZ (q_, time + q_.z);
					float d_ = length (max (abs (q_) - vec3 (0.6, 0.6, 1.5) + 0.4, 0.0)) - 0.4;
					d_ = fixDistance (d_, 0.4, 0.8);
					if (d_ < d) {
						q = q_;
						d = d_;
					}

					// Torus
					q_ = vec3 (p.xy, mod (p.z + 12.0, 24.0) - 0.5 * 24.0);
					d_ = length (vec2 (length (q_.xy) - 3.5, q_.z)) - 0.4;
					if (d_ < d) {
						q = q_;
						d = d_;
					}

					// Rotating spheres
					q_ = vRotateZ (q_, sin (time * 4.0));
					q_.xy = mod (q_.xy, 4.5) - 0.5 * 4.5;
					d_ = length (q_) - 0.5;
					if (d_ < d) {
						q = q_;
						d = d_;
					}

					// Final distance
					return d;
				}

				vec3 getNormal (in vec3 p) {
					vec2 h = vec2 (DELTA, -DELTA);
					vec3 q;
					return normalize (
						h.xxx * getDistance (p + h.xxx, q) +
						h.xyy * getDistance (p + h.xyy, q) +
						h.yxy * getDistance (p + h.yxy, q) +
						h.yyx * getDistance (p + h.yyx, q));
				}

				// Main function
				void main () {

					// Define the position and orientation of the camera
					vec3 rayOrigin = vec3 (0.0, 0.0, time * 6.0);
					vec3 cameraForward = vec3 (0.2 * cos (time), 0.2 * sin (time), 20.0 * cos (time * 0.1));
					vec3 cameraUp = vRotateZ (vec3 (0.0, 1.0, 0.0), PI * sin (time) * sin (time * 0.2));
					mat3 cameraOrientation;
					cameraOrientation [2] = normalize (cameraForward);
					cameraOrientation [0] = normalize (cross (cameraUp, cameraForward));
					cameraOrientation [1] = cross (cameraOrientation [2], cameraOrientation [0]);
					vec3 rayDirection = cameraOrientation * normalize (vec3 (fragScreen, CAMERA_FOCAL_LENGTH));

					// Ray marching
					vec3 p = rayOrigin;
					vec3 q;
					float rayLength = 0.0;
					float rayStepCounter = 0.0;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {
						float dist = getDistance (p, q);
						rayLength += dist;
						if (dist < DELTA || rayLength > RAY_LENGTH_MAX) {
							break;
						}
						p += dist * rayDirection;
						++rayStepCounter;
					}

					// Simulate the music info
					float soundBass = 0.2 + 0.6 * smoothstep (0.0, 1.0, cos (time));
					float soundTreble = 0.3 + 0.3 * cos (time * 1.2);

					// Compute the fragment color
					vec3 color;
					if (rayLength > RAY_LENGTH_MAX) {
						color = vec3 (0.0);
					} else {

						// Object color
						vec3 normal = getNormal (p);
						float hue = (p.z + time) * 0.1;
						float saturation = 0.8 + (0.2 + 0.8 * soundTreble) * 0.4 * sin (q.x * 10.0) * sin (q.y * 10.0) * sin (q.z * 10.0);
						float value = 1.0 - 0.8 * soundBass;
						color = hsv2rgb (vec3 (hue, saturation, value));

						// Lighting
						vec3 lightDirection = normalize (LIGHT);
						vec3 reflectDirection = reflect (rayDirection, normal);
						float diffuse = max (0.0, dot (normal, lightDirection));
						float specular = pow (max (0.0, dot (reflectDirection, lightDirection)), SPECULAR_POWER) * SPECULAR_INTENSITY;
						float fade = pow (1.0 - rayLength / RAY_LENGTH_MAX, FADE_POWER);
						color = ((AMBIENT + diffuse) * color + specular) * fade;

						// Special effect
						color *= max (1.0, 10.0 * sin (p.z * 0.1 - time * 4.0) - 7.0);

						// Gamma correction
						color = pow (color, vec3 (GAMMA));
					}

					// Another special effect
					color.r = mix (color.r, rayStepCounter * 2.0 / float (RAY_STEP_MAX), soundBass);

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

//////////////////////////////////
// SCENE 5 - "Glass Polyhedron" //
//////////////////////////////////
#elif defined (SCENE_BIT0) && !defined (SCENE_BIT1) && defined (SCENE_BIT2)

				// Rendering parameters
				#define RAY_LENGTH_MAX		120.0
				#define RAY_BOUNCE_MAX		4
				#define RAY_STEP_MAX		50
				#define ALPHA				0.6
				#define REFRACT_INDEX		1.55
				#define LIGHT				vec3 (-1.0, 0.5, 0.0)
				#define AMBIENT				0.2
				#define SPECULAR_POWER		3.0
				#define SPECULAR_INTENSITY	0.8
				#define GLOW_FACTOR			1.0
				#define DELTA				0.01

				// Rotation matrix
				mat3 rotate (in vec3 angle) {
					float c = cos (angle.x);
					float s = sin (angle.x);
					mat3 rx = mat3 (1.0, 0.0, 0.0, 0.0, c, s, 0.0, -s, c);

					c = cos (angle.y);
					s = sin (angle.y);
					mat3 ry = mat3 (c, 0.0, -s, 0.0, 1.0, 0.0, s, 0.0, c);

					c = cos (angle.z);
					s = sin (angle.z);
					mat3 rz = mat3 (c, s, 0.0, -s, c, 0.0, 0.0, 0.0, 1.0);

					return rz * ry * rx;
				}

				// Distance to the scene
				vec3 k;
				float getDistance (in vec3 p) {
					float repeat = 25.0;
					vec3 q = p + repeat * 0.5;
					k = floor (q / repeat);
					q -= repeat * (k + 0.5);
					p = rotate (k) * q;

					float top = p.y - 3.0;
					float angleStep = PI / max (2.0, abs (k.x + 2.0 * k.y + 4.0 * k.z));
					float angle = angleStep * (0.5 + floor (atan (p.x, p.z) / angleStep));
					float side = cos (angle) * p.z + sin (angle) * p.x - 2.0;
					float bottom = -p.y - 3.0;

					return max (top, max (side, bottom));
				}

				// Distance to the scene
				float getDistance (in vec3 p, in bool materialFrom, out bool materialTo) {

					float materialDist = getDistance (p);
					materialTo = materialDist < 0.0;
					return materialFrom ? -materialDist : materialDist;
				}

				// Normal at a given point
				vec3 getNormal (in vec3 p) {
					const vec2 h = vec2 (DELTA, -DELTA);
					return normalize (
						h.xxx * getDistance (p + h.xxx) +
						h.xyy * getDistance (p + h.xyy) +
						h.yxy * getDistance (p + h.yxy) +
						h.yyx * getDistance (p + h.yyx)
					);
				}

				// Color of the last probed point
				vec3 lightDirection = normalize (LIGHT);
				vec3 getColor (in vec3 direction, in vec3 normal) {
					vec3 color = max (sin (k * k), 0.2);
					float relfectionDiffuse = max (0.0, dot (normal, lightDirection));
					float relfectionSpecular = pow (max (0.0, dot (reflect (direction, normal), lightDirection)), SPECULAR_POWER) * SPECULAR_INTENSITY;
					return (AMBIENT + relfectionDiffuse) * color + relfectionSpecular;
				}

				// Color of the background
				vec3 getBackgroundColor (in vec3 direction) {
					return vec3 (0.1, 0.1, 0.5) * (0.2 + 0.8 * max (0.0, dot (-direction, lightDirection)));
				}

				// Main function
				void main () {

					// Define the ray corresponding to this fragment
					vec3 direction = normalize (vec3 (fragScreen, 2.0));

					// Set the camera
					vec3 origin = vec3 ((12.0 * cos (time * 0.1)), 10.0 * sin (time * 0.2), 12.0 * sin (time * 0.1));
					vec3 forward = -origin;
					vec3 up = vec3 (sin (time * 0.3), 2.0, 0.0);
					mat3 rotation;
					rotation [2] = normalize (forward);
					rotation [0] = normalize (cross (up, forward));
					rotation [1] = cross (rotation [2], rotation [0]);
					direction = rotation * direction;

					// Cast the initial ray
					bool materialTo = false;
					float rayLength = 0.0;
					float rayStepCount = 0.0;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {
						float dist = max (getDistance (origin, false, materialTo), DELTA);
						rayLength += dist;
						if (materialTo || rayLength > RAY_LENGTH_MAX) {
							break;
						}
						origin += direction * dist;
						++rayStepCount;
					}

					// Check whether we hit something
					float alpha = 1.0;
					vec3 color = vec3 (0.0);
					if (materialTo) {

						// The ray continues...
						bool materialFrom = false;
						float refractIndexFrom = 1.0;
						for (int rayBounce = 1; rayBounce < RAY_BOUNCE_MAX; ++rayBounce) {

							// Get the normal
							vec3 normal;
							if (!materialTo) {
								normal = -getNormal (origin);
							} else {
								normal = getNormal (origin);

								// Basic lighting
								color += getColor (direction, normal) * (1.0 - ALPHA) * alpha;
								alpha *= ALPHA;
							}

							// Interface with the material
							float refractIndexTo;
							vec3 refraction;
							if (materialTo) {
								refractIndexTo = REFRACT_INDEX;
								refraction = refract (direction, normal, refractIndexFrom / refractIndexTo);
							} else {
								refractIndexTo = 1.0;
								refraction = refract (direction, normal, refractIndexFrom);
							}
							if (dot (refraction, refraction) < DELTA) {
								direction = reflect (direction, normal);
								origin += direction * DELTA * 2.0;
							} else {
								direction = refraction;
								materialFrom = materialTo;
								refractIndexFrom = refractIndexTo;
							}

							// Ray marching
							for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {
								float dist = max (getDistance (origin, materialFrom, materialTo), DELTA);
								rayLength += dist;
								if (materialFrom != materialTo || rayLength > RAY_LENGTH_MAX) {
									break;
								}
								origin += direction * dist;
								++rayStepCount;
							}

							// Check whether we hit something
							if (materialFrom == materialTo) {
								break;
							}
						}
					}

					// Get the background color
					color += getBackgroundColor (direction) * alpha;

					// Glow effect
					color += GLOW_FACTOR * rayStepCount / float (RAY_STEP_MAX * RAY_BOUNCE_MAX);

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

/////////////////////////
// SCENE 6 - "VR Maze" //
/////////////////////////
#elif !defined (SCENE_BIT0) && defined (SCENE_BIT1) && defined (SCENE_BIT2)

				// Rendering parameters
				#define FOV					80.0
				#define RAY_STEP_MAX		20
				#define RAY_LENGTH_MAX		10.0
				#define EDGE_LENGTH			0.1
				#define BUMP_RESOLUTION		500.0
				#define BUMP_INTENSITY		0.3
				#define AMBIENT_NORMAL		0.2
				#define AMBIENT_HIGHLIGHT	2.5
				#define SPECULAR_POWER		2.0
				#define SPECULAR_INTENSITY	0.3
				#define FADE_POWER			1.5
				#define GAMMA				0.8
				#define DELTA				0.002

				// PRNG (unpredictable)
				float randUnpredictable (in vec3 seed) {
					seed = fract (seed * vec3 (5.6789, 5.4321, 6.7890));
					seed += dot (seed.yzx, seed.zxy + vec3 (21.0987, 12.3456, 15.1273));
					return fract (seed.x * seed.y * seed.z * 5.1337);
				}

				// PRNG (predictable)
				float randPredictable (in vec3 seed) {
					return fract (11.0 * sin (3.0 * seed.x + 5.0 * seed.y + 7.0 * seed.z));
				}

				// Check whether there is a block at a given voxel edge
				float block (in vec3 p, in vec3 n) {
					vec3 block = floor (p + 0.5 + n * 0.5);
					vec3 blockEven = mod (block, 2.0);
					float blockSum = blockEven.x + blockEven.y + blockEven.z;
					return max (step (blockSum, 1.5), step (blockSum, 2.5) * step (0.5, randPredictable (block))) *
						step (4.5, mod (block.x, 32.0)) *
						step (2.5, mod (block.y, 16.0)) *
						step (4.5, mod (block.z, 32.0));
				}

				// Cast a ray
				vec3 hit (in vec3 rayOrigin, in vec3 rayDirection, in float rayLengthMax, out float rayLength, out vec3 hitNormal) {

					// Launch the ray
					vec3 hitPosition = rayOrigin;
					vec3 raySign = sign (rayDirection);
					vec3 rayInv = 1.0 / rayDirection;
					vec3 rayLengthNext = (0.5 * raySign - fract (rayOrigin + 0.5) + 0.5) * rayInv;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {

						// Reach the edge of the voxel
						rayLength = min (rayLengthNext.x, min (rayLengthNext.y, rayLengthNext.z));
						hitNormal = step (rayLengthNext.xyz, rayLengthNext.yzx) * step (rayLengthNext.xyz, rayLengthNext.zxy) * raySign;
						hitPosition = rayOrigin + rayLength * rayDirection;

						// Check whether we hit a block
						if (block (hitPosition, hitNormal) > 0.5 || rayLength > rayLengthMax) {
							break;
						}

						// Next voxel
						rayLengthNext += hitNormal * rayInv;
					}

					// Return the hit point
					return hitPosition;
				}

				// Main function
				void main () {

					// Set the position of the head
					vec3 headPosition = vec3 (64.0 * cos (time * 0.1), 9.0 + 9.25 * cos (time * 0.5), 2.0 + 2.25 * cos (time));

					// Set the orientation of the head
					float yawAngle = time;
					float pitchAngle = time * 0.2;

					float cosYaw = cos (yawAngle);
					float sinYaw = sin (yawAngle);
					float cosPitch = cos (pitchAngle);
					float sinPitch = sin (pitchAngle);

					mat3 headRotate;
					headRotate [0] = vec3 (cosYaw, 0.0, -sinYaw);
					headRotate [1] = vec3 (sinYaw * sinPitch, cosPitch, cosYaw * sinPitch);
					headRotate [2] = vec3 (sinYaw * cosPitch, -sinPitch, cosYaw * cosPitch);

					// Set the position of the light
					vec3 lightPosition = headPosition + headRotate * vec3 (0.2 * sin (time * 2.0), 0.2 * sin (time * 3.0), 0.2 * sin (time) + 0.5);

					// Define the ray corresponding to this fragment
					vec3 rayOrigin = headPosition;
					vec3 rayDirection = headRotate * normalize (vec3 (fragScreen, 1.0 / tan (FOV * PI / 360.0)));

					// Cast a ray
					float hitDistance;
					vec3 hitNormal;
					vec3 hitPosition = hit (rayOrigin, rayDirection, RAY_LENGTH_MAX, hitDistance, hitNormal);
					vec3 hitUV = hitPosition * abs (hitNormal.yzx + hitNormal.zxy);

					// Basic edge detection
					vec3 edgeDistance = fract (hitUV + 0.5) - 0.5;
					vec3 edgeDirection = sign (edgeDistance);
					edgeDistance = abs (edgeDistance);

					vec3 hitNormalAbs = abs (hitNormal);
					vec2 edgeSmooth = vec2 (dot (edgeDistance, hitNormalAbs.yzx), dot (edgeDistance, hitNormalAbs.zxy));
					float highlightIntensity = (1.0 - block (hitPosition + edgeDirection * hitNormalAbs.yzx, hitNormal)) * smoothstep (0.5 - EDGE_LENGTH, 0.5 - EDGE_LENGTH * 0.5, edgeSmooth.x);
					highlightIntensity = max (highlightIntensity, (1.0 - block (hitPosition + edgeDirection * hitNormalAbs.zxy, hitNormal)) * smoothstep (0.5 - EDGE_LENGTH, 0.5 - EDGE_LENGTH * 0.5, edgeSmooth.y));
					highlightIntensity = max (highlightIntensity, (1.0 - block (hitPosition + edgeDirection, hitNormal)) * smoothstep (0.5 - EDGE_LENGTH, 0.5 - EDGE_LENGTH * 0.5, min (edgeSmooth.x, edgeSmooth.y)));

					// Set the object color
					vec3 color = cos ((hitPosition + hitNormal * 0.5) * 0.05);
					color = hsv2rgb (vec3 (color.x + color.y + color.z + highlightIntensity * 0.05, 1.0, 1.0));

					// Lighting
					vec3 lightDirection = hitPosition - lightPosition;
					float lightDistance = length (lightDirection);
					lightDirection /= lightDistance;

					float lightIntensity = min (1.0, 1.0 / lightDistance);
					float lightHitDistance;
					vec3 lightHitNormal;
					hit (hitPosition - hitNormal * DELTA, -lightDirection, lightDistance, lightHitDistance, lightHitNormal);
					lightIntensity *= step (lightDistance, lightHitDistance);

					// Bump mapping
					vec3 bumpUV = floor (hitUV * BUMP_RESOLUTION) / BUMP_RESOLUTION;
					hitNormal = normalize (hitNormal + (1.0 - highlightIntensity) * BUMP_INTENSITY * (hitNormal.yzx * (randUnpredictable (bumpUV) - 0.5) + hitNormal.zxy * (randUnpredictable (bumpUV + 1.0) - 0.5)));

					// Shading
					float ambient = mix (AMBIENT_NORMAL, AMBIENT_HIGHLIGHT, highlightIntensity);
					float diffuse = max (0.0, dot (hitNormal, lightDirection));
					float specular = pow (max (0.0, dot (reflect (rayDirection, hitNormal), lightDirection)), SPECULAR_POWER) * SPECULAR_INTENSITY;
					color = (ambient + diffuse * lightIntensity) * color + specular * lightIntensity;
					color *= pow (max (0.0, 1.0 - hitDistance / RAY_LENGTH_MAX), FADE_POWER);

					// Adjust the gamma
					color = pow (color, vec3 (GAMMA));

					// Set the fragment color
					gl_FragColor = vec4 (color, 1.0);
				}

//////////////////////////////////////
// SCENE 7 - "Cotton candy Pac-Man" //
//////////////////////////////////////
#else

				// Rendering parameters
				#define CAMERA_FOCAL_LENGTH	3.0
				#define RAY_STEP_MAX		100
				#define RAY_LENGTH_MAX		500.0
				#define NOISE_FACTOR		4.0
				#define DENSITY_FACTOR		0.1
				#define DIST_CORRECTION		0.8
				#define DIST_MIN			1.5

				// PRNG
				float rand (in vec3 seed) {
					seed = fract (seed * vec3 (5.6789, 5.4321, 6.7890));
					seed += dot (seed.yzx, seed.zxy + vec3 (21.0987, 12.3456, 15.1273));
					return fract (seed.x * seed.y * seed.z * 5.1337);
				}

				// Noise
				float noise (in vec3 p) {
					vec3 f = fract (p);
					p = floor (p);
					f = f * f * (3.0 - 2.0 * f);
					vec2 n = vec2 (1.0, 0.0);
					return mix (
						mix (
							mix (rand (p + n.yyy), rand (p + n.xyy), f.x),
							mix (rand (p + n.yxy), rand (p + n.xxy), f.x),
							f.y
						),
						mix (
							mix (rand (p + n.yyx), rand (p + n.xyx), f.x),
							mix (rand (p + n.yxx), rand (p + n.xxx), f.x),
							f.y
						),
						f.z
					);
				}

				// FBM
				float fbm (in vec3 p) {
					return noise (p) + noise (p * 2.0) / 2.0 + noise (p * 4.0) / 4.0;
				}

				// Distance to the scene and color of the closest point
				vec2 distScene (in vec3 p) {

					// Velocity, spacing of the gums
					float v = time * 100.0;
					const float gumPeriod = 60.0;

					// Pac-Man
					float body = length (p);
					body = max (body - 32.0, 27.0 - body);
					float eyes = 10.0 - length (vec3 (abs (p.x) - 15.0, p.y - 20.0, p.z - 20.0));
					float mouthAngle = PI * (0.07 + 0.07 * cos (2.0 * v * PI / gumPeriod));
					float mouthTop = dot (p, vec3 (0.0, -cos (mouthAngle), sin (mouthAngle))) - 2.0;
					mouthAngle *= 2.5;
					float mouthBottom = dot (p, vec3 (0.0, cos (mouthAngle), sin (mouthAngle)));
					float pacMan = max (max (body, eyes), min (mouthTop, mouthBottom));
					vec2 d = vec2 (pacMan, 0.13);
					vec3 P = p;

					// Gums
					vec3 q = vec3 (p.x, p.y + 6.0, mod (p.z + v, gumPeriod) - gumPeriod * 0.5);
					float gum = max (length (q) - 6.0 * min (p.z / 20.0, 1.0), -p.z);
					if (gum < d.x) {
						d = vec2 (gum, 0.35);
						P = q;
					}

					// Ghost
					v = 130.0 + 60.0 * cos (time * 3.0);
					q = vec3 (p.xy, p.z + v);
					body = length (vec3 (q.x, max (q.y - 4.0, 0.0), q.z));
					body = max (body - 28.0, 22.0 - body);
					eyes = 10.0 - length (vec3 (abs (q.x) - 14.0, q.y - 10.0, q.z - 22.0));
					float bottom = (q.y + 28.0 + 4.0 * cos (p.x * 0.4) * cos (p.z * 0.4)) * 0.7;
					float ghost = max (max (body, eyes), -bottom);
					if (ghost < d.x) {
						d = vec2 (ghost, 0.76);
						P = q;
					}

					// FBM
					d.x += NOISE_FACTOR * (fbm (P * 0.5) - 1.4);
					d.y += 0.1 * (noise (P * 0.5) - 0.5);
					return d;
				}

				// Main function
				void main () {

					// Define the position of the camera
					float cameraDist = 200.0 + 125.0 * cos (time * 0.3);
					vec3 rayOrigin = vec3 (cameraDist * cos (time), 5.0 + 90.0 * sin (time * 0.5), cameraDist * sin (time));

					// Define the orientation of the camera
					vec3 cameraLookAt = vec3 (0.0, 0.0, 40.0 * sin (time * 0.5) - 20.0);
					vec3 cameraForward = cameraLookAt - rayOrigin;
					vec3 cameraUp = vec3 (0.2 * cos (time * 0.1), 1.0, 0.2 * sin (time * 0.1));
					mat3 cameraOrientation;
					cameraOrientation [2] = normalize (cameraForward);
					cameraOrientation [0] = normalize (cross (cameraUp, cameraForward));
					cameraOrientation [1] = cross (cameraOrientation [2], cameraOrientation [0]);
					vec3 rayDirection = cameraOrientation * normalize (vec3 (fragScreen, CAMERA_FOCAL_LENGTH));

					// Set the sky color
					vec3 skyColor = vec3 (0.1, 0.1, 0.4) * (1.0 - abs (rayDirection.y));

					// Ray marching
					float densityTotal = 0.0;
					vec3 colorTotal = vec3 (0.0);
					float rayLength = 0.0;
					for (int rayStep = 0; rayStep < RAY_STEP_MAX; ++rayStep) {

						// Compute the maximum density
						float densityMax = 1.0 - rayLength / RAY_LENGTH_MAX;
						if (densityTotal > densityMax) {
							break;
						}

						// Get the scene information
						vec3 p = rayOrigin + rayDirection * rayLength;
						vec2 data = distScene (p);
						float dist = data.x * DIST_CORRECTION;
						if (dist < 0.0) {

							// Compute the local density
							float densityLocal = (densityTotal - densityMax) * dist * DENSITY_FACTOR;
							densityTotal += densityLocal;

							// Update the color
							vec3 colorLocal = hsv2rgb (vec3 (data.y, 0.6, 1.0));
							colorTotal += colorLocal * densityLocal;
						}

						// Go ahead
						rayLength += max (dist, DIST_MIN);
					}
					colorTotal += skyColor * (1.0 - densityTotal);

					// Set the fragment color
					gl_FragColor = vec4 (colorTotal, 1.0);
				}

/////////
// END //
/////////
#endif

			// Fragment shader: end
			#endif

			ENDGLSL
		}
	}
}
