shader_type canvas_item;
render_mode unshaded;
varying vec2 local_point;
void vertex() { local_point = VERTEX; }
void fragment() {
    float distance_to_center = length(local_point / vec2(26.0, 9.0));
    float opacity = (1.0 - smoothstep(0.18, 1.0, distance_to_center)) * 0.28;
    COLOR = vec4(0.035, 0.027, 0.045, opacity * COLOR.a);
}
