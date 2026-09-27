shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform bool active=false;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
float hash(float n){return fract(sin(n*127.13+31.7)*43758.54);}
float crack(float x){
    float n=floor(x*23.),t=fract(x*23.);
    return mix(hash(n),hash(n+1.),t)*.34+.33;
}
void fragment(){
    float cell=floor(UV.x*23.),t=fract(UV.x*23.);
    float center=crack(UV.x),d=abs(UV.y-center);
    float width=mix(.026,.088,hash(cell+78.))*(.55+.45*sin(t*3.14159));
    // Narrow black fissures and broad chipped edges replace the luminous spine.
    float seam=1.-smoothstep(width*.6,width,d);
    float chipped=1.-smoothstep(width+.016,width+.057,d);
    float branch=0.,branchEdge=0.;
    for(int i=0;i<9;i++){
        float k=float(i),start=.035+k*.112,span=.035+hash(k+21.)*.046;
        float u=(UV.x-start)/span;
        float y=crack(start)+(i%2==0?1.:-1.)*(u*.34+abs(u-.45)*.14);
        float gate=step(0.,u)*step(u,1.);
        branch=max(branch,(1.-smoothstep(.008,.027*(1.-u)+.009,abs(UV.y-y)))*gate);
        branchEdge=max(branchEdge,(1.-smoothstep(.028,.055,abs(UV.y-y)))*gate);
    }
    float ends=smoothstep(0.,.025,UV.x)*(1.-smoothstep(.965,1.,UV.x));
    float front=active?1.-smoothstep(age*3.4,age*3.4+.12,UV.x):smoothstep(0.,.20,age);
    float gap=smoothstep(.10,.22,hash(cell+110.));
    float core=max(seam*gap,branch*.8);
    float edge=max(chipped,branchEdge*.8);
    float glow=active?seam*pow(max(0.,sin(UV.x*109.+.8)),6.)*.65:0.;
    vec3 color=mix(vec3(.31,.26,.20),vec3(.025,.019,.012),core);
    color=mix(color,vec3(.9,.43,.12),glow);
    COLOR=vec4(color,max(edge*.75,core*.97)*ends*front)*layer_color;
}
