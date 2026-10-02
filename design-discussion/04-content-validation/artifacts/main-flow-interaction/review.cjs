const { chromium } = require('/Users/qingshi/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('node:fs');
const path = require('node:path');
const dir=__dirname;
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'});
 const page=await browser.newPage({viewport:{width:992,height:720}});
 const errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:8764/preview.html');
 const f=page.frameLocator('iframe');
 await f.locator('#myteam-flow .screen .brand').waitFor();
 const ids=await f.locator('[data-field=page] option').evaluateAll(ns=>ns.map(n=>n.value));
 const metrics=[];
 fs.mkdirSync(path.join(dir,'qa'),{recursive:true});
 for(const id of ids){
  await f.locator('[data-field=page]').selectOption(id);
  const m=await f.locator('.screen').evaluate(screen=>{
   const rect=screen.getBoundingClientRect();
   const body=screen.querySelector('.body').getBoundingClientRect();
   const candidates=[...screen.querySelectorAll('.candidate')].map(x=>{const r=x.getBoundingClientRect();return {bottom:r.bottom-body.bottom,right:r.right-rect.right};});
   const clipping=[...screen.querySelectorAll('.content button,.content select,.content h2,.content h3')].filter(e=>{
    const r=e.getBoundingClientRect();return r.width&&r.height&&(r.bottom>body.bottom+1||r.right>rect.right+1||r.left<rect.left-1);
   }).map(e=>e.textContent.slice(0,30));
   return {width:rect.width,height:rect.height,bodyHeight:body.height,clipping,candidates};
  });
  metrics.push({id,...m});
  if(['opening','recruit','opponent','prepare','spells','roster','production','ascension','report','reward'].includes(id))await f.locator('.screen').screenshot({path:path.join(dir,'qa',id+'.png')});
 }
 fs.writeFileSync(path.join(dir,'qa','layout.json'),JSON.stringify({errors,metrics},null,2));
 console.log(JSON.stringify({errors,clipped:metrics.filter(x=>x.clipping.length)},null,2));
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
