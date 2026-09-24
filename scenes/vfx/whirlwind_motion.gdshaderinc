uniform float age = 0.0;
uniform float sweep_duration = .66;
uniform float tail_duration = .20;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform float cutoff_time = -1.0;
float whirl_arrival(float phase) {
    return (1.0-sqrt(max(0.0,1.0-phase)))*sweep_duration;
}
vec2 whirl_life(float u) {
    float tail = 0.0;
    float head = 0.0;
    for(int turn=0;turn<2;turn++) {
        float arrival=whirl_arrival((u+float(turn))*.5);
        float passed=age-arrival;
        float allowed=cutoff_time<0.0 ? 1.0 : step(arrival,cutoff_time);
        float born=smoothstep(0.0,.012,passed)*allowed;
        tail=max(tail,born*(1.0-smoothstep(.03,tail_duration,passed)));
        head=max(head,born*(1.0-smoothstep(.012,.05,passed)));
    }
    if(motion_amount<.5){
        tail=.18*smoothstep(0.0,.08,age)*(1.0-smoothstep(sweep_duration,sweep_duration+tail_duration,age));
        head=0.0;
    }
    return vec2(tail,head);
}
