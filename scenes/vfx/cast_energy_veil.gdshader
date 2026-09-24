shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec4 tint : source_color = vec4(1.0, 0.14, 0.22, 1.0);
uniform vec4 core_tint : source_color = vec4(1.0, 0.8, 0.55, 1.0);
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform float duration = 0.62;
uniform float burst = 1.0;

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float noise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1, 0)), f.x),
        mix(hash(i + vec2(0, 1)), hash(i + vec2(1, 1)), f.x), f.y);
}

void fragment() {
    float p = clamp(age / duration, 0.0, 1.0);
    float t = age * motion_amount;
    vec2 q = (UV - vec2(0.5, 0.51)) * 2.0;
    float opening = smoothstep(0.06, 0.36, age) * motion_amount;
    float radius = 0.31 + opening * 0.28 * burst;
    float silhouette = 1.0 - smoothstep(0.63, 0.98, abs(q.y));
    float flow = noise(vec2(q.x * 6.5, q.y * 4.0 + t * 7.0));
    float fine = noise(vec2(q.x * 18.0, q.y * 9.0 + t * 12.0));
    float envelope = smoothstep(0.0, 0.055, age) * (1.0 - smoothstep(0.17, duration, age));
    // Separate asymmetric sheets lean inward at the shoulders. Their lower
    // fronts cross the torso; the edge dissolves before the finer afterglints.
    float left = -radius + 0.10 * sin(q.y * 5.0 + t * 8.0) + q.y * 0.09;
    float right = radius + 0.09 * sin(q.y * 6.0 - t * 7.0 + 1.6) - q.y * 0.05;
    float sheet_l = exp(-pow((q.x - left) / 0.14, 2.0));
    float sheet_r = exp(-pow((q.x - right) / 0.11, 2.0));
    float front_curve = 0.30 - q.x * q.x * 0.50 - t * mix(1.7, 0.85, burst);
    float front = exp(-pow((q.y - front_curve - 0.065 * sin(q.x * 9.0 + t * 9.0)) / 0.14, 2.0));
    front *= 1.0 - smoothstep(radius * 0.75, radius + 0.17, abs(q.x));
    float breakup = smoothstep(p * 0.75 - 0.05, p * 0.75 + 0.28, flow * 0.74 + fine * 0.26);
    float body = (sheet_l * 0.58 + sheet_r * 0.48 + front * 0.64) * silhouette * breakup;
    float edge = pow(max(body, 0.0), 3.0);
    float soft = exp(-dot(q * vec2(1.7, 1.2), q * vec2(1.7, 1.2)) * 4.0) * 0.09;
    vec3 color = mix(tint.rgb, core_tint.rgb, clamp(edge * 0.65, 0.0, 0.65));
    COLOR = vec4(color, min(0.9, body + soft) * envelope * tint.a * (1.0 - release));
}
