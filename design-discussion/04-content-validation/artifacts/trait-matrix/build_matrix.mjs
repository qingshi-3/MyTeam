import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { FileBlob, SpreadsheetFile, Workbook } from '@oai/artifact-tool';

// The design JSON owns all game content. This builder only lays it out.
const directory = path.dirname(fileURLToPath(import.meta.url));
const planSource = await fs.readFile(path.join(directory, 'plan.json'), 'utf8');
const plan = JSON.parse(planSource);
const sourceHash = createHash('sha256').update(planSource).digest('hex');
const font = 'Microsoft YaHei';
const colors = { ink: '#243344', navy: '#263D52', blue: '#EAF1F8', pale: '#F5F7FA', white: '#FFFFFF', line: '#CAD5E1' };
const col = index => String.fromCharCode(65 + index);
const asText = value => Array.isArray(value) ? value.join('；') : typeof value === 'object' && value ? Object.entries(value).map(([k,v]) => `${k}：${asText(v)}`).join('\n') : String(value ?? '');

assert.equal(plan.professions.length, 7);
assert.equal(plan.systems.length, 7);
assert.equal(new Set(plan.units.map(u => u.id)).size, plan.units.length);
const professions = [...plan.professions, { id: '', name: '— 无职业羁绊' }];
const systems = [...plan.systems, { id: '', name: '— 无体系羁绊' }];
const matches = (tags, id) => id === '' ? tags.length === 0 : tags.includes(id);
const axisText = item => item.id === '' ? `${item.name}\n单轴单位保留另一轴归属。` : [item.name, item.identity, `对象：${item.beneficiaries}`, ...item.tiers.map(t => `${t.count}名：${t.effect}`), `限制：${item.limits}`].join('\n');
const unitText = unit => [`${unit.id}  ${unit.name}  · ${unit.tier}阶`, unit.role, `接口：${unit.interfaces.join('、')}`, ...(unit.mappingStatus ? [unit.mappingStatus] : []), 'A / P 详见备注或下方单位详情'].join('\n');
const noteText = unit => [unitText(unit), `职业：${unit.classes.map(id => professions.find(x => x.id === id)?.name ?? id).join('、') || '无职业羁绊'}`, `体系：${unit.systems.map(id => systems.find(x => x.id === id)?.name ?? id).join('、') || '无体系羁绊'}`, `主动 A：${asText(unit.active)}`, `被动 P：${asText(unit.passive)}`, ...(unit.mapping ? [`现有内容映射：${asText(unit.mapping)}`] : [])].join('\n\n');
const lineCount = (text, width) => text.split('\n').reduce((n, line) => n + Math.max(1, Math.ceil([...line].reduce((a,c) => a + (c.charCodeAt(0)>255 ? 1 : .54), 0)/width)), 0);
const workbook = Workbook.create();
const sheet = workbook.worksheets.add('职业×体系规划');
sheet.showGridLines = false;
sheet.tabColor = colors.navy;
sheet.getRange('A1').values = [[plan.title]];
sheet.getRange('B1:I1').merge();
sheet.getRange('B1').values = [[`${plan.status}。同ID多处出现只计一个单位；各标签分别计数。空白交叉为有意留空。A/P完整详情在同表下方，也保存在单元格备注。`]];
sheet.getRange('A2:I2').values = [['职业 / 体系', ...systems.map(s => s.name)]];
sheet.getRange('A3:I3').values = [[`${plan.principles[0]}\n\n${plan.status}\n\n共用规则、完整单位详情列于矩阵下方。\n备注可悬停预览，或右键“显示备注”后调整框大小。`, ...systems.map(axisText)]];
const expectedCells = new Map();
const noteAddresses = [];
const blocks = [];
let row = 4;
let placementCount = 0;
for (const profession of professions) {
  const groups = systems.map(system => plan.units.filter(u => matches(u.classes, profession.id) && matches(u.systems, system.id)));
  const height = Math.max(1, ...groups.map(g => g.length));
  const end = row + height - 1;
  blocks.push({ profession: profession.name, start: row, end });
  if (height > 1) sheet.getRange(`A${row}:A${end}`).merge();
  sheet.getRange(`A${row}`).values = [[axisText(profession)]];
  const axisHeight = lineCount(axisText(profession), 28) * 18 + 24;
  let unitHeight = 96;
  for (let j=0;j<groups.length;j++) {
    const group = groups[j];
    if (!group.length) {
      if (height > 1) sheet.getRange(`${col(j+1)}${row}:${col(j+1)}${end}`).merge();
      sheet.getRange(`${col(j+1)}${row}`).values = [['—（有意留空）']];
      continue;
    }
    for (let k=0;k<group.length;k++) {
      const unit = group[k];
      const cell = `${col(j+1)}${row+k}`;
      if (k===group.length-1 && row+k<end) sheet.getRange(`${cell}:${col(j+1)}${end}`).merge();
      const value = unitText(unit);
      sheet.getRange(cell).values = [[value]];
      expectedCells.set(cell, value);
      unitHeight = Math.max(unitHeight, lineCount(value, 27)*18+24);
      workbook.notes.add({ id: `${sheet.name}:${cell}`, target: { cell: { sheetName: sheet.name, sheetId: sheet.sheetId, address: cell } }, authorId: '', createdAt: '', body: { plainText: noteText(unit) } });
      noteAddresses.push(cell);
      placementCount++;
    }
  }
  const rowHeightPx = Math.max(unitHeight, Math.ceil(axisHeight / height));
  assert.ok(rowHeightPx <= 540, `Row height must be split further: ${profession.name}`);
  sheet.getRange(`A${row}:I${end}`).format.rowHeightPx = rowHeightPx;
  sheet.getRange(`A${row}:I${end}`).format.fill = blocks.length % 2 ? colors.white : colors.pale;
  sheet.getRange(`A${row}:A${end}`).format.fill = colors.blue;
  sheet.getRange(`A${row}:I${end}`).format.borders = { bottom: { style:'medium', color:colors.line } };
  row = end+1;
}
const lastMatrixRow = row-1;
sheet.getRange(`A${row}:I${row}`).merge();
sheet.getRange(`A${row}`).values = [['共用规则与计数口径']];
sheet.getRange(`A${row}:I${row}`).format.fill = colors.blue;
sheet.getRange(`A${row}:I${row}`).format.rowHeightPx = 32;
row++;
for (const principle of plan.principles) {
  sheet.getRange(`A${row}:I${row}`).merge();
  sheet.getRange(`A${row}`).values = [[principle]];
  sheet.getRange(`A${row}:I${row}`).format.rowHeightPx = 32;
  row++;
}
const lastMatrixAndRulesRow = row-1;
row++;
const detailTitleRow=row;
sheet.getRange(`A${row}:D${row}`).merge();
sheet.getRange(`A${row}`).values=[['单位详情（每个ID仅列一次）']];
sheet.getRange(`A${row}:D${row}`).format.rowHeightPx=32;
row++;
const detailHeaderRow=row;
sheet.getRange(`A${row}:D${row}`).values=[['单位 / 标签 / 职责','主动 A','被动 P','现有内容映射']];
sheet.getRange(`A${row}:D${row}`).format.rowHeightPx=32;
row++;
const detailStartRow=row;
const detailRows=[];
for(const unit of plan.units) {
  const labels=[`${unit.id}  ${unit.name}  · ${unit.tier}阶`, `职业：${unit.classes.map(id=>professions.find(x=>x.id===id)?.name ?? id).join('、')||'无职业羁绊'}`, `体系：${unit.systems.map(id=>systems.find(x=>x.id===id)?.name ?? id).join('、')||'无体系羁绊'}`,unit.role,`接口：${unit.interfaces.join('、')}`].join('\n');
  const cells=[labels,asText(unit.active),asText(unit.passive),[unit.mappingStatus,asText(unit.mapping)].filter(Boolean).join('\n')];
  sheet.getRange(`A${row}:D${row}`).values=[cells];
  const pixels=Math.max(...cells.map((value,index)=>lineCount(value,index===0?28:26)*18+24));
  assert.ok(pixels<=540,`Detail row too tall: ${unit.id}`);
  sheet.getRange(`A${row}:D${row}`).format.rowHeightPx=pixels;
  sheet.getRange(`A${row}:D${row}`).format.fill = detailRows.length%2 ? colors.pale : colors.white;
  detailRows.push({id:unit.id,row,pixels});
  row++;
}
const lastRow = row-1;
const all = sheet.getRange(`A1:I${lastRow}`);
all.format.font = { name: font, size: 11, color: colors.ink };
all.format.wrapText = true;
all.format.verticalAlignment = 'top';
all.format.columnWidthPx = 380;
sheet.getRange(`A1:A${lastRow}`).format.columnWidthPx = 415;
sheet.getRange('A1:I1').format.rowHeightPx = 48;
sheet.getRange('A1').format.font = { name:font, size:15, bold:true };
sheet.getRange('A2:I2').format = { fill: colors.navy, font:{name:font,size:11,bold:true,color:colors.white}, rowHeightPx:32, horizontalAlignment:'center', verticalAlignment:'center' };
sheet.getRange('A3:I3').format.fill = colors.blue;
sheet.getRange(`A${detailTitleRow}:D${detailTitleRow}`).format.font={name:font,size:15,bold:true};
sheet.getRange(`A${detailHeaderRow}:D${detailHeaderRow}`).format={fill:colors.navy,font:{name:font,size:11,bold:true,color:colors.white},horizontalAlignment:'center',verticalAlignment:'center'};
const topHeight = Math.max(...sheet.getRange('A3:I3').values[0].map((v,i) => lineCount(String(v),i===0?28:26)*18+24));
assert.ok(topHeight<=540, 'System headers need additional rows.');
sheet.getRange('A3:I3').format.rowHeightPx = topHeight;
sheet.freezePanes.freezeRows(2);
sheet.freezePanes.freezeColumns(1);
workbook.recalculate();
for (const [cell, expected] of expectedCells) assert.equal(sheet.getRange(cell).values[0][0],expected);
const inspected = await workbook.inspect({ kind:'table', range:`'${sheet.name}'!A1:C5`, include:'values,formulas', tableMaxRows:5, tableMaxCols:3, tableMaxCellChars:100, maxChars:1500 });
const errors = await workbook.inspect({ kind:'match', searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#NUM!|#SPILL!|#CALC!', options:{useRegex:true,maxResults:20},maxChars:1000 });
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(path.join(directory,'trait-matrix.xlsx'));
const reopened = await SpreadsheetFile.importXlsx(await FileBlob.load(path.join(directory,'trait-matrix.xlsx')));
const restored = reopened.worksheets.getItemAt(0);
for (const [cell,expected] of expectedCells) assert.equal(restored.getRange(cell).values[0][0],expected);
assert.deepEqual(restored.getRange('A3:I3').values,sheet.getRange('A3:I3').values);
assert.deepEqual(restored.getRange(`A${detailStartRow}:D${lastRow}`).values,sheet.getRange(`A${detailStartRow}:D${lastRow}`).values);
const renders = [{name:'matrix-overview',range:`A1:I${lastMatrixAndRulesRow}`,scale:.4},{name:'matrix-headers-left',range:'A2:D3',scale:1},{name:'matrix-headers-right',range:'E2:I3',scale:1}];
for(let i=0;i<blocks.length;i+=2) {
  const start=blocks[i].start,end=blocks[Math.min(i+1,blocks.length-1)].end;
  renders.push({name:`matrix-body-${i+1}-left`,range:`A${start}:D${end}`,scale:1},{name:`matrix-body-${i+1}-right`,range:`E${start}:I${end}`,scale:1});
}
const tallestDetail = detailRows.reduce((a,b)=>a.pixels>=b.pixels?a:b);
renders.push({name:'matrix-details-first',range:`A${detailTitleRow}:D${detailStartRow+1}`,scale:1},{name:'matrix-details-longest',range:`A${tallestDetail.row}:D${tallestDetail.row}`,scale:1},{name:'matrix-details-last',range:`A${lastRow-1}:D${lastRow}`,scale:1});
for(const spec of renders) {
  const png = await workbook.render({sheetName:sheet.name,range:spec.range,scale:spec.scale,format:'png'});
  await fs.writeFile(path.join(directory,`${spec.name}.png`),new Uint8Array(await png.arrayBuffer()));
}
const md = [`# ${plan.title}`, '', plan.status, '', ...plan.principles.map(p=>`- ${p}`), '', '| 职业 / 体系 | '+systems.map(s=>s.name).join(' | ')+' |','|'+Array(9).fill('---').join('|')+'|'];
for(const profession of professions) md.push('| '+profession.name+' | '+systems.map(system=>plan.units.filter(u=>matches(u.classes,profession.id)&&matches(u.systems,system.id)).map(u=>`${u.id} ${u.name}（${u.tier}阶）<br>${u.role}<br>接口：${u.interfaces.join('、')}`).join('<br><br>')||'—（有意留空）').join(' | ')+' |');
md.push('','## 轴效果','');
for(const axis of [...plan.professions,...plan.systems]) md.push(`### ${axis.name}`,'',axisText(axis),'');
md.push('## 单位详情','');
for(const unit of plan.units) md.push(`### ${unit.id} ${unit.name}`,'',noteText(unit),'');
await fs.writeFile(path.join(directory,'matrix.md'),md.join('\n'));
await fs.writeFile(path.join(directory,'verification.json'),JSON.stringify({sourceHash,sheetCount:1,sheetName:sheet.name,units:plan.units.length,placementCount,noteCount:noteAddresses.length,unitsWithMapping:plan.units.filter(u=>u.mapping).length,emptyAxes:true,headerRoundTrip:true,unitRoundTrip:true,detailRoundTrip:true,detailStartRow,detailRows,blocks,renders: renders.map(r=>r.name),inspection:inspected.ndjson,errors:errors.ndjson},null,2));
console.log(JSON.stringify({sourceHash,units:plan.units.length,placementCount,lastRow,topHeight,renders:renders.length}));
