/* Deterministic browser rendering and visual-artifact checks, without a web server. */
const fs=require('fs');
const path=require('path');
const {pathToFileURL}=require('url');
const {chromium}=require('C:/Users/85065/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=__dirname;
const selected=process.argv.find(x=>x.startsWith('--screens='))?.split('=')[1]?.split(',');
const boardsOnly=process.argv.includes('--boards-only');
const errors=[];
const audit=[];
const escaped=x=>String(x).replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
async function render(){
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 try{
  const page=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:1});
  page.on('pageerror',e=>errors.push(String(e)));
  page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  await page.goto(pathToFileURL(path.join(root,'index.html')).href+'?export=1');
  await page.waitForFunction(()=>window.DESIGN_READY===true);
  const screens=await page.evaluate(()=>window.DESIGN_SCREENS);
  fs.mkdirSync(path.join(root,'screens'),{recursive:true});
  if(!boardsOnly){
   const all=[{id:'style-guide',file:'00-style-guide.png'},...screens];
   for(const screen of all){
    if(selected&&!selected.includes(screen.id))continue;
    await page.evaluate(id=>window.showScreen(id),screen.id);
    await page.waitForFunction(()=>window.DESIGN_READY===true);
    const result=await page.evaluate(()=>{
     const stage=document.getElementById('canvas').getBoundingClientRect();
     const broken=[...document.querySelectorAll('#canvas img')].filter(i=>!i.complete||!i.naturalWidth).map(i=>i.getAttribute('src'));
     const outside=[...document.querySelectorAll('#canvas h1,#canvas h2,#canvas h3,#canvas p,#canvas .button,#canvas .tab,#canvas .tag,#canvas .cell-label,#canvas .setting-label,#canvas .section-label')].filter(e=>{const r=e.getBoundingClientRect();return r.width>0&&r.height>0&&(r.left<stage.left-1||r.top<stage.top-1||r.right>stage.right+1||r.bottom>stage.bottom+1);}).map(e=>({text:e.textContent.slice(0,70),box:JSON.parse(JSON.stringify(e.getBoundingClientRect()))}));
     return {broken_images:broken,outside_canvas:outside,size:[stage.width,stage.height],image_count:document.querySelectorAll('#canvas img').length};
    });
    await page.locator('#canvas').screenshot({path:path.join(root,'screens',screen.file),animations:'disabled'});
    audit.push({screen:screen.id,...result});
    console.log(screen.id+': '+result.image_count+' images, '+result.broken_images.length+' missing, '+result.outside_canvas.length+' outside');
   }
  }
  fs.writeFileSync(path.join(root,'screen-manifest.json'),JSON.stringify({date:'2026-10-28',artboard:[1920,1080],screens},null,2));
  if(!selected){
   fs.mkdirSync(path.join(root,'boards'),{recursive:true});
   const groups=[{file:'hero-overview',title:'SILENT / 全界面重设计',subtitle:'深岩 · 暖金 · 羊皮纸 / 基于项目现有素材',screens:['start','creation','hud-dungeon','inventory','journal','forge-weapon'].map(id=>screens.find(s=>s.id===id)),cols:3,width:3240},...Array.from({length:5},(_,i)=>({file:'board-'+(i+1),title:'SILENT / '+['启程与游玩','行囊与手札','城镇与成长','远行与结算','设置与反馈'][i],subtitle:`全界面设计稿 ${i*8+1}–${Math.min(40,(i+1)*8)} / 40　·　每张原图 1920 × 1080`,screens:screens.slice(i*8,i*8+8),cols:4,width:3840})),{file:'all-pages-overview',title:'SILENT / 40 个页面与关键状态',subtitle:'统一界面系统　·　项目现有资源　·　2026.10.28',screens,cols:5,width:3840}];
   for(const group of groups){
    const html=`<!doctype html><html lang="zh-CN"><meta charset="utf-8"><title>${escaped(group.title)}</title><style>*{box-sizing:border-box}body{margin:0;background:#111b23;color:#e4d6b9;font-family:'Microsoft YaHei',sans-serif}.board{padding:64px 66px 55px;width:${group.width}px}header{display:flex;justify-content:space-between;align-items:flex-end;border-bottom:1px solid #a38b5955;padding-bottom:32px;margin-bottom:35px}h1{font:400 48px Georgia,'Microsoft YaHei',serif;letter-spacing:3px;margin:0}header p{font-size:20px;color:#a7b2a8;margin:17px 0 0}.number{font:33px Georgia;color:#b69f76}.grid{display:grid;grid-template-columns:repeat(${group.cols},1fr);gap:30px 26px}.thumb img{display:block;width:100%;aspect-ratio:16/9;object-fit:cover;border:1px solid #7f8b7055}.caption{display:flex;gap:15px;align-items:center;padding-top:14px;font-size:20px;color:#cdcdbb}.caption small{font:17px Georgia;color:#bca16b}.footer{border-top:1px solid #7e8c7144;margin-top:38px;padding-top:22px;font-size:16px;color:#9aa99f;display:flex;justify-content:space-between}</style><div class="board"><header><div><h1>${escaped(group.title)}</h1><p>${escaped(group.subtitle)}</p></div><div class="number">UI DESIGN / 2026</div></header><div class="grid">${group.screens.map(s=>`<div class="thumb"><img src="../screens/${s.file}" alt="${escaped(s.title)}"><div class="caption"><small>${String(s.number).padStart(2,'0')}</small>${escaped(s.title)}</div></div>`).join('')}</div><div class="footer"><span>Fantasy UI · Bamao UI Pack · 项目职业模型 · 项目环境背景</span><span>设计预览 / 数据为排版示例</span></div></div></html>`;
    const htmlFile=path.join(root,'boards',group.file+'.html');fs.writeFileSync(htmlFile,html);
    await page.setViewportSize({width:group.width,height:2200});
    await page.goto(pathToFileURL(htmlFile).href);
    await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode().catch(()=>{}))));
    await page.locator('.board').screenshot({path:path.join(root,'boards',group.file+'.png'),animations:'disabled'});
    console.log('Board '+group.file);
   }
  }
  const report={generated_at:new Date().toISOString(),artboard:[1920,1080],screen_count:40,rendered:audit.length,errors,audit,source_images_verified:'see source-manifest.json; copied byte-for-byte'};
  fs.writeFileSync(path.join(root,selected?'qa-smoke.json':'qa-report.json'),JSON.stringify(report,null,2));
  if(errors.length||audit.some(a=>a.broken_images.length||a.outside_canvas.length))process.exitCode=2;
 }finally{await browser.close();}
}
render().catch(e=>{console.error(e);process.exit(1)});
