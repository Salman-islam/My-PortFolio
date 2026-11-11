"""Run against a disposable development database only. Adds test records."""
import argparse, json, os, sys, time, subprocess, tempfile, urllib.request, urllib.error, uuid
from pathlib import Path
from datetime import datetime,timedelta,timezone
parser=argparse.ArgumentParser();parser.add_argument('--base-url');parser.add_argument('--dotnet',default='dotnet');parser.add_argument('--project',default=Path(__file__).resolve().parents[1].name);args=parser.parse_args()
slug=args.project
base=args.base_url or 'http://127.0.0.1:5089'
server=None;log=None
if not args.base_url:
 directory=Path(__file__).resolve().parent/'ApiHarness'
 temp=tempfile.TemporaryDirectory();log=open(Path(temp.name)/'server.log','w+')
 env=os.environ.copy();env['ASPNETCORE_ENVIRONMENT']='Testing';env['TestDb']=str(Path(temp.name)/'test.db');env['DOTNET_ROOT']=str(Path(args.dotnet).resolve().parent) if '/' in args.dotnet else env.get('DOTNET_ROOT','')
 server=subprocess.Popen([args.dotnet,'run','--project',str(directory),'--no-build','--urls',base],env=env,stdout=log,stderr=log)
count=0
password='PortfolioDemo!2026';suffix=uuid.uuid4().hex[:8]
def req(method,path,body=None,token=None,expected=200):
 global count
 headers={'Content-Type':'application/json'}
 if token:headers['Authorization']='Bearer '+token
 r=urllib.request.Request(base+'/api/'+path,data=None if body is None else json.dumps(body).encode(),headers=headers,method=method)
 try:
  with urllib.request.urlopen(r,timeout=20) as response:status=response.status;raw=response.read()
 except urllib.error.HTTPError as e:status=e.code;raw=e.read()
 assert status==expected,f'{method} {path}: expected {expected}, got {status}: {raw.decode()}'
 count+=1
 return json.loads(raw) if raw else None
def login(email):return req('POST','auth/login',{'email':email,'password':password})['token']
def ws(t):return req('GET','workspace',token=t)
def record(key,id,t):return next(x for x in ws(t)[key] if x['id']==id)
def future(days=14):
 d=datetime.now(timezone.utc).date()+timedelta(days=days)
 while d.weekday()>3:d+=timedelta(days=1)
 return d
try:
 for i in range(100):
  try: req('GET','health');break
  except (urllib.error.URLError,ConnectionError):
   if server and server.poll() is not None:log.seek(0);raise RuntimeError(log.read())
   time.sleep(.2)
 else:raise RuntimeError('API did not start')
 req('GET','workspace',expected=401)
 req('POST','auth/login',{'email':'admin@example.com','password':'wrong'},expected=401)
 admin=login('admin@example.com');assert req('GET','auth/me',token=admin)['role']=='Admin'
 if slug=='peopleops':
  emp=login('employee@example.com');req('GET','users',token=emp,expected=403)
  req('POST','departments',{'name':'Forbidden'},emp,403)
  dept=req('POST','departments',{'name':'QA '+suffix},admin)
  req('PUT','departments/'+str(dept['id']),{'name':'Quality '+suffix,'version':dept['version']},admin)
  req('PUT','departments/'+str(dept['id']),{'name':'Stale','version':dept['version']},admin,409)
  data={'name':'Test Employee','email':suffix+'@example.com','password':password,'departmentId':dept['id'],'jobTitle':'QA Engineer','startDate':'2025-01-01','annualAllowance':2}
  created=req('POST','employees',data,admin);eid=created['id'];et=login(data['email'])
  assert len(ws(et)['employees'])==1
  req('DELETE','departments/'+str(dept['id']),token=admin,expected=409)
  req('POST','attendance/check-in',{},et,204);req('POST','attendance/check-in',{},et,409);req('POST','attendance/check-out',{},et,204);req('POST','attendance/check-out',{},et,400)
  start=future();end=start+timedelta(days=1)
  leave={'employeeId':eid,'start':str(start),'end':str(end),'reason':'Test leave'}
  req('POST','leaves',leave,et,204);req('POST','leaves',leave,et,409)
  l=ws(et)['leaves'][0];req('POST',f"leaves/{l['id']}/decision",{'status':'Approved','note':'ok','version':l['version']},et,403)
  req('POST',f"leaves/{l['id']}/decision",{'status':'Approved','note':'Approved for test','version':l['version']},admin,204)
  assert record('employees',eid,admin)['remaining']==(0 if start.year==datetime.now().year else 2)
  req('POST',f"leaves/{l['id']}/decision",{'status':'Approved','note':'Duplicate','version':l['version']},admin,409)
  later=start+timedelta(days=7);req('POST','leaves',{'employeeId':eid,'start':str(later),'end':str(later),'reason':'Over balance'},et,204)
  second=ws(et)['leaves'][0];req('POST',f"leaves/{second['id']}/decision",{'status':'Approved','note':'Over budget','version':second['version']},admin,409)
  first=record('leaves',l['id'],admin);req('POST',f"leaves/{l['id']}/cancel",{'version':first['version']},et,204)
  assert record('employees',eid,admin)['remaining']==2
  second=record('leaves',second['id'],admin);req('POST',f"leaves/{second['id']}/decision",{'status':'Rejected','note':'Not needed','version':second['version']},admin,204)
  req('POST','leaves',{**leave,'employeeId':ws(emp)['employees'][0]['id']},et,403)
  e=record('employees',eid,admin);req('PUT',f'employees/{eid}',{**data,'name':'Updated Employee','version':e['version']},admin,204)
 elif slug=='stockpilot':
  staff=login('staff@example.com');req('POST','products',{'name':'X'},staff,403)
  body={'name':'Test widget','sku':suffix,'category':'Testing','price':12.5,'reorderLevel':2,'active':True}
  p=req('POST','products',body,admin);pid=p['id'];req('POST','products',body,admin,409)
  req('POST',f'products/{pid}/adjust',{'delta':-1,'reason':'Invalid','version':p['version']},admin,409)
  req('POST',f'products/{pid}/adjust',{'delta':5,'reason':'Opening stock','version':p['version']},admin,204)
  p=record('products',pid,admin);assert p['stock']==5
  req('POST',f'products/{pid}/adjust',{'delta':1,'reason':'Stale','version':1},admin,409)
  customer=req('POST','parties',{'name':'Test Customer','email':suffix+'@example.com','kind':'Customer'},staff)
  supplier=req('POST','parties',{'name':'Test Supplier','email':'s'+suffix+'@example.com','kind':'Supplier'},staff)
  purchase={'kind':'Purchase','partyId':supplier['id'],'lines':[{'productId':pid,'quantity':3,'unitPrice':10}]}
  po=req('POST','orders',purchase,staff);o=record('orders',po['id'],admin)
  req('POST',f"orders/{o['id']}/post",{'version':o['version']},staff,204);assert record('products',pid,admin)['stock']==8
  req('POST',f"orders/{o['id']}/post",{'version':o['version']},staff,409);assert record('products',pid,admin)['stock']==8
  sale={'kind':'Sale','partyId':customer['id'],'lines':[{'productId':pid,'quantity':9,'unitPrice':12.5}]}
  so=req('POST','orders',sale,staff);o=record('orders',so['id'],admin)
  req('POST',f"orders/{o['id']}/post",{'version':o['version']},staff,409);assert record('products',pid,admin)['stock']==8
  sale['lines'][0]['quantity']=2;req('PUT',f"orders/{o['id']}",{**sale,'version':o['version']},staff,204)
  o=record('orders',o['id'],admin);assert o['total']==25
  req('POST',f"orders/{o['id']}/post",{'version':o['version']},staff,204);assert record('products',pid,admin)['stock']==6
  posted=record('orders',o['id'],admin);req('POST',f"orders/{o['id']}/cancel",{'version':posted['version']},staff,409)
  req('DELETE',f'products/{pid}',token=admin,expected=409)
  req('POST','orders',{**sale,'partyId':supplier['id']},staff,400)
  cancel=req('POST','orders',sale,staff);o=record('orders',cancel['id'],admin);req('POST',f"orders/{o['id']}/cancel",{'version':o['version']},staff,204)
  movements=ws(admin)['movements'];assert sum(x['delta'] for x in movements if x['product']=='Test widget')==6
 elif slug=='deskflow':
  customer=login('customer@example.com');staff=login('staff@example.com')
  cat=req('POST','categories',{'name':'QA '+suffix},admin)
  data={'title':'Test issue','description':'Steps to reproduce','priority':'High','categoryId':cat['id']}
  ticket=req('POST','tickets',data,customer);tid=ticket['id'];t=record('tickets',tid,admin)
  req('POST',f'tickets/{tid}/status',{'status':'Resolved','version':t['version']},admin,409)
  req('POST',f'tickets/{tid}/comments',{'body':'private','internal':True},customer,403)
  req('POST',f'tickets/{tid}/comments',{'body':'Internal investigation','internal':True},staff,204)
  req('POST',f'tickets/{tid}/comments',{'body':'Public response','internal':False},staff,204)
  detail=req('GET',f'tickets/{tid}',token=customer);assert [c['body'] for c in detail['comments']]==['Public response']
  assert len(req('GET',f'tickets/{tid}',token=staff)['comments'])==2
  other=req('POST','users',{'name':'Other customer','email':suffix+'@example.com','password':password,'role':'Customer'},admin,201)
  ot=login(suffix+'@example.com');req('GET',f'tickets/{tid}',token=ot,expected=403);assert not ws(ot)['tickets']
  agent=req('GET','auth/me',token=staff)['id'];req('POST',f'tickets/{tid}/assign',{'assignedToId':agent,'version':t['version']},admin,204)
  for status in ['In progress','Resolved']:
   t=record('tickets',tid,admin);req('POST',f'tickets/{tid}/status',{'status':status,'version':t['version']},staff,204)
  t=record('tickets',tid,admin);assert t['resolvedAt'];req('POST',f'tickets/{tid}/status',{'status':'Open','version':t['version']},customer,204)
  assert record('tickets',tid,admin)['resolvedAt'] is None
  for status in ['In progress','Resolved','Closed']:
   t=record('tickets',tid,admin);req('POST',f'tickets/{tid}/status',{'status':status,'version':t['version']},staff,204)
  req('POST',f'tickets/{tid}/comments',{'body':'Too late'},customer,409)
  req('DELETE',f"categories/{cat['id']}",token=admin,expected=409)
  assert len(req('GET',f'tickets/{tid}',token=admin)['history'])>=7
 elif slug=='propertyhub':
  resident=login('resident@example.com');staff=login('staff@example.com');w=ws(resident);rid=w['residents'][0]['id'];aid=w['amenities'][0]['id'];pid=w['properties'][0]['id']
  allw=ws(admin);other=next(x for x in allw['amenities'] if x['propertyId']!=pid)
  start=datetime.now(timezone.utc)+timedelta(days=5);end=start+timedelta(hours=1)
  booking={'amenityId':aid,'residentId':rid,'start':start.isoformat(),'end':end.isoformat(),'guests':2,'purpose':'Test meeting'}
  req('POST','bookings',{**booking,'amenityId':other['id']},resident,403)
  req('POST','bookings',{**booking,'guests':999},resident,400)
  req('POST','bookings',{**booking,'end':(start+timedelta(hours=5)).isoformat()},resident,400)
  req('POST','bookings',booking,resident,204);req('POST','bookings',booking,resident,409)
  b=ws(resident)['bookings'][0]
  query=f"availability?amenityId={aid}&start="+urllib.parse.quote(start.isoformat())+'&end='+urllib.parse.quote(end.isoformat())
  assert req('GET',query,token=resident)['available'] is False
  req('POST',f"bookings/{b['id']}/cancel",{'version':b['version']},resident,204)
  assert req('GET',query,token=resident)['available'] is True
  req('POST','bookings',booking,resident,204)
  amenity=record('amenities',aid,admin);req('PUT',f'amenities/{aid}',{**amenity,'active':False},admin,409)
  room=allw['residents'][0]['roomId'];req('POST','residents',{'name':'Duplicate','email':suffix+'@example.com','password':password,'propertyId':pid,'roomId':room},admin,409)
  p=req('POST','properties',{'name':'Test property '+suffix,'address':'Test street'},admin)
  req('POST','announcements',{'propertyId':p['id'],'title':'Private','body':'Other property','startsAt':(start-timedelta(days=10)).isoformat(),'endsAt':end.isoformat()},admin)
  assert not any(x['title']=='Private' for x in ws(resident)['announcements'])
  req('POST','announcements',{'propertyId':pid,'title':'Expired','body':'Old notice','startsAt':(start-timedelta(days=10)).isoformat(),'endsAt':(start-timedelta(days=6)).isoformat()},admin)
  assert not any(x['title']=='Expired' for x in ws(resident)['announcements'])
  r=allw['residents'][0];req('PUT',f"residents/{rid}",{'name':r['name'],'email':r['email'],'password':'','propertyId':p['id'],'roomId':None,'version':r['version']},admin,409)
  req('DELETE',f'rooms/{room}',token=admin,expected=409)
  req('POST','properties',{'name':'Forbidden','address':'x'},resident,403)
 else:raise AssertionError('Unknown project '+slug)
 # Shared authorization, account revocation, audit, and missing-resource checks.
 role='Admin' if slug=='peopleops' else 'Staff'
 user=req('POST','users',{'name':'Disposable User','email':'disabled'+suffix+'@example.com','password':password,'role':role},admin,201)
 token=login(user['email']);req('GET','workspace',token=token)
 req('PUT',f"users/{user['id']}/active",{'active':False},admin,204)
 req('GET','workspace',token=token,expected=401)
 assert len(req('GET','audit',token=admin))>0
 print(f'{slug}: PASS ({count} HTTP assertions plus business-state assertions)')
finally:
 if server:
  server.terminate()
  try:server.wait(timeout=10)
  except subprocess.TimeoutExpired:server.kill()
  log.close();temp.cleanup()
