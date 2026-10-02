const {chromium}=require('/Users/qingshi/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const path=require('node:path'),fs=require('node:fs'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'}),page=await browser.newPage({viewport:{width:1632,height:1100}});
 await page.goto('http://127.0.0.1:8764/preview.html',{waitUntil:'domcontentloaded'});const f=page.frameLocator('iframe');await f.locator('.screen[data-page=opening]').waitFor();await page.waitForTimeout(750);
 const out=path.resolve(__dirname,'../qa/opening');await f.locator('.screen').screenshot({path:path.join(out,'wide.png')});
 const measure=()=>f.locator('.screen').evaluate(screen=>{const r=screen.getBoundingClientRect();return {width:r.width,height:r.height,overflow:document.documentElement.scrollWidth>document.documentElement.clientWidth,overlaps:[...screen.querySelectorAll('.hero-card')].flatMap(c=>{const n=c.querySelector('h3').getBoundingClientRect(),f=c.querySelector('.facts').getBoundingClientRect(),b=c.getBoundingClientRect();return n.bottom>f.top+1||f.bottom>b.bottom?['card-info']:[];})};});
 const wide=await measure();assert.ok(!wide.overflow);
 await f.locator('[data-act=review-toggle]').click();await f.locator('[data-act=sample-state][data-value="2"]').click();assert.equal(await f.locator('.seat.filled').count(),2);await f.locator('[data-act=sample-state][data-value="1"]').click();assert.equal(await f.locator('.seat.filled').count(),1);await f.locator('[data-act=sample-state][data-value="0"]').click();assert.equal(await f.locator('.seat.filled').count(),0);await f.locator('[data-act=review-toggle]').click();
 await page.setViewportSize({width:352,height:1400});await page.waitForTimeout(200);const narrow=await measure();assert.ok(!narrow.overflow);await f.locator('.screen').screenshot({path:path.join(out,'narrow.png')});
 fs.writeFileSync(path.join(out,'layout.json'),JSON.stringify({wide,narrow,reviewStates:'0/1/2 passed'},null,2));console.log(JSON.stringify({wide,narrow}));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
