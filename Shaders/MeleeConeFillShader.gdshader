shader_type canvas_item;

// Control how much of the shape is filled (0.0 = empty, 1.0 = full)
uniform float fill_amount : hint_range(0.0, 1.0) = 1.0;

// Cone angle in degrees (360 = full circle)
uniform float cone_angle : hint_range(0.0, 360.0) = 360.0;

// Cone rotation offset in degrees
uniform float cone_rotation : hint_range(0.0, 360.0) = 0.0;

// Colors
uniform vec4 fill_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 empty_color : source_color = vec4(0.0, 0.0, 0.0, 0.0);

// Smooth edge transition
uniform float edge_smoothness : hint_range(0.0, 0.1) = 0.01;

void fragment() {
    // Get UV coordinates centered at (0.5, 0.5)
    vec2 uv = UV - 0.5;

    // Calculate distance from center
    float dist = length(uv);

    // Calculate angle from center (-PI to PI)
    float angle = atan(uv.y, uv.x);

    // Convert to 0-360 degree range, starting from top (negative Y)
    float degrees = mod(degrees(angle) + 90.0 + cone_rotation, 360.0);

    // Check if point is within cone angle
    float cone_mask = 1.0;
    if (cone_angle < 360.0) {
        float half_angle = cone_angle * 0.5;
        float angle_from_center = abs(degrees - 180.0);
        cone_mask = smoothstep(half_angle + edge_smoothness, half_angle - edge_smoothness, angle_from_center);
    }

    // Calculate fill based on distance from center
    // fill_amount of 1.0 means fill to radius 0.5 (edge of shape)
    float fill_radius = fill_amount * 0.5;
    float fill_mask = smoothstep(fill_radius + edge_smoothness, fill_radius - edge_smoothness, dist);

    // Combine masks
    float final_mask = fill_mask * cone_mask;

    // Mix colors based on mask
    vec4 final_color = mix(empty_color, fill_color, final_mask);

    COLOR = final_color;
}