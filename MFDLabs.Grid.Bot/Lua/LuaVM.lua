local ctx = function() {0} end
--[[
File Name: LuaVM.lua
Written By: Liam Meshorer	
Description: Disables specific things in the datamodel, by virtualizing the function environment
--]] 

local execution_env = {}

do
	local args = ...
	local isAdmin = args['isAdmin']
	local isVmEnabledForAdmins = args['isVmEnabledForAdmins']
	local shouldVirtualize = isAdmin and isVmEnabledForAdmins or true
	
	local setfenv = setfenv
	local getfenv = getfenv
	local setmetatable = setmetatable
	local getmetatable = getmetatable
	local type = type
	local select = select
	local tostring = tostring
	local newproxy = newproxy
	local game = game
	
	--[[ Type Definitions ]]	
	type VirtualizedObject = {
		_type: string;
		_proxy: any;
		
		get_proxy: (self: VirtualizedObject) -> any;
		
		__index: (self: any, key: any) -> any;		
		__newindex: (self: any, key: any, value: any) -> any;
		__tostring: (self: any) -> any;
		__call: (self: any, ...any) -> any;
		__concat: (self: any, other: any) -> any;
		__unm: (self: any) -> any;
		__add: (self: any, other: any) -> any;
		__sub: (self: any, other: any) -> any;
		__mul: (self: any, other: any) -> any;
		__div: (self: any, other: any) -> any;
		__mod: (self: any, other: any) -> any;
		__pow: (self: any, other: any) -> any;
		__eq: (self: any, other: any) -> any;
		__lt: (self: any, other: any) -> any;
		__le: (self: any, other: any) -> any;
		__len: (self: any) -> any;
	}
	
	type VirtualizedSignal = {
		_signal: RBXScriptSignal;
	} & VirtualizedObject
	
	type VirtualizedInstance = {
		_instance: Instance;	
	} & VirtualizedObject
	
	type VirtualizedInstanceData = {
		[Instance]: VirtualizedInstance,
		get_wrapped_instance: (self: VirtualizedInstanceData, instance: Instance) -> VirtualizedInstance;
		get_wrapped_signal: (self: VirtualizedInstanceData, signal: RBXScriptSignal, signal_path: string) -> VirtualizedSignal;
		get_wrapped_value: (self: VirtualizedInstanceData, value: any) -> any;
		
		_blocked_classnames: {[string]: boolean?};
		_blocked_class_properties: {[string]: {[string]: boolean?}};
		_blocked_methods: {[(...any) -> ...any]: boolean?};
		_virtualized_signals: {[string]: VirtualizedSignal};
		_proxy_map: {[any]: VirtualizedObject};
		
		add_blocked_classnames: (self: VirtualizedInstanceData, classNames: {string}) -> nil;
		is_classname_blocked: (self: VirtualizedInstanceData, className: string) -> boolean;
		
		add_blocked_class_properties: (self: VirtualizedInstanceData, className: string, properties: {string}) -> nil;
		is_class_property_blocked: (self: VirtualizedInstanceData, className: string, property: string) -> boolean;
		
		add_blocked_methods: (self: VirtualizedInstanceData, instance: Instance, blocked_methods: {string}) -> nil;
		is_method_blocked: (self: VirtualizedInstanceData, method: (...any) -> ...any) -> boolean;
		
		get_proxy: (self: VirtualizedInstanceData, proxy: any) -> VirtualizedObject | nil;
	}
	
	type VirtualizedEnvironmentData = {
		_environment: {[string]: any};
		
		add_native_globals: (self: VirtualizedEnvironmentData, names: {string}) -> nil;
		apply_global: (self: VirtualizedEnvironmentData, keys: {string}, value: any) -> nil;
		get_environment: (self: VirtualizedEnvironmentData) -> {[string]: any};
	}
	
	local instance_data: VirtualizedInstanceData = nil;
	local environment_data: VirtualizedEnvironmentData = nil;
	
	-- [[ Code Definitions ]]
	local function VirtualizeSignal(signal: RBXScriptSignal): (any, VirtualizedSignal)
		local wrapper: VirtualizedSignal = {
			_type = 'RBXScriptSignal',
			_proxy = newproxy(true),
			_signal = signal,
			
			get_proxy = function(self: VirtualizedObject): any
				return self._proxy
			end,

			__index = function(self: VirtualizedSignal, key: any): any				
				if key:lower() == "connect" or key:lower() == "connectparallel" or key:lower() == "once" then
					local method = (self._signal :: any)[key]
					if typeof(method) ~= "function" then
						return method
					end
					
					return function(callback)
						method(function(...)
							local event_input = instance_data:get_wrapped_value({...})
							if #event_input > 0 then
								callback(unpack(event_input))
							end
						end)	
					end
				else
					return (self._signal :: any)[key]
				end
			end,

			__newindex = function(self: VirtualizedSignal, key: any, value: any)
				-- Signals are read-only; no need for filtering
				(self._signal :: any)[key] = value
			end,
			

			__tostring = function(self: VirtualizedSignal) return tostring(self._signal) end,
			__eq = function(self: VirtualizedSignal, other) return self == other end,
			
			__call = function(self: VirtualizedSignal, ...)     error("attempt to call a RBXScriptSignal value") end,
			__concat = function(self: VirtualizedSignal, other) error(("attempt to concatenate RBXScriptSignal with %s"):format(typeof(other))) end,
			__unm = function(self: VirtualizedSignal)           error("attempt to perform arithmethic (unm) on RBXScriptSignal") end,
			__add = function(self: VirtualizedSignal, other)    error(("attempt to perform arithmetic (add) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__sub = function(self: VirtualizedSignal, other)    error(("attempt to perform arithmetic (sub) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__mul = function(self: VirtualizedSignal, other)    error(("attempt to perform arithmetic (mul) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__div = function(self: VirtualizedSignal, other)    error(("attempt to perform arithmetic (div) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__mod = function(self: VirtualizedSignal, other)    error(("attempt to perform arithmetic (mod) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__pow = function(self: VirtualizedSignal, other)    error(("attempt to perform arithmetic (pow) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__lt = function(self: VirtualizedSignal, other)     error(("attempt to compare RBXScriptSignal < %s"):format(typeof(other))) end,
			__le = function(self: VirtualizedSignal, other)     error(("attempt to compare RBXScriptSignal <= %s"):format(typeof(other))) end,
			__len = function(self: VirtualizedSignal)           error("attempt to get length of a RBXScriptSignal value") end,

		}

		-- Set up the proxy metatable
		local metatable = getmetatable(wrapper._proxy)
		metatable.__metatable = 'The metatable is locked' 
		for method_name, method_func in pairs(wrapper) do
			if method_name:sub(1, 2) == '__' then
				metatable[method_name] = function(...)
					method_func(wrapper, ...)
				end
			end
		end		

		return wrapper._proxy, wrapper
	end
	
	local function VirtualizeInstance(instance: Instance): (any, VirtualizedInstance)
		local wrapper: VirtualizedInstance = {
			_type = 'Instance',
			_proxy = newproxy(true),
			_instance = instance,
			
			get_proxy = function(self: VirtualizedObject): any
				return self._proxy
			end,
			
			__index = function(self: VirtualizedInstance, key: any): any
				if type(key) == string then
					if instance_data:is_class_property_blocked(self._instance.ClassName, key) then
						return error(string.format("The property by the name of '%s' is disabled.", key))
					end
				end
				
				local value = (self._instance :: any)[key]
				if typeof(value) == "function" then
					if instance_data:is_method_blocked(value) then
						return error(string.format("The method by the name of '%s' is disabled.", key))
					end
					
					return function(...)
						local function_return = {value(...)}
						function_return = instance_data:get_wrapped_value(function_return)
						return unpack(function_return)
					end
				elseif typeof(value) == "RBXScriptSignal" then
					return instance_data:get_wrapped_signal(value, self._instance:GetFullName() .. key):get_proxy()
				else
					return instance_data:get_wrapped_value(value)
				end
			end,
			
			__newindex = function(self: VirtualizedInstance, key: any, value: any)
				if type(key) == string then
					if instance_data:is_class_property_blocked(self._instance.ClassName, key:lower()) then
						return error(string.format("The property by the name of '%s' is disabled.", key))
					end
				end
				
				(self._instance :: any)[key] = value
			end,
			
			
			__tostring = function(self: VirtualizedInstance) return tostring(self._instance) end,
			__eq = function(self: VirtualizedInstance, other) return self == other end,
			
			--[[ The following metamethods will always throw an error like regular Instances ]]
			__call = function(self: VirtualizedInstance, ...)     error("attempt to call a Instance value") end,
			__concat = function(self: VirtualizedInstance, other) error(("attempt to concatenate Instance with %s"):format(typeof(other))) end,
			__unm = function(self: VirtualizedInstance)           error("attempt to perform arithmethic (unm) on Instance") end,
			__add = function(self: VirtualizedInstance, other)    error(("attempt to perform arithmetic (add) on Instance and %s"):format(typeof(other))) end,
			__sub = function(self: VirtualizedInstance, other)    error(("attempt to perform arithmetic (sub) on Instance and %s"):format(typeof(other))) end,
			__mul = function(self: VirtualizedInstance, other)    error(("attempt to perform arithmetic (mul) on Instance and %s"):format(typeof(other))) end,
			__div = function(self: VirtualizedInstance, other)    error(("attempt to perform arithmetic (div) on Instance and %s"):format(typeof(other))) end,
			__mod = function(self: VirtualizedInstance, other)    error(("attempt to perform arithmetic (mod) on Instance and %s"):format(typeof(other))) end,
			__pow = function(self: VirtualizedInstance, other)    error(("attempt to perform arithmetic (pow) on Instance and %s"):format(typeof(other))) end,
			__lt = function(self: VirtualizedInstance, other)     error(("attempt to compare Instance < %s"):format(typeof(other))) end,
			__le = function(self: VirtualizedInstance, other)     error(("attempt to compare Instance <= %s"):format(typeof(other))) end,
			__len = function(self: VirtualizedInstance)           error("attempt to get length of a Instance value") end,

		}

		-- Set up the proxy metatable
		local metatable = getmetatable(wrapper._proxy)
		metatable.__metatable = 'The metatable is locked' 
		for method_name, method_func in pairs(wrapper) do
			if method_name:sub(1, 2) == '__' then
				metatable[method_name] = function(...)
					method_func(wrapper, ...)
				end
			end
		end		
		
		return wrapper._proxy, wrapper
	end
	
	local instance_data: VirtualizedInstanceData = {
		_blocked_classnames = {};
		_blocked_class_properties = {};
		_blocked_methods = {};
		_virtualized_signals = {};
		_proxy_map = {};
		
		get_wrapped_value = function(self: VirtualizedInstanceData, value: any): any
			--[[ Filter an arbitrary value (i.e. values returned from the ROBLOX API) and wrap/block native types ]]
			if typeof(value) == "Instance" then
				if self:is_classname_blocked(value.ClassName) then
					return nil
				end
				
				-- Iterate through descendants for blocked classnames
				for _, instance: Instance in value:GetDescendants() do
					if self:is_classname_blocked(instance.ClassName) then
						instance:Destroy()
					end
				end
				
				return self:get_wrapped_instance(value):get_proxy()
			elseif typeof(value) == "table" then
				local output = {}
				for _, item in value do
					table.insert(output, self:get_wrapped_value(item))
				end
				
				return output
			else
				return value
			end			
		end,
		
		get_wrapped_instance = function(self: VirtualizedInstanceData, instance: Instance): VirtualizedInstance
			-- Check for a cache hit
			if self[instance] then return self[instance] end
			
			-- Instantiate the virtualized instance and add it to the cahe
			local proxy, wrapper = VirtualizeInstance(instance)
			self._proxy_map[proxy] = wrapper
			self[instance] = wrapper
			return proxy
		end,
		
		get_wrapped_signal = function(self: VirtualizedInstanceData, signal: RBXScriptSignal, signal_path: string): VirtualizedSignal
			-- Check if the signal path already exists. We need to use a path as the RBXScriptSignal type cannot be used as a key
			if self._virtualized_signals[signal_path] then return self._virtualized_signals[signal_path] end
			
			-- Instantiate the virtualized signal and add it to the cahe
			local proxy, wrapper = VirtualizeSignal(signal)
			self._proxy_map[proxy] = wrapper
			self._virtualized_signals[signal_path] = wrapper
			return proxy
		end,
		
		add_blocked_classnames = function(self: VirtualizedInstanceData, classNames: {string})
			for _, className in classNames do
				self._blocked_classnames[className:lower()] = true
			end
		end,
		
		is_classname_blocked = function(self: VirtualizedInstanceData, className: string): boolean
			return self._blocked_classnames[className:lower()] ~= nil			
		end,
		
		add_blocked_class_properties = function(self: VirtualizedInstanceData, className: string, properties: {string})
			if not self._blocked_class_properties[className:lower()] then 
				self._blocked_class_properties[className:lower()] = {}
			end
			
			for _, property in properties do
				self._blocked_class_properties[className:lower()][property:lower()] = true
			end
		end,
		
		is_class_property_blocked = function(self: VirtualizedInstanceData, className: string, property: string): boolean
			if not self._blocked_class_properties[className:lower()] then 
				return false
			end
			
			return self._blocked_class_properties[className:lower()][property:lower()] ~= nil
		end,
		
		add_blocked_methods = function(self: VirtualizedInstanceData, instance: Instance, methods: {string})
			-- Get the real method's function in-memory
			local lua_methods = {}
			for _, method in methods do
				local func, err = pcall(function()
					return (instance :: any)[method]
				end)
				assert(err == nil, string.format("Instance of type %s does not have a method %s", instance.ClassName, method))
				self._blocked_methods[func] = true
			end
		end,
		
		is_method_blocked = function(self: VirtualizedInstanceData, method: (...any) -> ...any): boolean
			return self._blocked_methods[method] ~= nil
		end,
		
		get_proxy = function(self: VirtualizedInstanceData, proxy: any): VirtualizedObject | nil
			if self._proxy_map[proxy] then
				return self._proxy_map[proxy]
			else
				return nil
			end
		end,
	}
	
	local environment_data: VirtualizedEnvironmentData = {
		_environment = {};
		
		add_native_globals = function(self: VirtualizedEnvironmentData, names: {string})
			for _, name in names do
				local global = getfenv(0)[name]
				assert(global, string.format("Global %s does not exist", name))
				self._environment[name] = global
			end
		end,
		
		apply_global = function(self: VirtualizedEnvironmentData, keys: {string}, value: any)
			for _, key in keys do
				self._environment[key] = value
			end
		end,
		
		get_environment = function(self: VirtualizedEnvironmentData)
			if not shouldVirtualize then
				return getfenv(0)
			end
			
			return self._environment
		end,
	}
	
	-- [[ Instance Filtering Definitions ]]
	instance_data:add_blocked_classnames({ --[[ Blocked instance types ]]
		"Script",
		"ModuleScript",
		"CoreScript",
		"NetworkClient",
		"NetworkMarker",
		"NetworkServer",
		"NetworkPeer",
		"NetworkReplicator",
		"NetworkSettings"
	})
	instance_data:add_blocked_classnames({ --[[ Blocked service types ]]
		"HttpRbxApiService"
	})
	instance_data:add_blocked_methods(game:GetService("HttpService"), {
		"HttpGetAsync", 
		"HttpPostAsync",
		"RequestAsync",
		"RequestInternal"
	})
	instance_data:add_blocked_methods(game, {
		"HttpGet", 
		"HttpAsync",
		"HttpPost",
		"HttpPostAsync",
		"Load"
	})
	instance_data:add_blocked_class_properties("HttpService", {"HttpEnabled"})
	instance_data:add_blocked_class_properties("ScriptContext", {"ScriptsEnabled"})
	
	--[[ Environment Definitions ]]
	environment_data:add_native_globals({ --[[ Lua Globals ]]
		"assert", "collectgarbage", "error", "getmetatable", "setmetatable", "ipairs", "pairs", "_G",
		"pcall", "print", "rawequal", "rawget", "rawset", "select", "tonumber", "tostring", "unpack", "xpcall"
	})
	environment_data:add_native_globals({ -- [[ Roblox Globals ]]
		"delay", "elapsedTime", "gcinfo", "spawn", "stats", "tick", "time", "wait", "warn", "Enum", "shared"
	})
	environment_data:add_native_globals({ -- [[ Libraries ]]
		"bit32", "coroutine", "math", "os", "string", "table", "task", "utf8"
	})
	environment_data:apply_global({"game", "Game"}, instance_data:get_wrapped_instance(game):get_proxy())
	environment_data:apply_global({"workspace", "Workspace"}, instance_data:get_wrapped_instance(workspace):get_proxy())
	environment_data:apply_global({"typeof"}, function(value: any)
		local object = instance_data:get_proxy(value) 
		if object then
			-- We're doing this to make our virtualized objects indistinguishable from their native types
			return object._type
		else
			return typeof(value)
		end
	end)
	environment_data:apply_global({"Instance"}, {
		new = function(instance_type)
			if instance_data:is_classname_blocked(instance_type) then
				return error(("The instance type by the name of '%s' is disabled."):format(instance_type))
			end
			
			return instance_data:get_wrapped_instance(Instance.new(instance_type)):get_proxy()
		end,
	})
	
	execution_env = environment_data:get_environment()
end

setfenv(ctx, execution_env)
local result = ctx()

if typeof(result) == "Instance" then
	result = tostring(result)
elseif typeof(result) == "table" then
	result = game:GetService("HttpService"):JSONEncode(result)
elseif result ~= nil then
	result = tostring(result)
end

return result -- This will actually make the check for LUA_TARRAY redundant.
