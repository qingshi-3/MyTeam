shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    vec2 p = UV - 0.5;
    float r = length(p);
    // Roll the interior faster than the silhouette, preserving dark folds.
    float turn = (age * 1.5 + sin(r * 18.0 - age * 8.0) * 0.10) * motion_amount;
    turn *= 1.0 - smoothstep(0.30, 0.50, r);
    vec2 uv = mat2(vec2(cos(turn), sin(turn)), vec2(-sin(turn), cos(turn))) * p + 0.5;
    uv += vec2(sin(p.y * 21.0 - age * 9.0), cos(p.x * 19.0 + age * 7.0))
        * 0.009 * motion_amount * (1.0 - smoothstep(0.38, 0.49, r));
    vec4 ink = texture(TEXTURE, uv);
    COLOR = ink * layer_color;
}
