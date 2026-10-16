const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const app = fs.readFileSync(path.join(__dirname, '../src/wwwroot/app.js'), 'utf8');
const start = app.indexOf('  function retainedResponseMessage(');
const end = app.indexOf('\n  function acceptState(', start);
const keep = vm.runInNewContext('(' + app.slice(start,end).trim() + ')');
const prior = {journeyId:'a',screen:{id:'crypto'},responseMessage:'Rejected: review and retry'};
assert.equal(keep(prior,{journeyId:'a',screen:{id:'crypto'},revision:99},false),prior.responseMessage);
assert.equal(keep(prior,{journeyId:'b',screen:{id:'crypto'}},false),'');
assert.equal(keep(prior,{journeyId:'a',screen:{id:'travel'}},false),'');
assert.equal(keep(prior,{journeyId:'a',screen:{id:'crypto'},responseMessage:''},true),'');
assert.equal(keep(null,{journeyId:'a',screen:{id:'crypto'},responseMessage:'New error'},true),'New error');
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
  console.log('PASS rejection retention, transitions, heartbeat-only reconciliation, stop cleanup');
})().catch(e=>{console.error(e);process.exitCode=1;});
