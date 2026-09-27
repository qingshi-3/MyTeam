import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const web = path.dirname(fileURLToPath(import.meta.url));
const samples = path.join(web, 'assets');
const mime = { '.html':'text/html; charset=utf-8', '.css':'text/css; charset=utf-8', '.js':'text/javascript', '.json':'application/json', '.png':'image/png', '.wasm':'application/wasm', '.txt':'text/plain; charset=utf-8', '.ogg':'audio/ogg', '.mp4':'video/mp4' };
const server = http.createServer((req,res) => {
  try {
    const url = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    const isSample = url.startsWith('/samples/');
    const root = isSample ? samples : web;
    const relative = isSample ? url.slice(9) : url.slice(1) || 'index.html';
    const target = path.resolve(root, relative);
    if (!target.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
    if (!fs.existsSync(target) || !fs.statSync(target).isFile()) { res.writeHead(404).end(); return; }
    res.writeHead(200, {'Content-Type':mime[path.extname(target)] || 'application/octet-stream', 'Cache-Control':'no-cache'});
    fs.createReadStream(target).pipe(res);
  } catch { res.writeHead(400).end(); }
});
server.listen(5187, '127.0.0.1', () => console.log('VFX samples: http://127.0.0.1:5187'));
