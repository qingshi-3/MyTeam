// Static design proof only. Production SVG files are read, never overwritten.
// NODE_PATH=<bundled node_modules> node render-native-icons.cjs
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sharp = require('sharp');
const { createCanvas, loadImage, GlobalFonts } = require('@napi-rs/canvas');
const root = path.resolve(__dirname, '../../../..');
GlobalFonts.registerFromPath('/System/Library/Fonts/Supplemental/Songti.ttc', 'StudySong');
const entries = [
  ['攻击', 'keyword-attack', '22', 'iron'],
  ['法强', 'keyword-magic-damage', '0', 'violet'],
  ['生命', 'health', '155', 'red'],
  ['防御', 'keyword-armor', '1', 'iron'],
  ['攻击速度', 'keyword-attack-speed', '×1', 'bronze'],
  ['攻击距离', 'reach', '5.2', 'iron'],
  ['暴击率', 'critical-chance', '0%', 'bronze'],
  ['暴击伤害', 'critical-damage', '150%', 'bronze'],
  ['吸血', 'keyword-lifesteal', '0%', 'bone'],
  ['闪避', 'keyword-dodge', '0%', 'bronze'],
  ['控制抗性', 'control-resistance', '0%', 'iron'],
  ['移动速度', 'move-speed', '×1', 'leather'],
  ['法力上限', 'mana', '60', 'blue'],
  ['初始法力', 'mana-start', '20', 'iron'],
  ['每秒回蓝', 'mana-regeneration', '5', 'iron'],
  ['攻击回蓝', 'mana-attack', '10', 'iron'],
  ['受击回蓝', 'mana-damage', '50', 'iron'],
  ['单次回蓝上限', 'mana-hit-cap', '20', 'iron'],
  ['治疗强度', 'healing', '0', 'bone'],
  ['主动技能', 'skill-active', '', 'bronze'],
  ['被动技能', 'skill-passive', '', 'bronze'],
];
const palettes = {
  iron: ['#e0d9c5', '#aca693', '#6f7068'],
  bronze: ['#dfc999', '#b39460', '#766343'],
  bone: ['#e8ddc1', '#cbb991', '#948369'],
  leather: ['#c39b74', '#936b48', '#674631'],
  blue: ['#a2c6cf', '#5a9db0', '#3b6778'],
  violet: ['#c5b8dd', '#9c83b4', '#6c587e'],
  red: ['#ce8075', '#ad4e48', '#733630'],
};
const records = [];
function nativeIcon(file, material, styled) {
  const source = fs.readFileSync(path.join(root, 'assets/ui/icons', `${file}.svg`), 'utf8');
  const body = source.replace(/^[\s\S]*?<svg\b[^>]*>/, '').replace(/<\/svg>\s*$/, '');
  if (!styled) return source;
  const sets = { face: palettes[material], blue: palettes.blue,
    amber: palettes.bronze, rust: palettes.red, pale: palettes.bone };
  const defs = Object.entries(sets).map(([id, colors]) =>
    `<linearGradient id="${id}" x1="0" y1="0" x2=".5" y2="1" gradientUnits="objectBoundingBox"><stop stop-color="${colors[0]}"/><stop offset=".38" stop-color="${colors[1]}"/><stop offset="1" stop-color="${colors[2]}"/></linearGradient>`).join('');
  // Substitute paints only. Every path, primitive, stroke width and cutout is retained.
  const painted = body.replace(/#fff\b/gi, 'url(#face)').replace(/#62bcec/gi, 'url(#blue)')
    .replace(/#e1bb72/gi, 'url(#amber)').replace(/#d88864/gi, 'url(#rust)')
    .replace(/#f1dbac/gi, 'url(#pale)');
  const geometry = s => [...s.matchAll(/\b(?:d|cx|cy|r|x|y|width|height|stroke-width|fill-rule|stroke-linecap|stroke-linejoin)="[^"]*"/g)].map(m => m[0]);
  if (JSON.stringify(geometry(body)) !== JSON.stringify(geometry(painted))) throw new Error(`Geometry changed: ${file}`);
  return `<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="-2 -2 28 28"><defs>${defs}
  <filter id="relief" x="-25%" y="-25%" width="150%" height="160%" color-interpolation-filters="sRGB">
    <feGaussianBlur in="SourceAlpha" stdDeviation=".3" result="soft"/>
    <feOffset in="soft" dx=".3" dy=".9" result="shadowMask"/><feFlood flood-color="#100e0b" flood-opacity=".7"/>
    <feComposite in2="shadowMask" operator="in" result="shadow"/>
    <feOffset in="SourceAlpha" dy=".42" result="down"/>
    <feComposite in="SourceAlpha" in2="down" operator="out" result="topEdge"/>
    <feFlood flood-color="#f4e9d2" flood-opacity=".5"/><feComposite in2="topEdge" operator="in" result="light"/>
    <feOffset in="SourceAlpha" dy="-.42" result="up"/>
    <feComposite in="SourceAlpha" in2="up" operator="out" result="bottomEdge"/>
    <feFlood flood-color="#201b16" flood-opacity=".55"/><feComposite in2="bottomEdge" operator="in" result="dark"/>
    <feMerge><feMergeNode in="shadow"/><feMergeNode in="SourceGraphic"/><feMergeNode in="dark"/><feMergeNode in="light"/></feMerge>
  </filter></defs><g filter="url(#relief)">${painted}</g></svg>`;
}
async function iconImage(file, material, styled) {
  const svg = nativeIcon(file, material, styled);
  const png = await sharp(Buffer.from(svg), {density: 288}).png().toBuffer();
  return loadImage(png);
}
async function nameRibbon(axe) {
  const raster = path.join(__dirname, 'd-relic-refined.png');
  const {data, info} = await sharp(raster).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const original = Buffer.from(data);
  // Local non-generative texture repair: remove only the rejected arrowhead.
  // Keep name, numerals, heart and the complete ribbon edge pixel-identical.
  const box = {x:765, y:196, w:67, h:92};
  const rows = [];
  for (let j=0; j<box.h; j++) {
    const sides = [[], []];
    for(let c=0;c<3;c++) {
      for(let s=0;s<2;s++) {
        let sum=0, n=0;
        for(let yy=-3; yy<=3; yy++) for(let xx=0; xx<3; xx++) {
          const x=s ? box.x+box.w+xx : box.x-xx-1;
          sum+=original[((box.y+j+yy)*info.width+x)*4+c]; n++;
        }
        sides[s][c]=sum/n;
      }
    }
    rows.push(sides);
  }
  for(let j=0;j<box.h;j++) for(let i=0;i<box.w;i++) {
    const edge=Math.min(i/2,(box.w-1-i)/2,j/2,(box.h-1-j)/2,1);
    const t=i/(box.w-1), p=((box.y+j)*info.width+box.x+i)*4;
    const grain=Math.sin(i*1.87+j*4.1)*1.3 + Math.sin(i*.19+j*.27)*1.7;
    for(let c=0;c<3;c++) data[p+c]=Math.round(original[p+c]*(1-edge)+(rows[j][0][c]*(1-t)+rows[j][1][c]*t+grain)*edge);
  }
  const repaired = await sharp(data,{raw:info}).extract({left:748,top:180,width:747,height:128}).png().toBuffer();
  const canvas=createCanvas(747,128), ctx=canvas.getContext('2d');
  ctx.drawImage(await loadImage(repaired),0,0);
  ctx.drawImage(axe,9,17,87,87);
  await fs.promises.writeFile(path.join(__dirname,'name-ribbon-native-attack.png'),canvas.toBuffer('image/png'));
  return canvas;
}
async function main() {
  const rendered=[];
  for(const [label,file,value,mat] of entries) {
    const original=await iconImage(file,mat,false), styled=await iconImage(file,mat,true);
    rendered.push({label,file,value,original,styled});
    records.push({label,source:`assets/ui/icons/${file}.svg`,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'assets/ui/icons',`${file}.svg`))).digest('hex'),geometryPreserved:true});
  }
  const ribbon=await nameRibbon(rendered[0].styled);
  const canvas=createCanvas(1540,1200), ctx=canvas.getContext('2d');
  ctx.fillStyle='#24231f';ctx.fillRect(0,0,1540,1200);
  const txt=(str,x,y,size=22,color='#e6dbc6',font='StudySong')=>{ctx.font=`${size}px ${font}`;ctx.fillStyle=color;ctx.fillText(str,x,y);};
  txt('旧图标，原轮廓。只调整材质。',64,65,37);
  txt('沿用项目 SVG 原路径 · 名称栏保留 · 攻击恢复现用战斧',65,105,21,'#aaa08e');
  ctx.drawImage(ribbon,135,137,1270,217.6);
  txt('同一图形：左为现有源图，右为材质预览；下方为紧凑显示',65,411,21,'#b7aa93');
  ctx.strokeStyle='#514b3d';ctx.lineWidth=1;ctx.beginPath();ctx.moveTo(65,433);ctx.lineTo(1475,433);ctx.stroke();
  rendered.forEach((e,i)=>{
    const x=65+(i%7)*205,y=470+Math.floor(i/7)*220;
    txt(e.label,x,y,22);
    ctx.drawImage(e.original,x,y+34,34,34);
    txt('→',x+48,y+60,18,'#776d5a','Arial');
    ctx.drawImage(e.styled,x+78,y+13,91,91);
    ctx.drawImage(e.styled,x+11,y+124,29,29);
    txt(e.value || (i===19?'主动':'被动'),x+49,y+149,30,'#e8dfcd', e.value?'Georgia':'StudySong');
  });
  ctx.strokeStyle='#514b3d';ctx.beginPath();ctx.moveTo(65,1122);ctx.lineTo(1475,1122);ctx.stroke();
  txt('无新符号 · 无属性底板 · 无交叉武器装饰',65,1162,20,'#a99d88');
  txt('静态局部稿 / 尚未替换游戏资源',1130,1162,18,'#a99d88');
  await fs.promises.writeFile(path.join(__dirname,'native-icon-material-proof.png'),canvas.toBuffer('image/png'));
  await fs.promises.writeFile(path.join(__dirname,'native-icon-proof-sources.json'),JSON.stringify({scope:'Static preview; native geometry retained; production resources untouched',records},null,2)+'\n');
  console.log(`Rendered ${rendered.length} source-preserving icons and approved ribbon attack correction.`);
}
main().catch(e=>{console.error(e);process.exitCode=1;});
