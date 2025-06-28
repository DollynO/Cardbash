shader_type canvas_item;

// progress: 1.0 = full circle gray, 0.0 = fully cleared.
// We will fade out (make transparent) everything whose angle > progress.
float progress : hint_range(0.0, 1.0) = 1.0;
// base_color: the gray color you want. Alpha here should be 1.0.
@uniform vec4 base_color : hint_color = vec4(0.3, 0.3, 0.3, 1.0);

void fragment() {
    // UV goes from (0,0) bottom-left to (1,1) top-right of the ColorRect.
    // Recenter UV so (0.5,0.5) is the circle’s center.
    vec2 center = vec2(0.5, 0.5);
    vec2 offset = UV - center;
    float angle = atan(offset.y, offset.x);

    // By default atan() returns in [-PI..PI]. We want 0 at “12 o’clock”,
    // and increase clockwise. So:
    //  1) Add PI/2 to shift zero to upward direction.
    //  2) If negative, wrap by +2*PI.
    float shifted = angle + (PI * 0.5);
    if (shifted < 0.0) {
        shifted += PI * 2.0;
    }

    // Normalize to [0..1] by dividing by 2*PI.
    float fraction = shifted / (2.0 * PI);

    // If fraction ≤ progress, paint the gray. Otherwise make it transparent.
    if (fraction <= progress) {
        COLOR = base_color;
    } else {
        // fully clear
        COLOR = vec4(base_color.rgb, 0.0);
    }
}
