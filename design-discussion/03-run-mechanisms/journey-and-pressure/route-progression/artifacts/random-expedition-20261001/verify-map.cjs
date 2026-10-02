const { chromium } = require(process.env.PLAYWRIGHT_PACKAGE || 'playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
(async () => {
  const browser = await chromium.launch({ headless: true, channel: 'chrome' });
  try {
    const page = await browser.newPage({ viewport: { width: 736, height: 1000 }, colorScheme: 'light' });
    const errors = []; page.on('pageerror', e => errors.push(String(e)));
    await page.goto('file://' + path.join(__dirname, 'map-preview.html'));
    const frame = page.frameLocator('iframe');
    await frame.locator('.cell').first().waitFor();
    const reports = [];
    for (let iteration = 0; iteration < 6; iteration++) {
      const m = await frame.locator('#random-expedition').evaluate(root => root._designAudit);
      const adjacent = i => [i % 16 ? i - 1 : -1, i % 16 < 15 ? i + 1 : -1, i >= 16 ? i - 16 : -1, i < 240 ? i + 16 : -1].filter(i => i >= 0);
      const flood = (start, blocked) => { const d = new Map([[start, 0]]), q = [start]; for (let j = 0; j < q.length; j++) for (const n of adjacent(q[j])) if (!blocked.includes(m.cells[n].kind) && !d.has(n)) { d.set(n, d.get(q[j]) + 1); q.push(n); } return d; };
      const walkable = m.cells.filter(c => c.kind !== 'wall').length;
      assert.equal(m.cells.length, 256);
      assert.equal(await frame.locator('.cell').count(), 256);
      assert.equal(m.bosses.length, 3);
      assert.equal(m.cells.filter(c => !['path', 'wall'].includes(c.kind)).length, 64);
      assert.equal(flood(m.start, ['wall']).size, walkable);
      assert.equal(flood(m.start, ['wall', 'boss']).size, walkable - 3);
      assert.equal(new Set(m.bosses.map(i => m.cells[i].region)).size, 3);
      const safe = flood(m.start, ['wall', 'boss', 'elite', 'battle']);
      for (const i of m.opening.filter(i => m.cells[i].kind !== 'battle')) assert(safe.has(i));
      const startDistances = m.bosses.map(i => flood(m.start, ['wall']).get(i));
      assert(startDistances.every(d => d >= 7));
      for (let i = 0; i < 3; i++) for (let j = i + 1; j < 3; j++) assert(flood(m.bosses[i], ['wall']).get(m.bosses[j]) >= 8);
      for (const hub of m.hubs) for (const i of hub.places) assert([2, 3, 4].includes(flood(hub.boss, ['wall']).get(i)));
      reports.push({ seed: m.seed, start: m.start, bosses: m.bosses, startDistances, safeCells: safe.size });
      if (iteration === 0) {
        fs.writeFileSync(path.join(__dirname, 'example-map.json'), JSON.stringify(m, null, 2));
        await frame.locator('[data-kind="boss"]').first().click();
        assert((await frame.locator('.explain').innerText()).includes('钥印'));
        await page.waitForTimeout(150);
        await frame.locator('#random-expedition').screenshot({ path: path.join(__dirname, 'map-overview.png') });
      }
      if (iteration < 5) await frame.locator('.reroll').click();
    }
    assert.equal(new Set(reports.map(r => r.seed)).size, 6);
    await frame.locator('.view').selectOption('se');
    assert.equal(await frame.locator('.cell').count(), 64);
    await page.setViewportSize({ width: 320, height: 1000 });
    await page.emulateMedia({ colorScheme: 'dark' });
    await page.waitForTimeout(150);
    const fit = await frame.locator('#random-expedition').evaluate(root => ({ width: root.clientWidth, scrollWidth: root.scrollWidth, outer: document.documentElement.clientWidth, bodyScroll: document.documentElement.scrollWidth }));
    assert(fit.scrollWidth <= fit.width && fit.bodyScroll <= fit.outer, JSON.stringify(fit));
    await frame.locator('.cell').first().click();
    await frame.locator('#random-expedition').screenshot({ path: path.join(__dirname, 'map-quadrant-dark.png') });
    assert.equal(errors.length, 0, errors.join('\n'));
    fs.writeFileSync(path.join(__dirname, 'verification.json'), JSON.stringify({ reports, fit, errors, scope: 'Six illustrative seeds; geometry and isolated browser interactions only. Does not validate combat balance, meaningful route choices, resource sufficiency, or production generation.' }, null, 2));
    console.log(JSON.stringify({ checkedSeeds: reports.length, interaction: 'select, reroll, quadrant', fit, errors }));
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exit(1); });
