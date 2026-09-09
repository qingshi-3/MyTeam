shader_type canvas_item;
render_mode unshaded, blend_add;

// A single optical glint or dust mote. Birth, travel and fade belong to its track.
uniform float rays : hint_range(0.0, 1.0) = 1.0;
uniform float core_strength = 1.0;
uniform float corona_strength = 0.30;
uniform vec4 tint : source_color = vec4(1.0);
varying vec4 particle_color;
void vertex() { particle_color = COLOR; }
void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float r2 = dot(p, p);
    float core = exp(-r2 * 110.0) * core_strength;
    float corona = exp(-r2 * 12.0) * corona_strength;
    float horizontal = exp(-abs(p.x) * 6.0 - abs(p.y) * 68.0);
    float vertical = exp(-abs(p.y) * 4.8 - abs(p.x) * 80.0);
    float alpha = (core + corona + rays * (horizontal + vertical) * 0.6)
        * (1.0 - smoothstep(0.72, 1.0, length(p)));
    COLOR = vec4(particle_color.rgb * tint.rgb, alpha * particle_color.a * tint.a);
}
