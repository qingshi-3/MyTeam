shader_type canvas_item;
render_mode unshaded, blend_mix;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;

float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7))) * 43758.5453); }
void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    float scan = exp(-pow((UV.y - fract(age * 0.24 * motion_amount)) / 0.1, 2.0));
    float intro = smoothstep(0.0, 0.12, age);
    float noise = hash(floor(UV * 32.0));
    float fragment_fade = 1.0 - smoothstep(noise * 0.55, noise * 0.55 + 0.42, release);
    vec3 color = tex.rgb * (0.9 + 0.2 * scan) + vec3(0.07,0.04,0.14) * scan * tex.a;
    COLOR = vec4(color, tex.a * intro * fragment_fade * (1.0 - release));
}
