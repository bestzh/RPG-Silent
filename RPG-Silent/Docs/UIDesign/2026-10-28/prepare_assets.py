"""Copy existing project art into a portable, provenance-tracked design package.

Source images are copied byte-for-byte. No project asset is modified.
"""
from pathlib import Path
from PIL import Image
import hashlib
import json
import re
import shutil

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
ASSETS = OUT / 'assets'
ASSETS.mkdir(parents=True, exist_ok=True)
manifest = []
mapping = {}

def copy(source, name, role):
    source = ROOT / source
    if not source.is_file():
        raise FileNotFoundError(source)
    dest = ASSETS / name
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, dest)
    record = {'source': source.relative_to(ROOT).as_posix(), 'design_file': dest.relative_to(OUT).as_posix(), 'role': role, 'sha256': hashlib.sha256(source.read_bytes()).hexdigest()}
    if source.suffix.lower() in ('.png', '.jpg', '.jpeg', '.webp'):
        with Image.open(source) as im:
            record['size'] = list(im.size)
    meta = Path(str(source) + '.meta')
    if meta.is_file():
        m = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if m:
            record['guid'] = m.group(1)
    manifest.append(record)
    mapping[name.rsplit('.', 1)[0]] = record['design_file']

for src, name in [('BG.png','bg-castle.png'), ('BG2.png','bg-hall.png'), ('BG3.png','bg-workshop.png'), ('SettingBG.png','bg-portal.png'), ('Loding.png','bg-loading.png')]:
    copy('Assets/Resources/BG/' + src, name, '原项目背景，直接复用')
for src, name in [('UI_Panel_Window_Blank.png','panel.png'), ('UI_Panel_Paper.png','paper.png'), ('UI_Panel_Paper_Inset.png','paper-inset.png'), ('UI_Button_Grey.png','button-grey.png'), ('UI_Button_Orange.png','button-gold.png'), ('UI_Bar.png','bar-frame.png'), ('UI_Bar_Fill_Red.png','bar-red.png'), ('UI_Bar_Fill_Blue.png','bar-blue.png'), ('UI_Bar_Fill_Orange.png','bar-gold.png')]:
    copy('Assets/Fantasy UI/PNG/' + src, name, 'Fantasy UI 九宫格面板、按钮或进度条')
copy('Assets/Resources/UI/ui-divider.png','divider.png','原项目装饰分割线')
copy('Assets/Fonts/LongCang-Regular.ttf','longcang.ttf','项目中文标题字体')
copy('Assets/Bamao/BamaoUIPack/Sprites/Shop/paper button.png','button-paper.png','Bamao 现有纸质按钮')
for key in ['Bag1','Book','Map','Hammer','Award','Archer','Attack','Energy','Fire','Heart','Skull','Target','InvHelmet','InvChest','InvBoots','InvRing','Gem','Coin','Flag','Star','Question','InvPotion','Lock','Check','Settings','Sound','Music','Volume','Shield','Snowflake','Lightning','Cross','ArrowLeft','ArrowRight','Exit','Home']:
    src = ROOT / ('Assets/Fantasy UI/Simple Vector Icons/128px/UI_Icon_' + key + '.png')
    if src.is_file():
        copy(src.relative_to(ROOT), 'glyph-' + key.lower() + '.png', 'Fantasy UI 现有符号，用于导航、槽位和临时技能符号')
shop = 'Assets/Bamao/BamaoUIPack/Sprites/Shop/'
for src, name in [('icon_armor_shade.png','item-armor.png'), ('icon_armor_beginner_shade.png','item-armor-basic.png'), ('icon_boots_shade.png','item-boots.png'), ('icon_boots_beginner_shade.png','item-boots-basic.png'), ('icon_ring_shade.png','item-ring.png'), ('icon_shield_shade.png','item-shield.png'), ('icon_sword_shade.png','item-sword.png'), ('icon_sword2_shade.png','item-greatsword.png'), ('icon_sword3_shade.png','item-sword-rare.png'), ('icon_mana_red_shade.png','item-potion-red.png'), ('icon_mana_blue_shade.png','item-potion-blue.png'), ('icon_mana_green_shade.png','item-potion-green.png'), ('icon_gold_shade.png','item-gold.png'), ('icon_mine_shade.png','item-ore.png'), ('icon_diamond.png','item-gem.png'), ('blue crystal.png','item-crystal.png'), ('Red crystal.png','item-crystal-red.png'), ('icon_envelope_shade.png','item-letter.png'), ('icon_KEY_shade.png','item-key.png')]:
    copy(shop + src, name, 'Bamao 现有手绘物品图标，拟统一映射到对应物品类别')
for src, name in [('E_magic book.png','item-book.png'), ('E_magic book 2.png','item-book-blue.png')]:
    copy('Assets/Bamao/BamaoUIPack/Sprites/Equipment/' + src, name, 'Bamao 现有魔法书图标')
for src, name in [('town-portal-world-final.png','world-town.png'), ('lock-defense-Dungeon02.png','world-dungeon.png'), ('adventure-training.png','world-training.png')]:
    copy('Tools/Validation/Captures/' + src, name, '项目既有验证截图；仅用作设计场景底图参考')

counts = {}
for folder in ['Assets/Fantasy UI/PNG','Assets/Fantasy UI/Resource Icons','Assets/Fantasy UI/Simple Vector Icons/128px','Assets/Bamao/BamaoUIPack/Sprites','Assets/Prefabs/PlayerAppearances']:
    counts[folder] = len(list((ROOT/folder).rglob('*.png' if 'PlayerAppearances' not in folder else '*.prefab')))
(OUT/'source-manifest.json').write_text(json.dumps({'date':'2026-10-28','source_root':str(ROOT),'source_inventory_counts':counts,'files':manifest}, ensure_ascii=False, indent=2), encoding='utf-8')
(OUT/'asset-map.json').write_text(json.dumps(mapping, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'copied':len(manifest),'inventory':counts,'output':str(OUT)}, ensure_ascii=False))
