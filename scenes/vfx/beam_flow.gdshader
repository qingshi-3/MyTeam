shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }

void fragment() {
    float d = abs(UV.y - .5);
    float grain = texture(TEXTURE, vec2(fract(UV.x * 1.7 - age * 3.0 * motion_amount), .28 + UV.y * .44)).a;
    float core = exp(-d*d*180.0);
    float sheath = exp(-d*d*18.0) * (.35 + grain*.65);
    float ends = smoothstep(0.0, .035, UV.x) * (1.0-smoothstep(.965, 1.0, UV.x));
    float envelope = smoothstep(0.0, .012, age) * (1.0-smoothstep(.18,1.0,progress));
    vec3 tint = mix(vec3(.12,.4,.95),vec3(.78,.97,1.0),core);
    COLOR=vec4(tint,(core*.88+sheath*.6)*ends*envelope*(1.0-release))*layer_color;
}
