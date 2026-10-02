shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 uv=UV;
    float front=motion_amount<0.5 ? 1.0 : clamp(age/0.055,0.0,1.0);
    float down=1.0-smoothstep(front-0.035,front+0.015,uv.y);
    float fade=1.0-smoothstep(0.18,0.36,age);
    // A clear shaft and softer shoulders replace the equal, opaque stripes.
    float shrink=mix(1.0,0.68,smoothstep(0.18,0.36,age));
    float center=0.49+0.009*sin(uv.y*8.0-age*4.0*motion_amount);
    float x=(uv.x-center)/shrink;
    float cap=smoothstep(0.025,0.13,uv.y)*(1.0-smoothstep(0.94,1.0,uv.y));
    float core=exp(-x*x*330.0)*0.72;
    float shoulder=exp(-x*x*58.0)*0.18;
    float veil=exp(-x*x*15.0)*0.035;
    // Two unequal filaments live within the beam, with separate tip/fade masks.
    float left=exp(-pow((uv.x-0.31)/0.026,2.0))
        *smoothstep(0.13,0.20,uv.y)*(1.0-smoothstep(0.84,0.95,uv.y))
        *(1.0-smoothstep(0.14,0.31,age))*0.24;
    float right=exp(-pow((uv.x-0.67)/0.034,2.0))
        *smoothstep(0.035,0.08,uv.y)*(1.0-smoothstep(0.72,0.86,uv.y))
        *(1.0-smoothstep(0.18,0.34,age))*0.17;
    float body=(core+shoulder+veil)*cap*fade;
    float threads=left+right;
    vec3 radiance=vec3(1.0,0.97,0.80)*core*cap*fade
        +vec3(1.0,0.83,0.46)*(shoulder+veil)*cap*fade
        +vec3(1.0,0.77,0.31)*threads;
    float alpha=body+threads;
    vec3 rgb=radiance/max(alpha,0.001);
    COLOR=vec4(rgb,alpha*down*(1.0-release))*layer_color;
}
