/* Export design images and check actual browser layout and image decoding. */
const fs=require('fs'),path=require('path'),{pathToFileURL}=require('url');
const {chromium}=require('C:/Users/85065/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=__dirname;
const selected=process.argv.find(x=>x.startsWith('--screens='))?.split('=')[1]?.split(',');
const boardsOnly=process.argv.includes('--boards-only');
const esc=s=>String(s).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
async function render(){
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 const audit=[],errors=[];
 try{
  const page=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:1});
  page.on('pageerror',e=>errors.push(String(e)));
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  await page.goto(pathToFileURL(path.join(root,'index.html')).href+'?export=1');
  await page.waitForFunction(()=>window.DESIGN_READY===true);
  const screens=await page.evaluate(()=>window.DESIGN_SCREENS);
  fs.mkdirSync(path.join(root,'screens'),{recursive:true});
  if(!boardsOnly)for(const screen of [{id:'style-guide',file:'00-style-guide.png'},...screens]){
   if(selected&&!selected.includes(screen.id))continue;
   await page.evaluate(id=>window.showScreen(id),screen.id);
   await page.waitForFunction(()=>window.DESIGN_READY===true);
   const result=await page.evaluate(()=>{
    const stage=document.getElementById('canvas').getBoundingClientRect();
    const broken=[...document.querySelectorAll('#canvas img')].filter(i=>!i.complete||!i.naturalWidth).map(i=>i.getAttribute('src'));
    const outside=[...document.querySelectorAll('#canvas h1,#canvas h2,#canvas h3,#canvas p,#canvas .button,#canvas .tab,#canvas .tag,#canvas .cell-label,#canvas .setting-label,#canvas .section-label')].filter(e=>{const r=e.getBoundingClientRect();return r.width>0&&r.height>0&&(r.left<stage.left-1||r.top<stage.top-1||r.right>stage.right+1||r.bottom>stage.bottom+1);}).map(e=>({text:e.textContent.slice(0,55),rect:JSON.parse(JSON.stringify(e.getBoundingClientRect()))}));
    const panelOverflow=[...document.querySelectorAll('#canvas .frame h2,#canvas .frame h3,#canvas .frame p,#canvas .frame .button,#canvas .frame .tag')].filter(e=>{const r=e.getBoundingClientRect(),p=e.closest('.frame').getBoundingClientRect();return r.width>0&&r.height>0&&(r.left<p.left+4||r.right>p.right-4||r.top<p.top+4||r.bottom>p.bottom-6);}).map(e=>({text:e.textContent.slice(0,55),frame:e.closest('.frame').className}));
    const used=[...new Set([...document.querySelectorAll('#canvas img')].map(i=>i.getAttribute('src')))];
    const source=new Set(used.filter(u=>u.includes('skill-')));
    return {size:[stage.width,stage.height],broken_images:broken,outside_canvas:outside,panel_overflow:panelOverflow,image_count:document.querySelectorAll('#canvas img').length,used_images:used,skill_icon_count:source.size};
   });
   await page.locator('#canvas').screenshot({path:path.join(root,'screens',screen.file),animations:'disabled'});
   audit.push({screen:screen.id,...result});
   console.log(`${screen.id}: ${result.broken_images.length} missing, ${result.outside_canvas.length} outside, ${result.panel_overflow.length} panel overflow`);
  }
  fs.writeFileSync(path.join(root,'screen-manifest.json'),JSON.stringify({date:'2026-10-29',version:'Windworn V3',artboard:[1920,1080],game_page_state_count:40,reference_sheet_count:2,screens},null,2));
  if(!selected){
   const game=screens.filter(s=>s.id!=='skills-visual-guide');
   const groups=[{file:'hero-overview',title:'SILENT / 全界面设计 · WINDWORN V3',subtitle:'苔绿皮革 × 手札纸页 × 旧黄铜 / 个性化视觉提案',screens:['start','creation','hud-dungeon','inventory','journal','mentor'].map(id=>game.find(s=>s.id===id)),cols:3,width:3240},...Array.from({length:5},(_,i)=>({file:'board-'+(i+1),title:'SILENT / '+['开始、角色与旅途','行囊、手札与收藏','职业与主城服务','锻造、远行与结算','偏好、加载与反馈'][i],subtitle:`页面与状态 ${i*8+1}–${(i+1)*8} / 40　·　单张 1920 × 1080`,screens:game.slice(i*8,i*8+8),cols:4,width:3840})),{file:'all-pages-overview',title:'SILENT / 40 个页面与关键状态',subtitle:'风痕手札 / 40 个页面与状态 / 保留现有资源与功能 / 2026.10.29',screens:game,cols:5,width:3840},{file:'reference-overview',title:'SILENT / 视觉与技能图标规范',subtitle:'物件感、职业差异与 21 个技能的星图式编排',screens:[{number:0,title:'视觉规范与组件',file:'00-style-guide.png'},screens.find(s=>s.id==='skills-visual-guide')],cols:2,width:3240}];
   fs.mkdirSync(path.join(root,'boards'),{recursive:true});
   for(const group of groups){
    const html=`<!doctype html><html lang="zh-CN"><meta charset="utf-8"><style>*{box-sizing:border-box}body{margin:0;background:#172d25;color:#e8dfc6;font-family:'Microsoft YaHei',sans-serif}.board{padding:58px 64px 42px;width:${group.width}px}header{display:flex;justify-content:space-between;align-items:flex-end;border-bottom:1px solid #ad955b55;padding-bottom:29px;margin-bottom:33px}h1{font-size:44px;letter-spacing:4px;font-weight:400;margin:0}header p{font-size:20px;color:#afba9d;margin:18px 0 0}.edition{color:#ceb581;font:24px Georgia;letter-spacing:3px}.grid{display:grid;grid-template-columns:repeat(${group.cols},1fr);gap:30px 26px}.thumb img{width:100%;aspect-ratio:16/9;display:block;border:1px solid #ad955b66}.caption{display:flex;gap:14px;padding-top:12px;font-size:20px;color:#d3d9d8}.caption small{font:17px Georgia;color:#cdb07a}.footer{display:flex;justify-content:space-between;border-top:1px solid #8b987b44;padding-top:20px;margin-top:34px;font-size:16px;color:#a1b394}</style><div class="board"><header><div><h1>${esc(group.title)}</h1><p>${esc(group.subtitle)}</p></div><div class="edition">UI DESIGN / 03</div></header><div class="grid">${group.screens.map(s=>`<div class="thumb"><img src="../screens/${s.file}"><div class="caption"><small>${String(s.number).padStart(2,'0')}</small>${esc(s.title)}</div></div>`).join('')}</div><div class="footer"><span>SYNTY INTERFACE × PONETI × 项目角色与场景资源</span><span>设计预览 · 尚未修改游戏界面 · 数值为排版示例</span></div></div></html>`;
    const file=path.join(root,'boards',group.file+'.html');fs.writeFileSync(file,html);
    await page.setViewportSize({width:group.width,height:2200});await page.goto(pathToFileURL(file).href);
    await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));
    await page.locator('.board').screenshot({path:path.join(root,'boards',group.file+'.png'),animations:'disabled'});
    console.log('board '+group.file);
   }
  }
  const report={generated_at:new Date().toISOString(),artboard:[1920,1080],game_page_state_count:40,reference_sheet_count:2,rendered:audit.length,errors,audit};
  fs.writeFileSync(path.join(root,selected?'qa-smoke.json':'qa-report.json'),JSON.stringify(report,null,2));
  if(errors.length||audit.some(a=>a.broken_images.length||a.outside_canvas.length||a.panel_overflow.length))process.exitCode=2;
 }finally{await browser.close();}
}
render().catch(e=>{console.error(e);process.exit(1)});
