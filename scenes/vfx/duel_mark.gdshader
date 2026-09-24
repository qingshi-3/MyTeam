shader_type canvas_item;
render_mode unshaded;
float segment(vec2 p, vec2 a, vec2 b) {
    vec2 d = b-a; return length(p-a-d*clamp(dot(p-a,d)/dot(d,d),0.0,1.0));
}
void fragment() {
    vec2 p = UV*2.0-1.0;
    float radius = length(p);
    float angle = atan(p.y,p.x);
    float ring = (1.0-smoothstep(0.012,0.042,abs(radius-0.83))) * smoothstep(0.25,0.50,abs(sin(angle*2.0)));
    float blade = min(segment(p,vec2(-.32,.32),vec2(.3,-.3)),segment(p,vec2(.32,.32),vec2(-.3,-.3)));
    float guard = min(segment(p,vec2(-.36,.1),vec2(-.1,.36)),segment(p,vec2(.36,.1),vec2(.1,.36)));
    float swords = 1.0 - smoothstep(.025,.055,min(blade,guard));
    COLOR = vec4(1.0,.74,.24,max(ring*.75,swords)) * COLOR;
}
