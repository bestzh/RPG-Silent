"""Export scalar fields and simple Unity YAML lists as reference data, without dependencies."""
from pathlib import Path
import re,json
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
def scalar(value):
    value=value.strip()
    if value.startswith('"'):
        return json.loads(re.sub(r'\\x([a-fA-F0-9]{2})',lambda m:'\\u00'+m[1],value))
    if re.fullmatch(r'-?\d+',value):return int(value)
    if re.fullmatch(r'-?\d+\.\d+',value):return float(value)
    return value
def asset(p):
    text=p.read_text(encoding='utf-8-sig')
    def fold(match):
        value=re.sub(r'\\\r?\n[ \t]+','',match[0])
        return re.sub(r'\r?\n[ \t]+',' ',value)
    text=re.sub(r'"(?:\\.|[^"\\])*"',fold,text,flags=re.S)
    data={'source':p.relative_to(ROOT).as_posix()}
    for key,value in re.findall(r'^  ([A-Za-z]\w*): (.*)$',text,re.M):
        if not value.startswith('{'):data[key]=scalar(value)
    for key in ['Objectives','Rewards','Choices','Materials']:
        match=re.search(r'^  '+key+r':\s*\n(.*?)(?=^  [A-Za-z]|\Z)',text,re.M|re.S)
        if not match:continue
        rows=[]
        for block in re.split(r'^  - ',match[1],flags=re.M)[1:]:
            row={}
            for k,v in re.findall(r'^\s*([A-Za-z]\w*): ?(.*)$',block,re.M):
                if not v.startswith('{'):row[k]=scalar(v)
            rows.append(row)
        data[key]=rows
    pages=re.search(r'^  Pages:\s*\n(.*?)(?=^  [A-Za-z]|\Z)',text,re.M|re.S)
    if pages:data['Pages']=[scalar(v) for v in re.findall(r'^  - (.*)$',pages[1],re.M)]
    return data
items=json.loads((OUT/'item-catalog.json').read_text(encoding='utf-8'))
sets=[asset(p) for p in (ROOT/'Assets/Resources/EquipmentSets').glob('*.asset')]
quests=[asset(p) for p in (ROOT/'Assets/Resources/Quests').rglob('*.asset')]
adventure=ROOT/'Assets/Resources/Adventure'
achievements=[asset(p) for p in sorted(adventure.glob('Achievement*.asset'))]
stories=[asset(p) for p in sorted(adventure.glob('Story*.asset'))]
recipes=[asset(p) for p in sorted(adventure.glob('forge-*.asset'))]
item_by_guid={}
for item in items:
    meta=Path(str(ROOT/item['source'])+'.meta').read_text(encoding='utf-8')
    item_by_guid[re.search(r'^guid: (\w+)',meta,re.M)[1]]=item
for recipe in recipes:
    text=(ROOT/recipe['source']).read_text(encoding='utf-8-sig')
    match=re.search(r'^  Result: .*guid: (\w+)',text,re.M)
    item=item_by_guid.get(match[1]) if match else None
    if item:recipe['ResultItemId']=item['Id'];recipe['ResultName']=item['DisplayName']
lessons=[dict(asset(p),id=p.stem) for p in sorted((ROOT/'Assets/Resources/ClassSkills').glob('*.asset'))]
source=(ROOT/'Assets/Scripts/UI/MainUI.Tutorial.cs').read_text(encoding='utf-8-sig')
titles=re.search(r'TutorialTitles\s*=\s*\{(.*?)\}',source,re.S)[1]
titles=re.findall(r'"([^"]+)"',titles)
groups={'LIVE_ITEMS':items,'LIVE_SETS':sets,'LIVE_QUESTS':quests,'LIVE_ACHIEVEMENTS':achievements,'LIVE_STORIES':stories,'LIVE_RECIPES':recipes,'LIVE_LESSONS':lessons,'LIVE_TUTORIAL_TITLES':titles}
(OUT/'v5-data.js').write_text('\n'.join('const '+k+'='+json.dumps(v,ensure_ascii=False)+';' for k,v in groups.items())+'\n',encoding='utf-8')
(OUT/'configuration-catalog.json').write_text(json.dumps(groups,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:len(v) for k,v in groups.items()},ensure_ascii=False))
