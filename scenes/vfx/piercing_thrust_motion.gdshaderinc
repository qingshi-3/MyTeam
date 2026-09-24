uniform float age=0.0;
uniform float sweep_duration=.18;
uniform float tail_duration=.36;
uniform float cutoff_time=-1.0;
uniform float release=0.0;
uniform float motion_amount=1.0;
float thrust_arrival(float u){return (1.0-sqrt(max(0.0,1.0-u)))*sweep_duration;}
vec3 thrust_shape(vec2 uv){
    float passed=age-thrust_arrival(uv.x);
    float allowed=cutoff_time<0.0?1.0:step(thrust_arrival(uv.x),cutoff_time);
    float path_time=cutoff_time<0.0?age:min(age,cutoff_time);
    float q=clamp(path_time/sweep_duration,0.0,1.0);
    float front=1.0-pow(1.0-q,2.0);
    float behind=mix(1.0,front,motion_amount)-uv.x;
    // A long straight point leads into a rear shoulder. The short, offset
    // shoulder taper must not form the symmetric oval of a flying light orb.
    float nose=pow(clamp(behind/.58,0.0,1.0),1.25);
    float rear=1.0-smoothstep(.55,.84,behind);
    float width=.004+.37*nose*rear;
    float center=-.04*smoothstep(.32,.59,behind)*rear;
    float cross=abs(uv.y-.5)*2.0;
    float silhouette=(1.0-smoothstep(max(0.0,width-.012),width+.008,abs((uv.y-.5)*2.0-center)))
        *smoothstep(0.0,.006,behind)*(1.0-step(.84,behind));
    float head=silhouette*(1.0-smoothstep(sweep_duration,sweep_duration+.16,age));
    float tail=exp(-pow(cross/.13,2.0))*smoothstep(0.0,.012,passed)
        *(1.0-smoothstep(.02,tail_duration,passed))*allowed;
    float tip=exp(-pow(cross/.026,2.0))*head;
    if(motion_amount<.5){
        float envelope=smoothstep(0.0,.06,age)*(1.0-smoothstep(sweep_duration,sweep_duration+tail_duration,age));
        head=silhouette*.22*envelope;
        tail=0.0;tip=0.0;
    }
    return vec3(head,tail,tip)*smoothstep(0.0,.025,uv.x);
}
