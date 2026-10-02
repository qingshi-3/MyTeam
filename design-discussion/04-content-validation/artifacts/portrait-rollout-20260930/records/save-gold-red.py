import hashlib
import json
import shutil
import struct
import sys
from pathlib import Path

data = json.load(sys.stdin)
root = Path(__file__).resolve().parent.parent
source = Path(data['generated_source'])
destination = root / 'after' / (data['id'] + '.png')
destination.parent.mkdir(parents=True, exist_ok=True)
shutil.copyfile(source, destination)
original = root / 'before' / (data['id'] + '.png')
data['input_path'] = str(original)
data['output_path'] = str(destination)
data['input_sha256'] = hashlib.sha256(original.read_bytes()).hexdigest()
data['output_sha256'] = hashlib.sha256(destination.read_bytes()).hexdigest()
data['dimensions'] = list(struct.unpack('>II', destination.read_bytes()[16:24]))
data['mode'] = 'built-in imagegen edit'
data['visual_validation'] = 'Agent visual inspection completed; user aesthetic acceptance not yet recorded.'
(root / 'records' / (data['id'] + '.json')).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'id': data['id'], 'dimensions': data['dimensions'], 'output_sha256': data['output_sha256']}))
