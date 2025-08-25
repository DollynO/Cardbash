shader_type canvas_item;

uniform float fill_amount : hint_range(0.0, 1.0); // From 0 to 1
uniform vec4 fill_color : source_color = vec4(0.0, 0.0, 0.0, 0.0); //

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec2 uv = UV * 2.0 - 1.0;
	float angle = atan(uv.y, uv.x) + PI/2.0f;
	float PI2 = PI * 2.0f;
	
	if (angle< 0.0) {
		angle += PI2;
	}
	
	float dist = length(uv);
	float cutoff = fill_amount * PI2;
	
	bool inside_circle = dist <=  1.0;
	bool inside_sector = angle <= cutoff;
	
	if (inside_sector) {
		COLOR = (COLOR + fill_color) / 2.0f;
	}
	// Called for every pixel the material is visible on.
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
