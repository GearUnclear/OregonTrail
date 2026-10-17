const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const app = fs.readFileSync(path.join(__dirname, '../src/wwwroot/app.js'), 'utf8');
const start = app.indexOf('  function withRetainedResponse(');
const end = app.indexOf('\n  function acceptState(', start);
assert.ok(start >= 0 && end > start, 'withRetainedResponse must precede acceptState in app.js');
const keep = vm.runInNewContext('(' + app.slice(start,end).trim() + ')');
const rejected = {journeyId:'a',screen:{id:'crypto'},responseMessage:'Rejected: review and retry',responseFromAction:true};
assert.equal(keep(rejected,{journeyId:'a',screen:{id:'crypto'},revision:99},false).responseMessage,rejected.responseMessage);
assert.equal(keep(keep(rejected,{journeyId:'a',screen:{id:'crypto'},revision:99},false),{journeyId:'a',screen:{id:'crypto'},revision:100},false).responseMessage,
  rejected.responseMessage,'retention must survive repeated ticks');
assert.equal(keep(rejected,{journeyId:'b',screen:{id:'crypto'}},false).responseMessage,'');
assert.equal(keep(rejected,{journeyId:'a',screen:{id:'travel'}},false).responseMessage,'');
assert.equal(keep(rejected,{journeyId:'a',screen:{id:'crypto'},responseMessage:''},true).responseMessage,'');
assert.equal(keep(null,{journeyId:'a',screen:{id:'crypto'},responseMessage:'New error'},true).responseMessage,'New error');
const notice = {journeyId:'a',screen:{id:'travel'},responseMessage:'The game server took too long to respond.',responseFromAction:false};
assert.equal(keep(notice,{journeyId:'a',screen:{id:'travel'},revision:5},false).responseMessage,'',
  'connection notices must clear once live state resumes');
let next=0, reads=0;
const timers=new Map();
const setTimeout=(fn,ms)=>{const id=++next;timers.set(id,{fn,ms});return id;};
const clearTimeout=id=>timers.delete(id);
class Events {
  static OPEN=1; static CLOSED=2; static instances=[];
  constructor(){this.readyState=1;this.handlers={};Events.instances.push(this);}
  addEventListener(name,fn){this.handlers[name]=fn;}
  close(){this.readyState=2;}
}
const document={baseURI:'https://example.test/oregon/',hidden:false,addEventListener(){},removeEventListener(){}};
const window={setTimeout,clearTimeout,EventSource:Events,addEventListener(){},removeEventListener(){}};
const context={window,document,navigator:{onLine:true},EventSource:Events,URL,AbortController,setTimeout,clearTimeout,
  fetch:async()=>{reads++;return {status:200,ok:true,headers:{get:()=>`r${reads}`},json:async()=>({revision:reads,screen:{id:'travel'}})};}};
vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../src/wwwroot/bridge.js'),'utf8'),context);
const watch=window.OregonTrailApi.watch(()=>{},message=>{throw new Error(message)},()=>{});
const drain=()=>new Promise(resolve=>setImmediate(resolve));
(async()=>{
  await drain();await drain();
  assert.equal(reads,1);
  const source=Events.instances[0];assert.ok(source);
  source.handlers.heartbeat();
  const reconcile=[...timers.entries()].find(([,t])=>t.ms===15000);
  assert.ok(reconcile,'OPEN stream with heartbeats must still schedule reconciliation');
  timers.delete(reconcile[0]);reconcile[1].fn();await drain();await drain();
  assert.equal(reads,2,'conditional reconciliation must read current state without action replay');
  watch.stop();
  assert.equal(timers.size,0,'stop must cancel watchdog and reconciliation timers');
  console.log('PASS rejection retention, notice clearing, transitions, heartbeat-only reconciliation, stop cleanup');
})().catch(e=>{console.error(e);process.exitCode=1;});
