/* Final source-contract updates. This HTML prototype has no connection to game saves. */
const v5BaseReset=v5ResetInventory;
v5ResetInventory=function(mode='inventory'){
 v5BaseReset(mode);vx.quantities={};
 if(vx.outfit){vx.draftArmor=vx.armor.slice();vx.draftWeapons=vx.weapons.slice();vx.draftActive=vx.active;vx.draftArmor[1]='new-chest';vx.draftArmor[4]='amulet';}
 if(mode==='equipment-effects'){vx.bag.push({key:'effect-sword',id:'legendary-sword',rank:0,count:1});vx.selected='effect-sword';vx.weapons[0]='effect-sword';}
};
const v5BaseSelect=v5Select;
v5Select=function(key){
 if(!vx.outfit){v5BaseSelect(key);return;}
 const e=v5Entry(key);if(!e)return;vx.selected=key;const i=v5Item(e.id);
 if(vx.filter!=='待领取'&&i.Kind===0)vx.draftArmor[i.Slot]=key;
 vx.trying=true;vx.message=i.Kind===1?'选择试穿到武器槽 1 或 2，再确认整套。':'整套试穿未装备；可继续选择其他部位，最后一次确认。';v5Redraw();
};
function v5CanApplyDraft(){return vx.draftArmor&&[...vx.draftArmor,...vx.draftWeapons].filter(Boolean).every(k=>vx.level>=v5Item(v5Entry(k).id).RequiredLevel)&&new Set(vx.draftWeapons).size===vx.draftWeapons.length;}
const v5BaseHandle=v5Handle;
v5Handle=function(action){
 const [type,value]=action.split(':'),e=v5Entry(vx.selected);
 if(type==='outfit'&&!vx.outfit){vx.outfit=vx.trying=true;vx.filter='全部';vx.query='';vx.draftArmor=vx.armor.slice();vx.draftWeapons=vx.weapons.slice();vx.draftActive=vx.active;vx.message='依次点击防具组合试穿；两把武器分别选择预览槽位。';v5Redraw();return;}
 if(type==='bulk'){vx.filter='全部';vx.query='';}
 if(type==='wear'&&vx.outfit&&v5Item(e?.id)?.Kind===1){vx.draftWeapons[Number(value)]=e.key;vx.draftActive=Number(value);vx.message='武器已加入试穿方案，确认整套前实际装备保持不变。';v5Redraw();return;}
 if(type==='apply-outfit'||type==='wear'&&vx.outfit){
  if(v5CanApplyDraft()){vx.armor=vx.draftArmor.slice();vx.weapons=vx.draftWeapons.slice();vx.active=vx.draftActive;vx.outfit=vx.trying=false;vx.message='设计预览：整套装备已一起更新，未写入游戏与存档。';}else vx.message='整套暂不能确认：检查装备等级及两个武器槽是否重复。';v5Redraw();return;
 }
 if(type==='count-up'||type==='count-down'){const item=v5Entry(value);vx.quantities[value]=Math.max(1,Math.min(item.count,(vx.quantities[value]??item.count)+(type==='count-up'?1:-1)));v5Redraw();return;}
 if(type==='effects'){vx.modal='effects';v5Redraw();return;}
 v5BaseHandle(action);
};
const v5BaseDetails=v5Details;
v5Details=function(){
 let html=v5BaseDetails();const e=v5Entry(vx.selected),i=v5Item(e?.id);
 if(vx.outfit&&i?.Kind===1)html=html.replace('确认装入武器槽 1','试穿到武器槽 1').replace('装入武器槽 2','试穿到武器槽 2');
 if(vx.outfit&&i?.Kind===0&&!v5Worn(e.key)){
  let a=vx.armor.slice();a[i.Slot]=e.key;html=html.replace(v5StatsTable(e,false,v5Totals(),v5Totals(a)),v5StatsTable(e,false,v5Totals(),v5Totals(vx.draftArmor)));
  const count=vx.draftArmor.filter(k=>v5Entry(k)?.id.startsWith('crystal-')).length;html=html.replace(/<p class="v5-set-note">.*?<\/p>/,'<p class="v5-set-note">'+v5SetSummary(count,false)+' 变化包含整套试穿。</p>');
 }
 if(vx.outfit)html=html.replaceAll('换装后','整套确认后').replace(/data-v5="(sell|dismantle|discard)"/g,'disabled data-v5="$1"').replace('出售与分解需在主城进行。','整套试穿期间先确认或取消，再处理物品。');
 if(i?.SpecialEffect&&i.Rarity>=4)html=html.replace('<div class="v5-detail-controls">',v5Button('精准回能 I · 特殊效果规则','effects','soft small-button')+'<div class="v5-detail-controls">');
 return html;
};
const v5BaseBag=v5Bag;
v5Bag=function(){let html=v5BaseBag().replace('18 / 24 格',vx.bag.length+' / 24 格');if(vx.outfit)html=html.replace('<div class="v5-bag-tools">','<div class="v5-bag-tools">'+v5Button('确认整套','apply-outfit','primary small-button',!v5CanApplyDraft()));return html;};
const v5BaseInventory=v5Inventory;
v5Inventory=function(){let html=v5BaseInventory().replace('背包 18 / 24','背包 '+vx.bag.length+' / 24');if(vx.outfit){const changes=vx.draftArmor.map((k,j)=>k!==vx.armor[j]?V5_SLOT_NAMES[j]+' → '+v5FullName(v5Entry(k)):'').filter(Boolean);html=html.replace(vx.message,'整套试穿：'+(changes.join('；')||'尚未修改')+'。确认前，当前穿戴不变。');}return html;};
const v5BasePreview=v5PreviewModel;
v5PreviewModel=function(){
 if(vx.outfit){const state=v5Entry(vx.draftArmor?.[1])?.id==='warden-chest'?'trial':'current';return `<img class="model" src="assets/models/loadout-${state}.png" alt="整套试穿代表组合预览">`;}
 return v5BasePreview();
};
const v5BaseModal=v5InventoryModal;
v5InventoryModal=function(){
 if(vx.modal==='effects')return v5Dialog('装备特殊效果',`<h2>传说单手剑 · 精准回能 I</h2><p>精准格挡成功额外恢复 12 点能量；普通格挡和闪避不触发。冷却 6 秒。</p>${v5Facts([['当前状态','武器槽 1 · 当前使用 · 生效'],['同类规则','仅取最高品质，不叠加'],['卸下 / 再装备','失效 / 不刷新本场冷却']])}<p class="muted">仅当前武器和已穿戴防具饰品生效；试穿不提供战斗效果。死亡、结算、切场景和读档清空临时状态。</p>`,v5Button('返回物品详情','close-modal','primary'));
 let html=v5BaseModal();
 if(vx.modal==='bulk'){
  for(const k of vx.chosen){const n=vx.quantities[k]??v5Entry(k).count;const prefix='data-v5="count-down:'+k+'">−</button><b>×';html=html.replace(prefix+v5Entry(k).count+'</b>',prefix+n+'</b>');}
  if(vx.bulk==='sell'){const total=vx.chosen.reduce((s,k)=>s+v5Item(v5Entry(k).id).SellPrice*(vx.quantities[k]??v5Entry(k).count),0);html=html.replace(/获得 \d+ 金币/,'获得 '+total+' 金币');}
 }
 return html;
};
const v5BaseShop=v5Shop;
v5Shop=function(state='purchase-list'){return v5BaseShop(state).replaceAll('35 金币',v5Item('forest-chest').SellPrice+' 金币');};
let v5ChosenMap=null;
const v5BasePortal=v5PortalCompact;
v5PortalCompact=function(mode=0,training=false,crystal=false){
 if(v5ChosenMap&&!training)crystal=v5ChosenMap==='crystal';
 return v5BasePortal(mode,training,crystal).replaceAll('284731',crystal?'284875':'284941').replaceAll('80 金币 / 80 经验','160 金币 / 160 经验').replace('在本图时限内通关可得额外挑战奖励。','在 '+(crystal?'7':'5')+' 分钟内通关可得额外挑战奖励。');
};
document.addEventListener('click',event=>{
 const button=event.target.closest('#canvas button[data-screen]');if(!button)return;const route=button.dataset.screen;
 if(route==='portal-crystal')v5ChosenMap='crystal';else if(route==='portal-forest')v5ChosenMap='forest';else if(v5ModePages.includes(route))v5ChosenMap=document.querySelector('.v5-destination-mini h2')?.textContent==='水晶洞窟'?'crystal':'forest';
},true);
const v5BaseHudExtra=v5HudExtra;
v5HudExtra=function(kind){return v5BaseHudExtra(kind).replaceAll('284731','284941');};
const v5BaseStyle=v5Style;
v5Style=function(){return v5BaseStyle().replace('当前 42 个 UI 脚本','当前 43 个 UI 脚本');};
RENDERERS['style-guide']=v5Style;
let v5TutorialPage=0;
v5Tutorial=function(review=false){
 if(!review)return v5PreviousHudTown()+v5Dialog('新手引导 1 / 14','<h2>熟悉移动</h2><p>使用当前绑定的方向键移动。<br>鼠标控制视角，按 Ctrl 显示鼠标后操作按钮。</p><p class="v5-inline-note">已完成 0 / 14 · 自动保存</p>',v5Route('返回游戏','hud-town','primary')+v5Route('查看全部步骤','tutorial-review','soft'));
 const start=v5TutorialPage*7;
 return v5PreviousHudTown()+v5Dialog('新手引导 · 可随时回看',`<div class="v5-split"><section><h2>步骤 · 第 ${v5TutorialPage+1} / 2 页</h2>${LIVE_TUTORIAL_TITLES.slice(start,start+7).map((n,j)=>`<div class="v5-checklist" style="margin:0"><div><span>${start+j+1}. ${n}</span><span>${start+j<2?'已完成':'待完成'}</span></div></div>`).join('')}<div class="v5-tutorial-pagination"><button class="button soft small-button" data-v5-tutorial="0" ${!v5TutorialPage?'disabled':''}>上一页</button><button class="button soft small-button" data-v5-tutorial="1" ${v5TutorialPage?'disabled':''}>下一页</button></div></section><section><h2>查看与继续</h2><p>按当前绑定完成动作，实际完成后计入进度。支持提前完成步骤、跳过、退出后继续。</p><p class="v5-inline-note">14 项标题直接来自当前 TutorialProgress；无额外引导领奖步骤。</p></section></div>`,v5Route('返回游戏','hud-town','primary')+v5Route('跳过剩余引导','hud-town','soft'));
};
document.addEventListener('click',event=>{const e=event.target.closest('[data-v5-tutorial]');if(!e)return;event.stopImmediatePropagation();v5TutorialPage=Number(e.dataset.v5Tutorial);showScreen('tutorial-review');},true);
const v5BaseJournal=v5Journal;
v5Journal=function(page='tasks'){
 if(page!=='achievements')return v5BaseJournal(page);
 return v5Workspace('成就奖励','本角色独立记录；达成后手动领取。',`<div class="v5-tabs">${[['任务','journal'],['主线章节','journal-chapters'],['成就奖励','achievements'],['冒险图鉴','codex']].map(([n,id])=>v5Route(n,id,id==='achievements'?'active':'')).join('')}</div><div class="v5-workspace-head"><h2>冒险中的收获</h2><span>共 ${LIVE_ACHIEVEMENTS.length} 项 · 目标与奖励来自配置</span></div>${LIVE_ACHIEVEMENTS.slice(0,4).map((a,j)=>v5Row(a.Title,a.Description+' · '+(j===0?a.Required:0)+' / '+a.Required+' · 奖励 '+a.Gold+' 金币 / '+a.Experience+' 经验 / 锻造碎片 ×'+a.ForgeShards,'glyph-award',v5Route(j===0?'领取奖励':'尚未达成','achievements',j===0?'primary small-button':'soft small-button disabled'))).join('')}<p class="v5-inline-note">完成进度为设计示例；正式页面显示当前角色记录。</p>`,'steward','journal');
};
const v5BaseSettings=settings;
settings=function(index=0){
 let html=v5BaseSettings(index);
 if(index===3)html=html.replace('<p class="tiny muted" style="margin-top:28px">',settingRow('伤害数字','控制战斗伤害飘字显示',toggle(),'开启')+settingRow('反馈强度','控制受击、格挡与命中视觉反馈',slider(70),'70%')+'<p class="tiny muted" style="margin-top:28px">');
 html=html.replace('恢复默认后可重新调整；以应用后的设置为准。','应用后保存；返回撤销未应用修改；恢复默认仅预览。').replace('选择“应用设置”后保存。','应用后保存；<br>返回撤销未应用修改。');
 if(index===2)html=html.replace('点击键位开始重新绑定，按 Esc 可取消。','点击键位改键；Esc 取消。冲刺与翻滚共享按键；重复键和保留键会被拒绝。').replace('data-demo="true"','data-screen="rebind-active"');
 return html;
};
RENDERERS['rebind-active']=()=>settings(2)+v5Dialog('按下新的按键','<h2>正在修改：向前移动</h2><p>当前绑定 W<br>按 Esc 取消并保留原绑定；重复键和保留键会被拒绝。<br>冲刺与翻滚共享按键。完成后选择“应用设置”保存。</p>',v5Route('取消 · 保留 W','settings-controls','soft'));
SCREENS.find(s=>s.id==='settings-game').description='全局难度、HUD、小地图、屏幕震动、伤害数字、反馈强度六项；应用保存、关闭撤销未应用修改。';
showScreen(params.get('screen')||location.hash.slice(1)||'inventory');
