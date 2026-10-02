from pathlib import Path
import hashlib
import html
import json

base = Path(__file__).resolve().parent
plan = json.loads((base / "plan.json").read_text())
items = [x for x in plan["items"] if (base / "after" / (x["id"] + ".png")).is_file()]
installed = (base / "replacement.json").is_file()
page = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>剩余立绘优化 · 32张</title><style>
*{box-sizing:border-box}body{margin:0;padding:26px;background:#211e1a;color:#ece5d9;font:16px/1.6 system-ui,sans-serif}main{max-width:1600px;margin:auto}h1{font-size:26px;margin:0 0 10px}p{color:#c5b9a9;margin:10px 0 20px}a{color:#d6c6a0}.grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:22px}article{min-width:0}img{display:block;width:100%}h2{font-size:18px;margin:8px 0 0}small{color:#a59d91;display:block}nav{display:flex;gap:14px;flex-wrap:wrap;margin:4px 0 10px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0}figcaption{padding:8px 0}button,select{background:#39332c;color:#e9dfce;border:1px solid #625342;padding:9px 14px;font:inherit;border-radius:4px}.filters{display:flex;gap:12px;margin:18px 0 25px}dialog{width:min(1250px,94vw);max-height:94vh;background:#27221d;color:#ece5d9;border:1px solid #756a57;padding:22px}dialog::backdrop{background:#000b}.modal-head{display:flex;align-items:center;justify-content:space-between;gap:10px}.modal-head h2{margin:0}dialog .pair img{width:100%;max-height:76vh;object-fit:contain}details{margin:10px 0}summary{cursor:pointer;color:#cbb995}@media(max-width:1000px){.grid{grid-template-columns:repeat(3,1fr)}}@media(max-width:680px){.grid{grid-template-columns:repeat(2,1fr)}.pair{grid-template-columns:1fr}body{padding:16px}}
</style><main><h1>剩余立绘 · 统一画法优化</h1>'''
page += '<p>' + ('32张已替换正式素材。' if installed else f'当前已生成 {len(items)} / 32 张，正在逐张复查。') + ' 加深外轮廓，整理发束与衣褶，收敛零碎阴影和装饰，搭配低饱和柔光背景。原先已完成的三张保留。</p>'
page += '<div class="filters"><select id="group"><option value="all">全部</option><option value="f1">金甲与日耀</option><option value="f2">赤影</option><option value="f3">流沙</option><option value="f4">暗影与血族</option><option value="f5">龙兽与机械</option><option value="f6">冰晶与寒霜</option><option value="boss">首领</option></select><a href="shared-style.txt">通用提示词</a></div><div class="grid">'
for item in items:
    key = item["id"]
    name = html.escape(item["name"])
    stamp = hashlib.sha256((base / "after" / (key + ".png")).read_bytes()).hexdigest()[:12]
    page += f'<article data-group="{key.split("_")[0]}"><a href="after/{key}.png?v={stamp}" class="compare" data-key="{key}" data-name="{name}"><img src="after/{key}.png?v={stamp}" loading="lazy" width="1024" height="1536" alt="{name}优化稿"></a><h2>{name}</h2><small>{key}</small><nav><a class="compare" href="before/{key}.png" data-key="{key}" data-name="{name}">前后对照</a><a href="records/{key}.json">完整提示词</a></nav>'
    if (base / "runtime" / (key + ".png")).is_file():
        runtime_stamp = hashlib.sha256((base / "runtime" / (key + ".png")).read_bytes()).hexdigest()[:12]
        page += f'<details><summary>游戏卡框效果</summary><a href="runtime/{key}.png?v={runtime_stamp}"><img loading="lazy" src="runtime/{key}.png?v={runtime_stamp}" alt="{name}正式卡片"></a></details>'
    page += '</article>'
page += '''</div><dialog id="modal"><div class="modal-head"><h2 id="title"></h2><button id="close">关闭</button></div><div class="pair"><figure><figcaption>优化前</figcaption><img id="before" alt="优化前"></figure><figure><figcaption>优化后</figcaption><img id="after" alt="优化后"></figure></div></dialog><script>
const modal=document.querySelector('#modal');document.querySelectorAll('.compare').forEach(a=>a.onclick=e=>{e.preventDefault();const k=a.dataset.key;document.querySelector('#title').textContent=a.dataset.name+' · '+k;document.querySelector('#before').src='before/'+k+'.png';document.querySelector('#after').src=document.querySelector('article img[src*="/'+k+'.png"]').src;modal.showModal()});document.querySelector('#close').onclick=()=>modal.close();document.querySelector('#group').onchange=e=>document.querySelectorAll('article').forEach(a=>a.hidden=e.target.value!=='all'&&a.dataset.group!==e.target.value);
</script></main></html>'''
(base / "index.html").write_text(page)
print("Gallery", len(items), "installed", installed)
