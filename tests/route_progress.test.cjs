const assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm'),path=require('node:path');
const src=fs.readFileSync(path.join(__dirname,'../src/wwwroot/app.js'),'utf8');
const start=src.indexOf('  function routeProgress('),end=src.indexOf('\n  function routeStops(',start);
function element(tag,cls,text){return {tag,cls,text,style:{},attrs:{},children:[],setAttribute(k,v){this.attrs[k]=v;}};}
function append(el,...children){el.children.push(...children);return el;}
const render=vm.runInNewContext('('+src.slice(start,end).trim()+')',{element,append,clamp:(n,a,b)=>Math.max(a,Math.min(b,n)),integer:n=>String(n)});
function check(index,count,miles,expected){const out=render({currentStopIndex:index,stops:Array(count).fill({}),milesTraveled:miles,totalMiles:100});assert.equal(out.children[1].children[0].style.width,expected);return out;}
check(0,5,0,'0%');
const mid=check(2,5,28039,'50%');assert.match(mid.children[2].text,/Odometer: 28039/);
check(4,5,28039,'100%');check(2,6,28039,'40%');check(0,0,0,'0%');
assert.equal(mid.children[1].attrs['aria-valuenow'],'2');
console.log('PASS route stops independent of odometer, final destination, inserted route choice, empty state and ARIA');
