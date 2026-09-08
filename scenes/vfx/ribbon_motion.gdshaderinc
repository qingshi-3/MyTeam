// Shared by the painted stroke and its light spill, so both follow exactly
// the same per-instance arrival, tail drift and reduced-motion behavior.
uniform float age = 0.0;
uniform float sweep_duration = 0.17;
uniform float tail_duration = 0.24;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
float arrival(float u) {
    return (1.0 - sqrt(max(0.0, 1.0 - u))) * sweep_duration;
}
float passed_time(float u) {
    return mix(age - arrival(u), age, 1.0 - motion_amount);
}
void vertex() {
    vec2 tangent = COLOR.rg * 2.0 - 1.0;
    float passed = max(0.0, age - arrival(UV.x));
    VERTEX += tangent * min(passed, tail_duration) * 28.0 * motion_amount;
    COLOR = vec4(1.0);
}
