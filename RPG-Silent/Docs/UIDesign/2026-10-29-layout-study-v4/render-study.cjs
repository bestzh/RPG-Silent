const fs=require('fs'),path=require('path');
const {pathToFileURL}=require('url');
const {chromium}=require('C:/Users/85065/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=__dirname;
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 const page=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:1});
 const errors=[];page.on('pageerror',e=>{errors.push(String(e));console.error('PAGE:',String(e));});
 const audit=[];
 fs.mkdirSync(path.join(root,'screens'),{recursive:true});
 try{
  await page.goto(pathToFileURL(path.join(root,'index.html')).href+'?export=1&screen=direction');
  await page.waitForFunction(()=>window.DESIGN_READY);
  const pages=await page.evaluate(()=>window.DESIGN_PAGES);
  const only=process.argv.find(x=>x.startsWith('--screens='))?.split('=')[1].split(',');
  for(const screen of pages.filter(p=>!only||only.includes(p.id))){
   await page.evaluate(id=>window.showDesign(id),screen.id);
   await page.waitForFunction(()=>window.DESIGN_READY);
   const check=await page.evaluate(()=>{
    const c=document.getElementById('canvas'),rect=c.getBoundingClientRect();
    const imgs=[...c.querySelectorAll('img')];
    const outside=[...c.querySelectorAll('h1,h2,h3,p,button,.bottom-line,.skill-loadout,.gear-label,.wearer-stats')].filter(e=>{
     const r=e.getBoundingClientRect(),css=getComputedStyle(e);
     return css.display!=='none'&&r.width&&r.height&&(r.left<rect.left-1||r.top<rect.top-1||r.right>rect.right+1||r.bottom>rect.bottom+1);
    }).map(e=>({tag:e.tagName,class:e.className,text:e.textContent.slice(0,65)}));
    return {size:[rect.width,rect.height],broken_images:imgs.filter(i=>!i.complete||!i.naturalWidth).map(i=>i.getAttribute('src')),outside_canvas:outside,image_count:imgs.length,used_images:[...new Set(imgs.map(i=>i.getAttribute('src')))]};
   });
   await page.locator('#canvas').screenshot({path:path.join(root,'screens',screen.file),animations:'disabled'});
   audit.push({screen:screen.id,...check});console.log(screen.id+': '+check.broken_images.length+' missing, '+check.outside_canvas.length+' outside');
  }
  if(!only){
   fs.writeFileSync(path.join(root,'screen-manifest.json'),JSON.stringify({date:'2026-10-29',version:'Layout study V4',scope:'6 key page concepts + design rationale; not a complete UI revision',artboard:[1920,1080],screens:pages},null,2));
   const report={rendered:audit.length,errors,audit,passed:errors.length===0&&audit.every(x=>!x.broken_images.length&&!x.outside_canvas.length)};
   fs.writeFileSync(path.join(root,'qa-report.json'),JSON.stringify(report,null,2));
   fs.mkdirSync(path.join(root,'boards'),{recursive:true});
   const html=`<!doctype html><html lang="zh-CN"><meta charset="utf-8"><style>*{box-sizing:border-box}body{margin:0;background:#172e36;color:#dedfca;font-family:'Microsoft YaHei',sans-serif}.board{width:3600px;padding:64px 62px 44px}header{display:flex;justify-content:space-between;align-items:end;padding-bottom:30px;border-bottom:1px solid #b4c6aa44;margin-bottom:36px}h1{font-size:45px;font-weight:400;letter-spacing:5px;margin:0}header p{font-size:21px;letter-spacing:1px;color:#9bb4af;margin-bottom:0}header>span{font:23px Georgia;color:#d4c187;letter-spacing:4px}.grid{display:grid;grid-template-columns:repeat(3,1fr);gap:35px 30px}.thumb img{width:100%;display:block;aspect-ratio:16/9}.thumb>div{font-size:24px;margin-top:16px;color:#dfdfc7;letter-spacing:3px}.thumb p{font-size:17px;line-height:1.8;color:#9cb6b0;margin:10px 0 0}.thumb small{font:17px Georgia;color:#c7b784;margin-right:18px}footer{border-top:1px solid #b5c5a433;display:flex;justify-content:space-between;font-size:16px;color:#88a5a2;padding-top:23px;margin-top:38px}</style><div class="board"><header><div><h1>SILENT / 界面构图研究</h1><p>场景、人物、徽记、路线、边缘与开放列表 · 六类页面，各有重心</p></div><span>LAYOUT STUDY / 04</span></header><div class="grid">${pages.slice(1).map((s,i)=>`<div class="thumb"><img src="../screens/${s.file}"><div><small>0${i+1}</small>${s.title}</div><p>${['场景与菜单共享画面，角色成为视觉重心。','穿戴围绕人物，物品保留有用的网格。','招式使用独立徽记，当前选择有明确焦点。','沿路线选择目的地，信息不再切成卡片。','信息稳定贴近边缘，中央留给动作判断。','对齐控件与文字，以留白代替完整外框。'][i]}</p></div>`).join('')}</div><footer><span>设计概念 · 本轮未修改游戏代码或 Assets</span><span>场景底图含 Synty 示例插图，非实际游戏关卡与 Boss</span></footer></div></html>`;
   fs.writeFileSync(path.join(root,'boards','layout-overview.html'),html);
   await page.goto(pathToFileURL(path.join(root,'boards','layout-overview.html')).href);
   await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));
   await page.setViewportSize({width:3600,height:2200});
   await page.locator('.board').screenshot({path:path.join(root,'boards','layout-overview.png')});
   console.log('overview exported; passed='+report.passed);if(!report.passed)process.exitCode=2;
  }else fs.writeFileSync(path.join(root,'qa-smoke.json'),JSON.stringify({errors,audit},null,2));
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
