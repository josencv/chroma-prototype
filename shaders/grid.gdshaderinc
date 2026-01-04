// grid.gdshaderinc
// World-space grid rendering logic for blockout meshes.
// Include this in your main shader and call compute_grid_color() in fragment().

// Grid uniforms (set these in the including shader)
uniform vec4 base_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 minor_color : source_color = vec4(0.75, 0.75, 0.75, 1.0);
uniform vec4 major_color : source_color = vec4(0.55, 0.55, 0.55, 1.0);

uniform float major_step_m = 1.0;
uniform int minor_divisions = 4;

uniform float minor_thickness_m = 0.02;
uniform float major_thickness_m = 0.04;

uniform float triplanar_sharpness = 8.0;

// Per-instance tint (works like a "modulate" for blockout pieces)
instance uniform vec4 tint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

// Internal functions

float grid_line_coverage_1d(float coord_world, float step_world, float thickness_world) {
	float scaled = coord_world / step_world;
	float t = fract(scaled);
	float dist_to_line = min(t, 1.0 - t);

	float half_width = (thickness_world / step_world) * 0.5;
	float aa = max(fwidth(scaled), 1e-6);

	return 1.0 - smoothstep(half_width, half_width + aa, dist_to_line);
}

float grid_coverage_2d(vec2 world_xy, float step_world, float thickness_world) {
	float x = grid_line_coverage_1d(world_xy.x, step_world, thickness_world);
	float y = grid_line_coverage_1d(world_xy.y, step_world, thickness_world);
	return max(x, y);
}

float grid_triplanar_coverage(vec3 world_pos, vec3 world_normal, float step_world, float thickness_world) {
	vec3 n = abs(world_normal);
	vec3 w = pow(n, vec3(triplanar_sharpness));
	float denom = max(w.x + w.y + w.z, 1e-6);
	w /= denom;

	float cov_xy = grid_coverage_2d(world_pos.xy, step_world, thickness_world);
	float cov_xz = grid_coverage_2d(world_pos.xz, step_world, thickness_world);
	float cov_yz = grid_coverage_2d(world_pos.yz, step_world, thickness_world);

	// Normal axis maps to the plane perpendicular to it.
	return (w.z * cov_xy) + (w.y * cov_xz) + (w.x * cov_yz);
}

// Main grid color computation
// Call this in fragment() with world position and normal
vec3 compute_grid_color(vec3 world_pos, vec3 world_normal) {
	float major_cov = grid_triplanar_coverage(world_pos, world_normal, major_step_m, major_thickness_m);
	float minor_cov = 0.0;

	if (minor_divisions > 1) {
		float minor_step = major_step_m / float(minor_divisions);
		minor_cov = grid_triplanar_coverage(world_pos, world_normal, minor_step, minor_thickness_m);
		minor_cov = clamp(minor_cov - major_cov, 0.0, 1.0);
	}

	vec3 col = base_color.rgb;
	col = mix(col, minor_color.rgb, minor_cov * minor_color.a);
	col = mix(col, major_color.rgb, major_cov * major_color.a);

	// Apply instance tint
	col *= tint_color.rgb;

	return col;
}
