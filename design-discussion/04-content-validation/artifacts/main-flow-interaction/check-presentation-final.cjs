const {chromium}=require('/Users/qingshi/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'}),page=await browser.newPage({viewport:{width:992,height:650}}),errors=[],checks=[];
 page.on('pageerror',e=>errors.push(e.message));await page.goto('http://127.0.0.1:8764/preview.html');const f=page.frameLocator('iframe');await f.locator('.brand').waitFor();
 const click=(a,v)=>f.locator('[data-act="'+a+'"]'+(v!==undefined?'[data-value="'+v+'"]':'')).first().click();
 const jump=async id=>{if(await f.locator('.review').isHidden())await click('review-toggle');await f.locator('[data-field=page]').selectOption(id);await click('review-toggle');await page.mouse.move(0,0);await page.waitForTimeout(320);};
 const ok=n=>checks.push(n),out=path.join(__dirname,'qa/presentation');
 await f.locator('.screen').screenshot({path:path.join(out,'menu.png')});
 await jump('route');await f.locator('.screen').screenshot({path:path.join(out,'route.png')});
 await jump('spells');await click('preset');assert.equal(await f.locator('.toast').count(),0);await page.mouse.move(0,0);await page.waitForTimeout(320);await f.locator('.screen').screenshot({path:path.join(out,'spells.png')});ok('预设由连接状态反馈，不重复弹文字说明');
 await jump('opponent');
 const overlaps=await f.locator('.opponent-card').evaluateAll(cards=>cards.flatMap(card=>{const blocks=[...card.children].map(x=>({text:x.textContent,r:x.getBoundingClientRect()}));return blocks.slice(1).filter((x,i)=>x.r.top<blocks[i].r.bottom-1).map(x=>x.text);}));assert.deepEqual(overlaps,[]);await f.locator('.screen').screenshot({path:path.join(out,'opponent.png')});ok('选敌卡的阵容、奖励、代价不重叠');
 for(const width of [1800,768,352]){await page.setViewportSize({width,height:900});for(const id of ['opening','recruit']){await jump(id);const result=await f.locator('.screen').evaluate(screen=>{const r=screen.getBoundingClientRect();return {client:document.documentElement.clientWidth,scroll:document.documentElement.scrollWidth,clipped:[...screen.querySelectorAll('.candidate')].some(n=>{const b=n.getBoundingClientRect();return b.right>r.right+1||b.left<r.left-1;})};});assert.ok(result.scroll<=result.client+1,JSON.stringify({width,id,...result}));assert.equal(result.clipped,false);}}ok('截图涉及的两种选人布局在宽、收窄窗口不横向裁切');
 await page.setViewportSize({width:992,height:650});await jump('opening');const id=await f.locator('.hero-card [data-act=choose]').first().getAttribute('data-value');
 for(let i=0;i<6;i++)await click('choose',id);await page.waitForTimeout(450);assert.equal(await f.locator('.pointer-flight').count(),0);assert.equal(await f.locator('.candidate.chosen').count(),0);ok('快速重复选取消后无残留飞行头像');
 await page.emulateMedia({reducedMotion:'reduce'});await click('choose',id);assert.equal(await f.locator('.pointer-flight').count(),0);const animations=await f.locator('#myteam-flow').evaluate(n=>n.getAnimations({subtree:true}).filter(a=>a.playState==='running').length);assert.equal(animations,0);assert.equal(await f.locator('.seat.filled').count(),1);ok('减弱动效保留选中状态并停止位移特效');
 assert.deepEqual(errors,[]);const sha256=crypto.createHash('sha256').update(fs.readFileSync(path.join(__dirname,'prototype.html'))).digest('hex');fs.writeFileSync(path.join(out,'final-checks.json'),JSON.stringify({checks,errors,sha256},null,2));console.log(JSON.stringify({checks,errors,sha256}));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
