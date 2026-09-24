#!/usr/bin/env python3
"""Report content invariants and likely semantic overlaps for human review."""
import collections
import json
import re
from pathlib import Path
from pool import audit, DATA, MANIFEST

records=json.loads(DATA.read_text())
manifest=json.loads(MANIFEST.read_text())
counts=audit(records,manifest,complete=True)
# Exact normalized title/concept collisions are rejected at submission time. These
# broader lexical comparisons are candidates for editorial review, not proof of
# duplicated concepts: related objects may support genuinely different premises.
stop=set('a an and are as at be but by for from how i in into is it its of on or our the their this to we what with you your car family road video travel one that they us not about every has have can'.split())
def terms(text):
    return {word for word in re.findall('[a-z]+',text.lower()) if word not in stop and len(word)>2}
pairs=[]
for index,a in enumerate(records):
    aa=terms(a['title']+' '+a['conceptKey'])
    for b in records[index+1:]:
        bb=terms(b['title']+' '+b['conceptKey'])
        overlap=len(aa&bb)/len(aa|bb)
        if overlap>=.30:
            pairs.append({'a':a['id'],'b':b['id'],'titleA':a['title'],'titleB':b['title'],'similarity':round(overlap,3)})
pairs.sort(key=lambda p:-p['similarity'])
hints={'cape-coral':r'cape coral', 'pigeon-gorge':r'pigeon (?:river|gorge)', 'francine-flood':r'francine|i-10', 'bucees':r'buc[- ]?ee', 'touchdown-jesus':r'touchdown jesus|monroe', 'wall-drug':r'wall drug', 'carhenge':r'carhenge', 'texas-detour':r'i-44|texas detour', 'big-texan':r'big texan', 'cadillac-ranch':r'cadillac ranch', 'salt-lake':r'salt lake', 'iowa-fair':r'butter cow|iowa|state fair', 'springfield':r'springfield', 'sovereign-crossing':r'sovereign|columbia/snake', 'portland':r'portland', 'cascades':r'cascades|i-90|highway 1', 'tacoma':r'tacoma|no kings', 'gorge-fork':r'gorge fork', 'columbia-bridge':r'columbia i-5|columbia bridge', 'i405':r'i-405', 'seattle':r'seattle'}
geo_review=[]
for r in records:
    body=(r['title']+' '+r['premise']).lower()
    for geo,pattern in hints.items():
        if re.search(pattern,body) and geo not in r['locations']:
            geo_review.append({'id':r['id'],'hint':geo,'locks':r['locations'],'title':r['title'],'premise':r['premise']})
report={'geoReviewCandidates':geo_review,'counts':counts,'scores':{'minimum':min(r['potential'] for r in records),'maximum':max(r['potential'] for r in records),'mean':round(sum(r['potential'] for r in records)/len(records),2)},'locationCounts':dict(sorted(collections.Counter(geo for r in records for geo in r['locations']).items())),'lexicalReviewCandidates':pairs,'note':'Human semantic and geography audit is recorded in review.md; lexical candidates are not automatically rejected.'}
Path('tools/creator-content/audit-report.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
