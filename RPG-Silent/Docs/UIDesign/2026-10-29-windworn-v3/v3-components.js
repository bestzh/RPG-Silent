/* Windworn: standalone artboards only, with the existing page coverage intact. */
function v3Header(name,currency=true,back='hud-town'){
 return `<div class="screen-header"><div class="v3-wordmark-small">SILENT <span>· ${name}</span></div><div class="row">${currency?`<div class="currency"><span>${icon('coin')} 2,450</span><span class="small muted">剑士 · Lv.12</span></div>`:''}<button class="close" data-screen="${back}">${key('Esc')} 收起</button></div></div>`;
}
function v3Heading(title,sub=''){
 const notes={'行囊与装备':'战利品，与旧日纪念','待领取物品':'留给下一程的礼物','选择你的起点':'你的故事，从这里写起','继续你的冒险':'那些尚未写完的故事','冒险手札':'折起这一页，继续向前','旅途的偏好':'让旅途合你心意','成长与修习':'把锋芒磨成自己的模样','职业进阶':'从此，走自己的路','选择下一段旅途':'下一程，风会带路'};
 return `<div class="page-heading"><span class="v3-hand-note">${notes[title]||'在旅途中，慢慢成为自己'}</span><h1>${title}</h1>${sub?`<div class="subtitle">${sub}</div>`:''}</div>`;
}
function v3Seal(glyph='attack',text=''){
 return `<div class="v3-wax-seal">${icon(glyph)}${text?`<span>${text}</span>`:''}</div>`;
}
function v3Start(){
 return bg('castle')+`<div class="v3-start-veil"></div><div class="v3-start-rule"><span>沉默之境 · 风痕手札</span><span>一个人，也能走很远。</span></div><div class="v3-start-title"><span class="v3-start-note">把故事，留在路上。</span><div class="wordmark">SILENT</div><h1>风起于静默之时</h1></div><div class="v3-start-sketch"></div><div class="v3-hero-model">${model()}<span>远行者 / THE WANDERER</span></div><div class="v3-start-nav"><div class="v3-menu-index">01</div><div>${btn('继续这段旅途 <span>→</span>','characters','primary')}${btn('写下新的故事','creation')}${btn('整理旅途偏好','settings-screen')}${btn('暂别这个世界','','soft')}</div></div><div class="v3-start-letter"><span>致尚未归来的旅人</span><p>带上旧剑，穿过山谷。<br>去遇见值得记住的人，<br>也去成为，想成为的自己。</p>${v3Seal('flag','S')}</div><div class="v3-start-save"><b>最近的足迹</b><span>剑士 · Lv.12　/　主城　/　3 个角色存档</span></div><div class="v3-edition">WINDWORN<br><span>RPG · SILENT</span></div>`;
}
function v3Creation(){
 const c=classDefs[ui.classId];
 return bg('workshop','heavy')+header('创建角色',false,'start')+heading('选择你的起点','职业与外观 · 每一段旅途都独一无二')+`<div class="creation-left v3-class-ribbons">${Object.entries(classDefs).map(([id,cl],i)=>`<button class="v3-class-ribbon ${ui.classId===id?'selected':''}" data-class="${id}"><span class="v3-ribbon-no">0${i+1}</span>${icon(cl.glyph)}<div><h2>${cl.name}</h2><small>${cl.role}</small></div><span class="v3-ribbon-arrow">↗</span></button>`).join('')}<p class="v3-hand-note" style="margin:40px 18px">剑、法术，或一张弓。<br>总有一条路，适合你。</p></div><div class="creation-stage v3-creation-stage"><div class="v3-class-orbit"></div><span class="v3-giant-class">${c.name}</span>${model(ui.classId,ui.gender)}<div class="model-caption" style="bottom:86px">拖动旋转 · 滚轮缩放</div><div class="gender-choice">${['male','female'].map((g,i)=>`<button class="tab ${ui.gender===g?'active':''}" data-gender="${g}">${i?'女':'男'}</button>`).join('')}</div></div><aside class="v3-class-note paper"><span class="v3-paper-number">职业札记 / 0${Object.keys(classDefs).indexOf(ui.classId)+1}</span><h1>${c.name}</h1><p>${c.desc}</p><div class="rule"></div><div class="row">${item(ui.classId==='swordsman'?'sword':ui.classId==='mage'?'book':'sword-rare')}<div><small>初始武器</small><p>${c.weapon}</p></div></div><div class="rule"></div><small>10级开放转职</small><h3>${c.branches}</h3><p class="tiny muted" style="margin-top:13px">在主城导师处选择自己的职业道路。</p></aside><div class="creation-bottom">${btn('再想一想','start','soft')}<span class="small muted">${c.name} · ${ui.gender==='male'?'男':'女'} · 初始等级 1</span>${btn('带上行装，出发 →','hud-town','primary')}</div>`;
}
function v3Inventory(mode='inventory'){
 const potion=mode==='inventory-consumable',overflow=mode==='inventory-overflow',batch=mode==='inventory-batch';
 return bg('workshop','heavy')+header('行囊与装备')+heading(overflow?'待领取物品':'行囊与装备',overflow?'腾出背包空间，再带上新的收获。':'带在身上的，是装备，也是一路的故事。')+`<div class="v3-bag-count"><b>${overflow?'24':'18'} / 24</b><span>行囊容量　·　待领取 6</span></div><div class="v3-equipment-preview"><span class="v3-character-label">旅人的模样</span><div class="inventory-model model-stage">${model()}<div class="model-caption">拖动旋转 · 滚轮缩放</div></div><div class="equip-slots">${['invhelmet','invchest','invboots','invring','gem'].map((g,i)=>`<div class="equip-slot equip-${i}">${icon(g)}</div>`).join('')}</div><div class="v3-equipped-caption">剑士 <span>Lv.12</span></div><div class="inventory-stats"><span>生命 <b class="mint">+180</b></span><span>能量 <b class="mint">+40</b></span><span>伤害 <b class="mint">+12%</b></span><span>减伤 <b class="mint">18%</b></span></div><div class="v3-set-note">${tag('守望套装 · 2 / 5','gold-tag')}<p class="tiny muted">2件效果已激活 · 生命 +40</p></div></div><div class="v3-bag-bookmark">随身物记</div><div class="paper v3-bag-paper"><div class="inventory-body">${tabs(['全部','防具饰品','武器','材料药品','待领取'],overflow?4:potion?3:0,['inventory','inventory','inventory','inventory-consumable','inventory-overflow'])}${bagGrid(mode)}<div class="inventory-bottom"><span>${batch?'已选 4 件 · 已锁定与已装备物品受保护':'第 1 / 1 页'}</span><span>${key('←')} ${key('→')}</span></div><div class="bag-toolbar">${btn('多件试穿','','soft small-button')}${btn(batch?'退出批量':'批量分解',batch?'inventory':'inventory-batch',batch?'primary small-button':'soft small-button')}${btn('配装方案','loadouts','soft small-button')}</div></div></div><div class="v3-item-note frame"><div class="inventory-details">${section(batch?'拆解与收获':overflow?'未收入行囊':'这件装备的故事')}${batch?batchDetails():potion?potionDetails():itemDetails(overflow)}<div class="rule" style="margin:18px 0"></div>${batch?'<p class="tiny muted">请核对物品与收益，再确认分解。</p>':overflow?`<div class="overflow-note">背包已满 · 请先腾出空间</div>${btn('领取可放入的物品','','primary wide disabled')}`:`<div class="row between"><span class="tiny muted">词条 · 强化 · 宝石独立保存</span>${tag('已锁定')}</div><div class="detail-actions">${btn('确认穿戴','','primary')}${btn('卸下','','soft')}</div><div class="detail-actions">${btn('出售','','soft')}${btn('分解','','soft')}${btn('丢弃','discard','soft')}</div>`}</div></div>${foot(batch?'选择物品，确认后才会分解。':'点选装备试穿，确认后应用属性。','B 收起行囊')}`;
}
function v3SkillBar(){
 const ids=['swordsman-1','swordsman-2','swordsman-3'];
 return `<div class="skill-bar v3-skill-belt">${Array.from({length:10},(_,i)=>`<div class="skill-slot ${i>2?'empty':''}" style="--rise:${[8,-2,-7,-4,0,4,8,12,16,20][i]}px">${i<3?skillArt(ids[i]):'<span class="empty-slot-dot">·</span>'}${i===1?'<div class="cooldown-mask"></div><span class="cooldown">3.2</span>':''}${key((i+1)%10)}</div>`).join('')}</div>`;
}
function v3Mentor(){
 return v2Mentor().replace('class="service-grid"','class="service-grid v3-lesson-scrolls"').replace('剑术研习','剑术手记');
}
function v3Portal(which=0){
 const training=which===2,crystal=which===1;
 const title=['迷影森林','水晶洞窟','训练场'][which];
 const dest=['sy-destination-forest','sy-destination-crystal','sy-destination-training'][which];
 return bg('portal','heavy')+header('远行之门')+heading('选择下一段旅途','副本、难度与挑战模式 · 出发前准备妥当')+`<div class="v3-portal-scene" style="background-image:url('${V2.assets[dest]}')"><span class="v3-portal-chapter">${['第一程 / 林间旧事','第二程 / 蓝晶回响','片刻停留 / 磨砺自己'][which]}</span><div class="v3-portal-title"><span>${['MISTY FOREST','CRYSTAL CAVERN','PRACTICE GROUNDS'][which]}</span><h1>${title}</h1><p>${training?'在下一次远行前，把每一次出手练得更从容。':crystal?'循着蓝晶的回声，去见深处的守望者。':'林间的求援仍在回荡，旧路通向新的故事。'}</p></div></div><div class="v3-portal-ribbons">${[['迷影森林','林地边界 · 探索与试炼'],['水晶洞窟','古老石桥 · 共鸣深处'],['训练场','连段练习 · 防御与闪避']].map(([name,desc],i)=>`<button class="v3-dest-ribbon ${i===which?'selected':''}" data-screen="${['portal-forest','portal-crystal','portal-training'][i]}"><span>0${i+1}</span><div><h2>${name}</h2><p>${desc}</p></div></button>`).join('')}</div><div class="v3-travel-ticket paper"><div><small>${training?'练习什么':'选择难度'}</small>${training?'<h3>连段、技能、防御与闪避</h3>':tabs(crystal?['普通','困难','噩梦']:['简单','普通','困难'],crystal?0:1)}<p>${training?'每五秒恢复生命与能量':'敌人生命 ×1　攻击 ×1　奖励 ×1'}</p></div><div><small>${training?'训练规则':'挑战模式'}</small>${training?'<h3>不发放金币、经验或物品</h3>':tabs(['自由探索','限时挑战','首领试炼'],crystal?1:0)}<p>${training?'保留当前装备与已学技能':'出发前，可在主城整理行囊与药品。'}</p></div>${btn(training?'去练一会儿 →':'向'+title+'出发 →',training?'training':'hud-dungeon','primary')}</div>${foot('Esc 返回主城','传送失败时保留重试与返回入口')}`;
}
function v3SkillGuide(){
 const order=['swordsman','sword-saint','berserker','mage','shaman','sharpshooter','ranger','beastmaster','assassin'];
 const colors=['#d5a763','#d3c88b','#cd8065','#8ebbc8','#9dae96','#baae6f','#8eb18b','#afae79','#b0a4c8'];
 return `<div class="v3-atlas-bg"></div><div class="v3-atlas-heading"><span>旅人技艺录 · 21 招 / 9 职业</span><h1>技艺，与星火</h1><p>每一招都有自己的模样，也有属于它的脾气。</p></div><div class="v3-atlas-compass">N<div>✧</div>S</div><div class="v3-skill-atlas">${order.map((id,i)=>{const cl=V2.classes.find(c=>c.id===id),skills=V2.skills.filter(s=>s.id.startsWith(id+'-'));return `<section class="v3-profession-cluster" style="--class-ink:${colors[i]};--drift:${[8,40,0,0,15,26,9,23,-7][i]}px"><div class="v3-cluster-heading"><span>0${i+1}</span><h2>${cl.name}</h2><i>${skills.length} 招</i></div><svg class="v3-skill-thread" viewBox="0 0 520 150" aria-hidden="true"><path d="M40,70 Q130,15 210,90 T445,70" fill="none" stroke="currentColor" stroke-width="1" stroke-dasharray="4 9"/></svg><div class="v3-constellation-nodes">${skills.map((s,j)=>`<div class="v3-skill-node" style="--node-rise:${[0,21,-8][j]}px">${skillArt(s.id)}<b>${s.SkillName}</b><small>Lv.${s.RequiredLevel} · ${s.CooldownTime} 秒</small></div>`).join('')}</div></section>`;}).join('')}</div><div class="v3-atlas-footer"><span>图标来自 PONETI · 名称与条件来自项目当前技能数据</span><span>视觉规范附录 · 游戏技能引用未改</span></div>`;
}
function v3StyleGuide(){
 return `<div class="v3-atlas-bg"></div><div class="v3-style-title"><span>RPG-SILENT / WINDWORN / 2026.10.29</span><h1>风痕手札</h1><p>界面也可以像旅途中的一件旧物。<br>有磨损、有温度，也有自己的性格。</p></div><div class="v3-style-hero">${model('ranger','female')}<div class="v3-style-orbit"></div>${v3Seal('flag','S')}</div><div class="v3-material-swatches">${[['#263b34','苔绿皮革'],['#decda5','泛黄纸页'],['#bc965c','旧黄铜'],['#a5684d','暖锈与封蜡'],['#91b7c7','法术冰蓝']].map(([c,t],i)=>`<div style="--material:${c};transform:rotate(${[-3,2,-2,3,-1][i]}deg)"><i></i><span>${t}</span></div>`).join('')}</div><div class="v3-style-component frame"><span class="v3-hand-note">整齐的逻辑，也能有自由的外形</span><h2>轻边框 · 大留白 · 有节奏的错位</h2><div class="row" style="margin:25px 0">${btn('向下一程出发 →','','primary')}${btn('再想一想','','soft')}${btn('确认丢弃','','danger')}</div><div class="v3-style-skills">${['swordsman-1','swordsman-3','mage-1','mage-2','ranger-1','beastmaster-1'].map(id=>skillArt(id)).join('')}</div><p>标题像手记，正文保持清楚。<br>主要操作 48–76px，正文 22–26px，辅助信息 18–20px。</p></div><div class="v3-style-note paper"><h2>不同的地方，不同的气息</h2><p>行囊，摊开一整张装备台。<br>任务，翻开夹着书签的手札。<br>导师，留下自己的技艺卷轴。<br>锻炉，带着余烬与金属的暖光。</p><span>功能一致，物件各有个性。</span></div><div class="v3-atlas-footer"><span>40 个页面与状态 + 2 张规范图 / 仅设计预览</span><span>保留已有资源，重做视觉与构图</span></div>`;
}

const v3OriginalShow=showScreen;
showScreen=async function(id){
 window.DESIGN_READY=false;
 const family=/forge|gem-|crafting|reforge|supplies/.test(id)?'forge':/journal|achievements|codex/.test(id)?'journal':/inventory|loadouts|discard/.test(id)?'inventory':/mentor|advancement|npc-/.test(id)?'mentor':/portal/.test(id)?'travel':'adventure';
 canvas.dataset.family=family;
 canvas.dataset.profession=ui.classId;
 const result=await v3OriginalShow(id);
 document.getElementById('gallery-counter').textContent=(id==='style-guide'?'00':String(result.number).padStart(2,'0'))+' / 41';
 return result;
};
window.showScreen=showScreen;
skillBar=v3SkillBar;
const v3OriginalHud=v2HudBase;
hudBase=(dungeon=false)=>v3OriginalHud(dungeon).replace(v2SkillBar(),v3SkillBar());
inventory=v3Inventory;
Object.assign(RENDERERS,{start:v3Start,creation:v3Creation,inventory:()=>v3Inventory(),'inventory-batch':()=>v3Inventory('inventory-batch'),'inventory-overflow':()=>v3Inventory('inventory-overflow'),'inventory-consumable':()=>v3Inventory('inventory-consumable'),mentor:v3Mentor,'portal-forest':()=>v3Portal(),'portal-crystal':()=>v3Portal(1),'portal-training':()=>v3Portal(2),'style-guide':v3StyleGuide,'skills-visual-guide':v3SkillGuide});
makeNav();
showScreen(params.get('screen')||location.hash.slice(1)||'start');
