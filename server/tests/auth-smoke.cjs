// Isolated local test accounts; no player credentials or saves are used.
const assert = require('node:assert/strict');
const {randomUUID}=require('node:crypto');
const base='http://127.0.0.1:7350';
const basic='Basic '+Buffer.from('localdevkey:').toString('base64');
async function post(path,body,auth=basic) {
  const r=await fetch(base+path,{method:'POST',headers:{'Content-Type':'application/json',Authorization:auth},body:JSON.stringify(body)});
  const text=await r.text();return {status:r.status,data:text?JSON.parse(text):{}};
}
const uid=token=>JSON.parse(Buffer.from(token.split('.')[1],'base64url')).uid;
async function main(){
  const id='auth-test-'+randomUUID(),email=id+'@example.com',password=randomUUID();
  const guest=await post('/v2/account/authenticate/device?create=true',{id});assert.equal(guest.status,200);
  const bearer='Bearer '+guest.data.token;
  const before=await post('/v2/rpc/game',JSON.stringify({action:'sync',revision:0}),bearer);assert.equal(before.status,200);
  const link=await post('/v2/account/link/email',{email,password},bearer);assert.equal(link.status,200);
  const login=await post('/v2/account/authenticate/email?create=false',{email,password});assert.equal(login.status,200);
  assert.equal(uid(login.data.token),uid(guest.data.token));
  const after=await post('/v2/rpc/game',JSON.stringify({action:'sync',revision:0}),'Bearer '+login.data.token);
  assert.equal(after.status,200);assert.equal(JSON.parse(after.data.payload).state.level,JSON.parse(before.data.payload).state.level);
  console.log('PASS email link preserves account identity and saved level');
  assert.notEqual((await post('/v2/account/authenticate/email?create=false',{email,password:'wrong-password'})).status,200);
  assert.notEqual((await post('/v2/account/authenticate/email?create=false',{email:'missing-'+email,password})).status,200);
  console.log('PASS wrong password and unknown account rejected');
  const other=await post('/v2/account/authenticate/device?create=true',{id:'auth-test-'+randomUUID()});
  assert.notEqual((await post('/v2/account/link/email',{email,password},'Bearer '+other.data.token)).status,200);
  console.log('PASS email cannot be linked to a second account');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
