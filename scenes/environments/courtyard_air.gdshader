shader_type canvas_item;
render_mode unshaded, blend_mix;

// Soft atmospheric layers: pend00, https://godotshaders.com/shader/god-rays/ (CC0 code).
// Sunlight stays parallel (Godot 2D lights documentation, Directional light).
// Beams extend from outside the frame; the scene supplies the presentation clock.
#include "res://scenes/environments/courtyard_lighting.gdshaderinc"
#include "res://scenes/environments/weather_common.gdshaderinc"

uniform vec2 canvas_size = vec2(960.0, 540.0);
uniform vec4 texture_rect = vec4(0.0, 0.0, 1.0, 1.0);
uniform float intensity : hint_range(0.0, 1.0) = 0.86;

float air_hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float air_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(air_hash(i), air_hash(i + vec2(1.0, 0.0)), f.x),
        mix(air_hash(i + vec2(0.0, 1.0)), air_hash(i + vec2(1.0)), f.x), f.y);
}

float shaft(vec2 uv, vec4 beam, float seed) {
    float across = (uv.x - beam.x - uv.y * 0.53) / beam.y;
    if (abs(across) > 1.5 || uv.y > beam.z + 0.090) return 0.0;
    // Occlusion/scattering principle: NVIDIA GPU Gems 3, chapter 13.
    // This ground-plane extrusion is our 2D approximation, not volume ray tracing.
    // Reconstruct parallel light above the shared ground footprint. Integrate
    // its depth instead of stretching the cloud value of one row into a stripe.
    // Each source sample is identical to the ground's actual illuminated patch.
    float transmitted = 0.0;
    for (int i = -2; i <= 2; i++) {
        float ground_depth = beam.z + float(i) * 0.035;
        vec2 footprint = courtyard_ray_footprint(uv, ground_depth);
        float above_ground = 1.0 - smoothstep(ground_depth - 0.035, ground_depth + 0.020, uv.y);
        transmitted += courtyard_ground_pool(footprint, beam) * above_ground;
    }
    // Normalize the five samples of the authored depth profile. Cloud movement
    // changes the footprint and its entire light path together; no local screen
    // shadow mask cuts the air into disconnected pieces.
    transmitted *= 0.46;
    float wisps = 0.78 + 0.22 * air_noise(vec2(across * 1.5 + seed,
        uv.y * 1.4 - environment_elapsed * 0.28));
    float distance_fade = 1.0 - smoothstep(-0.25, 1.55, uv.y) * 0.30;
    return transmitted * 0.81 * wisps * distance_fade;
}

float shafts(vec2 uv) {
    return shaft(uv, courtyard_beam(0), 1.1)
        + shaft(uv, courtyard_beam(1), 3.2)
        + shaft(uv, courtyard_beam(2), 5.3)
        + shaft(uv, courtyard_beam(3), 7.1);
}

void fragment() {
    vec2 uv = texture_rect.xy + UV * texture_rect.zw;
    float rays = weather_overcast >= 1.0 ? 0.0 : shafts(uv) * (1.0 - weather_overcast);
    float dust = 0.0;
    if (!reduced_motion) {
        // One bounded mote per selected cell; no full-screen particle loop.
        vec2 grid = vec2(24.0, 12.0);
        vec2 cell = floor(UV * grid);
        float seed = air_hash(cell + vec2(8.3, 2.7));
        float age = fract(environment_elapsed / (7.0 + seed * 5.0) + seed * 8.0);
        vec2 local = vec2(mix(0.22, 0.46, age) + seed * 0.35,
            mix(0.84, 0.18, age));
        vec2 point = (cell + local) / grid;
        vec2 pixel = (UV - point) * canvas_size;
        float speck = 1.0 - smoothstep(0.45, 1.45, length(pixel));
        float life = smoothstep(0.0, 0.16, age) * (1.0 - smoothstep(0.78, 1.0, age));
        dust = step(0.76, seed) * speck * life * min(1.0, rays * 4.0) * 0.75;
    }
    // Warm scattering reduces local contrast within a beam rather than clipping bright paving.
    // Smooth optical-depth response keeps the core brighter without clipping
    // neighbouring layers into one flat white plateau when shafts overlap.
    float opacity = 1.0 - exp(-(rays * intensity + dust));
    COLOR = vec4(vec3(1.0, 0.94, 0.80), opacity);
}
