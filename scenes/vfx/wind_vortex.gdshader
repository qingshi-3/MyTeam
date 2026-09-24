shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float entry = 0.0;
uniform float release = 0.0;
uniform float height = 178.0;
uniform float radius = 100.0;
uniform float back_pass = 0.0;
uniform float motion_amount = 1.0;
uniform vec2 ground_x = vec2(1.0, 0.0);
uniform vec2 ground_y = vec2(0.0, 0.6);
uniform sampler2D flow_texture : repeat_enable, filter_linear_mipmap;
varying float depth;
varying float azimuth;
varying float altitude;

void vertex() {
    float h = UV.y;
    float theta = UV.x * 3.14159265 + back_pass * 3.14159265;
    float bend = sin(h * 5.4 + age * 1.1) * 12.0 * h;
    float funnel = 0.18 + 0.82 * pow(h, 1.3);
    float uneven = 1.0 + sin(theta * 3.0 - age * 2.4 + h * 8.0) * .045;
    float r = radius * funnel * uneven;
    float lift = height * h * mix(1.0,.55 + .45 * entry,motion_amount);
    VERTEX = (ground_x * cos(theta) + ground_y * sin(theta)) * r * mix(1.0,.65 + .35 * entry,motion_amount)
        + vec2(bend, -lift - release * release * 28.0 * h * motion_amount);
    depth = sin(theta);
    azimuth = theta;
    altitude = h;
}

void fragment() {
    float h = altitude;
    float swirl = azimuth / 6.2831853 - age * (.7 + h * .22) + h * 1.75;
    // The generated asset is a horizontal flow strip with transparent padding.
    // Sample its painted middle; wrapping the padding would sever the lower body.
    vec4 broad = texture(flow_texture, vec2(swirl, mix(.27,.70,h) + sin(azimuth + age) * .025));
    vec4 finer = texture(flow_texture, vec2(swirl * 1.65 + .37, .46 + sin(h*9.0-age*.8)*.17));
    float broad_ink=broad.a*(.40+.60*dot(broad.rgb,vec3(.3,.5,.2)));
    float fine_ink=finer.a*(.40+.60*dot(finer.rgb,vec3(.3,.5,.2)));
    float stream = broad_ink * .68 + fine_ink * .26;
    float helix = .5 + .5 * sin(azimuth * 2.0 - age * 5.0 + h * 19.0);
    float veil = pow(stream, .78) * (.58 + .42 * helix);
    float top = 1.0 - smoothstep(.84, 1.0, h);
    float base = smoothstep(0.0, .065, h);
    float seam = smoothstep(0.0, .14, abs(depth));
    float body = veil * (.27 + .32 * max(0.0, depth)) + stream * .15;
    float light = pow(helix, 7.0) * stream * .13;
    float erosion = smoothstep(release * 1.2 - .2, release * 1.2 + .18, stream + .15);
    vec3 color = mix(vec3(.19,.43,.49), vec3(.72,.95,.95), clamp(veil + light + max(depth,0.)*.2,0.,1.));
    COLOR = vec4(color, (body + light) * base * top * seam * entry * erosion * (1.0-release));
}
