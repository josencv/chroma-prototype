// absorption_stamps.gdshaderinc
// Stamp-based saturation depletion for absorption visuals.
// Include this in your main shader and call apply_absorption_drain() in fragment().

// Maximum stamp count (N = 16)
const int MAX_STAMPS = 16;

// Recovery modes
const int RECOVERY_CONTINUOUS = 0;  // Mode A: Smooth fade
const int RECOVERY_STEPPED = 1;     // Mode B: Discrete steps
const int RECOVERY_AT_ONCE = 2;     // Mode C: Snap reset after delay

// Stamp buffer uniforms
// Each stamp: vec4(centerX, centerZ, yMin, yMax), vec4(radius, t0, 0, 0)
uniform vec4 u_stamp_pos[16];   // centerX, centerZ, yMin, yMax
uniform vec4 u_stamp_data[16];  // radius, t0, unused, unused
uniform int u_stamp_count = 0;

// Recovery mode selection
uniform int u_recovery_mode : hint_range(0, 2, 1) = 0;

// Recovery timing
uniform float u_recover_seconds = 4.0;
uniform float u_recovery_delay = 1.0;  // Delay before continuous recovery starts

// Stepped mode parameters
uniform float u_recover_step_interval = 0.5;  // Seconds between steps
uniform int u_recover_step_count = 8;          // Number of steps to full recovery

// Edge feathering (0 = hard edge, > 0 = smooth border in world units)
uniform float u_edge_feather = 0.3;

// Grayscale conversion using luminance weights
vec3 to_grayscale(vec3 color) {
	float lum = dot(color, vec3(0.299, 0.587, 0.114));
	return vec3(lum);
}

// Evaluate drain contribution from a single stamp
float eval_stamp_drain(vec3 world_pos, float now, int idx) {
	vec4 pos_data = u_stamp_pos[idx];
	vec4 stamp_data = u_stamp_data[idx];

	float cx = pos_data.x;
	float cz = pos_data.y;
	float y_min = pos_data.z;
	float y_max = pos_data.w;

	float radius = stamp_data.x;
	float t0 = stamp_data.y;

	// Skip invalid stamps (t0 <= 0 means unused slot)
	if (t0 <= 0.0 || radius <= 0.0) {
		return 0.0;
	}

	// Height gate
	float in_height = step(y_min, world_pos.y) * step(world_pos.y, y_max);
	if (in_height < 0.5) {
		return 0.0;
	}

	// Radius check (XZ plane)
	float dx = world_pos.x - cx;
	float dz = world_pos.z - cz;
	float d2 = dx * dx + dz * dz;
	float r2 = radius * radius;

	// In radius gate (with optional feathering)
	float in_radius = 0.0;
	if (u_edge_feather > 0.0) {
		// Smooth edge using distance (requires sqrt, but N is small)
		float dist = sqrt(d2);
		in_radius = 1.0 - smoothstep(radius - u_edge_feather, radius, dist);
	} else {
		// Hard edge (no sqrt)
		in_radius = step(d2, r2);
	}

	if (in_radius < 0.001) {
		return 0.0;
	}

	// Time-based recovery calculation depends on mode
	float elapsed = now - t0;
	float drain_01 = 0.0;

	if (u_recovery_mode == RECOVERY_CONTINUOUS) {
		// Mode A: Continuous fade with initial delay
		// Stay fully drained during delay period, then fade continuously
		if (elapsed < u_recovery_delay) {
			drain_01 = 1.0;
		} else {
			float recovery_elapsed = elapsed - u_recovery_delay;
			float recover_01 = clamp(recovery_elapsed / u_recover_seconds, 0.0, 1.0);
			drain_01 = 1.0 - recover_01;
		}
	}
	else if (u_recovery_mode == RECOVERY_STEPPED) {
		// Mode B: Stepped recovery in discrete chunks
		float steps = floor(elapsed / u_recover_step_interval);
		float recover_01 = clamp(steps / float(u_recover_step_count), 0.0, 1.0);
		drain_01 = 1.0 - recover_01;
	}
	else if (u_recovery_mode == RECOVERY_AT_ONCE) {
		// Mode C: Full drain until recover_seconds, then instant snap to recovered
		drain_01 = elapsed < u_recover_seconds ? 1.0 : 0.0;
	}

	return drain_01 * in_radius;
}

// Compute total drain from all stamps (combine with max)
float compute_total_drain(vec3 world_pos, float now) {
	float drain_final = 0.0;

	// Unroll for better performance on some GPUs
	for (int i = 0; i < MAX_STAMPS; i++) {
		if (i >= u_stamp_count) break;
		float drain_stamp = eval_stamp_drain(world_pos, now, i);
		drain_final = max(drain_final, drain_stamp);
	}

	return drain_final;
}

// Apply absorption drain to a color (desaturates based on drain amount)
vec3 apply_absorption_drain(vec3 color, vec3 world_pos, float now) {
	float drain = compute_total_drain(world_pos, now);

	// Lerp toward grayscale based on drain amount
	vec3 gray = to_grayscale(color);
	return mix(color, gray, drain);
}
