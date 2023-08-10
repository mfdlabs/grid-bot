--[[
File Name: SafeLuaMode.lua
Written By: Nikita Petko
Description: Disables specific things in the datamodel, by virtualizing the function environment
Modifications:
	21/11/2021 01:16 => Removed the game to script check because it was returning nil (we aren't running under a script so it's nil)
--]] 
local args = ...
local isAdmin = args['isAdmin'] -- might be able to be hacked, but we'll see
local isVmEnabledForAdmins = args['isVmEnabledForAdmins']

local shouldVirtualize = isAdmin and isVmEnabledForAdmins or true

local blacklistedServices = {{
	"httprbxapiservice",
	"testservice"
}}

local blacklistedInstanceTypes = {{
	"script",
	"modulescript",
	"corescript",
	"localscript",
	"networkclient",
	"networkmarker",
	"networkserver",
	"networkpeer",
	"networkreplicator",
	"networksettings",
	"testservice"
}}

local blacklistedProps = {{
	getasync = {{"httpservice"}},
	postasync = {{"httpservice"}},
	requestasync = {{"httpservice"}},
	requestinternal = {{"httpservice"}},
	httpget = {{"datamodel"}},
	httpgetasync = {{"datamodel"}},
	httppost = {{"datamodel"}},
	httppostasync = {{"datamodel"}},
	run = {{"runservice", "testservice"}},
	loadasset = {{"insertservice"}},
	load = {{"datamodel"}}
}};

local DebugService = nil
if isAdmin then
    DebugService = {{}};
    DebugService.__index = DebugService;
    DebugService.__metatable = "This metatable is locked";
    function DebugService:__tostring()
        return "DebugService";
    end

    function DebugService.new()
        local service = {{
            _last = nil,
            _capsule = nil,
            _wrap = nil,
            _unwrap = nil,
            _original = nil,
            _wrapper = nil
        }};

        setmetatable(service, DebugService);

        return service;
    end

    function DebugService:setLast(last)
        self._last = last
    end

    function DebugService:getLast()
        return self._last
    end

    function DebugService:setMeta(capsule, original, wrapper)
        self._capsule = capsule
        self._original = orginal
        self._wrapped = wrapped
    end

    function DebugService:getMeta()
        return {{
            capsule = self._capsule,
            original = self._original,
            wrapped = self._wrapped
        }};
    end

    function DebugService:setWrappers(wrap, unwrap)
        self._wrap = wrap
        self._unwrap = unwrap
    end

    function DebugService:wrap(...)
        return self._wrap(...)
    end

    function DebugService:unwrap(...)
        return self._unwrap(...)
    end
end


if shouldVirtualize then
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

    local debugService = isAdmin and DebugService.new() or nil

    local Capsule = {{}}
    Capsule.__metatable = "This debug metatable is locked."

    local last = nil
	
	function _is_blacklisted_service(serviceName)
		local name = serviceName:lower()
		
		return table.find(blacklistedServices, name) ~= nil
	end
	
	function _is_blacklisted_instance(instanceName)
		local name = instanceName:lower()
		
		return table.find(blacklistedInstanceTypes, name) ~= nil
	end
	
	function _is_blacklisted(instance, propName)
		local name = propName:lower()
		local instanceName = typeof(instance) == "Instance" and 
							 instance.ClassName:lower() or 
							 ""
		
		local prop = blacklistedProps[name]
		
		return prop ~= nil and (#prop == 0 or table.find(prop, instanceName) ~= nil)
	end

    function Capsule:__index(k)
        if isAdmin then print(k, tostring(last)) end

        if typeof(k) ~= "string" then
            k = tostring(k)
        end

        k = k:gsub("[^%w%s_]+", "")
		
		local lower = k:lower()
		
        if lower == "getservice" or lower == "service" or lower == "findservice" then
            return function(...)
                local t = {{...}}

                if isAdmin and t[2] == "DebugService" then
					last = debugService
                    return debugService
                end
				
				if _is_blacklisted_service(t[2]) then
					error(string.format(
							"The service by the name of '%s' is inaccessible.", 
							t[2]
						))
				end

                local service = game[k](game, t[2])
				
                last = service
				
                return service
            end
        end
		
		if lower == "new" and last == Instance then
			return function(...)
                local t = {{...}}

				if _is_blacklisted_instance(t[1]) then
					error(string.format(
							"The instance type by the name of '%s' is inaccessible.", 
							t[1]
						))
				end

                local inst = Instance.new(t[1])
				
                last = inst
				
                return inst
            end
		end
		
        -- todo: clean up the check, because it looks kludgy
        if _is_blacklisted(last, k) then
			if typeof(last) == "Instance" then
				error(string.format(
						"'%s.%s' is inaccessible.", 
						last:GetFullName(),
						k
					))
			else
				error(string.format(
						"'%s' is inaccessible.", 
						k
					))
			end
		end
		
        last = self[k]

        if isAdmin then debugService:setLast(last) end

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

    if isAdmin then
        debugService:setMeta(Capsule, original, wrapper);
    end

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

    if isAdmin then
        debugService:setWrappers(wrap, unwrap)
    end

    for key, metamethod in next, Capsule do Capsule[key] = wrap(metamethod) end

    local ret = setfenv(1, wrap(getfenv(1)))
    local new = getfenv(1)
    setfenv(1, new)
end

local result = (function()

	local isAdmin = nil
	local isVmEnabledForAdmins = nil
	local args = nil
	local shouldVirtualize = nil
	local blacklistedServices = nil
	local blacklistedInstanceTypes = nil
	local blacklistedProps = nil

{0}

end)()

if typeof(result) == "Instance" then
    result = tostring(result)
elseif typeof(result) == "table" then
    result = game:GetService("HttpService"):JSONEncode(result)
elseif result ~= nil then
    result = tostring(result)
end

return result -- This will actually make the check for LUA_TARRAY redundant.