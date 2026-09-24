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
    float fade = (1.0 - smoothstep(0.35, 0.9, age)) * (1.0 - release);
    float detail = 0.78 + 0.14 * sin(angle * 19.0 + r * 31.0) + 0.08 * sin(angle * 37.0 - r * 47.0);
    float alpha;
    vec3 tint;
    if (layer == 0) {
        // The outer edge is readable immediately: the expanding wave is decorative, not a delayed hit.
        float rim = band(r, 0.982, 0.018);
        float halo = exp(-abs(r - 0.971) * 50.0) * 0.18;
        float accents = pow(max(0.0, cos(angle * 8.0)), 24.0) * band(r, 0.94, 0.032) * 0.4;
        alpha = (rim * detail * 0.76 + halo + accents) * fade * clip;
        tint = mix(vec3(1.0, 0.25, 0.06), vec3(1.0, 0.74, 0.27), rim);
    } else {
        float travel = 1.0 - pow(1.0 - clamp(age / 0.32, 0.0, 1.0), 2.0);
        float echo = 1.0 - pow(1.0 - clamp((age - 0.075) / 0.36, 0.0, 1.0), 2.0);
        float wave = band(r, 0.12 + 0.85 * travel, 0.05) * (1.0 - travel);
        wave += band(r, 0.1 + 0.86 * echo, 0.04) * (1.0 - echo) * step(0.075, age) * 0.45;
        float trails = pow(max(0.0, sin(angle * 13.0 + sin(angle * 7.0))), 14.0);
        float wake = trails * exp(-abs(r - 0.7 * travel) * 16.0) * (1.0 - travel) * 0.16;
        alpha = (wave * detail * 0.65 + wake) * clip * fade * motion_amount;
        tint = vec3(1.0, 0.53, 0.18);
    }
    COLOR = vec4(tint, alpha) * COLOR;
}
