/* Content refinements verified against the current configuration and service formulas. */
function v5PreviewModel(){
 const item=v5Item(v5Entry(vx.selected)?.id);
 const chest=v5Entry(vx.armor[1]);
 const state=vx.armor.every(k=>!k)?'empty':vx.trying&&item?.Kind===1?'weapon':chest?.id==='warden-chest'||vx.trying&&item?.Id==='warden-chest'?'trial':'current';
 return `<img class="model" src="assets/models/loadout-${state}.png" alt="当前装备定义渲染的角色外观">`;
}
function v5StatsTable(entry,worn,before,after){
 const item=v5Item(entry.id);
 const head=worn?['装备属性','基础','强化','合计']:['全身装备加成','当前','换装后','变化'];
 return `<div class="v5-stat-table"><div class="table-head">${head.map(t=>'<span>'+t+'</span>').join('')}</div>${v5StatNames.map((name,j)=>{
  const suffix=j>1?'%':'';
  if(worn){const base=item[v5StatKeys[j]]||0,bonus=Math.ceil(base*entry.rank/10);return `<div><span>${name}</span><b>${base}${suffix}</b><b>+${bonus}${suffix}</b><em>${base+bonus}${suffix}</em></div>`;}
  const d=after[j]-before[j];return `<div><span>${name}</span><b>${before[j]}${suffix}</b><b>${after[j]}${suffix}</b><em class="${d>0?'mint':d<0?'red':'muted'}">${d>0?'+':''}${d}${suffix}</em></div>`;
 }).join('')}</div>`;
}
function v5SetSummary(nextCount,worn){
 const count=vx.armor.filter(k=>v5Entry(k)?.id.startsWith('crystal-')).length;
 if(worn)return '晶辉套装 · 实际穿戴 '+count+' / 5';
 if(count>=4&&nextCount<4)return `换装将使晶辉套装 ${count} → ${nextCount} 件，失去能量 +15。`;
 return '套装效果、宝石与强化均计入全身变化。';
}
function v5PortalCompact(mode=0,training=false,crystal=false){
 const names=['自由探索','限时挑战','首领试炼','随机远征','首领连战','千层试炼'];
 const summary=[
  '探索宝箱、机关和隐藏支路，可选择精英事件。金币与经验自动吸附，装备与材料靠近后手动拾取。',
  '在本图时限内通关可得额外挑战奖励。超时仍可继续探索，已拾取战利品保留。',
  '只挑战强化首领：额外生命 ×2、伤害 ×1.25。击败后获取试炼奖励，探索节点按此模式关闭。',
  '固定种子 284731 · 规则 v1<br>近战前锋 · 精英：护盾 / 治疗<br>可选节点：补给箱、精英伏击',
  '共 6 轮首领连战，每轮限时 10 分钟。敌人随轮次增强；本轮保存成功后恢复四分之一生命与能量，可休整、继续或主动结束。',
  '最高 1000 层，每 5 层为首领，其余层轮换敌人与精英。每层限时 10 分钟。重开从第 1 层开始，首次与里程碑记录保持。'
 ];
 const rules=mode===3?'守卫据点：90 秒内，在 3 米圈内累计守卫 25 秒，并清除两批守卫；离圈或敌人在圈内时停止计时。':mode>3?'奖励和纪录待保存时不能继续或结束离场；保存成功后再选择下一步。':'全局游戏难度影响承伤，副本难度另行缩放敌人生命、攻击与奖励。';
 const dest=training?'训练场':crystal?'水晶洞窟':'迷影森林';
 return bg('portal','heavy')+header('传送')+heading('选择目的地与挑战方式','目的地、难度和模式各自点选；规则与确认入口保持可见。')+`<nav class="v5-service-nav">${[['迷影森林','portal-forest'],['水晶洞窟','portal-crystal'],['训练场','portal-training']].map(([n,r])=>v5Route(n,r,'soft '+(n===dest?'active':''))).join('')}</nav><div class="v5-workspace v5-portal-compact"><div class="v5-destination-mini" style="background-image:url('assets/${training?'world-town':crystal?'sy-destination-crystal':'sy-destination-forest'}.png')"><h2>${dest}</h2></div><div class="v5-workspace-head"><h2>${training?'训练内容':'选择模式'}</h2><span>当前角色：剑士 Lv.12　${training?'':`· ${v5Route('难度：普通 ▾',crystal?'portal-crystal':v5ModePages[mode],'soft small-button')}`}</span></div><div class="v5-portal-columns"><nav>${training?'<h2>练习</h2><p>连段<br>技能<br>防御<br>闪避</p>':names.map((name,i)=>v5Route(name,v5ModePages[i],'soft '+(mode===i?'active':''))).join('')}</nav><section><h2>${training?'训练场':names[mode]}</h2><p>${training?'使用当前装备和已学技能。<br>每五秒恢复生命与能量，训练不发放金币、经验与物品。':summary[mode]}</p><div class="v5-status-strip">${training?'可随时重置练习或返回主城；主城仍禁止战斗。':rules}</div>${mode===3&&!training?`<p class="tiny muted">事件成功额外 80 金币 / 80 经验，本组合只发放一次。<br>重试或读档沿用种子，场景与事件进度重置。</p>${v5Route('更换组合 · 进入时替换','portal-expedition','soft small-button')}`:mode>3?'<p class="tiny muted">纪录按角色、地图、模式与难度分别保存。</p>':'<p class="tiny muted">普通难度：敌人生命 ×1、攻击 ×1、奖励 ×1。</p>'}</section></div><div class="v5-actions-bottom">${v5Route('返回主城','hud-town','soft')}${v5Route(training?'进入训练':'确认出发',training?'training':'hud-dungeon','primary')}</div></div>`+foot('Esc 返回主城','更换组合只在确认进入时替换当前远征记录');
}
function v5Forge(armor=false){
 v5ResetInventory('inventory');
 const entry=v5Entry(armor?'c':'w1'),item=v5Item(entry.id),next=entry.rank+1;
 const names=['生命','能量','伤害','减伤'];
 return v5Workspace(armor?'防具饰品强化':'武器强化','强化必定成功，最高 +5；确认费用后只强化所选的这一件装备。',`<div class="v5-tabs">${v5Route('武器强化','forge-weapon',armor?'':'active')}${v5Route('防具饰品强化','forge-armor',armor?'active':'')}${v5Route('宝石工坊','forge-gems','')}</div><div class="v5-split"><section><h2>选择具体装备</h2>${(armor?[...vx.armor.filter(Boolean),'new-chest']:vx.weapons).map((key,j)=>{const e=v5Entry(key);return v5Row(v5FullName(e),(armor?V5_SLOT_NAMES[v5Item(e.id).Slot]+' · '+(v5Worn(e.key)?'已穿戴':'未穿戴'):'武器槽 '+(j+1)+' · '+(j===0?'当前使用':'备用')),'',v5Route('查看',armor?'forge-armor':'forge-weapon','soft small-button'));}).join('')}</section><section><div class="v5-selected-item">${v5GearArt(entry)}<div><h2>${v5FullName(entry)}</h2><p>强化 +${entry.rank} → +${next}</p></div></div>${armor?`<div class="v5-invoice">${v5StatKeys.map((k,j)=>{const n=item[k]||0;return `<div><span>${names[j]}</span><b>${n+Math.ceil(n*entry.rank/10)} → ${n+Math.ceil(n*next/10)}${j>1?'%':''}</b></div>`;}).join('')}</div>`:`${v5Facts([['普攻强化加成','+20% → +30%'],['强化目标','武器槽 1 的原实例']])}`}<div class="v5-status-strip">消耗 ${next*100} 金币${armor?' · 锻造碎片 ×'+next*2:''}<br>持有：2,450 金币${armor?' · 锻造碎片 24':''}</div>${v5Route('确认强化 · '+next*100+' 金币',armor?'forge-armor':'forge-weapon','primary wide')}<p class="tiny muted" style="margin-top:18px">${armor?'基础属性按总强化等级取整；词条、宝石保持。':'先装入武器槽后强化；两件同名武器分别保存等级。'}</p></section></div>`,'smith','forge-weapon');
}
function v5NpcQuests(){const q=LIVE_QUESTS.find(q=>q.Id==='main-forest');return v5Workspace('任务委托','接取、完成目标、回到对应 NPC 提交；追踪可在手札中取消。',`<div class="v5-workspace-head"><h2>剑士导师的委托</h2><span>好感 10 · 全部 / 主线 / 支线 / 职业 / 特殊 / 重复</span></div>${v5Row(q.Title,q.Description,'glyph-flag',v5Route('接取任务','journal','primary small-button'))}${v5Row('职业考验','同一趟正式副本中完成职业动作并成功通关，训练与长期挑战不计。','glyph-attack',v5Route('查看条件','journal','soft small-button'))}<div class="v5-status-strip">重复委托交付后，需要再通关一趟普通副本刷新。<br>目标已完成时显示“提交并领取奖励”，无须在手札另找领奖按钮。</div>`,'mentor','npc-quests');}
function v5Story(confirm=false){
 const story=LIVE_STORIES.find(s=>s.Id==='market-aid'),choice=story.Choices[0];
 let html=bg('hall','heavy')+header('商人罗莎')+heading(story.Title,'交谈 / 故事 · 本角色好感与分支记录')+`<div class="v5-workspace" style="left:260px;right:260px"><span class="section-label">罗莎 · 第 2 / ${story.Pages.length} 段</span><h2 style="margin:23px 0 30px">${story.Title}</h2><p class="v5-story-copy">${story.Pages[story.Pages.length-1]}</p><div class="v5-story-choices">${story.Choices.map(c=>v5Route(c.Label,'story-confirm','soft')).join('')}</div><p class="v5-inline-note">先选择，再确认本次决定；选择结果保存在当前角色。</p></div>`+foot('Esc 先取消待确认选择','故事与接取条件来自当前配置');
 if(confirm)html+=v5Dialog('确认这个决定？',`<h2>${choice.Label}</h2><p>${choice.Outcome}</p>${v5Facts([['金币奖励','+'+choice.Gold],['经验奖励','+'+choice.Experience],['本 NPC 好感','+'+choice.Affinity],['分支记录','保存到当前角色']])}<p>取消不改变剧情或好感。放弃相关任务不会撤回已保存的决定。</p>`,v5Route('重新考虑','npc-story','primary')+v5Route('确认决定','npc-story','soft'));
 return html;
}
function v5Recipes(){return v5Workspace('装备配方锻造','按任务和配方条件解锁；费用、材料与产物一起核对。',`<div class="v5-tabs">${v5Route('装备配方','crafting','active')}${v5Route('词条重铸','reforge','')}</div>${LIVE_RECIPES.slice(0,4).map(r=>v5Row(r.ResultName||r.DisplayName||r.Id,'任务：'+(LIVE_QUESTS.find(q=>q.Id===r.UnlockQuestId)?.Title||'对应任务')+' · '+r.Gold+' 金币 · 锻造碎片 ×'+r.ForgeShards+' · 晶核碎片 ×'+r.CrystalShards,'gear-chest',v5Route('锻造一件','crafting','primary small-button'))).join('')}<p class="v5-inline-note">共 ${LIVE_RECIPES.length} 张配方，向下滚动查看。满包时沿用产物的待领取规则。</p>`,'smith','crafting');}
function v5Reforge(review=false){const item=v5Item('warden-chest'),gold=100+75*item.Rarity,shards=2+2*item.Rarity;let html=v5Workspace('词条重铸','先选择具体实例，再核对费用与保留内容。',`<div class="v5-tabs">${v5Route('装备配方','crafting','')}${v5Route('词条重铸','reforge','active')}</div><div class="v5-split"><section><h2>选择待重铸装备</h2>${v5Row('守望护甲 +1','史诗 · 未穿戴 · 未锁定','gear-candidate',v5Route('查看并确认','reforge-review','primary small-button'))}<p class="v5-inline-note">已穿戴也可选择；锁定、普通品质与堆叠物品不可重铸。</p></section><section><h2>原物品保留</h2><p class="v5-inline-note">品质、强化等级与宝石保持。<br>重新生成随机词条，结果可能变弱。</p>${v5Facts([['消耗金币',gold+''],['锻造碎片','×'+shards]])}${v5Route('核对并重铸','reforge-review','primary')}</section></div>`,'smith','crafting');if(review)html+=v5Dialog('确认消耗并重铸',`<h2>守望护甲 +1 · 原实例</h2><p>品质、强化与宝石保留；重新生成随机词条。<br>结果可能变弱。</p>${v5Facts([['消耗金币',gold+''],['锻造碎片','×'+shards]])}<p>取消不消耗资源，确认失效或保存失败时原物品保持。</p>`,v5Route('取消','reforge','primary')+v5Route('确认重铸','reforge','danger'));return html;}
RENDERERS['portal-forest']=()=>v5PortalCompact();RENDERERS['portal-crystal']=()=>v5PortalCompact(0,false,true);RENDERERS['portal-training']=()=>v5PortalCompact(0,true);
for(const [j,id] of v5ModePages.entries())if(j>0)RENDERERS[id]=()=>v5PortalCompact(j);
RENDERERS['forge-weapon']=()=>v5Forge();RENDERERS['forge-armor']=()=>v5Forge(true);RENDERERS['npc-quests']=v5NpcQuests;RENDERERS['npc-story']=()=>v5Story();RENDERERS['story-confirm']=()=>v5Story(true);RENDERERS['crafting']=v5Recipes;RENDERERS['reforge']=()=>v5Reforge();RENDERERS['reforge-review']=()=>v5Reforge(true);
const v5OldJournal=v5Journal;
v5Journal=function(page='tasks'){
 let html=v5OldJournal(page);
 if(page==='tasks'){
  const q=LIVE_QUESTS.find(x=>x.Id==='main-forest');
  html=html.replace('<span>按任务目标完成当前遭遇</span><span>进行中</span>',`<span>${q.Objectives[0].Label}</span><span>4 / ${q.Objectives[0].Required}</span>`).replace('<span>返回对应 NPC 提交</span><span>未提交</span>',`<span>${q.Objectives[1].Label}</span><span>0 / ${q.Objectives[1].Required}</span>`);
 }else if(page==='achievements'){
  for(const [j,old] of ['初入冒险','精进剑术','探索者','收藏的乐趣'].entries())html=html.replace(old,LIVE_ACHIEVEMENTS[j].Title);
 }
 return html;
};
const v5OldTutorial=v5Tutorial;
v5Tutorial=function(review=false){let html=v5OldTutorial(review);if(review){for(const [j,n] of ['熟悉移动','装备与切换武器','有效普攻','完成翻滚','完成防御','手动拾取','与 NPC 接取任务'].entries())html=html.replace((j+1)+'. '+n,(j+1)+'. '+LIVE_TUTORIAL_TITLES[j]);}return html;};
/* Initial render follows the final functional contract updates. */
