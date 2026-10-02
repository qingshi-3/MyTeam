shader_type canvas_item;
render_mode unshaded;
uniform int shape = 0;
uniform float age = 0.0;
uniform float motion_amount = 1.0;
uniform vec4 tint : source_color = vec4(0.25, 0.65, 1.0, 1.0);
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float alpha = 0.0;
    float core = 0.0;
    float time = age * motion_amount;
    if (shape == 0) { // orb: bright center, two unequal bands, dim corona
        float r = length(p);
        float a = atan(p.y, p.x);
        float rim = abs(r - (0.57 + 0.035 * sin(a * 3.0 - time * 10.0)));
        alpha = exp(-r*r*4.5)*0.85 + exp(-rim*rim*600.0)*0.55;
        core = exp(-r*r*18.0);
    } else if (shape == 1) { // water: large head and two trailing streams
        float y = p.y + 0.12*sin(p.x*5.0-time*13.0)*(1.0-p.x)*motion_amount;
        float head = length(vec2((p.x-0.38)*1.7, y*2.2));
        float tail = abs(y - 0.14*sin(p.x*7.0-time*9.0))/(0.08+0.12*(p.x+1.0));
        alpha = (1.0-smoothstep(0.65,0.94,head)) + exp(-tail*tail)*smoothstep(-1.0,-0.5,p.x)*(1.0-smoothstep(0.0,0.6,p.x));
        core = exp(-head*head*6.0);
    } else if (shape == 2) { // one main soul and two tiny orbiting wisps
        vec2 q = vec2(p.x-0.18,p.y);
        float r = length(q*vec2(1.8,2.7));
        alpha = exp(-r*r*2.0);
        core = exp(-r*r*8.0);
        for (int i=0; i<2; i++) {
            float a=time*7.0+float(i)*3.14159;
            vec2 center=vec2(-0.34+0.08*cos(a),0.36*sin(a));
            float w=length((p-center)*vec2(5.0,6.0));
            alpha += exp(-w*w*1.8)*0.65;
        }
        float wispy=(p.y-0.1*sin(p.x*6.0-time*8.0))*7.0;
        alpha += exp(-wispy*wispy)*smoothstep(-1.0,-0.6,p.x)*(1.0-smoothstep(-0.3,0.2,p.x))*0.4;
    } else if (shape == 3) { // rotating crescent, open concave back
        float r=length(p);
        float rim=abs(r-0.62);
        float forward=smoothstep(-0.35,0.65,p.x/max(r,0.001));
        alpha=exp(-rim*rim*450.0)*forward;
        alpha += exp(-rim*rim*45.0)*forward*0.24;
        core=exp(-rim*rim*1000.0)*forward;
    } else { // hard, short bullet with tapered tracer; no round spell corona
        float head=length(vec2((p.x-0.26)*2.8,p.y*7.0));
        float line=abs(p.y)*13.0;
        alpha=(1.0-smoothstep(0.72,1.0,head)) + exp(-line*line)*smoothstep(-0.95,-0.45,p.x)*(1.0-smoothstep(0.0,0.5,p.x))*0.7;
        core=exp(-head*head*4.0);
    }
    vec3 color=mix(tint.rgb*0.65, tint.rgb, clamp(alpha,0.0,1.0));
    color=mix(color,vec3(1.0,0.98,0.84),core*0.85);
    COLOR=vec4(color,clamp(alpha,0.0,1.0)*tint.a)*layer_color;
}
