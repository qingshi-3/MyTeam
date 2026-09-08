shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    float clock = age * motion_amount;
    float rise = smoothstep(0.0, 0.8, age);
    vec2 uv = UV;
    uv.y += clock * 0.22;
    uv.x += sin(UV.y * 10.0 + clock * 5.0) * 0.015 * motion_amount;
    vec4 tex = texture(TEXTURE, clamp(uv, vec2(0.001), vec2(0.999)));
    float inside = step(0.0, uv.y) * step(uv.y, 1.0);
    // Rising packets replace a circular dissolve: light enters at the feet,
    // travels up the ribbons, then the last packet exits above the body.
    float height = 1.0 - UV.y;
    float head = smoothstep(height - 0.08, height + 0.08, age * 2.3);
    float tail = 1.0 - smoothstep(height - 0.10, height + 0.10, (age - 0.58) * 1.7);
    float packets = 0.75 + 0.25 * sin(height * 23.0 - clock * 16.0 + UV.x * 6.0);
    COLOR = vec4(tex.rgb * (0.95 + packets * 0.4), tex.a * inside * head * tail * packets * (1.0 - release)) * layer_color;
}
