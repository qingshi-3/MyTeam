shader_type canvas_item;
render_mode unshaded;
uniform float lift=0.0;
uniform float arrival=0.0;
uniform float fade=0.0;
uniform float release=0.0;
// Crop transparent padding without moving the authored ground contact.
uniform vec4 source_region=vec4(.101,.059,.799,.883);
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    float y=UV.y-(1.0-lift);
    vec2 image_uv=source_region.xy+vec2(UV.x,y)*source_region.zw;
    vec4 rock=texture(TEXTURE,image_uv);
    float clip=step(0.0,y)*step(y,1.0);
    float alpha=pow(rock.a,2.2)*clip;
    vec3 color=rock.rgb*(1.08+arrival*.26)+vec3(.11,.057,.018)*arrival;
    float ground_edge=1.0-smoothstep(.972,1.0,UV.y);
    COLOR=vec4(color,alpha*ground_edge*(1.0-fade)*(1.0-release))*layer_color;
}
