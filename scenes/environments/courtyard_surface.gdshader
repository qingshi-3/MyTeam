shader_type canvas_item;
render_mode unshaded;

#include "res://scenes/environments/courtyard_lighting.gdshaderinc"
#include "res://scenes/environments/weather_common.gdshaderinc"

void fragment() {
    vec4 albedo = texture(TEXTURE, UV);
    COLOR = vec4(weather_surface(albedo.rgb, UV), albedo.a);
}
