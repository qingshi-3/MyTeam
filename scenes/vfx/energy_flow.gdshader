shader_type canvas_item;
render_mode unshaded;

uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
uniform float flow_speed = 1.1;
uniform float distortion : hint_range(0.0, 0.04) = 0.009;
varying vec4 layer_color;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x),
        mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}
void vertex() { layer_color = COLOR; }
void fragment() {
    vec2 p = UV - 0.5;
    float radius = length(p);
    float angle = atan(p.y, p.x);
    float clock = age * flow_speed * motion_amount;
    // Integer angular harmonics keep the polar seam continuous.
    float wave = sin(angle * 5.0 - clock * 3.0 + radius * 18.0);
    float filaments = sin(angle * 11.0 + clock * 2.0 - radius * 31.0);
    float n = noise(p * 19.0 + vec2(clock * 0.45, -clock * 0.3));
    vec2 radial = p / max(radius, 0.001);
    vec2 tangent = vec2(-radial.y, radial.x);
    vec2 uv = UV + (radial * wave + tangent * filaments * 0.5)
        * distortion * motion_amount * smoothstep(0.1, 0.4, radius);
    vec4 tex = texture(TEXTURE, clamp(uv, vec2(0.001), vec2(0.999)));
    float flow = 0.70 + 0.30 * smoothstep(-0.5, 0.9, wave + filaments * 0.35);
    float reveal = smoothstep(0.0, 0.13, age - radius * 0.16);
    float decay = sustained ? release : max(release, clamp((progress - 0.42) / 0.58, 0.0, 1.0));
    float dissolve = smoothstep(decay * 1.2 - 0.15, decay * 1.2 + 0.05,
        n * 0.65 + (wave * 0.5 + 0.5) * 0.35);
    float ripple = exp(-pow((radius - (1.0 - impact) * 0.65) * 24.0, 2.0)) * impact;
    COLOR = vec4(tex.rgb * (flow + ripple * 1.5),
        tex.a * reveal * dissolve * (0.8 + flow * 0.2)) * layer_color;
}
