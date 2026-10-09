shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
uniform int layer = 0;

float band(float distance, float center, float width) {
    return 1.0 - smoothstep(width * 0.4, width, abs(distance - center));
}

void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float r = length(p);
    float angle = atan(p.y, p.x);
    float clip = 1.0 - smoothstep(0.989, 1.0, r);
    float fade = (sustained ? smoothstep(0.0, 0.12, age) : 1.0 - progress) * (1.0 - release);
    float detail = 0.78 + 0.14 * sin(angle * 19.0 + r * 31.0) + 0.08 * sin(angle * 37.0 - r * 47.0);
    float alpha;
    vec3 tint;
    if (layer == 0) {
        // The outer edge is readable immediately: the expanding wave is decorative, not a delayed hit.
        float rim = band(r, 0.982, 0.018);
        float halo = exp(-abs(r - 0.971) * 50.0) * 0.18;
        float accents = pow(max(0.0, cos(angle * 8.0)), 24.0) * band(r, 0.94, 0.032) * 0.4;
        alpha = (rim * detail * 0.76 + halo + accents) * fade * clip;
        tint = mix(vec3(0.18, 0.5, 0.25), vec3(0.65, 0.95, 0.65), rim);
    } else { alpha = 0.0; tint = vec3(0.0); }
    COLOR = vec4(tint, alpha) * COLOR;
}
