shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float r = length(p);
    float t = max(age, 0.0);
    float spread = motion_amount < 0.5 ? 0.65 : 0.18 + 0.67 * (1.0 - exp(-t * 7.5));
    float rim = exp(-pow((r - spread) * 23.0, 2.0)) * exp(-t * 5.0);
    float core = exp(-r * r * 32.0) * exp(-t * 18.0);
    float halo = exp(-r * r * 5.0) * exp(-t * 6.0) * 0.20;
    float rays = exp(-abs(p.x) * 7.0 - abs(p.y) * 62.0)
        + exp(-abs(p.y) * 7.0 - abs(p.x) * 62.0);
    vec3 color = mix(vec3(0.18, 0.40, 1.0), vec3(0.75, 0.96, 1.0), clamp(core + rim, 0.0, 1.0));
    float alpha = (rim * 0.65 + core + halo + rays * exp(-t * 15.0) * 0.7)
        * (1.0 - smoothstep(0.89, 1.0, r)) * (1.0 - release);
    COLOR = vec4(color, alpha) * tint;
}
