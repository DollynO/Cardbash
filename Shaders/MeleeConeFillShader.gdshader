shader_type canvas_item;

uniform float fill_amount : hint_range(0.0, 100.0); // From 0 to 1
uniform vec4 fill_color : source_color = vec4(1.0, 0.5, 0.0, 1.0); // Orange default

void fragment() {
	vec2 uv = UV;
    if ( length(uv) < fill_amount / 200.0f) {
        COLOR = fill_color;
    }
}
