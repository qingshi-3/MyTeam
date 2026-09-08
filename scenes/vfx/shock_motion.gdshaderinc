// Same radial motion for the bright crest and its textured pressure wake.
uniform float impulse_share = 0.65;
uniform float impulse_rate = 14.0;
float shock_radius(float progress, float motion) {
    float t = clamp(progress, 0.0, 1.0);
    float travel = impulse_share * (1.0 - exp(-impulse_rate * t)) + (1.0 - impulse_share) * t;
    return mix(0.36, 0.025 + 0.435 * travel, motion);
}
