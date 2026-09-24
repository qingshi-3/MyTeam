shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform bool active=false;
uniform float cone_slope=0.7002075;
float hash(vec2 p){return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453);}
float noise(vec2 p){vec2 i=floor(p),f=fract(p);f=f*f*(3.0-2.0*f);return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);}
void fragment(){
 vec2 q=vec2(UV.x,(UV.y-.5)*cone_slope*2.0);float r=length(q);float side=abs(q.y)-q.x*cone_slope;
 float boundary=(1.0-smoothstep(.008,.025,abs(side)))*(1.0-smoothstep(.96,1.0,r));
 float n=noise(vec2(q.x*9.0-age*7.0,q.y*10.0+sin(q.x*7.0-age*4.0)));
 float mask=(1.0-smoothstep(-.055,.018,side+(n-.45)*.12))*(1.0-smoothstep(.78+n*.16,1.0,r));
 float throat=exp(-length(vec2(q.x-.06,q.y))*22.0);
 if(active){
  float strands=sin(q.y*37.0/(q.x+.4)+q.x*11.0-age*8.0)+n*.65;
  float torn=smoothstep(-.45,.3,strands);
  float core=mask*(1.0-smoothstep(.01,.25,abs(q.y)+q.x*.14+(n-.5)*.2));
  float ember=smoothstep(.62,.85,n)*mask;
  COLOR=vec4(mix(vec3(.93,.22,.035),vec3(1.0,.88,.46),core),mask*(.16+torn*.34+ember*.12));
 }
 else{COLOR=vec4(mix(vec3(.82,.38,.14),vec3(1.0,.83,.37),throat),boundary*.3+throat*(.2+min(age,1.0)*.5));}
}
