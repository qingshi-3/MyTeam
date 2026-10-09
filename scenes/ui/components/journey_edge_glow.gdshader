shader_type canvas_item;
render_mode unshaded;

uniform vec4 glow_color : source_color = vec4(1.0, 0.69, 0.30, 0.72);
uniform float expansion = 1.10;
uniform float radius = 0.020;
varying vec4 canvas_tint;

void vertex() { canvas_tint = COLOR; }

float silhouette(sampler2D source_texture, vec2 point) {
    if (point.x < 0.0 || point.y < 0.0 || point.x > 1.0 || point.y > 1.0) return 0.0;
    return texture(source_texture, point).a;
}

void fragment() {
    vec2 point = (UV - vec2(0.5)) * expansion + vec2(0.5);
    float alpha = silhouette(TEXTURE, point) * 0.25;
    alpha += (silhouette(TEXTURE, point + vec2(radius, 0.0)) + silhouette(TEXTURE, point - vec2(radius, 0.0))
        + silhouette(TEXTURE, point + vec2(0.0, radius)) + silhouette(TEXTURE, point - vec2(0.0, radius))) * 0.125;
    alpha += (silhouette(TEXTURE, point + vec2(radius, radius)) + silhouette(TEXTURE, point - vec2(radius, radius))
        + silhouette(TEXTURE, point + vec2(radius, -radius)) + silhouette(TEXTURE, point + vec2(-radius, radius))) * 0.0625;
    COLOR = vec4(glow_color.rgb, glow_color.a * alpha) * canvas_tint;
}
