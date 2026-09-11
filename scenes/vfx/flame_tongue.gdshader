shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float visibility = 1.0;
uniform float particle_progress = 0.0;
uniform float motion_amount = 1.0;
uniform float tip_sway = 0.0;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+1.),f.x),f.y);}
void fragment(){
    float h=1.-UV.y;
    float flow=age*1.45;
    float n=noise(vec2(UV.x*6.,UV.y*4.+flow));
    float fine=noise(vec2(UV.x*14.+2.,UV.y*9.+flow*1.8));
    vec2 uv=UV;
    uv.x+=(n-.5)*.20*h*motion_amount;
    // Optional local tip movement. The lower flame and source silhouette stay intact.
    uv.x+=tip_sway*smoothstep(.35,.9,h)*sin(age*7.-h*3.)*motion_amount;
    uv.y+=(fine-.5)*.10*h*motion_amount;
    vec4 fire=texture(TEXTURE,clamp(uv,vec2(.001),vec2(.999)));
    float tears=smoothstep(.12+h*.24,.35+h*.24,n*.7+fine*.3+.22*(1.-h));
    // Suppress broad baked haze while retaining the authored flame boundary.
    float alpha=pow(fire.a,1.35)*mix(1.,tears,h*motion_amount);
    alpha*=1.-smoothstep(.62,.98,particle_progress);
    float heat=clamp(fire.g+.13*(fine-.5)*motion_amount,0.,1.);
    vec3 color=mix(fire.rgb,vec3(1.,.8,.20),smoothstep(.55,.96,heat)*.38);
    COLOR=vec4(color,alpha*visibility)*layer_color;
}
