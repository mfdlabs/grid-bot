local args = ...;

game:GetService("ContentProvider"):SetBaseUrl("https://www.sitetest4.robloxlabs.com")

local script = [[
local h = setfenv
local k = getfenv
local e = setmetatable
local c = getmetatable
local g = type
local f = select
local j = tostring
local b = newproxy
local a = print
local d = next
local q = {}
q.__metatable = "This debug metatable is locked."
local i = nil
local _ = {}
function q:__index(_)
    if _:lower() == "getservice" then
        return function(...)
            local t = {...}
            local service = game:GetService(t[2])
            if service == game:GetService("HttpService") then
                i = service
            end
            return service
        end
    end
    if i == game or i == game:GetService("HttpService") then
        if _:lower() == "httpget" then
            return nil
        elseif _:lower() == "httppost" then
            return nil
        elseif _:lower() == "requestasync" then
            return nil
        elseif _:lower() == "getasync" then
            return nil
        elseif _:lower() == "postasync" then
            return nil
        end
    elseif typeof(i) == "Instance" then
        if
            i:IsA("NetworkClient") or i:IsA("NetworkMarker") or i:IsA("NetworkPeer") or i:IsA("NetworkReplicator") or
                i:IsA("NetworkServer") or
                i:IsA("NetworkSettings")
         then
            return nil
        end
    elseif i == script then
        return nil
    end
    i = self[_]
    return self[_]
end
function q:__newindex(a, _)
    self[a] = _
end
function q:__call(...)
    self(...)
end
function q:__concat(_)
    return self .. _
end
function q:__unm()
    return -self
end
function q:__add(_)
    return self + _
end
function q:__sub(_)
    return self - _
end
function q:__mul(_)
    return self * _
end
function q:__div(_)
    return self / _
end
function q:__mod(_)
    return self % _
end
function q:__pow(_)
    return self ^ _
end
function q:__tostring()
    return j(self)
end
function q:__eq(_)
    return self == _
end
function q:__lt(_)
    return self < _
end
function q:__le(_)
    return self <= _
end
function q:__len()
    return #self
end
local _ = {__index = q}
local l = e({}, {__mode = "k"})
local o = e({}, {__mode = "v"})
local m
local i
local _ = {[h] = function(d, c)
        if g(d) == "number" and d > 0 then
            d = d + 2
        elseif d == o[d] then
            d = l[d]
        end
        local _, a = pcall(k, d)
        local b = c
        if not _ or a == o[a] then
            b = c
        else
            b = l[c]
        end
        return m(h(d, b))
    end, [k] = function(a, _)
        if g(a) == "number" and a > 0 then
            a = a + 1
        elseif a == o[a] then
            a = l[a]
        end
        return m(k(a))
    end}
local p, n = 1, 0
function i(...)
    if p > n then
        p = 1
        n = f("#", ...)
        if n == 0 then
            return
        end
    end
    local b = f(p, ...)
    if b then
        if g(b) == "function" then
            local a = o[b]
            if a then
                local _ = l[a]
                if _ == b then
                    return a
                else
                    return _
                end
            else
                a = function(...)
                    return i(b(m(...)))
                end
                o[a] = a
                o[b] = a
                l[a] = b
                return a
            end
        elseif o[b] then
            b = l[o[b\]\]
        end
    end
    p = p + 1
    if p <= n then
        return b, i(...)
    else
        return b
    end
end
function m(...)
    if p > n then
        p = 1
        n = f("#", ...)
        if n == 0 then
            return
        end
    end
    local f = f(p, ...)
    if f then
        local h = o[f]
        if not h then
            local a = g(f)
            if a == "function" then
                if _[f] then
                    h = _[f]
                else
                    local _ = f
                    h = function(...)
                        return m(_(i(...)))
                    end
                end
            elseif a == "table" then
                h = e({}, q)
            elseif a == "userdata" then
                h = b(true)
                local a = c(h)
                for b, _ in d, q do
                    a[b] = _
                end
            else
                h = f
            end
            o[f] = h
            o[h] = h
            l[h] = f
        end
        f = h
    end
    p = p + 1
    if p <= n then
        return f, m(...)
    else
        return f
    end
end
for a, _ in d, q do
    q[a] = m(_)
end
local b = {}
local _ = h(1, m(k(1)))
local c = k(1)
for a, _ in pairs(b) do
    c[a] = _
end
h(1, c)
local c = {}

]]

return loadstring('local h=setfenv local k=getfenv local e=setmetatable local c=getmetatable local g=type local f=select local j=tostring local b=newproxy local a=print local d=next local q={} q.__metatable="This debug metatable is locked." local i=nil local _={} function q:__index(_) if _:lower() == "getservice" then return function(...) local t = {...} local service = game:GetService(t[2]) if service == game:GetService("HttpService") then i = service end return service end end if i==game or i==game:GetService("HttpService")then if _:lower()=="httpget"then return nil elseif _:lower()=="httppost"then return nil elseif _:lower()=="requestasync"then return nil elseif _:lower()=="getasync"then return nil elseif _:lower()=="postasync"then return nil end elseif typeof(i)=="Instance"then if i:IsA("NetworkClient")or i:IsA("NetworkMarker")or i:IsA("NetworkPeer")or i:IsA("NetworkReplicator")or i:IsA("NetworkServer")or i:IsA("NetworkSettings")then return nil end elseif i==script then return nil end i=self[_] return self[_]end function q:__newindex(a,_)self[a]=_ end function q:__call(...)self(...)end function q:__concat(_)return self.._ end function q:__unm()return-self end function q:__add(_)return self+_ end function q:__sub(_)return self-_ end function q:__mul(_)return self*_ end function q:__div(_)return self/_ end function q:__mod(_)return self%_ end function q:__pow(_)return self^_ end function q:__tostring()return j(self)end function q:__eq(_)return self==_ end function q:__lt(_)return self<_ end function q:__le(_)return self<=_ end function q:__len()return#self end local _={__index=q} local l=e({},{__mode="k"}) local o=e({},{__mode="v"}) local m local i local _={[h]=function(d,c)if g(d)=="number"and d>0 then d=d+2 elseif d==o[d]then d=l[d]end local _,a=pcall(k,d) local b=c if not _ or a==o[a]then b=c else b=l[c]end return m(h(d,b))end,[k]=function(a,_)if g(a)=="number"and a>0 then a=a+1 elseif a==o[a]then a=l[a]end return m(k(a))end} local p,n=1,0 function i(...)if p>n then p=1 n=f("#",...) if n==0 then return end end local b=f(p,...) if b then if g(b)=="function"then local a=o[b] if a then local _=l[a] if _==b then return a else return _ end else a=function(...)return i(b(m(...)))end o[a]=a o[b]=a l[a]=b return a end elseif o[b]then b=l[o[b]]end end p=p+1 if p<=n then return b,i(...)else return b end end function m(...)if p>n then p=1 n=f("#",...) if n==0 then return end end local f=f(p,...) if f then local h=o[f] if not h then local a=g(f) if a=="function"then if _[f]then h=_[f]else local _=f h=function(...)return m(_(i(...)))end end elseif a=="table"then h=e({},q)elseif a=="userdata"then h=b(true) local a=c(h) for b,_ in d,q do a[b]=_ end else h=f end o[f]=h o[h]=h l[h]=f end f=h end p=p+1 if p<=n then return f,m(...)else return f end end for a,_ in d,q do q[a]=m(_)end local b={} local _=h(1,m(k(1))) local c=k(1) for a,_ in pairs(b)do c[a]=_ end h(1,c) local c={}' .. args["script"])()
