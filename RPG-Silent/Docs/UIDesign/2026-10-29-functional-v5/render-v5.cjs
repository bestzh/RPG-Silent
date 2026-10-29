const fs=require('fs'),path=require('path');
const {pathToFileURL}=require('url');
const {chromium}=require('C:/Users/85065/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=__dirname,only=process.argv.find(x=>x.startsWith('--screens='))?.split('=')[1].split(',');
const esc=s=>String(s).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 const page=await browser.newPage({viewport:{width:1920,height:1080}}),errors=[],audit=[];
 page.on('pageerror',e=>{errors.push(String(e));console.error(String(e));});
 fs.mkdirSync(path.join(root,'screens'),{recursive:true});
 try{
  await page.goto(pathToFileURL(path.join(root,'index.html')).href+'?export=1&screen=inventory');await page.waitForFunction(()=>window.DESIGN_READY);
  const screens=await page.evaluate(()=>window.DESIGN_SCREENS),all=[{id:'style-guide',title:'功能与交互规范',number:0,file:'00-style-guide.png'},...screens];
  for(const s of all.filter(s=>!only||only.includes(s.id))){
   await page.evaluate(id=>window.showScreen(id),s.id);await page.waitForFunction(()=>window.DESIGN_READY);
   const result=await page.evaluate(()=>{
    const c=document.getElementById('canvas').getBoundingClientRect();
    const es=[...document.querySelectorAll('#canvas h1,#canvas h2,#canvas h3,#canvas p,#canvas button,#canvas .v5-total-stats,#canvas .v5-equipment-rows')];
    const outside=es.filter(e=>{const r=e.getBoundingClientRect();return r.width&&r.height&&(r.left<c.left-1||r.right>c.right+1||r.top<c.top-1||r.bottom>c.bottom+1);}).map(e=>({class:e.className,text:e.textContent.slice(0,60)}));
    const panels=[...document.querySelectorAll('#canvas .frame h2,#canvas .frame h3,#canvas .frame p,#canvas .frame .button')].filter(e=>{const r=e.getBoundingClientRect(),p=e.closest('.frame').getBoundingClientRect();return r.width&&r.height&&(r.left<p.left+3||r.right>p.right-3||r.top<p.top+3||r.bottom>p.bottom-5);}).map(e=>({text:e.textContent.slice(0,60),parent:e.closest('.frame').className}));
    const images=[...document.querySelectorAll('#canvas img')],w=document.querySelector('.v5-workspace'),a=document.querySelector('.v5-workspace>.v5-actions-bottom');
    const obscured=a?[...w.querySelectorAll('p,h2,h3,.v5-fact-grid,.v5-route-modes')].filter(e=>!e.closest('.v5-actions-bottom')&&e.getBoundingClientRect().bottom>a.getBoundingClientRect().top-8).map(e=>e.textContent.slice(0,60)):[];
    return {size:[c.width,c.height],broken_images:images.filter(i=>!i.complete||!i.naturalWidth).map(i=>i.src),outside_canvas:outside,panel_overflow:panels,action_overlap:obscured,image_count:images.length};
   });
   await page.locator('#canvas').screenshot({path:path.join(root,'screens',s.file),animations:'disabled'});audit.push({id:s.id,...result});
   console.log(s.id+': missing '+result.broken_images.length+', outside '+result.outside_canvas.length+', panels '+result.panel_overflow.length+', overlap '+result.action_overlap.length);
  }
  const report={rendered:audit.length,game_page_state_count:screens.filter(s=>s.id!=='skills-visual-guide').length,reference_count:2,errors,audit,passed:!errors.length&&audit.every(x=>!x.broken_images.length&&!x.outside_canvas.length&&!x.panel_overflow.length&&!x.action_overlap.length)};
  fs.writeFileSync(path.join(root,only?'qa-smoke.json':'qa-report.json'),JSON.stringify(report,null,2));
  if(!only){
   fs.writeFileSync(path.join(root,'screen-manifest.json'),JSON.stringify({version:'Functional V5',artboard:[1920,1080],game_page_state_count:report.game_page_state_count,screens},null,2));
   fs.mkdirSync(path.join(root,'boards'),{recursive:true});
   const game=screens.filter(s=>s.id!=='skills-visual-guide'),hero=['inventory','inventory-tryon','inventory-weapon','warehouse-deposit','specialization','portal-expedition'].map(id=>screens.find(s=>s.id===id));
   const groups=[{name:'hero-overview',title:'功能匹配 V5 · 六个关键页面',screens:hero,cols:3,width:3600},...Array.from({length:Math.ceil(game.length/8)},(_,i)=>({name:'board-'+(i+1),title:'页面与关键状态 '+(i*8+1)+'–'+Math.min((i+1)*8,game.length),screens:game.slice(i*8,i*8+8),cols:4,width:3840})),{name:'all-pages-overview',title:'当前功能 · '+game.length+' 个页面与关键状态',screens:game,cols:6,width:3840}];
   for(const g of groups){
    const html=`<!doctype html><html lang="zh-CN"><meta charset="utf-8"><style>*{box-sizing:border-box}body{margin:0;background:#162d34;color:#e0e3d0;font-family:'Microsoft YaHei',sans-serif}.board{width:${g.width}px;padding:55px 60px 36px}header{border-bottom:1px solid #a9ba9750;margin-bottom:32px;padding-bottom:25px;display:flex;justify-content:space-between;align-items:end}h1{font-weight:400;font-size:40px;letter-spacing:4px;margin:0}header p{font-size:19px;color:#a5b9ac;margin:17px 0 0}header>span{font:23px Georgia;color:#d1ba83;letter-spacing:4px}.grid{display:grid;grid-template-columns:repeat(${g.cols},1fr);gap:28px 24px}.thumb img{width:100%;aspect-ratio:16/9;display:block}.thumb>div{font-size:21px;margin-top:12px;color:#dce0c9}.thumb small{font:16px Georgia;color:#c4ad79;margin-right:15px}footer{margin-top:30px;border-top:1px solid #a4b4913f;padding-top:18px;font-size:15px;color:#8ea99e;display:flex;justify-content:space-between}</style><div class="board"><header><div><h1>SILENT / ${esc(g.title)}</h1><p>已穿戴与试穿分开 · 入口直接点选 · 关键结果明确反馈</p></div><span>FUNCTIONAL / V5</span></header><div class="grid">${g.screens.map(s=>`<div class="thumb"><img src="../screens/${s.file}"><div><small>${String(s.number).padStart(2,'0')}</small>${esc(s.title)}</div></div>`).join('')}</div><footer><span>当前第一方功能对应设计 · 未修改游戏代码、Assets 或真实存档</span><span>示例数据用于设计评审；场景含 Synty 示例图片</span></footer></div></html>`;
    const file=path.join(root,'boards',g.name+'.html');fs.writeFileSync(file,html);await page.goto(pathToFileURL(file).href);await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));await page.setViewportSize({width:g.width,height:2400});await page.locator('.board').screenshot({path:path.join(root,'boards',g.name+'.png')});console.log('board '+g.name);
   }
  }
  if(!report.passed)process.exitCode=2;
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
