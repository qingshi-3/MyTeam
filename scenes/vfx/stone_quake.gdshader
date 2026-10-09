shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float impact = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
uniform float half_angle = 0.610865;
void fragment() {
 vec2 p=UV*2.0-1.0;
 float r=length(p);
 float angle=abs(atan(p.y,p.x));
 float disk=1.0-smoothstep(0.984,1.0,r);
 float rim=(1.0-smoothstep(0.012,0.024,abs(r-0.974)))*disk;
 float alpha=(rim*0.75+disk*0.045)*(1.0-release);
 COLOR=vec4(0.78,0.64,0.40,alpha)*COLOR;
}
