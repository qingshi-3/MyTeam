shader_type canvas_item;
render_mode unshaded;

uniform float age = 0.0;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float release : hint_range(0.0, 1.0) = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
uniform float reveal_fraction : hint_range(0.05, 0.8) = 0.38;
uniform float dissolve_start : hint_range(0.4, 0.9) = 0.58;
uniform vec4 edge_color : source_color = vec4(1.0, 0.65, 0.42, 1.0);
varying vec4 layer_color;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x),
        mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}
void vertex() { layer_color = COLOR; }
void fragment() {
    // The authored stroke runs from bottom-left to top-right. Rotation of the
    // second sprite rotates this travel direction with the stroke itself.
    float along = (UV.x - UV.y + 1.0) * 0.5;
    float turbulence = noise(UV * 23.0 + vec2(-age * 5.0, age * 2.0));
    float swing = clamp(progress / reveal_fraction, 0.0, 1.0);
    float front = mix(-0.08, 1.10, 1.0 - pow(1.0 - swing, 2.2));
    float coordinate = along + (turbulence - 0.5) * 0.045;
    float reveal = 1.0 - smoothstep(front - 0.035, front + 0.035, coordinate);
    float tip = exp(-pow((coordinate - front + 0.018) / 0.055, 2.0));
    float tail = clamp((progress - dissolve_start) / (1.0 - dissolve_start), 0.0, 1.0);
    // Dissolve from the beginning of the swing, with fragmented local edges.
    float erosion = max(tail, release);
    float dissolve = smoothstep(erosion * 1.35 - 0.28, erosion * 1.35 + 0.02,
        along * 0.65 + turbulence * 0.35);
    vec2 bend = vec2(1.0, 1.0) * sin(along * 3.14159)
        * (1.0 - swing) * 0.13 * motion_amount;
    vec4 tex = texture(TEXTURE, clamp(UV + bend, vec2(0.001), vec2(0.999)));
    float shimmer = 0.94 + 0.12 * sin(along * 32.0 - age * 28.0) * motion_amount;
    vec3 light = tex.rgb * shimmer + edge_color.rgb * tip * 0.65 * motion_amount;
    COLOR = vec4(light, tex.a * reveal * dissolve) * layer_color;
}
