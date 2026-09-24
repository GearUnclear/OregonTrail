#!/usr/bin/env python3
"""Lock-protected authoring ledger for the 20-creator travel-video pool."""
import argparse
import fcntl
import json
import os
from pathlib import Path
import re
import tempfile
import unicodedata

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / 'src/Module/Creator/video-ideas.json'
MANIFEST = ROOT / 'tools/creator-content/manifest.json'
LOCK = ROOT / 'tools/creator-content/.pool.lock'
GEO = {'cape-coral','pigeon-gorge','francine-flood','bucees','touchdown-jesus','wall-drug','carhenge','texas-detour','big-texan','cadillac-ranch','salt-lake','iowa-fair','springfield','sovereign-crossing','portland','cascades','tacoma','gorge-fork','columbia-bridge','i405','seattle'}
FIELDS = {'id','title','premise','conceptKey','potential','locations','authorBatch'}

def normalized(value):
    return ''.join(c for c in unicodedata.normalize('NFKD',value).casefold() if c.isalnum())

def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile('w', dir=path.parent, delete=False) as output:
        json.dump(value, output, indent=2, ensure_ascii=False)
        output.write('\n')
        temporary = output.name
    os.replace(temporary,path)

def validate_batch(records, batch):
    assert len(records) == 10, 'Each author must submit exactly ten ideas'
    assert {r['id'] for r in records} == {f'video-{n:03d}' for n in range((batch-1)*10+1,batch*10+1)}, 'Wrong ID range'
    assert sum(not r['locations'] for r in records) >= 8, 'At least eight ideas per batch must be generic'
    counts = [0,0,0]
    for r in records:
        assert set(r) == FIELDS, f'Invalid schema: {r}'
        assert r['authorBatch'] == batch
        assert all(isinstance(r[k],str) and r[k].strip() for k in ('id','title','premise','conceptKey'))
        assert re.fullmatch('[a-z0-9]+(?:-[a-z0-9]+)*',r['conceptKey']), 'conceptKey must be a lowercase semantic slug'
        assert len(r['title']) <= 100, 'Title is too long'
        assert isinstance(r['potential'],int) and not isinstance(r['potential'],bool)
        score = r['potential']
        assert 5 <= score <= 98 and (score <= 69 or score >= 80), 'Use weak, middling or great score bands'
        counts[0 if score <= 35 else 1 if score <=69 else 2] += 1
        assert isinstance(r['locations'],list) and set(r['locations']) <= GEO
        assert len(r['locations']) == len(set(r['locations']))
        assert 'seattle' not in r['locations'], 'Destination ends the trail; avoid unreachable Seattle footage'
    assert counts == [6,3,1], f'Expected six weak, three middling, one great; got {counts}'

def audit(records, manifest, complete=False):
    for key,func in [('id',lambda x:x),('title',normalized),('conceptKey',normalized)]:
        values = [func(r[key]) for r in records]
        assert len(values) == len(set(values)), f'Duplicate {key}'
    batches = sorted({r['authorBatch'] for r in records})
    for batch in batches:
        validate_batch([r for r in records if r['authorBatch']==batch],batch)
    if complete:
        assert len(records)==200 and batches==list(range(1,21))
        assert len(manifest['batches']) == 20
        assert all(b['status']=='accepted' and b['model']=='gpt-6-luna' and b['reasoningEffort']=='xhigh' and b['creatorTask'] for b in manifest['batches'])
        assert len({b['creatorTask'] for b in manifest['batches']})==20
        for entry in manifest['batches']:
            accepted = [r for r in records if r['authorBatch'] == entry['batch']]
            assert set(entry['acceptedIds']) == {r['id'] for r in accepted}, 'Manifest IDs differ from the accepted pool'
            original = json.loads((MANIFEST.parent / 'batches' / f"batch-{entry['batch']:02d}.json").read_text())
            assert sorted(original, key=lambda r: r['id']) == sorted(accepted, key=lambda r: r['id']), 'Author submission differs from the accepted pool'
    return {'ideas':len(records),'acceptedBatches':len(batches),'generic':sum(not r['locations'] for r in records),'geoLocked':sum(bool(r['locations']) for r in records),'weak':sum(r['potential']<=35 for r in records),'middling':sum(36<=r['potential']<=69 for r in records),'great':sum(r['potential']>=80 for r in records)}

def main():
    p=argparse.ArgumentParser()
    p.add_argument('command',choices=['append','replace','audit'])
    p.add_argument('--batch',type=int)
    p.add_argument('--task')
    p.add_argument('--file',type=Path)
    p.add_argument('--complete',action='store_true')
    a=p.parse_args()
    with LOCK.open('a') as lock:
        fcntl.flock(lock,fcntl.LOCK_EX)
        records=json.loads(DATA.read_text())
        manifest=json.loads(MANIFEST.read_text())
        if a.command != 'audit':
            assert a.batch is not None and 1<=a.batch<=20 and a.task and a.file
            incoming=json.loads(a.file.read_text())
            validate_batch(incoming,a.batch)
            prior=[r for r in records if r['authorBatch']==a.batch]
            assert (not prior) if a.command=='append' else len(prior)==10, 'Use append for a new author; replace for corrections'
            entry=manifest['batches'][a.batch-1]
            assert entry['creatorTask'] in (None,a.task), 'Only original author may correct accepted ideas'
            combined=[r for r in records if r['authorBatch']!=a.batch]+incoming
            combined.sort(key=lambda r:r['id'])
            audit(combined,manifest)
            entry.update(status='accepted',creatorTask=a.task,acceptedIds=[r['id'] for r in incoming])
            manifest['counts']=audit(combined,manifest)
            write_json(DATA,combined)
            write_json(MANIFEST,manifest)
            records=combined
        print(json.dumps(audit(records,manifest,a.complete),sort_keys=True))

if __name__=='__main__':
    main()
