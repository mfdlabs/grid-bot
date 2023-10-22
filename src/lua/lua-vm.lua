--[[
File Name: LuaVM.lua
Written By: Liam Meshorer
Description: Disables specific things in the datamodel, by virtualizing the function environment
--]]

local execution_env = {}

type FVariableManager = {
	_values: any;

	get_variable_map: (FVariableManager, string) -> any;
	get_variable_list: (FVariableManager, string) -> {string};
	add_int: (FVariableManager, string, number) -> number;
	add_flag: (FVariableManager, string, boolean) -> boolean;
	add_string: (FVariableManager, string, string) -> string;
}

local FVariable: FVariableManager = {
	_values = {},

	get_variable = function(self: FVariableManager, name: string): string | number | boolean
		return self._values[name]
	end,

	get_variable_map = function(self: FVariableManager, name: string)
		local map_data = self._values[name]
		assert(map_data, ("requested FVariable %s does not exist"):format(name))
		if #map_data == 0 then return {} end
		local rows, map = map_data:split("\n"), {}
		for _, row in pairs(rows) do
			local items = row:split(",")
			for i = 1, #items do
				items[i] = items[i]:gsub("%s+", "")
			end
			local key = items[1]
			table.remove(items, 1)
			map[key] = items
		end
		return map
	end,

	get_variable_list = function(self: FVariableManager, name: string): {string}
		local list_data = self._values[name]
		assert(list_data, ("requested FVariable %s does not exist"):format(name))
		if #list_data == 0 then return {} end
		local items = list_data:split(",")
		for i = 1, #items do
			items[i] = items[i]:gsub("%s+", "")
		end
		return items
	end,

	add_int = function(self: FVariableManager, name: string, default: number)
		success, self._values[name] = pcall(game.DefineFastInt, game, name, default)
		if not success then
			self._values[name] = default
		end

		return self._values[name]
	end,

	add_flag = function(self: FVariableManager, name: string, default: boolean)
		success, self._values[name] = pcall(game.DefineFastFlag, game, name, default)
		if not success then
			self._values[name] = default
		end

		return self._values[name]
	end,

	add_string = function(self: FVariableManager, name: string, default: string)
		success, self._values[name] = pcall(game.DefineFastString, game, name, default)
		if not success then
			self._values[name] = default
		end

		return self._values[name]
	end,
}

local max_result_length = FVariable:add_int("LuaVMMaxResultLength", 4096)

do
	local timeout = FVariable:add_int("LuaVMTimeout", 5)
	local max_log_length = FVariable:add_int("LuaVMMaxLogLength", 4096)
	local max_log_line_length = FVariable:add_int("LuaVMMaxLogLineLength", 200)
	local vm_enabled_for_admins = FVariable:add_flag("LuaVMEnabledForAdmins", true)

	local enable_log_message_prefixes = FVariable:add_flag("LuaVMEnableLogMessagePrefixes", true)

	FVariable:add_string("LuaVMBlacklistedClassNames", "")
	FVariable:add_string("LuaVMBlacklistedClassProperties", "")
	FVariable:add_string("LuaVMBlacklistedClassMethods", "")

	FVariable:add_string("LuaVMLuaGlobals", "pcall,wait,tostring")
	FVariable:add_string("LuaVMRobloxGlobals", "")
	FVariable:add_string("LuaVMLibraryGlobals", "coroutine,os,table")

	local args = ...
	local is_admin = args['is_admin']
	local should_virtualize = is_admin and vm_enabled_for_admins or true

	local setfenv = setfenv
	local getfenv = getfenv
	local setmetatable = setmetatable
	local getmetatable = getmetatable
	local type = type
	local select = select
	local tostring = tostring
	local newproxy = newproxy
	local game = game
	if args['unit_test'] then
		local assert = function(v, ...) if not v then print(v, ...) end end
	end

	--[[ Type Definitions ]]
	type VirtualizedObject = {
		_type: string;
		_proxy: any;
		_instance_data: VirtualizedInstanceData;

		get_proxy: (VirtualizedObject) -> any;
	}

	type VirtualizedSignal = {
		_signal: RBXScriptSignal;
	} & VirtualizedObject

	type VirtualizedInstance = {
		_instance: Instance;
	} & VirtualizedObject

	type VirtualizedInstanceData = {
		[Instance]: VirtualizedInstance,
		get_wrapped_instance: (VirtualizedInstanceData, Instance) -> VirtualizedInstance;
		get_wrapped_signal: (VirtualizedInstanceData, RBXScriptSignal, string) -> VirtualizedSignal;
		get_wrapped_value: (VirtualizedInstanceData, any) -> any;

		_blocked_classnames: {[string]: boolean?};
		_blocked_class_properties: {[string]: {[string]: boolean?}};
		_blocked_methods: {[({any}) -> any]: boolean?};
		_virtualized_signals: {[string]: VirtualizedSignal};
		_proxy_map: {[any]: VirtualizedObject};

		add_blocked_classnames: (VirtualizedInstanceData, {string}) -> nil;
		is_classname_blocked: (VirtualizedInstanceData, string) -> boolean;

		add_blocked_class_properties: (VirtualizedInstanceData, string, {string}) -> nil;
		is_class_property_blocked: (VirtualizedInstanceData, string, string) -> boolean;

		add_blocked_methods: (VirtualizedInstanceData, Instance, {string}) -> nil;
		is_method_blocked: (VirtualizedInstanceData, ({any}) -> {any}) -> boolean;

		get_proxy: (VirtualizedInstanceData, any) -> VirtualizedObject | nil;
	}

	type VirtualizedEnvironmentData = {
		_environment: {[string]: any};

		add_native_globals: (VirtualizedEnvironmentData, {string}) -> nil;
		apply_global: (VirtualizedEnvironmentData, {string}, any) -> nil;
		get_environment: (VirtualizedEnvironmentData) -> {[string]: any};
	}

	type LogData = {
		_data: string;
		_cap_exceeded: boolean;
		_surplus_rows: number;

		get_log_string: (LogData) -> string;
		add_log: (LogData, {string}, Enum.MessageType) -> nil;
	}

	local instance_data: VirtualizedInstanceData = nil;
	local environment_data: VirtualizedEnvironmentData = nil;
	local log_data: LogData = nil;

	-- [[ Code Definitions ]]
	local function VirtualizeSignal(signal: RBXScriptSignal, instance_data: VirtualizedInstanceData): (any, VirtualizedSignal)
		local wrapper: VirtualizedSignal = {
			_type = 'RBXScriptSignal',
			_proxy = newproxy(true),
			_signal = signal,
			_instance_data = instance_data,

			get_proxy = function(self: VirtualizedObject): any
				return self._proxy
			end,

			__index = function(self: VirtualizedSignal, key: any): any
				if key:lower() == "connect" or key:lower() == "connectparallel" or key:lower() == "once" then
					local method = self._signal[key]
					if typeof(method) ~= "function" then
						return method
					end

					return function(signal, callback)
						if signal ~= self._proxy then
							return
						end
						method(self._signal, function(...)
							local event_input = instance_data:get_wrapped_value({...})
							callback(unpack(event_input))
						end)
					end
				else
					return self._signal[key]
				end
			end,

			__newindex = function(self: VirtualizedSignal, key: any, value: any)
				-- Signals are read-only; no need for filtering
				self._signal[key] = value
			end,


			__tostring = function(self: VirtualizedSignal) return tostring(self._signal) end,

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
					local args = {...}
					table.remove(args, 1)
					return method_func(wrapper, table.unpack(args))
				end
			end
		end

		return wrapper._proxy, wrapper
	end

	local function VirtualizeInstance(instance: Instance, instance_data: VirtualizedInstanceData): (any, VirtualizedInstance)
		local wrapper: VirtualizedInstance = {
			_type = 'Instance',
			_proxy = newproxy(true),
			_instance = instance,
			_instance_data = instance_data,

			get_proxy = function(self: VirtualizedObject): any
				return self._proxy
			end,

			__index = function(self: VirtualizedInstance, key: any): any
				if type(key) == string then
					if self._instance_data:is_class_property_blocked(self._instance.ClassName, key) then
						return error(string.format("The property by the name of '%s' is disabled.", key))
					end
				end

				local value = self._instance[key]
				if typeof(value) == "function" then
					if self._instance_data:is_method_blocked(value) then
						return error(string.format("The method by the name of '%s' is disabled.", key))
					end

					return function(...)
						local args = {...}
						if args[1] == self._proxy then
							args[1] = self._instance
						end
						local function_return = {value(table.unpack(args))}
						function_return = self._instance_data:get_wrapped_value(function_return)
						return unpack(function_return)
					end
				elseif typeof(value) == "RBXScriptSignal" then
					return self._instance_data:get_wrapped_signal(value, self._instance:GetFullName() .. key):get_proxy()
				else
					return self._instance_data:get_wrapped_value(value)
				end
			end,

			__newindex = function(self: VirtualizedInstance, key: any, value: any)
				if type(key) == "string" then
					if self._instance_data:is_class_property_blocked(self._instance.ClassName, key:lower()) then
						return error(string.format("The property by the name of '%s' is disabled.", key))
					end
				end

				self._instance[key] = value
			end,

			__tostring = function(self: VirtualizedInstance) return tostring(self._instance) end,

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
					local args = {...}
					table.remove(args, 1)
					return method_func(wrapper, table.unpack(args))
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

				return self:get_wrapped_instance(value):get_proxy()
			elseif typeof(value) == "table" then
				local output = {}
				for _, item in pairs(value) do
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
			local proxy, wrapper = VirtualizeInstance(instance, self)
			self._proxy_map[proxy] = wrapper
			self[instance] = wrapper
			return wrapper
		end,

		get_wrapped_signal = function(self: VirtualizedInstanceData, signal: RBXScriptSignal, signal_path: string): VirtualizedSignal
			-- Check if the signal path already exists. We need to use a path as the RBXScriptSignal type cannot be used as a key
			if self._virtualized_signals[signal_path] then return self._virtualized_signals[signal_path] end

			-- Instantiate the virtualized signal and add it to the cahe
			local proxy, wrapper = VirtualizeSignal(signal, self)
			self._proxy_map[proxy] = wrapper
			self._virtualized_signals[signal_path] = wrapper
			return wrapper
		end,

		add_blocked_classnames = function(self: VirtualizedInstanceData, classNames: {string})
			for _, className in pairs(classNames) do
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

			for _, property in pairs(properties) do
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
			for _, method in pairs(methods) do
				local success, func = pcall(function()
					return instance[method]
				end)
				assert(success, string.format("Instance of type %s does not have a method %s", instance.ClassName, method))
				self._blocked_methods[func] = true
			end
		end,

		is_method_blocked = function(self: VirtualizedInstanceData, method: ({any}) -> {any}): boolean
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
			for _, name in pairs(names) do
				local global = getfenv(0)[name]
				assert(global, string.format("Global %s does not exist", name))
				self._environment[name] = global
			end
		end,

		apply_global = function(self: VirtualizedEnvironmentData, keys: {string}, value: any)
			for _, key in pairs(keys) do
				self._environment[key] = value
			end
		end,

		get_environment = function(self: VirtualizedEnvironmentData)
			if not should_virtualize then
				return getfenv(0)
			end

			return self._environment
		end,
	}

	local log_data: LogData = {
		_data = '';
		_surplus_rows = 0;
		_cap_exceeded = false;

		get_log_string = function(self: LogData): string
			local surplus_rows = ""
			if self._surplus_rows > 0 then
				surplus_rows = ("\n... (%d more lines)"):format(self._surplus_rows)
			end

			return self._data:sub(1, #self._data - 1) .. surplus_rows
		end,

		add_log = function(self, message_data: {string}, message_type: Enum.MessageType)
			if #message_data > 0 then
				for i = 1, #message_data do
					message_data[i] = tostring(message_data[i])
				end
			end
			local message = table.concat(message_data, " ")
			if self._cap_exceeded then
				self._surplus_rows += 1
				return
			end

			if #message > max_log_line_length then
				message = message:sub(0, max_log_line_length) .. ("... (%d more characters)"):format(#message - max_log_line_length)
			end

			local message_types = {
				[Enum.MessageType.MessageInfo] = 'INFO';
				[Enum.MessageType.MessageError] = 'ERROR';
				[Enum.MessageType.MessageWarning] = 'WARNING';
				[Enum.MessageType.MessageOutput] = 'INFO';
			}


			local log_milli = tostring(math.floor(math.fmod(os.clock(), 1) * 1000)):sub(0, 3)
			if #log_milli < 3 then
				log_milli = ("0"):rep(3 - #log_milli) .. log_milli
			end
			if enable_log_message_prefixes then
				self._data ..=  ("%s -- %s.%s -- %s\n"):format(message_types[message_type], os.date("%X"), log_milli, message)
			else
				self_data ..= message
			end
			if #self._data >= max_log_length then
				self._cap_exceeded = true
			end
		end,
	}

	-- [[ Instance Filtering Definitions ]]
	local blocked_classnames = FVariable:get_variable_list("LuaVMBlacklistedClassNames")
	instance_data:add_blocked_classnames(blocked_classnames)

	local blocked_methods = FVariable:get_variable_map("LuaVMBlacklistedClassMethods")
	for classname, methods in pairs(blocked_methods) do
		local success, obj
		if classname == "game" then
			success, obj = true, game
		end
		if not success then
			success, obj = pcall(game.GetService, game, classname)
		end
		if not success then
			success, obj = pcall(Instance.new, classname)
		end
		assert(success, ("unable to acquire object of type %s to block methods"):format(classname))
		instance_data:add_blocked_methods(obj, methods)
	end

	local blocked_properties = FVariable:get_variable_map("LuaVMBlacklistedClassProperties")
	for classname, properties in pairs(blocked_properties) do
		instance_data:add_blocked_class_properties(classname, properties)
	end

	--[[ Environment Definitions ]]
	environment_data:add_native_globals(FVariable:get_variable_list("LuaVMLuaGlobals"))
	environment_data:add_native_globals(FVariable:get_variable_list("LuaVMLibraryGlobals"))
	environment_data:add_native_globals(FVariable:get_variable_list("LuaVMRobloxGlobals"))

	environment_data:apply_global({"timeout"}, timeout)
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
	environment_data:apply_global({"get_log_string"}, function()
		return log_data:get_log_string()
	end)
	environment_data:apply_global({"print"}, function(...)
		log_data:add_log({...}, Enum.MessageType.MessageInfo)
	end)
	environment_data:apply_global({"warn"}, function(...)
		log_data:add_log({...}, Enum.MessageType.MessageWarning)
	end)
	environment_data:apply_global({"error"}, function(...)
		log_data:add_log({...}, Enum.MessageType.MessageError)
		error(...)
	end)
	execution_env = environment_data:get_environment()
	setfenv(1, execution_env)
end

local ctx = function() local get_log_string = nil; local FVariable = nil; local execution_env = nil; local max_result_length = nil; {0} end

type ReturnMetadata = {
	success: boolean?;
	execution_time: number?;
	error_message: string?;
	logs: string?;
}

local return_metadata: ReturnMetadata = {}
local result, success, finished, error_message, exec_time
local event = Instance.new('BindableEvent')
local exec_thread = coroutine.create(function()
	local start_time = os.clock()
	local output = {pcall(ctx)}
	exec_time = os.clock() - start_time
	success = output[1]
	if #output == 2 then
		result = output[2]
	else
		table.remove(output, 1)
		if #output > 0 then
			result = output
		end
	end
	event:Fire()
end)

local timeout_thread = coroutine.create(function()
	wait(timeout)
	exec_time = timeout
	event:Fire(true)
end)

event.Event:Connect(function(timeout)
	if timeout then
		return_metadata.error_message = "script exceeded timeout"
		return_metadata.success = false
	else
		if not success then
			local _, _, _, err = string.find(result, "(%[.*%]:%d+:)%s(.*)")
			if err then
				result = err
			end
			return_metadata.error_message, result = result, nil
		end
		return_metadata.success = success
	end
	return_metadata.execution_time = exec_time
	finished = true
end)

coroutine.resume(timeout_thread)
coroutine.resume(exec_thread)
repeat wait() until finished

if typeof(result) == "Instance" then
	result = tostring(result)
elseif typeof(result) == "table" then
	result = game:GetService("HttpService"):JSONEncode(result)
elseif result ~= nil then
	result = tostring(result)
end

if result and #result > max_result_length then
	result = result:sub(0, max_result_length)
end
return_metadata.logs = get_log_string()

return result, game:GetService("HttpService"):JSONEncode(return_metadata)
