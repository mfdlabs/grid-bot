﻿local a=...local b=a['isAdmin']if not b then warn("We are in a VM state, blocking specific methods is expected.")local setfenv=setfenv;local getfenv=getfenv;local setmetatable=setmetatable;local getmetatable=getmetatable;local type=type;local select=select;local tostring=tostring;local newproxy=newproxy;local next=next;local c={}c.__metatable="This debug metatable is locked."local d=nil;function c:__index(e)e=e:gsub("[^%w%s_]+","")if e:lower()=="getservice"then return function(...)local f={...}local g=game:GetService(f[2])if g==game:GetService("HttpService")or g==game:GetService("HttpRbxApiService")then d=g end;return g end end;if d==game or d==game:GetService("HttpService")or d==game:GetService("HttpRbxApiService")then if e:lower()=="postasyncfullurl"or e:lower()=="requestasyncfullurl"or e:lower()=="getasyncfullurl"or e:lower()=="postasync"or e:lower()=="requestasync"or e:lower()=="getasync"or e:lower()=="requestasync"or e:lower()=="httppostasync"or e:lower()=="httppost"or e:lower()=="httpgetasync"or e:lower()=="httpget"or e:lower()=="requestinternal"then return function(...)error(string.format("The method by the name of '%s' is disabled.",e))end end elseif typeof(d)=="Instance"then if d:IsA("NetworkClient")or d:IsA("NetworkMarker")or d:IsA("NetworkPeer")or d:IsA("NetworkReplicator")or d:IsA("NetworkServer")or d:IsA("NetworkSettings")then return nil end end;d=self[e]return self[e]end;function c:__newindex(e,h)self[e]=h end;function c:__call(...)self(...)end;function c:__concat(h)return self..h end;function c:__unm()return-self end;function c:__add(h)return self+h end;function c:__sub(h)return self-h end;function c:__mul(h)return self*h end;function c:__div(h)return self/h end;function c:__mod(h)return self%h end;function c:__pow(h)return self^h end;function c:__tostring()return tostring(self)end;function c:__eq(h)return self==h end;function c:__lt(h)return self<h end;function c:__le(h)return self<=h end;function c:__len()return#self end;local i={__index=c}local j=setmetatable({},{__mode="k"})local k=setmetatable({},{__mode="v"})local l;local m;local n={[setfenv]=function(o,p)if type(o)=="number"and o>0 then o=o+2 elseif o==k[o]then o=j[o]end;local q,r=pcall(getfenv,o)local s=p;if not q or r==k[r]then s=p else s=j[p]end;return l(setfenv(o,s))end,[getfenv]=function(o,p)if type(o)=="number"and o>0 then o=o+1 elseif o==k[o]then o=j[o]end;return l(getfenv(o))end}local t,u=1,0;function m(...)if t>u then t=1;u=select("#",...)if u==0 then return end end;local v=select(t,...)if v then if type(v)=="function"then local w=k[v]if w then local x=j[w]if x==v then return w else return x end else w=function(...)return m(v(l(...)))end;k[w]=w;k[v]=w;j[w]=v;return w end elseif k[v]then v=j[k[v]]end end;t=t+1;if t<=u then return v,m(...)else return v end end;function l(...)if t>u then t=1;u=select("#",...)if u==0 then return end end;local v=select(t,...)if v then local y=k[v]if not y then local z=type(v)if z=="function"then if n[v]then y=n[v]else local A=v;y=function(...)return l(A(m(...)))end end elseif z=="table"then y=setmetatable({},c)elseif z=="userdata"then y=newproxy(true)local B=getmetatable(y)for C,v in next,c do B[C]=v end else y=v end;k[v]=y;k[y]=y;j[y]=v end;v=y end;t=t+1;if t<=u then return v,l(...)else return v end end;for C,D in next,c do c[C]=l(D)end;local E=setfenv(1,l(getfenv(1)))local F=getfenv(1)setfenv(1,F)end;