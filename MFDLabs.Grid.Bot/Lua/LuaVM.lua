--[[
File Name: SafeLuaMode.lua
Written By: Nikita Petko
Description: Disables specific things in the datamodel, by virtualizing the function environment
Modifications:
	21/11/2021 01:16 => Removed the game to script check because it was returning nil (we aren't running under a script so it's nil)
--]] 
local args = ...
local isAdmin = args['isAdmin'] -- might be able to be hacked, but we'll see

if (not isAdmin) then
    warn("We are in a VM state, blocking specific methods is expected.")

    local setfenv = setfenv
    local getfenv = getfenv
    local setmetatable = setmetatable
    local getmetatable = getmetatable
    local type = type
    local select = select
    local tostring = tostring
    local newproxy = newproxy
    local next = next

    local Capsule = {{}}
    Capsule.__metatable = "This debug metatable is locked."

    local last = nil

    function Capsule:__index(k)
        if typeof(k) ~= "string" then
            k = tostring(k)
        end

        k = k:gsub("[^%w%s_]+", "")
        if k:lower() == "getservice" then
            return function(...)
                local t = {{...}}
                local service = game:GetService(t[2])
                if service == game:GetService("HttpService") or service ==
                    game:GetService("HttpRbxApiService") then
                    last = service
                end
                return service
            end
        end
        -- todo: clean up the check, because it looks kludgy
        if last == game or last == game:GetService("HttpService") or last ==
            game:GetService("HttpRbxApiService") then
            if k:lower() == "postasyncfullurl" or k:lower() ==
                "requestasyncfullurl" or k:lower() == "getasyncfullurl" or
                k:lower() == "postasync" or k:lower() == "requestasync" or
                k:lower() == "getasync" or k:lower() == "requestasync" or
                k:lower() == "httppostasync" or k:lower() == "httppost" or
                k:lower() == "httpgetasync" or k:lower() == "httpget" or
                k:lower() == "requestinternal" then
                return function(...)
                    error(string.format(
                              "The method by the name of '%s' is disabled.", k))
                end
            end
        elseif typeof(last) == "Instance" then
            if last:IsA("NetworkClient") or last:IsA("NetworkMarker") or
                last:IsA("NetworkPeer") or last:IsA("NetworkReplicator") or
                last:IsA("NetworkServer") or last:IsA("NetworkSettings") then
                return nil
            end
        end
        last = self[k]
        return self[k]
    end
    function Capsule:__newindex(k, v) self[k] = v end
    function Capsule:__call(...) self(...) end
    function Capsule:__concat(v) return self .. v end
    function Capsule:__unm() return -self end
    function Capsule:__add(v) return self + v end
    function Capsule:__sub(v) return self - v end
    function Capsule:__mul(v) return self * v end
    function Capsule:__div(v) return self / v end
    function Capsule:__mod(v) return self % v end
    function Capsule:__pow(v) return self ^ v end
    function Capsule:__tostring() return tostring(self) end
    function Capsule:__eq(v) return self == v end
    function Capsule:__lt(v) return self < v end
    function Capsule:__le(v) return self <= v end
    function Capsule:__len() return #self end
    local CapsuleMT = {{__index = Capsule}}

    local original = setmetatable({{}}, {{__mode = "k"}})
    local wrapper = setmetatable({{}}, {{__mode = "v"}})

    local wrap
    local unwrap

    local secureVersions = {{
        [setfenv] = function(target, newWrappedEnv)
            if type(target) == "number" and target > 0 then
                target = target + 2
            elseif target == wrapper[target] then
                target = original[target]
            end

            local success, oldEnv = pcall(getfenv, target)
            local newEnv = newWrappedEnv
            if not success or oldEnv == wrapper[oldEnv] then
                newEnv = newWrappedEnv
            else
                newEnv = original[newWrappedEnv]
            end

            return wrap(setfenv(target, newEnv))
        end,

        [getfenv] = function(target, newWrappedEnv)
            if type(target) == "number" and target > 0 then
                target = target + 1
            elseif target == wrapper[target] then
                target = original[target]
            end

            return wrap(getfenv(target))
        end
    }}

    local i, n = 1, 0

    function unwrap(...)
        if i > n then
            i = 1
            n = select("#", ...)

            if n == 0 then return end
        end

        local value = select(i, ...)
        if value then
            if type(value) == "function" then
                local wrappedFunc = wrapper[value]
                if wrappedFunc then
                    local originalFunc = original[wrappedFunc]
                    if originalFunc == value then
                        return wrappedFunc
                    else
                        return originalFunc
                    end
                else
                    wrappedFunc = function(...)
                        return unwrap(value(wrap(...)))
                    end
                    wrapper[wrappedFunc] = wrappedFunc
                    wrapper[value] = wrappedFunc
                    original[wrappedFunc] = value
                    return wrappedFunc
                end
            elseif wrapper[value] then
                value = original[wrapper[value]]
            end
        end

        i = i + 1
        if i <= n then
            return value, unwrap(...)
        else
            return value
        end
    end

    function wrap(...)
        if i > n then
            i = 1
            n = select("#", ...)

            if n == 0 then return end
        end

        local value = select(i, ...)
        if value then
            local wrapped = wrapper[value]

            if not wrapped then
                local vType = type(value)
                if vType == "function" then
                    if secureVersions[value] then
                        wrapped = secureVersions[value]
                    else
                        local func = value
                        wrapped = function(...)
                            return wrap(func(unwrap(...)))
                        end
                    end
                elseif vType == "table" then
                    wrapped = setmetatable({{}}, Capsule)
                elseif vType == "userdata" then
                    wrapped = newproxy(true)
                    local mt = getmetatable(wrapped)
                    for key, value in next, Capsule do
                        mt[key] = value
                    end
                else
                    wrapped = value
                end

                wrapper[value] = wrapped
                wrapper[wrapped] = wrapped
                original[wrapped] = value
            end

            value = wrapped
        end

        i = i + 1
        if i <= n then
            return value, wrap(...)
        else
            return value
        end
    end

    for key, metamethod in next, Capsule do Capsule[key] = wrap(metamethod) end

    local ret = setfenv(1, wrap(getfenv(1)))
    local new = getfenv(1)
    setfenv(1, new)
end

function wrapped_return()

{0}

end

local result = wrapped_return();
local temp = {{}}

if type(result) == "table" then
    for i, v in pairs(result) do
        if typeof(v) == "Instance" then
            temp[i] = ("<instance> (%s)"):format(v.Name)
        end
    end

    result = temp
end

if type(result) == "userdata" or type(result) == "table" then
    if typeof(result) == "Instance" then
        result = result:GetFullName() or tostring(result)
    else
        result = game:GetService("HttpService"):JSONEncode(result)
    end
end

return result -- This will actually make the check for LUA_TARRAY redundant.