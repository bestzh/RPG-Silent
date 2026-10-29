/* Design-only components. These files are not loaded by Unity or the game. */
const art=(key,cls='',alt='')=>`<img class="${cls}" src="${V2.assets[key]}" alt="${alt}">`;
const skillArt=(id,cls='')=>{const s=V2.skills.find(s=>s.id===id);return `<img class="skill-art ${cls}" src="${s.icon}" alt="${s.SkillName}">`;};
const crest=(cls='')=>`<div class="v2-crest ${cls}">${art('sy-crest-wings','crest-wings')}${art('sy-crest-shield','crest-shield')}${art('sy-crest-swords','crest-swords')}</div>`;

function v2Header(name,currency=true,back='hud-town'){
 return `<div class="screen-header"><div class="v2-breadcrumb">${art('sy-crest-dragon','mini-dragon')}<span class="mini-brand">SILENT</span><span class="breadcrumb-slash">/</span><span>${name}</span></div><div class="row">${currency?`<div class="currency"><span>${icon('coin')} 2,450</span><span class="small muted">剑士 · Lv.12</span></div>`:''}<button class="close" data-screen="${back}">返回 ${key('Esc')}</button></div></div>`;
}
function v2Heading(title,sub=''){
 const labels={'行囊与装备':'EQUIPMENT & INVENTORY','待领取物品':'PENDING REWARDS','选择你的道路':'CHARACTER CREATION','继续你的冒险':'YOUR ADVENTURERS','旅途的偏好':'SETTINGS','选择下一段旅途':'THE NEXT CHAPTER','冒险手札':'ADVENTURE JOURNAL'};
 return `<div class="page-heading"><div class="v2-eyebrow">${labels[title]||'SILENT / YOUR JOURNEY'}</div><h1>${title}</h1>${sub?`<div class="subtitle">${sub}</div>`:''}</div>`;
}
function v2Start(){
 return bg('castle')+`<div class="v2-start-vignette"></div><div class="v2-start-top"><span>沉默之境</span><span>一段关于远行与归途的冒险</span></div><div class="v2-start-brand">${crest()}<div class="wordmark">SILENT</div><div class="v2-brand-line">${art('sy-divider')}<span>循光而行 · 向未知出发</span></div></div><div class="v2-start-actions">${btn('继续冒险','characters','primary')}${btn('创建新角色','creation')}${btn('游戏设置','settings-screen')}${btn('退出游戏','','soft')}</div><div class="v2-start-save"><span class="v2-live-dot"></span><div><b>你的旅途，仍在继续</b><p>剑士 · Lv.12　主城　｜　3 个角色存档</p></div></div><div class="v2-start-story"><span>CHAPTER I</span><h2>风起于静默之时</h2><p>穿过山谷与高塔，<br>让每一次选择，成为自己的故事。</p></div><div class="screen-corner">RPG-SILENT　 /　 本地冒险</div>`;
}
function v2SkillBar(){
 const ids=['swordsman-1','swordsman-2','swordsman-3'];
 return `<div class="skill-bar">${Array.from({length:10},(_,i)=>`<div class="skill-slot ${i>2?'empty':''}">${i<3?skillArt(ids[i]):'<span class="empty-slot-dot">·</span>'}${i===1?'<div class="cooldown-mask"></div><span class="cooldown">3.2</span>':''}${key((i+1)%10)}</div>`).join('')}</div>`;
}
function v2HudBase(dungeon=false){
 return world(dungeon)+`<div class="v2-hud-edge"></div><div class="hud-status"><div class="v2-hud-portrait">${model()}${art('sy-portrait-ring','portrait-ring')}<span>12</span></div><div class="hud-vitals"><div class="name">剑士 <span>剑锋所至，长路无惧</span></div><div class="v2-vital-row"><div class="v2-vital-symbol">♥</div>${meter(dungeon?68:100,'red-meter')}<small>${dungeon?'680':'1,000'} / 1,000</small></div><div class="v2-vital-row"><div class="v2-vital-symbol energy">◆</div>${meter(82,'blue-meter')}<small>123 / 150</small></div></div></div><div class="hud-wallet"><span>${icon('coin')} 2,450</span><span class="tiny muted">经验　480 / 1,200</span></div>${map(dungeon)}<div class="hud-tracker"><div class="v2-tracker-label">${icon('flag')} 主线任务</div><h3>林地危机</h3><p>${dungeon?'通关迷影森林　0 / 1<br>返回职业导师处提交':'前往传送门<br>通关迷影森林　0 / 1'}</p><div class="v2-tracker-distance">${dungeon?'◇ 前方仍有敌人':'◇ 传送门 · 18 米'}</div><div class="tiny muted">${key('J')} 查看任务详情</div></div><div class="weapon-strip">${item('sword')}<div>训练单手剑<small>当前武器　${key('Tab')} 切换</small></div></div>${potions()}${v2SkillBar()}<div class="hud-nav"><button class="hud-action" data-screen="journal">${icon('book')} 手札 ${key('J')}</button><button class="hud-action" data-screen="inventory">${icon('bag1')} 行囊 ${key('B')}</button></div><div class="hud-exp"><span></span></div>`;
}
function v2Mentor(){
 const lessons=V2.skills.filter(s=>s.id.startsWith('swordsman-'));
 return npcBase()+`<div class="npc-shell frame"><div class="service-heading"><h2>剑术研习</h2><span class="small muted">剑士 · Lv.12　${tag('技能最高 5 级','gold-tag')}</span></div><div class="service-grid">${lessons.map((s,i)=>`<div class="service-card v2-skill-card"><div class="v2-skill-top">${skillArt(s.id)}<span class="v2-skill-rank">${i===2?'尚未学习':`等级 ${i+1} / ${s.MaxRank}`}</span></div><h3>${s.SkillName}</h3><p class="gold">${i===2?'Lv.'+s.RequiredLevel+' 解锁':'已学习 · 可升级'}</p><p>学习费用 ${s.LearnCost} 金币<br>能量 ${s.EnergyCost}　·　冷却 ${s.CooldownTime} 秒</p><div class="row" style="gap:10px">${btn(i===2?'学习技能':'升级技能','','primary small-button')}${btn('装配','','soft small-button')}</div></div>`).join('')}</div><div class="npc-loadout"><div class="row between"><span>装配至技能槽</span><div class="row"><span class="small muted">当前选择 ${key('3')}</span>${btn('卸下该槽技能','','soft small-button')}${btn('查看转职分支','advancement','small-button')}</div></div><div class="mini-slots">${Array.from({length:10},(_,i)=>`<div class="mini-slot ${i===2?'active':''}">${i<2?skillArt(lessons[i].id):key((i+1)%10)}</div>`).join('')}</div></div></div>${foot('新技能自动装入第一个空槽。','转职后仍可学习基础职业技能')}`;
}
function v2Advancement(){
 return npcBase('mentor','职业进阶','剑士导师 · 10级开放职业分支')+`<div class="npc-shell frame"><div class="service-heading"><h2>选择你的剑道</h2><span class="small muted">剑士 → 进阶职业</span></div><div class="service-grid v2-branches">${[['sword-saint','剑圣','精研剑气与范围斩击，以连段持续压制敌人。'],['berserker','狂战士','侧重强力打击与爆发，用攻势打破敌人防线。']].map(([id,name,desc],i)=>`<div class="service-card ${i===0?'chosen':''}"><div class="row between"><div class="v2-branch-icons">${skillArt(id+'-1')}${skillArt(id+'-2')}</div>${i===0?tag('当前选择','gold-tag'):''}</div><h2>${name}</h2><p>${desc}</p><div class="rule"></div><div class="row between"><span class="gold">Lv.10 · 300 金币</span>${btn(i===0?'确认转职':'选择该分支','','primary small-button')}</div></div>`).join('')}</div><div class="reforge-warning" style="margin-top:25px">本角色确认转职后无法改选另一分支。再次确认才会提交选择。</div></div>${foot('确认前可重新选择分支。','请核对职业与转职消耗')}`;
}
function v2StyleGuide(){
 return `<div class="v2-guide-bg"></div><div class="v2-guide"><div class="v2-guide-header"><div>${crest('guide-crest')}<div class="wordmark">SILENT</div><span class="v2-eyebrow">UI DESIGN / SYNTY EDITION / 2026.10.29</span></div><div><h1>徽章金，与深夜蓝</h1><p>让界面与 POLYGON 世界说同一种语言。<br>用精细纹饰建立秩序，用真实技能图标帮助辨认。</p></div></div><div class="v2-guide-swatches">${[['#101D2B','深夜蓝 · 主面板'],['#1B3042','青岩蓝 · 次表面'],['#DCC083','徽章金 · 焦点'],['#E8E6DC','月白 · 文字'],['#85B5CE','冰蓝 · 能量'],['#BB655E','朱砂 · 危险']].map(([color,label])=>`<div><i style="background:${color}"></i><b>${label}</b><small>${color}</small></div>`).join('')}</div><div class="v2-guide-columns"><div class="frame"><span class="v2-eyebrow">01 / MENUS</span><h2>同一套边框与操作层级</h2><div class="row" style="margin:32px 0">${btn('主要操作','','primary')}${btn('次要操作')}${btn('危险操作','','danger')}</div>${tabs(['当前分页','普通分页','普通分页'])}<p>正文 22–26px · 辅助 18–20px<br>64px 安全边距 · 8px 间距基准</p></div><div class="frame"><span class="v2-eyebrow">02 / WARRIOR HUD + ICONS</span><h2>信息清楚，技能各有面貌</h2><div class="v2-guide-skills">${['swordsman-1','swordsman-3','mage-1','mage-2','ranger-1','beastmaster-1'].map(id=>skillArt(id)).join('')}</div>${meter(72,'red-meter')}<div style="height:12px"></div>${meter(85,'blue-meter')}<p>Synty：面板、菜单、徽章、HUD、导航<br>PONETI：21 个技能的独立图标</p></div></div><div class="v2-guide-footer"><span>40 个页面与状态 + 视觉规范 + 全技能图标规范</span><span>中文正文为系统字体预览；正式发布仍需可分发的中文正文字体</span></div></div>`;
}
function v2SkillGuide(){
 const order=['swordsman','sword-saint','berserker','mage','shaman','sharpshooter','ranger','beastmaster','assassin'];
 return `<div class="v2-guide-bg"></div><div class="v2-skill-guide"><div class="v2-skill-guide-head"><div><span class="v2-eyebrow">VISUAL REFERENCE / 21 SKILLS</span><h1>每一个技能，都能一眼辨认</h1><p>依据项目当前技能名称与职业，选择 PONETI 独立图标。</p></div>${crest('guide-crest')}</div><div class="v2-skill-catalog">${order.map(id=>{const cl=V2.classes.find(c=>c.id===id),skills=V2.skills.filter(s=>s.id.startsWith(id+'-'));return `<div class="frame"><div class="row between"><h3>${cl.name}</h3><span class="tiny gold">${skills.length} SKILLS</span></div><div class="v2-skill-samples">${skills.map(s=>`<div>${skillArt(s.id)}<b>${s.SkillName}</b><small>Lv.${s.RequiredLevel} · ${s.CooldownTime} 秒</small></div>`).join('')}</div></div>`;}).join('')}</div><div class="v2-guide-footer"><span>图标建议已记录源路径与 GUID，可供后续 Unity 落地映射。</span><span>本页为设计附录 · 尚未修改游戏技能资源引用</span></div></div>`;
}

hudBase=v2HudBase;
skillBar=v2SkillBar;
Object.assign(RENDERERS,{start:v2Start,mentor:v2Mentor,advancement:v2Advancement,'style-guide':v2StyleGuide,'skills-visual-guide':v2SkillGuide});
for(const id of ['swordsman','mage','ranger']){
 const children=V2.classes.filter(c=>c.parent===id);
 if(children.length)classDefs[id].branches=children.map(c=>c.name).join(' / ');
}
SCREENS.push({id:'skills-visual-guide',number:41,title:'全职业技能图标规范',group:'设计附录',description:'21 个现有技能与新图标的完整对照；仅为视觉设计建议。',source:'Assets/Resources/ClassSkills + PONETI 6000 Fantasy Icons',file:'41-skills-visual-guide.png'});
makeNav();
document.getElementById('gallery-counter').textContent='';
showScreen(params.get('screen')||location.hash.slice(1)||'start');
