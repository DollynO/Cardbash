shader_type canvas_item;

// --- Controls ---
uniform float fill_amount : hint_range(0.0, 1.0) = 0.0; // 0 = no darkness, 1 = fully dark
uniform float edge_softness : hint_range(0.0, 1.0) = 0.187; // width of the transition band
uniform float darkness : hint_range(0.0, 1.0) = 0.9; // how black the dark area gets (1 = black)
uniform vec2  center = vec2(0.5, 0.5); // normalized (0-1) center position

// --- Noise controls ---
uniform float noise_scale : hint_range(0.5, 20.0) = 15.0;   // size of the noise pattern
uniform float noise_strength : hint_range(0.0, 1.0) = 0.517; // how jagged the border is
uniform float noise_speed = 4.0; // animate the border (0 = static)

// Sample the screen behind this canvas item
uniform sampler2D screen_tex : hint_screen_texture;

// Simple value-noise (no texture needed)
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}
float noise2d(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(a, b, u.x)
         + (c - a) * u.y * (1.0 - u.x)
         + (d - b) * u.x * u.y;
}

void fragment() {
	if (fill_amount > 0.0f) {
	    vec4 scene_col = texture(screen_tex, SCREEN_UV);

		    /// --- Replace the aspect/radius code with this ---
		// Viewport size in pixels
		vec2 res = 1.0 / SCREEN_PIXEL_SIZE;

		// Offset from center in *pixels*
		vec2 p_px = (UV - center) * res;

		// Pixel-space radius (no aspect issues)
		float r = length(p_px);

		// Max radius = distance from center to farthest corner in pixels
		float r_max = length(vec2(0.5 * res.x, 0.5 * res.y));

		// Normalized radius 0..1 (use this everywhere)
		float rn = r / r_max;

	    // Procedural noise to roughen the edge
	    float t = TIME * noise_speed;
	    float n = noise2d(UV * noise_scale + vec2(t, -t)) * 2.0 - 1.0;
	    rn += n * noise_strength * edge_softness;

	    // Fill mapping:
	    // inner = boundary where darkness starts (0..1, center->corner)
	    // outer = inner + edge width
	    float inner = clamp(1.0 - fill_amount, 0.0, 1.0);
	    float outer = clamp(inner + edge_softness, 0.0, 1.0);

	    // Mask grows from edges to center as fill_amount increases
	    float mask = smoothstep(inner, outer, rn);

	    // Darken toward black
	    vec3 out_rgb = mix(scene_col.rgb, vec3(0.0), mask * darkness);
	    COLOR = vec4(out_rgb, scene_col.a);
	}
}
