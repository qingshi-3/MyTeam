shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform bool active=false;
void fragment(){float line=.5+sin(floor(UV.x*27.0)*13.7)*.17;float d=abs(UV.y-line);float edge=1.0-smoothstep(.04,.07,d);float core=1.0-smoothstep(.012,.035,d);float ends=smoothstep(0.0,.03,UV.x)*(1.0-smoothstep(.95,1.0,UV.x));COLOR=vec4(mix(vec3(.25,.18,.10),vec3(1.0,.71,.32),core),edge*ends*(active?1.0:.45));}
