"""Replace only the embedded fragment; preserve the existing site's sandbox and CSP."""
from pathlib import Path
from html.parser import HTMLParser
import html, re, json, sys, hashlib

ROOT=Path(__file__).resolve().parent.parent
target=Path(sys.argv[1])
source=(ROOT/'prototype.html').read_text()
original=target.read_text()

class Inspect(HTMLParser):
    def __init__(self):
        super().__init__();self.csp=[];self.frames=[]
    def handle_starttag(self,tag,attrs):
        a=dict(attrs)
        if tag=='meta' and a.get('http-equiv','').lower()=='content-security-policy':self.csp.append(a.get('content'))
        if tag=='iframe':self.frames.append(a)

outer=Inspect();outer.feed(original)
assert len(outer.frames)==1
frame=outer.frames[0];inner=frame['data-srcdoc'];old_inner=Inspect();old_inner.feed(inner)

class Bounds(HTMLParser):
    def __init__(self,text):
        super().__init__();self.text=text;self.lines=text.splitlines(keepends=True);self.start=None;self.end=None;self.depth=0
    def charpos(self):
        line,col=self.getpos();return sum(map(len,self.lines[:line-1]))+col
    def handle_starttag(self,tag,attrs):
        if tag!='div':return
        if self.start is None and dict(attrs).get('id')=='myteam-flow':self.start=self.charpos();self.depth=1
        elif self.start is not None and self.end is None:self.depth+=1
    def handle_endtag(self,tag):
        if tag=='div' and self.start is not None and self.end is None:
            self.depth-=1
            if self.depth==0:self.end=self.charpos()+len('</div>')

b=Bounds(inner);b.feed(inner);assert b.start is not None and b.end is not None
new_inner=inner[:b.start]+source+inner[b.end:]
updated,n=re.subn(r'data-srcdoc="[^"]*"',lambda _: 'data-srcdoc="'+html.escape(new_inner,quote=True)+'"',original)
assert n==1
check=Inspect();check.feed(updated);ic=Inspect();ic.feed(check.frames[0]['data-srcdoc'])
assert outer.csp==check.csp and old_inner.csp==ic.csp and frame.get('sandbox')==check.frames[0].get('sandbox')
target.write_text(updated)
report={'sourceSha256':hashlib.sha256(source.encode()).hexdigest(),'sandbox':frame.get('sandbox'),'outerCspPreserved':True,'innerCspPreserved':True,'fragmentBytes':len(source.encode()),'siteFileBytes':len(updated.encode())}
(ROOT/'qa/opening/export.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
