shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = true;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    vec2 p = UV - 0.5;
    float radius = length(p);
    float angle = atan(p.y, p.x);
    // The body remains fixed. Independent mesh tracks supply orbital motion.
    vec4 body = texture(TEXTURE, UV);
    float reveal = smoothstep(0.0, 0.18, age);
    float dome = (1.0 - smoothstep(0.30, 0.43, radius)) * 0.055;
    float ripple_radius = (1.0 - impact) * 0.60;
    float ripple = exp(-pow((radius - ripple_radius) * 35.0, 2.0)) * impact;
    float edge = exp(-pow((radius - 0.385) * 35.0, 2.0));
    float breakup = sin(angle * 13.0 + radius * 42.0) * 0.5 + 0.5;
    float dissolve = smoothstep(release * 1.25 - 0.25, release * 1.25 + 0.05, breakup);
    vec3 light = body.rgb * 0.68 + vec3(0.25, 0.95, 0.8) * ripple * mix(0.4, 1.0, motion_amount);
    float alpha = body.a * 0.68 + dome + ripple * 0.32 + edge * impact * 0.15;
    COLOR = vec4(light, alpha * reveal * dissolve * (1.0 - release)) * layer_color;
}
