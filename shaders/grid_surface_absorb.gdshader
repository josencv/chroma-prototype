shader_type spatial;
render_mode cull_back, depth_draw_opaque;

// Grid Surface Absorb Shader
// Combines grid visuals with absorption stamp-based saturation depletion.
// Uses modular includes for maintainability.

#include "grid.gdshaderinc"
#include "absorption_stamps.gdshaderinc"

// Varyings for world-space data
varying vec3 v_world_pos;
varying vec3 v_world_normal;

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
	// Compute base grid color
	vec3 col = compute_grid_color(v_world_pos, v_world_normal);

	// Apply vertex color modulation (if any)
	col *= COLOR.rgb;

	// Apply absorption drain (desaturation from stamps)
	col = apply_absorption_drain(col, v_world_pos, TIME);

	ALBEDO = col;
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
	ALPHA = 1.0;
}
