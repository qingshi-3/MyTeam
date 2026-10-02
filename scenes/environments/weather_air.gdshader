shader_type canvas_item;
render_mode unshaded, blend_mix;

#include "res://scenes/environments/courtyard_lighting.gdshaderinc"
#include "res://scenes/environments/weather_common.gdshaderinc"

uniform vec2 canvas_size = vec2(960.0, 540.0);
uniform vec4 texture_rect = vec4(0.0, 0.0, 1.0, 1.0);

// Each rain seed owns both its airborne drop and ground response. Projection
// shears a vertical fall along the same wind vector; there is no collision body.
float rain_layer(vec2 uv, float layer) {
    vec2 pixel_span = canvas_size / texture_rect.zw * (540.0 / canvas_size.y);
    vec2 grid = vec2(52.0 - layer * 11.0, 9.0 - layer * 1.5);
    vec2 q = vec2(uv.x - weather_wind * uv.y, uv.y);
    vec2 cell = floor(q * grid);
    float rain = 0.0;
    float width = 0.55 + layer * 0.33;
    float trail = 11.0 + layer * 7.0;
    for (int i = 0; i < 10; i++) {
        vec2 id = cell + vec2(0.0, float(i));
        float seed = cloud_hash(id + vec2(layer * 31.7, 4.3));
        if (seed > weather_rain) continue;
        float other = cloud_hash(id.yx + vec2(13.1, layer * 8.9));
        vec2 landing = (id + vec2(0.32 + other * 0.36, 0.18 + seed * 0.64)) / grid;
        landing.x += weather_wind * landing.y;
        float period = 0.82 + other * 0.72;
        float age = mod(environment_elapsed + seed * 7.0, period);
        float fall_time = 0.50 - layer * 0.065;
        float height = (fall_time - age) * (1.55 + layer * 0.24);
        vec2 head = landing - vec2(weather_wind, 1.0) * height;
        vec2 delta = (uv - head) * pixel_span;
        float across = abs(delta.x - delta.y * weather_wind * pixel_span.x / pixel_span.y);
        float line = (1.0 - smoothstep(width * 0.25, width + 0.45, across))
            * smoothstep(-trail, -trail * 0.20, delta.y)
            * (1.0 - smoothstep(-0.5, 1.0, delta.y));
        rain += line * step(age, fall_time) * (0.32 + layer * 0.20);
        // The head disappears at the ground before ripples expand horizontally.
        if (i == 0 && age >= fall_time && age < fall_time + 0.30) {
            float life = (age - fall_time) / 0.30;
            vec2 ground_delta = (uv - landing) * pixel_span;
            float ring_distance = length(ground_delta * vec2(1.0, 2.5));
            float ring = 1.0 - smoothstep(0.30, 0.95, abs(ring_distance - (0.4 + life * 4.0)));
            float spray_height = sin(life * 3.14159) * 3.1;
            vec2 spray = abs(ground_delta - vec2(0.0, -spray_height));
            float beads = (1.0 - smoothstep(0.30, 1.0, abs(spray.x - life * 3.0)))
                * (1.0 - smoothstep(0.35, 1.0, spray.y));
            rain += (ring * 0.33 + beads * 0.25) * (1.0 - life)
                * weather_paving(landing);
        }
    }
    return rain;
}

float snow_layer(vec2 uv, float layer) {
    vec2 pixel_span = canvas_size / texture_rect.zw * (540.0 / canvas_size.y);
    vec2 grid = vec2(28.0 - layer * 6.0, 16.0 - layer * 3.0);
    float speed = 0.033 + layer * 0.019;
    vec2 flow = uv - vec2(weather_wind * 0.35, speed) * environment_elapsed;
    vec2 base = floor(flow * grid);
    float snow = 0.0;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 id = base + vec2(float(x), float(y));
            float seed = cloud_hash(id + vec2(21.3, layer * 17.0));
            if (seed > weather_snow * (0.75 - layer * 0.18)) continue;
            float other = cloud_hash(id.yx + vec2(layer * 9.1, 7.2));
            vec2 center = (id + vec2(seed, other)) / grid;
            // Per-flake flutter around a continuously falling trajectory.
            center.x += sin(environment_elapsed * (0.7 + seed) + other * 18.0) * (0.004 + layer * 0.002);
            vec2 delta = (flow - center) * pixel_span;
            float radius = (0.65 + layer * 0.68) * (0.7 + other * 0.55);
            float diamond = abs(delta.x) * 0.75 + abs(delta.y);
            float body = 1.0 - smoothstep(radius * 0.32, radius + 0.65, diamond);
            snow += body * (0.35 + layer * 0.22);
        }
    }
    return snow;
}

void fragment() {
    vec2 uv = texture_rect.xy + UV * texture_rect.zw;
    float particles = 0.0;
    if (!reduced_motion) {
        if (weather_rain > 0.0) {
            particles += rain_layer(uv, 0.0) + rain_layer(uv, 1.0) + rain_layer(uv, 2.0);
        }
        if (weather_snow > 0.0) {
            particles += snow_layer(uv, 0.0) + snow_layer(uv, 1.0) + snow_layer(uv, 2.0);
        }
    }
    float mist = cloud_noise(uv * vec2(4.3, 6.8) - vec2(environment_elapsed * 0.026, 0.0));
    mist *= 0.48 + cloud_noise(uv * vec2(8.0, 13.0) - vec2(environment_elapsed * 0.041, 0.0)) * 0.52;
    mist *= weather_mist * smoothstep(0.10, 0.50, UV.y) * 0.40;
    float flash = weather_flash();
    vec3 rain_color = mix(vec3(0.78, 0.86, 0.93), vec3(0.94, 0.97, 1.0), flash);
    vec3 particle_color = mix(rain_color, vec3(0.89, 0.94, 1.0), weather_snow);
    float opacity = clamp(particles * (1.0 + flash * 0.65), 0.0, 0.86);
    float fog_opacity = mist + flash * 0.07 * (1.0 - UV.y * 0.6);
    vec3 color = mix(vec3(0.55, 0.67, 0.76), particle_color, opacity / max(0.001, opacity + fog_opacity));
    COLOR = vec4(color, min(0.90, opacity + fog_opacity));
}
