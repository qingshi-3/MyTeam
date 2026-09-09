shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform sampler2D turbulence : repeat_enable, filter_linear_mipmap;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    vec2 p = (UV-.5)*2.0;
    float t = age * motion_amount;
    vec2 warp = texture(turbulence, UV * 1.7 + vec2(t*.018,-t*.014)).rg - .5;
    float n = texture(turbulence,UV*2.1 + warp*.24 + vec2(-t*.028,t*.017)).r;
    float m = texture(turbulence,UV*3.4 + vec2(t*.024,t*.013)).r;
    float radius = length(p);
    float shore = .83 + (texture(turbulence,UV*.8).r-.5)*.10;
    float expansion = mix(.18,1.0,smoothstep(0.0,.38,age));
    float wet = 1.0-smoothstep(shore*expansion-.035,shore*expansion+.025,radius);
    float rim = exp(-pow((radius-shore*expansion+.025)/.03,2.0));
    // Broad translucent body + moving narrow folds, not shaded lumps of mud.
    float fold = pow(max(0.0,1.0-abs(n-m)*2.0),8.0);
    float variation = smoothstep(.30,.75,n);
    vec3 body = mix(vec3(.018,.13,.09),vec3(.08,.40,.13),variation);
    body += vec3(.15,.27,.025)*fold*.32;
    body += rim*vec3(.10,.24,.02);
    float alpha = wet*(.28+variation*.16+fold*.08)+rim*.1;
    float breakup = smoothstep(release*.95,release*.95+.15,n*.5+.5);
    COLOR=vec4(body,alpha*(1.0-release)*breakup)*tint;
}
