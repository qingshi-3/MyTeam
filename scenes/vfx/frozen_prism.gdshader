shader_type canvas_item;
render_mode unshaded;
uniform float growth = 0.0;
uniform float age = 0.0;
uniform float release = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
varying vec4 tint;
void vertex() { tint = COLOR; }
float hash(vec2 p) { return fract(sin(dot(p, vec2(31.7, 74.3))) * 15437.53); }
void fragment() {
    vec4 ice = texture(TEXTURE, UV);
    // Preserve the authored broad light/dark planes. The small sheen travels
    // across those faces; the whole frozen shell neither pulses nor rotates.
    float clock = motion_amount < 0.5 ? 0.38 : age * 0.17;
    float phase = fract(clock);
    float band = exp(-pow((UV.y + UV.x * 0.28 - mix(-0.15, 1.45, phase)) * 19.0, 2.0));
    float sheen = band * pow(sin(phase * 3.14159265), 2.0) * 0.23;
    float bright_face = smoothstep(0.35, 0.85, max(ice.g, ice.b));
    vec3 color = ice.rgb + vec3(0.25, 0.65, 0.80) * bright_face * (sheen + impact * 0.20);
    vec2 grid = UV * vec2(4.0, 6.0);
    vec2 cell = floor(grid);
    float triangle = step(1.0, fract(grid.x) + fract(grid.y));
    float shard_delay = hash(cell + vec2(triangle * 13.0, triangle * 7.0)) * 0.35;
    float fragments = 1.0 - smoothstep(shard_delay, shard_delay + 0.52, release);
    if (motion_amount < 0.5) fragments = 1.0 - release;
    float alpha = pow(ice.a, 1.7) * smoothstep(0.0, 0.18, growth) * fragments * (1.0 - release);
    COLOR = vec4(color, alpha) * tint;
}
