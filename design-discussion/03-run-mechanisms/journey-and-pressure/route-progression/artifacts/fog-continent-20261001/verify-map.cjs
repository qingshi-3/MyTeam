const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(process.env.PLAYWRIGHT_PACKAGE || 'playwright');

(async () => {
  const browser = await chromium.launch({headless:true,channel:'chrome'});
  const page = await browser.newPage({viewport:{width:800,height:1200},deviceScaleFactor:2,colorScheme:'light'});
  const errors=[];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto('file://' + path.join(__dirname,'map-preview.html'));
  const frame = page.frameLocator('iframe');
  await frame.locator('.map-tile').last().waitFor();
  const count=await frame.locator('.map-tile').count();
  if(count!==64) throw new Error('Expected 64 cells, got '+count);
  const barriers=await frame.locator('.block-east,.block-south').count();
  if(barriers!==9) throw new Error('Expected 9 marked terrain boundaries, got '+barriers);
  await frame.locator('[data-cell="G6"]').click();
  if(!(await frame.locator('.selected-name').innerText()).includes('豺狼人寨')) throw new Error('Selection not reflected');
  if(!(await frame.locator('.selected-gain').innerText()).includes('G7')) throw new Error('Cell relationship not reflected');
  await frame.locator('[data-cell="H1"]').click();
  if(!(await frame.locator('.selected-action').innerText()).includes('终战')) throw new Error('Boss detail missing');
  await frame.locator('[data-cell="A7"]').click();
  await frame.locator('#fog-continent-map').screenshot({path:path.join(__dirname,'map-overview.png')});
  const checks=[];
  for(const [width,scheme] of [[800,'light'],[352,'light'],[800,'dark']]) {
    await page.setViewportSize({width,height:1400});
    await page.emulateMedia({colorScheme:scheme});
    await page.waitForTimeout(150);
    const measures=await frame.locator('#fog-continent-map').evaluate(root=>{
      const bounds=root.getBoundingClientRect();
      const tiles=[...root.querySelectorAll('.map-tile')];
      return {width:bounds.width,scrollWidth:root.scrollWidth,clientWidth:root.clientWidth,cells:tiles.length,
        overflowingLabels:tiles.filter(t=>[...t.querySelectorAll('.tile-name,.tile-kind')].some(e=>e.getBoundingClientRect().right>t.getBoundingClientRect().right+.5 || e.getBoundingClientRect().bottom>t.getBoundingClientRect().bottom+.5)).map(t=>t.dataset.cell)};
    });
    if(measures.width>width || measures.scrollWidth>measures.clientWidth+1 || measures.overflowingLabels.length) throw new Error('Layout overflow '+JSON.stringify(measures));
    checks.push({viewport:width,scheme,...measures});
    if(scheme==='dark') await frame.locator('#fog-continent-map').screenshot({path:path.join(__dirname,'map-dark.png')});
  }
  if(errors.length) throw new Error(errors.join('\n'));
  fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify({cells:count,barriers,interactions:['G6 updates relationship','H1 updates finale','A7 restores opening'],checks,errors},null,2)+'\n');
  await browser.close();
  console.log(JSON.stringify({cells:count,checks,errors}));
})().catch(error=>{console.error(error);process.exitCode=1});
