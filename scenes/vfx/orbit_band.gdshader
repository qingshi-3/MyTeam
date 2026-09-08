shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float release = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
uniform bool back_pass = false;
uniform float depth_blend = 0.3;
uniform float radius = 113.0;
uniform float flattening = 0.42;
uniform float plane_rotation = -0.3;
uniform float angular_speed = 1.8;
uniform float phase = 0.0;
uniform float arc_length = 1.9;
uniform float width = 15.0;
uniform vec4 tint : source_color = vec4(1.0, 0.75, 0.24, 1.0);
varying float depth;
vec2 rotate_plane(vec2 p) {
    float c = cos(plane_rotation), s = sin(plane_rotation);
    return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}
void vertex() {
    float angle = phase + age * angular_speed * motion_amount - sign(angular_speed) * arc_length * (1.0 - UV.x);
    depth = sin(angle);
    vec2 center = rotate_plane(vec2(cos(angle), depth * flattening) * radius);
    vec2 tangent = normalize(rotate_plane(vec2(-sin(angle), cos(angle) * flattening)));
    float taper = sin(UV.x * 3.14159265) * 0.8 + 0.2;
    VERTEX = center + vec2(-tangent.y, tangent.x) * (UV.y - 0.5) * width * taper;
}
void fragment() {
    vec4 ink = texture(TEXTURE, vec2(UV.x, mix(0.28, 0.72, UV.y)));
    float brightness = dot(ink.rgb, vec3(0.45, 0.35, 0.20));
    float front = smoothstep(-depth_blend, depth_blend, depth);
    // The transparent shell transmits a little light even from its far side.
    // Keep that spill above the body; otherwise its dense painted patches cut
    // holes in the band. Crossfade depth instead of slicing it at the equator.
    float pass_mask = back_pass ? (1.0 - front) * 0.26 : mix(0.16, 0.85, front);
    float trail = smoothstep(0.0, 0.25, UV.x);
    float appear = smoothstep(0.0, 0.22, age);
    vec3 light = mix(tint.rgb, vec3(1.0, 0.97, 0.8), brightness * 0.4);
    float soft_light = exp(-pow((UV.y - 0.5) * 5.0, 2.0)) * sin(UV.x * 3.14159265) * 0.16;
    COLOR = vec4(light * (1.0 + impact * 0.7), (ink.a * brightness + soft_light) * trail * appear * pass_mask * (1.0 - release));
}
