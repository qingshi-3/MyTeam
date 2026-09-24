"""User-authorized background removal and deterministic atlas assembly; does not generate poses."""
from pathlib import Path
from collections import deque
import hashlib,json
from PIL import Image

ROOT=Path(__file__).resolve().parent
SOURCE=ROOT/'source-pixel-sheet.png'
ACTIONS=[('idle','待机',7,True),('move','疾跑',10,True),('attack','双刃斩击',10,False),('skill_cast','突袭挑斩',10,False),('hit','受击',14,False),('defeated','倒下',8,False)]
SIZE=80

def remove_background(cell):
    w,h=cell.size
    pixels=list(cell.convert('RGB').get_flattened_data())
    # The generated checkerboard is neutral grey. Retain dark outlines and colored pixels.
    mask=[max(c)-min(c)>23 or max(c)<116 for c in pixels]
    visited=bytearray(w*h)
    groups=[]
    for start,on in enumerate(mask):
        if not on or visited[start]: continue
        q=[start];visited[start]=1;component=[]
        while q:
            i=q.pop();component.append(i);x=i%w;y=i//w
            for j in ((i-1 if x else -1),(i+1 if x+1<w else -1),(i-w if y else -1),(i+w if y+1<h else -1)):
                if j>=0 and mask[j] and not visited[j]: visited[j]=1;q.append(j)
        # Eliminate neutral checkerboard artifacts; keep detached colored blade strokes.
        saturated=sum(max(pixels[i])-min(pixels[i])>35 for i in component)
        if len(component)>=10 and saturated>=3: groups.append(component)
    clean=bytearray(w*h)
    for component in groups:
        for i in component: clean[i]=1
    # Flood from the boundary: enclosed light highlights belong to the character, not the background.
    outside=bytearray(w*h);q=deque()
    for y in range(h):
        for x in (0,w-1):
            i=y*w+x
            if not clean[i] and not outside[i]: outside[i]=1;q.append(i)
    for x in range(w):
        for y in (0,h-1):
            i=y*w+x
            if not clean[i] and not outside[i]: outside[i]=1;q.append(i)
    while q:
        i=q.popleft();x=i%w;y=i//w
        for j in ((i-1 if x else -1),(i+1 if x+1<w else -1),(i-w if y else -1),(i+w if y+1<h else -1)):
            if j>=0 and not clean[j] and not outside[j]: outside[j]=1;q.append(j)
    alpha=[255 if clean[i] or not outside[i] else 0 for i in range(w*h)]
    result=Image.new('RGBA',(w,h));result.putdata([(*c,alpha[i]) for i,c in enumerate(pixels)])
    return result

def main():
    source=Image.open(SOURCE).convert('RGB');w,h=source.size
    assert w%6==0 and h%6==0
    cw,ch=w//6,h//6
    frames=[];records=[]
    for row,(key,label,fps,loop) in enumerate(ACTIONS):
        folder=ROOT/'frames'/key;folder.mkdir(parents=True,exist_ok=True)
        for col in range(6):
            raw=source.crop((col*cw,row*ch,(col+1)*cw,(row+1)*ch))
            clean=remove_background(raw).resize((72,72),Image.Resampling.NEAREST)
            box=clean.getbbox();assert box
            # Match floor height without stretching characters. Skill jump keeps vertical lift.
            target_bottom=64 if key=='skill_cast' and col==3 else 68
            dy=target_bottom-box[3]
            frame=Image.new('RGBA',(SIZE,SIZE))
            frame.alpha_composite(clean,(4,dy))
            assert frame.getchannel('A').getbbox()
            frames.append(frame)
            dest=folder/f'{col:02}.png';frame.save(dest)
            records.append({'action':key,'frame':col,'sourceRect':[col*cw,row*ch,cw,ch],'translation':[4,dy],'bbox':frame.getbbox(),'file':str(dest.relative_to(ROOT)).replace('\\','/')})
    atlas=Image.new('RGBA',(480,480))
    for i,frame in enumerate(frames): atlas.alpha_composite(frame,((i%6)*SIZE,(i//6)*SIZE))
    atlas=atlas.quantize(colors=32,method=Image.Quantize.FASTOCTREE,dither=Image.Dither.NONE).convert('RGBA')
    atlas.save(ROOT/'atlas.png')
    # Use one palette across all clips; color index 255 is reserved for GIF transparency.
    palette=atlas.convert('RGB').quantize(colors=31,dither=Image.Dither.NONE)
    gifs=ROOT/'gifs';gifs.mkdir(exist_ok=True)
    for row,(key,label,fps,loop) in enumerate(ACTIONS):
        clip=[]
        for col in range(6):
            frame=atlas.crop((col*SIZE,row*SIZE,(col+1)*SIZE,(row+1)*SIZE))
            frame.save(ROOT/'frames'/key/f'{col:02}.png')
            big=frame.resize((320,320),Image.Resampling.NEAREST)
            indexed=big.convert('RGB').quantize(palette=palette,dither=Image.Dither.NONE)
            indexed.paste(255,mask=big.getchannel('A').point(lambda a:255 if a==0 else 0))
            clip.append(indexed)
        durations=[round(1000/fps)]*6
        if not loop: durations[-1]=900
        clip[0].save(gifs/(key+'.gif'),save_all=True,append_images=clip[1:],duration=durations,loop=0,transparency=255,disposal=2,optimize=False)
    lines=['[gd_resource type="SpriteFrames" load_steps=38 format=3]','', '[ext_resource type="Texture2D" path="res://assets/units/fox-mask-assassin/atlas.png" id="1"]','']
    for i in range(36):
        lines += [f'[sub_resource type="AtlasTexture" id="Frame_{i:02}"]','atlas = ExtResource("1")',f'region = Rect2({i%6*SIZE}, {i//6*SIZE}, {SIZE}, {SIZE})','']
    anim=[]
    for row,(key,label,fps,loop) in enumerate(ACTIONS):
        entries=', '.join('{"duration": 1.0, "texture": SubResource("Frame_%02d")}'%(row*6+c) for c in range(6))
        anim.append('{"frames": ['+entries+'], "loop": '+str(loop).lower()+', "name": &"'+key+'", "speed": '+str(float(fps))+'}')
    lines+=['[resource]','animations = ['+',\n'.join(anim)+']']
    (ROOT/'frames.tres').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    scene='''[gd_scene load_steps=2 format=3]

[ext_resource type="SpriteFrames" path="res://assets/units/fox-mask-assassin/frames.tres" id="1"]

[node name="FoxMaskAssassinPreview" type="Node2D"]

[node name="AnimatedSprite2D" type="AnimatedSprite2D" parent="."]
texture_filter = 1
position = Vector2(0, -28)
sprite_frames = ExtResource("1")
animation = &"idle"
autoplay = "idle"
'''
    (ROOT/'preview.tscn').write_text(scene,encoding='utf-8')
    metadata={'identity':'fox-mask-assassin','style':'pixel-art','size':[80,80],'atlasSize':[480,480],'authoredFacingRight':True,'userAestheticApproval':False,'runtimeAssigned':False,'actions':[{'key':k,'label':l,'fps':f,'loop':loop,'frames':6} for k,l,f,loop in ACTIONS],'frames':records,'sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'atlasSha256':hashlib.sha256((ROOT/'atlas.png').read_bytes()).hexdigest(),'processing':'User authorized programmatic checkerboard removal, nearest-neighbor reduction, per-frame floor alignment, and atlas assembly. No synthesized poses or interpolated frames.'}
    (ROOT/'manifest.json').write_text(json.dumps(metadata,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Built 36 RGBA frames, 480x480 atlas, 6 clips, SpriteFrames and preview scene.')
    print('bounds:',[r['bbox'] for r in records])

if __name__=='__main__': main()
