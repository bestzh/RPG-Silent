"""Copy selected project art for the design review; never write to Assets/."""
from pathlib import Path
import hashlib
import json
import re
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
OLD = OUT.parent / '2026-10-28'
mapping = json.loads((OLD / 'asset-map.json').read_text(encoding='utf-8'))
manifest = json.loads((OLD / 'source-manifest.json').read_text(encoding='utf-8'))
manifest['date'] = '2026-10-29'
manifest['version'] = 'Synty v2'
manifest['files'] = [f for f in manifest['files'] if not f['design_file'].startswith('assets/sy-')]

def copy(source, key, role, dest=None):
    source = ROOT / source
    assert source.is_file(), source
    dest = OUT / 'assets' / (dest or (key + source.suffix.lower()))
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, dest)
    data = {'source': source.relative_to(ROOT).as_posix(), 'design_file': dest.relative_to(OUT).as_posix(), 'role': role, 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()}
    meta = Path(str(source) + '.meta')
    if meta.exists():
        text = meta.read_text(encoding='utf-8-sig')
        guid = re.search(r'^guid: (\w+)', text, re.M)
        if guid: data['guid'] = guid.group(1)
        border = re.search(r'spriteBorder: \{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}', text)
        if border: data['sprite_border_LBRT'] = list(map(int, border.groups()))
    if source.suffix.lower() == '.png':
        with Image.open(source) as im: data['size'] = list(im.size)
    manifest['files'] = [f for f in manifest['files'] if f['design_file'] != data['design_file']]
    manifest['files'].append(data)
    mapping[key] = data['design_file']
    return data

menus = 'Assets/Synty/InterfaceFantasyMenus/Sprites/'
hud = 'Assets/Synty/InterfaceFantasyWarriorHUD/Sprites/'
for key, rel in {
    'sy-panel': 'FantasyMenus/SPR_FantasyMenus_Frame_Box_Large_01.png',
    'sy-panel-plain': 'FantasyMenus/SPR_FantasyMenus_Frame_Box_Large_08.png',
    'sy-panel-detail': 'FantasyMenus/SPR_FantasyMenus_Frame_Box_Large_02.png',
    'sy-button': 'FantasyMenus/SPR_FantasyMenus_Menu_Button_07_Frame.png',
    'sy-button-subtle': 'FantasyMenus/SPR_FantasyMenus_Menu_Button_05_Frame.png',
    'sy-button-bg': 'FantasyMenus/SPR_FantasyMenus_Menu_Button_07_Background.png',
    'sy-crest-shield': 'Crests/SPR_FantasyMenus_Crest_Shield_01_Gold.png',
    'sy-crest-wings': 'Crests/SPR_FantasyMenus_Crest_Wings_05_Gold.png',
    'sy-crest-swords': 'Crests/SPR_FantasyMenus_Crest_Greeble_SwordsCrossed_01.png',
    'sy-crest-dragon': 'Crests/SPR_FantasyMenus_Crest_Greeble_Dragonhead_01_Gold.png',
    'sy-flourish': 'FantasyMenus/SPR_FantasyMenus_Greeble_Curlicue_04.png',
    'sy-divider': 'FantasyMenus/SPR_FantasyMenus_Frame_Bar_07.png',
    'sy-pattern': 'General/SPR_FantasyMenus_Background_Pattern_01.png',
    'sy-medal': 'Icons_FantasyMenus/SPR_FantasyMenus_Icon_Medal_01.png',
}.items(): copy(menus + rel, key, 'Synty Fantasy Menus：界面边框、菜单、徽章与装饰')
for key, rel in {
    'sy-slot': 'HUD/SPR_HUD_FantasyWarrior_Frame_Box_Small_01.png',
    'sy-map-ring': 'HUD/SPR_HUD_FantasyWarrior_Ring_Large_02_Clean.png',
    'sy-portrait-ring': 'HUD/SPR_HUD_FantasyWarrior_Ring_Large_01_Clean.png',
    'sy-map-player': 'HUD/SPR_HUD_FantasyWarrior_Map_Player_01_Clean.png',
    'sy-boss-frame': 'HUD/SPR_HUD_FantasyWarrior_Tracery_FrameDetail_07.png',
    'sy-gauge': 'HUD/SPR_HUD_FantasyWarrior_Bar_Diamond_02.png',
}.items(): copy(hud + rel, key, 'Synty Fantasy Warrior HUD：HUD、状态条、槽位与地图')

nav = {
    'bag1': hud+'Icons_Inventory/ICON_FantasyWarrior_Inventory_Backpack_01_Clean.png',
    'coin': hud+'Icons_Inventory/ICON_FantasyWarrior_Inventory_Currency_01_Clean.png',
    'hammer': hud+'Icons_Inventory/ICON_FantasyWarrior_Inventory_Crafting_01_Clean.png',
    'invchest': hud+'Icons_Inventory/ICON_FantasyWarrior_Inventory_Armor_01_Clean.png',
    'archer': hud+'Icons_Inventory/ICON_FantasyWarrior_Inventory_Bows_01_Clean.png',
    'fire': hud+'Icons_Elements/ICON_FantasyWarrior_Element_Fire_01_Clean.png',
    'attack': hud+'Icons_Status/ICON_FantasyWarrior_Status_Attack_01_Clean.png',
    'book': menus+'Icons_FantasyMenus/SPR_FantasyMenus_Icon_Book_01.png',
    'award': menus+'Icons_FantasyMenus/SPR_FantasyMenus_Icon_Medal_01.png',
    'invhelmet': menus+'Icons_FantasyMenus/SPR_FantasyMenus_Icon_Helmet_01.png',
    'flag': menus+'Icons_FantasyMenus/SPR_FantasyMenus_Icon_Flag_01.png',
    'exit': menus+'Icons_FantasyMenus/SPR_FantasyMenus_Icon_Exit_01.png',
    'settings': menus+'Icons_FantasyMenus/SPR_FantasyMenus_Icon_Settings_01.png',
    'target': menus+'Icons_Settings/ICON_FantasyMenus_Settings_Controls_01_Clean.png',
}
for name, path in nav.items(): copy(path, 'glyph-'+name, 'Synty 统一导航与状态图标')
copy('Assets/Synty/InterfaceFantasyMenus/Fonts/Ortica/Ortica-Bold.otf', 'sy-title', 'Synty Menus 拉丁标题字体')
copy('Assets/Synty/InterfaceFantasyMenus/Fonts/Ortica/OFL.txt', 'sy-title-license', 'Ortica 字体随附许可', dest='licenses/Ortica-OFL.txt')

# Clean, already imported demo plates keep old baked UI text out of the mockups.
# They are visual references, not captures of the user's current game levels.
for key, source in {
    'world-town':'Assets/Synty/InterfaceFantasyWarriorHUD/Samples/Sprites/SPR_HUD_FantasyWarrior_ExampleScreenshot_3rdPersonOffset.png',
    'world-dungeon':'Assets/Synty/InterfaceFantasyWarriorHUD/Samples/Sprites/SPR_HUD_FantasyWarrior_ExampleScreenshot_3rdPersonCentered.png',
    'sy-destination-forest':'Assets/Synty/InterfaceFantasyMenus/Samples/Sprites/SPR_FantasyMenus_Example_Image_Continue_01.png',
    'sy-destination-crystal':'Assets/Resources/BG/SettingBG.png',
    'sy-destination-training':'Assets/Synty/InterfaceFantasyWarriorHUD/Samples/Sprites/SPR_HUD_FantasyWarrior_ExampleScreenshot_3rdPersonOffset.png',
}.items(): copy(source,key,'现有资源包/项目中的干净场景底图，仅作设计示意，不表示实际关卡')

vol = 'Assets/6000FantasyIcons/SkillsIcons/SkillIcons3/SkillVol3/'
skills = {
    'swordsman-1':'Skill_SwordAttack.png', 'swordsman-2':'Skill_HardEarthHit.png', 'swordsman-3':'Skill_SwordSplash.png',
    'sword-saint-1':'Skill_SwordSplash.png', 'sword-saint-2':'Skill_PiercingStrike.png',
    'berserker-1':'Skill_HardAxeAttack.png', 'berserker-2':'Skill_BattleCry.png',
    'mage-1':'Skill_FireBall.png', 'mage-2':'Skill_FrostNova.png', 'mage-3':'Skill_LightningStrike.png',
    'shaman-1':'Skill_LightningUltimate.png', 'shaman-2':'Skill_EarthShock.png',
    'ranger-1':'Skill_StrongArrowShoot.png', 'ranger-2':'Skill_ChaosArrow.png', 'ranger-3':'Skill_MeleeSplashCleave.png',
    'sharpshooter-1':'Skill_FocusArrow.png', 'sharpshooter-2':'Skill_RainOfArrows.png',
    'beastmaster-1':'Skill_ShadowMonster.png', 'beastmaster-2':'Skill_HoofStomp.png',
    'assassin-1':'Skill_ShadowDagger.png', 'assassin-2':'Skill_WhirlwindDaggers.png',
}
catalog=[]
skill_overrides={
    'beastmaster-1':'Bonus/SkillUpdate/Standart/SummonPet_1.png',
    'swordsman-3':'Bonus/SkillUpdate/Standart/AttackDoubleSword_1.png',
    'sword-saint-2':'Skillicons2/Skill_standart/swords_of_light.png',
    'ranger-2':'Bonus/Skill1_Standart/Big_fire_arrow.png',
}
for skill_id, filename in skills.items():
    source='Assets/6000FantasyIcons/SkillsIcons/'+skill_overrides[skill_id] if skill_id in skill_overrides else vol+filename
    record=copy(source, 'skill-'+skill_id, 'PONETI 6000 Fantasy Icons：独立技能图标视觉建议')
    skill_asset=ROOT/'Assets/Resources/ClassSkills'/f'{skill_id}.asset'
    text=skill_asset.read_text(encoding='utf-8-sig')
    skill={'id':skill_id, 'source_skill':skill_asset.relative_to(ROOT).as_posix(), 'icon':record['design_file'], 'icon_source':record['source'], 'icon_guid':record.get('guid')}
    for key in ['SkillName','RequiredLevel','LearnCost','UpgradeCost','MaxRank','CooldownTime','EnergyCost']:
        match=re.search(r'^  '+key+r': (.+)$',text,re.M)
        if match:
            value=match.group(1)
            skill[key]=json.loads(value) if value.startswith('"') else float(value) if '.' in value else int(value)
    catalog.append(skill)

class_files=list((ROOT/'Assets/Resources/Classes').glob('*.asset'))
classes=[]
for file in class_files:
    text=file.read_text(encoding='utf-8-sig')
    meta=Path(str(file)+'.meta').read_text(encoding='utf-8-sig')
    display=re.search(r'^  DisplayName: (.+)',text,re.M)
    parent=re.search(r'^  BaseClass: .*?guid: (\w+)',text,re.M)
    classes.append({'id':file.stem,'name':json.loads(display.group(1)) if display.group(1).startswith('"') else display.group(1), 'guid':re.search(r'^guid: (\w+)',meta,re.M).group(1), 'parent_guid':parent.group(1) if parent else None})
for entry in classes:
    entry['parent']=next((p['id'] for p in classes if p['guid']==entry['parent_guid']),None)
for item in catalog: item['class_name']=next(c['name'] for c in classes if c['id']==item['id'].rsplit('-',1)[0])

# Replace selected inventory illustrations with the new icon collection.
item_choices={
    'sword':'ICON_SM_Wep_Sword_01.png','sword-rare':'ICON_SM_Wep_Sword_02.png',
    'greatsword':'ICON_SM_Wep_Sword_2H_01.png', 'shield':'ICON_SM_Wep_Shield_01.png',
}
weapon_root=ROOT/hud/'Icons_Weapons'
for name,filename in item_choices.items():
    source=weapon_root/filename
    if source.exists():copy(source.relative_to(ROOT),'item-'+name,'Synty POLYGON 风格武器物品图标')

(OUT/'asset-map.json').write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'source-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'skill-icon-map.json').write_text(json.dumps({'date':'2026-10-29','status':'design proposal; game asset references unchanged','skills':catalog,'classes':classes},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'copied_sources':len(manifest['files']),'skill_icons':len(catalog),'classes':len(classes)},ensure_ascii=False))
