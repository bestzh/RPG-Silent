/* Functional V5: standalone design prototype, never connected to Unity or save files. */
const V5_SLOT_NAMES=['头部','护甲','靴子','戒指','项链'];
const V5_RARITY=['普通','精良','稀有','史诗','传说','神话'];
const V5_COLORS=['#d4ddd6','#8abf9f','#91b6db','#c3a0d8','#d7bb77','#d18e86'];
const v5Item=id=>LIVE_ITEMS.find(x=>x.Id===id);
const v5StatKeys=['Health','Energy','AttackPercent','DefensePercent'];
const v5StatNames=['生命','能量','伤害','减伤'];
let vx;
const invModes=new Set(['inventory','inventory-tryon','inventory-weapon','inventory-consumable','inventory-overflow','inventory-search','inventory-outfit','inventory-batch','inventory-bulk-sell','inventory-bulk-discard','inventory-bulk-review','inventory-low-level','inventory-save-failed','inventory-empty-slot','loadouts','discard','equipment-effects']);
function v5ResetInventory(mode='inventory'){
 const make=(key,id,rank=0,count=1,extra={})=>({key,id,rank,count,...extra});
 const heal=LIVE_ITEMS.find(x=>x.Kind===3&&x.UseEffect===1).Id,energy=LIVE_ITEMS.find(x=>x.Kind===3&&x.UseEffect===2).Id;
 const gem=LIVE_ITEMS.find(x=>x.IsGem&&x.Health>0).Id;
 vx={mode,level:12,filter:'全部',query:'',sort:0,descending:true,selected:'c',trying:false,bulk:null,chosen:[],outfit:false,message:'点选背包装备开始试穿；确认后才会正式穿戴。',
  bag:[make('h','crystal-head',1),make('c','crystal-chest',2),make('f','crystal-feet',1),make('r','crystal-ring'),make('w1','training-sword',2),make('w2','fine-greatsword',1),make('new-chest','warden-chest',1,1,{fresh:true}),make('new-weapon','rare-sword',0,1,{fresh:true}),make('amulet','warden-amulet'),make('forest-head','forest-head',0,1,{locked:true}),make('heal',heal,0,8),make('energy',energy,0,6),make('forge','forge-shard',0,24),make('crystals','crystal-shard',0,14),make('gem',gem,0,2),make('forest-feet','forest-feet'),make('warden-ring','warden-ring'),make('warden-head','warden-head')],
  armor:['h','c','f','r',null],weapons:['w1','w2'],active:0,potionKeys:['heal','energy','heal'],modal:null};
 if(['inventory-tryon','inventory-outfit','inventory-low-level','inventory-save-failed'].includes(mode)){vx.selected='new-chest';vx.trying=true;}
 if(mode==='inventory-outfit'){vx.outfit=true;vx.message='整套试穿中；左侧清单仍为实际穿戴，确认整套后才应用。';}
 if(mode==='inventory-weapon'){vx.selected='new-weapon';vx.trying=true;}
 if(mode==='inventory-consumable'){vx.selected='heal';vx.filter='药品';}
 if(mode==='inventory-overflow'){vx.filter='待领取';vx.selected='new-chest';vx.trying=false;vx.message='待领取物品不在背包中，不能试穿或直接穿戴。';}
 if(mode==='inventory-search'){vx.query='晶辉';vx.sort=1;}
 if(mode==='inventory-low-level'){vx.level=4;vx.message='可以试穿，达到装备等级后才能确认穿戴。';}
 if(mode==='inventory-save-failed'){vx.message='保存失败：本次穿戴未生效，实际装备与属性保持原状。请重试或取消试穿。';}
 if(mode==='inventory-empty-slot'){vx.armor=[null,null,null,null,null];vx.selected=null;}
 if(['inventory-batch','inventory-bulk-sell','inventory-bulk-discard','inventory-bulk-review'].includes(mode)){
  vx.bulk=mode==='inventory-batch'?'dismantle':mode==='inventory-bulk-discard'?'discard':'sell';vx.trying=false;vx.selected='new-chest';vx.chosen=['new-chest','forest-feet'];
  if(mode==='inventory-bulk-review')vx.modal='bulk';
 }
 if(mode==='loadouts')vx.modal='loadouts';
 if(mode==='discard'){vx.selected='crystals';vx.modal='discard';}
 if(mode==='equipment-effects')vx.modal='effects';
}
const v5Entry=key=>vx.bag.find(e=>e.key===key);
const v5Worn=key=>vx.armor.includes(key)||vx.weapons.includes(key);
const v5FullName=e=>e?v5Item(e.id).DisplayName+(e.rank?' +'+e.rank:''):'未穿戴';
function v5ItemImage(id){
 const i=v5Item(id);if(!i)return 'item-ore';
 if(i.Kind===0)return ['gear-head',id.startsWith('warden')?'gear-candidate':'gear-chest','gear-feet','gear-ring','gear-amulet'][i.Slot];
 if(i.Kind===1)return id.includes('greatsword')?'gear-greatsword':id.includes('staff')?'item-book':id.includes('bow')?'item-sword-rare':'gear-sword';
 if(i.Kind===3)return i.UseEffect===1?'item-potion-red':i.UseEffect===2?'item-potion-blue':'item-potion-green';
 return i.IsGem?'item-gem':id==='crystal-shard'?'item-crystal':'item-ore';
}
const v5Img=(file,cls='v5-art')=>`<img class="${cls}" src="assets/${file}.png" alt="">`;
const v5GearArt=e=>e?v5Img(v5ItemImage(e.id)):'';
const v5Button=(label,action='',kind='',disabled=false)=>`<button class="button ${kind}" ${action?`data-v5="${action}"`:''} ${disabled?'disabled':''}>${label}</button>`;
const v5Route=(label,route,kind='soft small-button')=>btn(label,route,kind);
function v5Header(name,currency=true,back='hud-town'){
 return `<div class="screen-header"><div class="v3-wordmark-small">SILENT <span>/ ${name}</span></div><div class="row">${currency?'<span class="currency">2,450 金币　<span class="muted small">剑士 · Lv.'+(vx&&invModes.has(current)?vx.level:12)+'</span></span>':''}${btn('Esc 返回',back,'soft small-button')}</div></div>`;
}
function v5Heading(name,sub=''){return `<div class="page-heading"><h1>${name}</h1>${sub?`<div class="subtitle">${sub}</div>`:''}</div>`;}
v3Header=v5Header;v3Heading=v5Heading;
function v5Totals(armor=vx.armor){
 const result=[0,0,0,0],sets={};
 for(const key of armor){const e=v5Entry(key);if(!e)continue;const i=v5Item(e.id);
  v5StatKeys.forEach((stat,n)=>result[n]+=(i[stat]||0)+Math.ceil((i[stat]||0)*e.rank/10)+(e.affixes?.[stat]||0)+(e.gemId?(v5Item(e.gemId)?.[stat]||0):0));
  if(i.SetGuid){const set=i.Id.split('-')[0];sets[set]=(sets[set]||0)+1;}
 }
 for(const [id,n] of Object.entries(sets)){const s=LIVE_SETS.find(x=>x.Id===id);if(!s)continue;if(n>=2)result[0]+=s.TwoPieceHealth;if(n>=4)result[1]+=s.FourPieceEnergy;if(n>=5)result[2]+=s.FivePieceAttackPercent;}
 result[3]=Math.min(50,result[3]);return result;
}
function v5EquipList(){
 const target=v5Item(v5Entry(vx.selected)?.id),sum=v5Totals(),crystal=vx.armor.filter(k=>v5Entry(k)?.id.startsWith('crystal-')).length;
 return `<section class="v5-equipped"><div class="v5-section-head"><h2>当前穿戴</h2><span class="v5-committed">● 已生效</span></div><div class="v5-equipment-rows">${vx.armor.map((key,i)=>{const e=v5Entry(key);return `<button class="v5-equip-row ${e?'':'empty'} ${vx.trying&&target?.Kind===0&&target.Slot===i?'replacement':''}" data-v5="select:${key||'empty-'+i}" data-slot="${i}"><div class="v5-slot-icon">${e?v5GearArt(e):icon(['invhelmet','invchest','invboots','invring','gem'][i])}</div><div><small>${V5_SLOT_NAMES[i]}${vx.trying&&target?.Kind===0&&target.Slot===i?' · 即将替换':''}</small><strong style="color:${e?V5_COLORS[v5Item(e.id).Rarity]:'#8b9e9a'}">${e?v5FullName(e):'未穿戴'}</strong></div></button>`;}).join('')}<div class="v5-weapons-label">两把武器</div>${vx.weapons.map((key,i)=>{const e=v5Entry(key);return `<button class="v5-equip-row weapon" data-v5="select:${key}"><div class="v5-slot-icon">${v5GearArt(e)}</div><div><small>武器槽 ${i+1} <em class="${i===vx.active?'gold':'muted'}">${i===vx.active?'当前使用':'备用'}</em></small><strong>${v5FullName(e)}</strong></div></button>`;}).join('')}</div><div class="v5-preview-status ${vx.trying?'trial':''}">${vx.trying?'试穿中 · 未装备':'当前外观'}</div><div class="v5-mini-model">${v5PreviewModel()}<span>拖动旋转 · 滚轮缩放</span></div><div class="v5-total-stats"><span>当前装备加成</span><div>${v5StatNames.map((n,i)=>`<label>${n}<b>${sum[i]}${i>1?'%':''}</b></label>`).join('')}</div><p>晶辉套装 ${crystal}/5 · ${crystal>=2?'2件生命 +60':'2件效果未激活'} · ${crystal>=4?'4件能量 +15':'4件效果未激活'}</p></div></section>`;
}
function v5VisibleItems(){
 let list=vx.filter==='待领取'?vx.bag.filter(e=>['new-chest','new-weapon','warden-ring'].includes(e.key)):vx.bag.slice();
 list=list.filter(e=>{const i=v5Item(e.id);return (vx.filter==='全部'||vx.filter==='待领取'||vx.filter==='防具'&&i.Kind===0||vx.filter==='武器'&&i.Kind===1||vx.filter==='材料'&&i.Kind===2&&!i.IsGem||vx.filter==='药品'&&i.Kind===3||vx.filter==='宝石'&&i.IsGem)&&i.DisplayName.toLowerCase().includes(vx.query.trim().toLowerCase());});
 const stat=[e=>v5Item(e.id).Rarity,e=>v5Item(e.id).RequiredLevel,e=>v5Item(e.id).Kind,e=>e.fresh?1:0][vx.sort];
 return list.sort((a,b)=>(stat(b)-stat(a))*(vx.descending?1:-1));
}
function v5Protected(e,action=vx.bulk){if(v5Worn(e.key))return '已装备';if(e.locked)return '已锁定';if(e.gemId&&action!=='discard')return '含宝石';if(action==='dismantle'&&v5Item(e.id).Kind!==0)return '仅分解防具';return '';}
function v5Bag(){const visible=v5VisibleItems(),overflow=vx.filter==='待领取';return `<section class="v5-bag"><div class="v5-section-head"><h2>${overflow?'待领取':'背包物品'}</h2><span>${overflow?'3 项待领取':'18 / 24 格'}</span></div><div class="v5-categories">${['全部','防具','武器','材料','待领取','药品','宝石'].map(t=>`<button data-v5="filter:${t}" class="${vx.filter===t?'active':''}">${t}</button>`).join('')}</div><div class="v5-search-row"><input id="v5-search" type="text" placeholder="搜索物品名称" value="${vx.query}" aria-label="搜索物品名称"><button data-v5="clear-search">清除</button><div>${['品质','等级','类型','获得'].map((t,i)=>`<button data-v5="sort:${i}" class="${vx.sort===i?'active':''}">${t}${vx.sort===i?(vx.descending?'↓':'↑'):''}</button>`).join('')}</div></div>${vx.bulk?`<div class="v5-bulk-banner">${{sell:'批量出售',discard:'批量丢弃',dismantle:'批量分解'}[vx.bulk]} · 已选 ${vx.chosen.length} 项 <button data-v5="end-bulk">退出多选</button></div>`:''}<div class="v5-grid">${Array.from({length:24},(_,j)=>{const e=visible[j];if(!e)return '<div class="v5-cell blank"></div>';const i=v5Item(e.id),blocked=vx.bulk&&v5Protected(e);return `<button class="v5-cell ${vx.selected===e.key?'selected':''} ${vx.chosen.includes(e.key)?'checked':''} ${blocked?'protected':''}" data-v5="select:${e.key}" data-key="${e.key}" style="--rarity:${V5_COLORS[i.Rarity]}"><small>${vx.bulk?(blocked||'可选择'):v5Worn(e.key)?'已装备':e.locked?'已锁定':e.fresh?'新':V5_RARITY[i.Rarity]}</small>${v5GearArt(e)}<span>${i.DisplayName}${e.rank?' +'+e.rank:''}</span><em>${e.count>1?'×'+e.count:'Lv.'+i.RequiredLevel}</em>${vx.chosen.includes(e.key)?'<b class="v5-check">✓</b>':''}</button>`;}).join('')}</div><div class="v5-grid-footer"><span>第 1 / 1 页 · ${visible.length} 项</span><span>${key('←')}　${key('→')}</span>${overflow?v5Button('领取可放入的物品','collect','primary small-button'):''}</div><div class="v5-bag-tools">${vx.bulk?v5Button('核对清单 · '+vx.chosen.length+' 项','review-bulk','primary',!vx.chosen.length):v5Button(vx.outfit?'取消整套':'多件试穿','outfit','soft small-button')+v5Route('配装方案','loadouts')+v5Button('批量分解','bulk:dismantle','soft small-button')+v5Button('批量出售','bulk:sell','soft small-button')+v5Button('批量丢弃','bulk:discard','soft small-button')}</div></section>`;}
function v5Details(){
 const e=v5Entry(vx.selected);if(!e)return `<aside class="v5-details"><h2>物品详情</h2><p class="muted">点击背包物品查看详情。<br>左侧清单一直显示实际穿戴。</p></aside>`;
 const i=v5Item(e.id),worn=v5Worn(e.key),gear=i.Kind===0,weapon=i.Kind===1,potion=i.Kind===3,overflow=vx.filter==='待领取';
 const previous=gear?v5Entry(vx.armor[i.Slot]):weapon?v5Entry(vx.weapons[vx.active]):null;
 let nextArmor=vx.armor.slice();if(gear)nextArmor[i.Slot]=e.key;
 const before=v5Totals(),after=gear?v5Totals(nextArmor):before;
 const nextCrystal=nextArmor.filter(k=>v5Entry(k)?.id.startsWith('crystal-')).length;
 const inactive=vx.bulk||overflow,canWear=vx.trying&&vx.level>=i.RequiredLevel&&!inactive;
 return `<aside class="v5-details"><div class="v5-detail-heading"><h2>物品详情</h2>${(gear||weapon)?v5Button(e.locked?'解除锁定':'锁定','lock','soft small-button',overflow||!!vx.bulk):''}</div><div class="v5-selected-item">${v5GearArt(e)}<div><h3 style="color:${V5_COLORS[i.Rarity]}">${v5FullName(e)}</h3><p>${V5_RARITY[i.Rarity]} · ${i.Kind===0?V5_SLOT_NAMES[i.Slot]:i.Kind===1?'武器':potion?'药品':i.IsGem?'宝石':'材料'} · 需要 Lv.${i.RequiredLevel}</p><span class="${worn?'mint':vx.trying?'gold':'muted'}">${overflow?'待领取 · 尚未入包':worn?'已装备 · 当前生效':vx.trying?'试穿预览 · 未装备':'已持有 ×'+e.count}</span></div></div><p class="v5-item-copy">${i.Description}</p>${gear?`<div class="v5-replacement"><span>${worn?'正在查看当前装备':'将替换 '+V5_SLOT_NAMES[i.Slot]}</span><b>${previous?v5FullName(previous):'该部位未穿戴'}</b></div>${v5StatsTable(e,worn,before,after)}<p class="v5-set-note">${v5SetSummary(nextCrystal,worn)}</p><p class="tiny muted">宝石：${e.gemId?v5Item(e.gemId).DisplayName:'空槽'} · 词条：${e.affixes?'查看详细属性':'无额外词条'}</p>`:weapon?`<div class="v5-replacement"><span>当前使用武器 · 槽 ${vx.active+1}</span><b>${v5FullName(previous)}</b></div><div class="v5-weapon-choices">${vx.weapons.map((k,j)=>`<div><small>武器槽 ${j+1} · ${j===vx.active?'当前使用':'备用'}</small><b>${v5FullName(v5Entry(k))}</b></div>`).join('')}</div><p class="tiny muted">普攻强化加成 +${e.rank*10}% · 每件武器独立强化<br>关闭背包后，沿用当前武器的拔收与动作流程。</p>`:potion?`<div class="v5-potion-info"><b>持有 ${e.count} 瓶</b><p>全部药品共享 8 秒冷却</p><p class="muted">关闭背包后使用快捷键，<br>或点击 HUD 药品槽。</p></div><div class="v5-quick-assign">${['Q','R','F'].map((k,j)=>v5Button('设为快捷 '+k,'potion:'+j,'soft small-button',!!inactive)).join('')}</div>`:`<div class="v5-potion-info"><b>持有 ${e.count} 件</b><p>${i.IsGem?'到主城铁匠处选择装备镶嵌。':'可用于锻造、强化等成长服务。'}</p></div>`}<div class="v5-detail-controls">${overflow?`<p class="gold">先领取到背包，再试穿或使用。</p>`:vx.bulk?'<p class="muted">多选期间只处理清单，退出多选可恢复穿戴与快捷栏操作。</p>':(gear||weapon)?`${vx.level<i.RequiredLevel?`<p class="red">需要 Lv.${i.RequiredLevel}，当前 Lv.${vx.level}；仍可预览外观。</p>`:''}<div class="v5-main-actions">${worn?v5Button('已装备','','primary',true):v5Button(weapon?'确认装入武器槽 1':vx.outfit?'确认整套':'确认穿戴','wear:0','primary',!canWear)}${weapon?v5Button('装入武器槽 2','wear:1','soft',!canWear):vx.trying?v5Button('取消试穿','cancel-try','soft'):v5Button('卸下','remove','soft',!worn)}</div>`:''}${!overflow&&!vx.bulk?`<div class="v5-danger-actions">${v5Button('出售','sell','soft small-button',worn||e.locked||!!e.gemId)}${v5Button('分解','dismantle','soft small-button',worn||e.locked||!!e.gemId||!gear)}${v5Button('丢弃','discard','soft small-button',worn||e.locked)}</div><p class="tiny muted">${worn?'已装备物品受保护，需先卸下。':e.locked?'已锁定；解除锁定后才能处理。':e.gemId?'出售或分解前，需到铁匠处拆下宝石。':'出售与分解需在主城进行。'}</p>`:''}</div></aside>`;
}
function v5Dialog(title,body,controls,cls=''){return `<div class="v5-modal-shade"><div class="v5-modal ${cls}"><div class="v5-modal-heading"><span class="section-label">请核对后确认</span><h1>${title}</h1></div><div class="v5-modal-body">${body}</div><div class="v5-modal-actions">${controls}</div></div></div>`;}
function v5InventoryModal(){
 if(!vx.modal)return '';
 if(vx.modal==='loadouts')return v5Dialog('配装方案',`<p>保存实际穿戴的五件防具、两把武器和当前武器槽。</p><div class="v5-preset-list">${['方案 1 · 当前装备','方案 2 · 已保存','方案 3 · 空方案'].map((t,j)=>`<div><h2>${t}</h2><p>${j<2?'头部、护甲、靴子、戒指、项链 · 两把武器':'可以保存当前实际穿戴'}</p><div>${v5Button('试穿','preset-preview','soft small-button',j===2)}${v5Button('应用','preset-apply','primary small-button',j===2)}${v5Button(j===2?'保存当前':'覆盖当前','preset-save','soft small-button')}${v5Button('清空','preset-clear','soft small-button',j===2)}</div></div>`).join('')}</div><p class="tiny muted">涉及武器的整套调整在主城完成；缺失装备时整套不更换。</p>`,v5Button('返回行囊','close-modal','soft'));
 if(vx.modal==='effects')return v5Dialog('装备特殊效果',`<h2>传说单手剑 · 精准回能 I</h2><p>具体触发条件、效果与冷却按所选装备显示。</p><div class="v5-invoice"><div><span>生效来源</span><b>当前武器槽 1</b></div><div><span>同类效果</span><b>仅取最高等级</b></div><div><span>状态</span><b>切换至其他武器后停止生效</b></div></div><p class="muted">这里只查看规则，不消耗物品。</p>`,v5Button('返回物品详情','close-modal','primary'));
 if(vx.modal==='bulk')return v5Dialog('核对'+({sell:'出售',discard:'丢弃',dismantle:'分解'}[vx.bulk]||'物品')+'清单',`<p>${vx.bulk==='discard'?'物品会放在脚边，可手动拾回；离开场景后消失。':'已穿戴、两武器槽、已锁定'+(vx.bulk==='sell'?'或镶嵌宝石':'及镶嵌宝石')+'的装备受保护。'}</p><div class="v5-review-list">${vx.chosen.map(k=>{const e=v5Entry(k);return `<div>${v5GearArt(e)}<span>${v5FullName(e)}<small>${V5_RARITY[v5Item(e.id).Rarity]}</small></span><button data-v5="count-down:${k}">−</button><b>×${e.count}</b><button data-v5="count-up:${k}">＋</button></div>`;}).join('')}</div><div class="v5-invoice"><div><span>共 ${vx.chosen.length} 项 · 第 1 / 1 页</span><b>${vx.bulk==='sell'?'获得 '+vx.chosen.reduce((s,k)=>s+v5Item(v5Entry(k).id).SellPrice*v5Entry(k).count,0)+' 金币':vx.bulk==='dismantle'?'获得锻造碎片 ×'+vx.chosen.reduce((s,k)=>s+1+v5Item(v5Entry(k).id).Rarity,0):'金币与经验不变'}</b></div></div>`,v5Button('取消 · 返回选择','close-modal','soft')+v5Button('确认清单','commit-bulk','danger'));
 if(vx.modal==='discard'){const e=v5Entry(vx.selected);return v5Dialog('丢弃物品',`<div class="v5-modal-item">${v5GearArt(e)}<h2>${v5FullName(e)}</h2></div><p>丢弃后放在脚边，可手动拾回。<br>离开场景或退出游戏后消失。</p><div class="v5-quantity"><button data-v5="discard-less">−</button><input id="v5-quantity" type="number" min="1" max="${e.count}" value="1" aria-label="丢弃数量"><button data-v5="discard-more">＋</button><span>持有 ${e.count} 件</span></div>`,v5Button('取消','close-modal','soft')+v5Button('确认丢弃','commit-discard','danger'));}
 return v5Dialog(vx.modal==='preset-confirm'?'确认覆盖方案':'确认'+(vx.modal==='sell'?'出售':'分解'),`<h2>${v5FullName(v5Entry(vx.selected))}</h2><p>${vx.modal==='sell'?'出售后 48 小时内可向铁匠原价回购，最多保留最近 24 项。':vx.modal==='preset-confirm'?'仅覆盖方案记录，背包物品不会被删除。':'装备会消耗并获得锻造碎片，无法还原。'}</p>`,v5Button('取消','close-modal','soft')+v5Button('确认操作','commit-single','danger'));
}
function v5Inventory(){return bg('workshop','heavy')+header('行囊与装备')+heading('行囊与装备','当前穿戴始终可见；点击物品比较，确认后才会更换。')+`<div class="v5-inventory-capacity">背包 18 / 24　·　待领取 3　·　${vx.outfit?'整套试穿中':'主城'}</div><div class="v5-inventory-layout">${v5EquipList()}${v5Bag()}${v5Details()}</div><div class="v5-inventory-notice ${vx.mode==='inventory-save-failed'?'error':''}">${vx.message}</div>${foot('B 关闭行囊 · Esc 优先取消输入、确认或试穿','物品名称、等级与套装规则来自当前配置')}${v5InventoryModal()}`;}
function v5Redraw(focus=false){
 window.DESIGN_READY=false;const caret=document.getElementById('v5-search')?.selectionStart;
 canvas.innerHTML=v5Inventory();
 if(focus){const e=document.getElementById('v5-search');e.focus();e.setSelectionRange(caret??vx.query.length,caret??vx.query.length);}
 Promise.all([...canvas.querySelectorAll('img')].map(i=>i.decode().catch(()=>{}))).then(()=>window.DESIGN_READY=true);
}
function v5Select(key){
 const e=v5Entry(key);if(!e){vx.message='该部位未穿戴装备，可从背包选择对应部位。';v5Redraw();return;}
 vx.selected=key;
 if(vx.bulk){const reason=v5Protected(e);if(reason)vx.message='该物品不可选择：'+reason;else{const p=vx.chosen.indexOf(key);if(p>=0)vx.chosen.splice(p,1);else vx.chosen.push(key);vx.message='已选择 '+vx.chosen.length+' 项，核对清单后才会处理。';}}
 else {const i=v5Item(e.id);vx.trying=vx.filter!=='待领取'&&(i.Kind===1||i.Kind===0&&!vx.armor.includes(key));vx.message=vx.trying?'正在试穿 '+v5FullName(e)+'；左侧清单与当前属性尚未改变。':'正在查看 '+v5FullName(e)+'。';}
 v5Redraw();
}
function v5Handle(action){
 const [type,value]=action.split(':');const e=v5Entry(vx.selected),i=e&&v5Item(e.id);
 if(type==='select'){v5Select(value);return;}
 if(type==='filter'){vx.filter=value;vx.selected=null;vx.trying=false;vx.query='';vx.message='已切换分类；当前穿戴保持不变。';}
 if(type==='sort'){const n=Number(value);if(vx.sort===n)vx.descending=!vx.descending;else{vx.sort=n;vx.descending=true;}}
 if(type==='clear-search')vx.query='';
 if(type==='cancel-try'){vx.trying=vx.outfit=false;vx.message='已取消试穿，恢复当前实际外观。';}
 if(type==='wear'&&i&&vx.trying&&vx.level>=i.RequiredLevel&&!vx.bulk&&vx.filter!=='待领取'){
  if(i.Kind===0)vx.armor[i.Slot]=e.key;else if(i.Kind===1)vx.weapons[Number(value)]=e.key;
  vx.trying=vx.outfit=false;vx.message='设计预览：已将 '+v5FullName(e)+' 标为实际穿戴。未写入游戏或存档。';
 }
 if(type==='remove'&&i?.Kind===0&&vx.armor[i.Slot]===e.key){vx.armor[i.Slot]=null;vx.message='该部位已卸下，物品仍在背包。';}
 if(type==='lock'&&e)e.locked=!e.locked;
 if(type==='outfit'){vx.outfit=!vx.outfit;vx.trying=vx.outfit;vx.message=vx.outfit?'多件试穿中，确认整套前不改变当前穿戴。':'已取消整套试穿。';}
 if(type==='bulk'){vx.bulk=value;vx.chosen=[];vx.trying=vx.outfit=false;vx.message='选择可处理物品，再核对清单。';}
 if(type==='end-bulk'){vx.bulk=null;vx.chosen=[];vx.message='已退出多选，物品未变化。';}
 if(type==='review-bulk'&&vx.chosen.length)vx.modal='bulk';
 if(['sell','dismantle','discard'].includes(type)&&e&&!v5Protected(e,type))vx.modal=type;
 if(type==='close-modal')vx.modal=null;
 if(type==='potion'){vx.potionKeys[Number(value)]=e.key;vx.message='已设为快捷 '+['Q','R','F'][Number(value)]+'；共享冷却保持不变。';}
 if(type==='collect')vx.message='已领取可放入的物品；剩余物品继续保留在待领取。';
 if(type.startsWith('preset')){if(type==='preset-save'||type==='preset-clear')vx.modal='preset-confirm';else if(type==='preset-preview'){vx.modal=null;vx.outfit=vx.trying=true;vx.message='正在预览方案，当前穿戴未改变。';}else{vx.modal=null;vx.message='方案只在装备完整、主城且状态允许时整套应用。';}}
 if(type==='commit-bulk'){vx.modal=null;vx.message='设计预览：确认清单后显示处理结果。游戏物品未改动。';vx.chosen=[];vx.bulk=null;}
 if(type==='commit-single'||type==='commit-discard'){vx.modal=null;vx.message='设计预览：确认后显示结果，游戏物品未改动。';}
 if(type==='discard-less'||type==='discard-more'){const n=document.getElementById('v5-quantity');n.value=Math.max(1,Math.min(e.count,Number(n.value)+(type==='discard-more'?1:-1)));return;}
 v5Redraw();
}
document.addEventListener('click',event=>{const button=event.target.closest('button[data-v5]');if(!button)return;event.preventDefault();event.stopImmediatePropagation();v5Handle(button.dataset.v5);},true);
document.addEventListener('input',event=>{if(event.target.id!=='v5-search')return;vx.query=event.target.value;v5Redraw(true);});
document.addEventListener('keydown',event=>{
 if(!invModes.has(current))return;const input=event.target.matches('input');
 if(event.key==='Escape'){
  event.preventDefault();event.stopImmediatePropagation();
  if(input){event.target.blur();return;}if(vx.modal){vx.modal=null;v5Redraw();return;}if(vx.bulk){vx.bulk=null;vx.chosen=[];v5Redraw();return;}if(vx.trying||vx.outfit){vx.trying=vx.outfit=false;v5Redraw();return;}showScreen('hud-town');
 }else if(event.key.toLowerCase()==='b'&&!input){event.preventDefault();event.stopImmediatePropagation();showScreen('hud-town');}
},true);
window.V5_STATE=()=>structuredClone(vx);
