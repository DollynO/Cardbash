shader_type canvas_item;

uniform vec4 team_color : source_color = vec4(1.0, 1.0, 1.0, 1.0); // Red by default
uniform vec4 mask_color = vec4(0.341, 0.227, 0.196, 1.0); // White as the replace target
uniform vec4 mask_color_2 = vec4(0.251, 0.153, 0.09, 1.0); // White as the replace target
uniform float tolerance = 0.1; // How close the color has to be

// Define UV limits in normalized space (0-1 range)
uniform vec2 region_min = vec2(0.0, 0.0); // bottom-left corner of region
//uniform vec2 region_max = vec2(1, 0.363); // top-right corner of region
uniform vec2 region_max = vec2(1, 1); // top-right corner of region

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
	
	vec4 team_color_2 = vec4(team_color.r * 0.73, team_color.g * 0.67, team_color.b * 0.67, 1.0); 

    // Only apply inside the defined UV region
    if (UV.x >= region_min.x && UV.x <= region_max.x &&
        UV.y >= region_min.y && UV.y <= region_max.y) {

        // If color is close to the mask color, apply team color
        if (distance(tex_color.rgb, mask_color.rgb) < tolerance) {
            tex_color.rgb = team_color.rgb;
        } else if (distance(tex_color.rgb, mask_color_2.rgb) < tolerance) {
            tex_color.rgb = team_color_2.rgb;
		}
    }

    COLOR = tex_color;
}