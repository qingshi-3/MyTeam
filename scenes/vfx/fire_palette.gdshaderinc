vec3 fire_color(float heat) {
    vec3 red = vec3(0.72, 0.035, 0.008);
    vec3 orange = vec3(1.0, 0.27, 0.015);
    vec3 yellow = vec3(1.0, 0.86, 0.25);
    vec3 light = mix(red, orange, smoothstep(0.12, 0.55, heat));
    light = mix(light, yellow, smoothstep(0.55, 0.85, heat));
    return mix(light, vec3(1.0, 0.97, 0.78), smoothstep(0.88, 1.0, heat));
}
