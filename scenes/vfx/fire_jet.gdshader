shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float supply_duration = .5;
uniform float transit_duration = .32;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+1.),f.x),f.y);}
void fragment(){
    float x=UV.x;
    float time=age*motion_amount;
    float n=noise(vec2(x*11.-time*9.,UV.y*12.));
    float m=noise(vec2(x*23.-time*17.,UV.y*23.+3.));
    float w=.025+pow(x,.8)*.36;
    float center=.5+(n-.5)*.12*x;
    float side=abs(UV.y-center)/w;
    float body=1.-smoothstep(.47+n*.52,.62+n*.53,side);
    float breakup=mix(1.,smoothstep(.27,.48,n*.55+m*.45),smoothstep(.12,.65,x));
    float local=age-x*transit_duration;
    float start=smoothstep(0.,.045,local);
    float stop=1.-smoothstep(supply_duration,supply_duration+.10,local);
    float heat=clamp((1.-side)*.48+(n*.6+m*.4)*.66-x*.10,0.,1.);
    vec3 color=mix(vec3(.48,.035,.008),vec3(1.,.24,.015),smoothstep(.12,.55,heat));
    color=mix(color,vec3(1.,.76,.16),smoothstep(.56,.88,heat));
    float edge=(1.-smoothstep(.38,.88,x))*(1.-release);
    COLOR=vec4(color,body*breakup*start*stop*edge)*layer_color;
}
