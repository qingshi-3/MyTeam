// Included after courtyard_lighting: one instance-owned environment_elapsed.
uniform float weather_overcast = 0.0;
uniform vec4 weather_sky : source_color = vec4(1.0);
uniform float weather_rain = 0.0;
uniform float weather_snow = 0.0;
uniform float weather_wetness = 0.0;
uniform float weather_snow_cover = 0.0;
uniform float weather_mist = 0.0;
uniform float weather_wind = 0.16;
uniform float weather_lightning = 0.0;

float weather_flash() {
    if (reduced_motion || weather_lightning <= 0.0) return 0.0;
    float cycle = floor(environment_elapsed / 11.7);
    float age = mod(environment_elapsed, 11.7) - 2.6
        - cloud_hash(vec2(cycle, 37.2)) * 3.0;
    // Distant sheet lightning: a brief main flash, weaker return, then decay.
    float first = smoothstep(0.0, 0.025, age) * exp(-max(0.0, age) * 12.0);
    float second = smoothstep(0.19, 0.215, age) * exp(-max(0.0, age - 0.19) * 14.0) * 0.42;
    return (first + second) * weather_lightning;
}

float weather_paving(vec2 uv) {
    // Authored courtyard ground plane only; no gameplay collision queries.
    float central = smoothstep(0.12, 0.24, uv.x) * (1.0 - smoothstep(0.86, 0.97, uv.x));
    return central * smoothstep(0.24, 0.32, uv.y);
}

vec3 weather_surface(vec3 albedo, vec2 uv) {
    vec3 illumination = mix(courtyard_illumination(uv),
        weather_sky.rgb * (0.90 + 0.10 * courtyard_cloud_transmission(uv)), weather_overcast);
    vec3 result = albedo * illumination;
    float stone = 1.0 - smoothstep(0.035, 0.16, albedo.g - (albedo.r + albedo.b) * 0.5);
    float paving = weather_paving(uv);
    if (weather_wetness > 0.0) {
        float puddle = smoothstep(0.40, 0.67, cloud_noise(uv * vec2(19.0, 31.0)));
        puddle *= stone * paving * weather_wetness;
        result *= 1.0 - weather_wetness * 0.11;
        // Broad, broken sky reflections. Stone seams remain visible.
        float reflection = 0.075 + 0.045 * cloud_noise(uv * vec2(13.0, 6.0)
            - vec2(environment_elapsed * 0.024, 0.0));
        result += vec3(0.56, 0.72, 0.81) * puddle * reflection;
    }
    if (weather_snow_cover > 0.0) {
        float luminance = dot(albedo, vec3(0.299, 0.587, 0.114));
        float broken_cover = smoothstep(0.28, 0.70, cloud_noise(uv * vec2(37.0, 52.0)));
        float caps = smoothstep(0.24, 0.67, luminance) * (0.40 + 0.60 * broken_cover);
        caps *= mix(0.58, 1.0, stone) * weather_snow_cover;
        vec3 snow = vec3(0.79, 0.86, 0.91) * (0.79 + luminance * 0.18);
        result = mix(result, snow, caps);
    }
    float flash = weather_flash();
    result += albedo * vec3(0.58, 0.68, 0.85) * flash;
    return result;
}
