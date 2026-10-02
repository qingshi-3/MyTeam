// Drifting cloud cover shared by paving, actors and shafts. No static tree shadow.
// Time is injected by the owning scene; shader TIME is deliberately unused.
uniform float environment_elapsed = 0.0;
uniform bool reduced_motion = false;

float cloud_hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float cloud_noise(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    // Continuous slope and curvature at cell boundaries keep the penumbra smooth.
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    return mix(mix(cloud_hash(cell), cloud_hash(cell + vec2(1.0, 0.0)), f.x),
        mix(cloud_hash(cell + vec2(0.0, 1.0)), cloud_hash(cell + vec2(1.0)), f.x), f.y);
}

vec2 courtyard_cloud_position(vec2 uv) {
    // Cloud-shadow scrolling: GDQuest godot-shaders, Shaders/clouds2D.gdshader.
    // Our field is procedural and uses the scene clock, with one constant wind.
    // No sine displacement, wrapped time or moving canopy cookie.
    return uv * vec2(3.2, 1.8) + vec2(4.7, 0.95)
        - vec2(0.12, 0.04) * environment_elapsed;
}

float courtyard_cloud_detail(vec2 p) {
    // Smaller cloud lobes drift a little faster in the same wind direction.
    return cloud_noise(p * 2.03 + vec2(5.7, 1.3)
        - vec2(0.035, 0.012) * environment_elapsed);
}

float courtyard_cloud_transmission(vec2 uv) {
    vec2 p = courtyard_cloud_position(uv);
    // Nested cloud masses at about 300 / 148 / 73 / 37 pixels (960x540).
    // Rotate the smaller scales to avoid aligned grid-shaped edges. All advect
    // continuously in the same wind; there is no per-frame random grain.
    mat2 detail_rotation = mat2(vec2(0.8, 0.6), vec2(-0.6, 0.8));
    vec2 fine_p = p * 4.13 - vec2(0.055, 0.018) * environment_elapsed;
    vec2 edge_p = p * 8.17 - vec2(0.085, 0.028) * environment_elapsed;
    float density = cloud_noise(p) * 0.38
        + courtyard_cloud_detail(p) * 0.30
        + cloud_noise(detail_rotation * fine_p + vec2(3.4, 7.1)) * 0.22
        + cloud_noise(transpose(detail_rotation) * edge_p + vec2(8.2, 2.6)) * 0.10;
    float coverage = smoothstep(0.37, 0.60, density);
    // Clouds attenuate direct sun while the cool ambient light remains available.
    return 1.0 - coverage * 0.84;
}

// Shared light geometry: top-edge opening, width, ground depth, relative energy.
// Three shafts meet visible paving; one continues below the view.
vec4 courtyard_beam(int index) {
    if (index == 0) return vec4(0.045, 0.085, 0.72, 1.0);
    if (index == 1) return vec4(0.180, 0.110, 0.58, 1.0);
    if (index == 2) return vec4(0.370, 0.090, 1.16, 0.85);
    return vec4(0.600, 0.080, 0.46, 0.65);
}

vec2 courtyard_ray_footprint(vec2 uv, float ground_depth) {
    return vec2(uv.x + (ground_depth - uv.y) * 0.53, ground_depth);
}

// One continuous cross-section built from a broad core, shoulder and faint halo.
// Shared by the paving footprint and its airborne projection; no thin bright line.
float courtyard_beam_profile(float across) {
    float a = abs(across);
    float core = exp2(-2.0 * pow(a / 0.58, 2.0));
    float shoulder = exp2(-2.0 * pow(a / 1.02, 2.0));
    float halo = exp2(-2.0 * pow(a / 1.55, 2.0));
    return (core * 0.52 + shoulder * 0.32 + halo * 0.16)
        * (1.0 - smoothstep(1.15, 1.5, a));
}

// An actual illuminated patch on the ground plane. Air is extruded from this
// same 2D field, not gated by a second noise layer or a single fixed-depth row.
float courtyard_ground_pool(vec2 uv, vec4 beam) {
    float depth = (uv.y - beam.z) / 0.050;
    float across = (uv.x - beam.x - uv.y * 0.53) / beam.y;
    if (abs(depth) > 2.6 || abs(across) > 1.5) return 0.0;
    float footprint = courtyard_beam_profile(across)
        * exp2(-depth * depth * 2.0);
    // Scattering is visible only through a genuine opening. Fully clouded ground
    // keeps ambient light but cannot emit a residual airborne white stripe.
    float opening = smoothstep(0.55, 0.90, courtyard_cloud_transmission(uv));
    return footprint * opening * beam.w;
}

float courtyard_sunlight(vec2 uv) {
    float sunlight = courtyard_cloud_transmission(uv);
    float landing = 0.0;
    for (int i = 0; i < 4; i++) landing += courtyard_ground_pool(uv, courtyard_beam(i));
    // Modify direct illumination so stone texture and actor integration are preserved.
    return sunlight + (1.0 - sunlight) * min(1.0, landing) * 0.65;
}

// Reserve highlight headroom for airborne scattering; shaded ambient is unchanged.
vec3 courtyard_illumination(vec2 uv) {
    return mix(vec3(0.64, 0.73, 0.75), vec3(1.10, 1.025, 0.84), courtyard_sunlight(uv));
}
