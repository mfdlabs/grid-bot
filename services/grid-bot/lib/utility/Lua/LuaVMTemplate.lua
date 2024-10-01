--[[
File Name: LuaVM.lua
Written By: Liam Meshorer
Description: Disables specific things in the datamodel, by virtualizing the function environment
--]]

local execution_env = {}

local FVariable = {
	_values = {},

	get_variable = function(self, name)
		return self._values[name]
	end,

	get_variable_map = function(self, name)
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

	get_variable_list = function(self, name)
		local list_data = self._values[name]
		assert(list_data, ("requested FVariable %s does not exist"):format(name))

		if #list_data == 0 then return {} end

		local items = list_data:split(",")
		for i = 1, #items do
			items[i] = items[i]:gsub("%s+", "")
		end

		return items
	end,

	add_int = function(self, name, default)
		success, self._values[name] = pcall(game.DefineFastInt, game, name, default)
		if not success then
			self._values[name] = default
		end

		return self._values[name]
	end,

	add_flag = function(self, name, default)
		success, self._values[name] = pcall(game.DefineFastFlag, game, name, default)
		if not success then
			self._values[name] = default
		end

		return self._values[name]
	end,

	add_string = function(self, name, default)
		success, self._values[name] = pcall(game.DefineFastString, game, name, default)
		if not success then
			self._values[name] = default
		end

		return self._values[name]
	end,
}

local timeout = FVariable:add_int("LuaVMTimeout", 5)
local get_log_string = nil
local max_result_length = FVariable:add_int("LuaVMMaxResultLength", 4096)

do
	local max_log_length = FVariable:add_int("LuaVMMaxLogLength", 4096)
	local max_log_line_length = FVariable:add_int("LuaVMMaxLogLineLength", 200)

	local enable_log_message_prefixes = FVariable:add_flag("LuaVMEnableLogMessagePrefixes", true)

	FVariable:add_string("LuaVMBlacklistedClassNames", "")
	FVariable:add_string("LuaVMBlacklistedClassProperties", "")
	FVariable:add_string("LuaVMBlacklistedClassMethods", "")

	FVariable:add_string("LuaVMLuaGlobals", "pcall,wait,tostring")
	FVariable:add_string("LuaVMRobloxGlobals", "")
	FVariable:add_string("LuaVMLibraryGlobals", "coroutine,os,table")

	local setfenv = setfenv
	local getfenv = getfenv
	local setmetatable = setmetatable
	local getmetatable = getmetatable
	local type = type
	local select = select
	local tostring = tostring
	local newproxy = newproxy
	local game = game

	local args = ...

	if args['unit_test'] then
		local assert = function(v, ...) if not v then print(v, ...) end end
	end

	local vm_enabled_for_admins = FVariable:add_flag("LuaVMEnabledForAdmins", true)

	local user_is_admin = args['is_admin']

	-- Case here, we only run the virtualized environment if the user is not an admin or the feature is enabled for admins
	local should_virtualize = not user_is_admin or vm_enabled_for_admins

	--[[ Type Definitions ]]

	local instance_data = nil;
	local environment_data = nil;
	local log_data = nil;

	-- [[ Code Definitions ]]
	local function VirtualizeSignal(signal, instance_data)
		local wrapper = {
			_type = 'RBXScriptSignal',
			_proxy = newproxy(true),
			_signal = signal,
			_instance_data = instance_data,

			get_proxy = function(self)
				return self._proxy
			end,

			__index = function(self, key)
				if key:lower() == "wait" then
					return function(signal)
						if signal ~= self._proxy then
							return
						end
						local method = self._signal[key]
						method(self._signal)
					end
				end
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

			__newindex = function(self, key, value)
				-- Signals are read-only; no need for filtering
				self._signal[key] = value
			end,


			__tostring = function(self) return tostring(self._signal) end,

			__call = function(self, ...)     error("attempt to call a RBXScriptSignal value") end,
			__concat = function(self, other) error(("attempt to concatenate RBXScriptSignal with %s"):format(typeof(other))) end,
			__unm = function(self)           error("attempt to perform arithmethic (unm) on RBXScriptSignal") end,
			__add = function(self, other)    error(("attempt to perform arithmetic (add) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__sub = function(self, other)    error(("attempt to perform arithmetic (sub) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__mul = function(self, other)    error(("attempt to perform arithmetic (mul) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__div = function(self, other)    error(("attempt to perform arithmetic (div) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__mod = function(self, other)    error(("attempt to perform arithmetic (mod) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__pow = function(self, other)    error(("attempt to perform arithmetic (pow) on RBXScriptSignal and %s"):format(typeof(other))) end,
			__lt = function(self, other)     error(("attempt to compare RBXScriptSignal < %s"):format(typeof(other))) end,
			__le = function(self, other)     error(("attempt to compare RBXScriptSignal <= %s"):format(typeof(other))) end,
			__len = function(self)           error("attempt to get length of a RBXScriptSignal value") end,

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

	local function VirtualizeInstance(instance, instance_data)
		local wrapper = {
			_type = 'Instance',
			_proxy = newproxy(true),
			_instance = instance,
			_instance_data = instance_data,

			get_proxy = function(self)
				return self._proxy
			end,

			__index = function(self, key)
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

			__newindex = function(self, key, value)
				if type(key) == "string" then
					if self._instance_data:is_class_property_blocked(self._instance.ClassName, key:lower()) then
						return error(string.format("The property by the name of '%s' is disabled.", key))
					end
				end

				self._instance[key] = value
			end,

			__tostring = function(self) return tostring(self._instance) end,

			--[[ The following metamethods will always throw an error like regular Instances ]]
			__call = function(self, ...)     error("attempt to call a Instance value") end,
			__concat = function(self, other) error(("attempt to concatenate Instance with %s"):format(typeof(other))) end,
			__unm = function(self)           error("attempt to perform arithmethic (unm) on Instance") end,
			__add = function(self, other)    error(("attempt to perform arithmetic (add) on Instance and %s"):format(typeof(other))) end,
			__sub = function(self, other)    error(("attempt to perform arithmetic (sub) on Instance and %s"):format(typeof(other))) end,
			__mul = function(self, other)    error(("attempt to perform arithmetic (mul) on Instance and %s"):format(typeof(other))) end,
			__div = function(self, other)    error(("attempt to perform arithmetic (div) on Instance and %s"):format(typeof(other))) end,
			__mod = function(self, other)    error(("attempt to perform arithmetic (mod) on Instance and %s"):format(typeof(other))) end,
			__pow = function(self, other)    error(("attempt to perform arithmetic (pow) on Instance and %s"):format(typeof(other))) end,
			__lt = function(self, other)     error(("attempt to compare Instance < %s"):format(typeof(other))) end,
			__le = function(self, other)     error(("attempt to compare Instance <= %s"):format(typeof(other))) end,
			__len = function(self)           error("attempt to get length of a Instance value") end,

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

	local instance_data = {
		_blocked_classnames = {};
		_blocked_class_properties = {};
		_blocked_methods = {};
		_virtualized_signals = {};
		_proxy_map = {};

		get_wrapped_value = function(self, value)
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

		get_wrapped_instance = function(self, instance)
			-- Check for a cache hit
			if self[instance] then return self[instance] end

			-- Instantiate the virtualized instance and add it to the cahe
			local proxy, wrapper = VirtualizeInstance(instance, self)
			self._proxy_map[proxy] = wrapper
			self[instance] = wrapper
			return wrapper
		end,

		get_wrapped_signal = function(self, signal, signal_path)
			-- Check if the signal path already exists. We need to use a path as the RBXScriptSignal type cannot be used as a key
			if self._virtualized_signals[signal_path] then return self._virtualized_signals[signal_path] end

			-- Instantiate the virtualized signal and add it to the cahe
			local proxy, wrapper = VirtualizeSignal(signal, self)
			self._proxy_map[proxy] = wrapper
			self._virtualized_signals[signal_path] = wrapper
			return wrapper
		end,

		add_blocked_classnames = function(self, classNames)
			for _, className in pairs(classNames) do
				self._blocked_classnames[className:lower()] = true
			end
		end,

		is_classname_blocked = function(self, className)
			return self._blocked_classnames[className:lower()] ~= nil
		end,

		add_blocked_class_properties = function(self, className, properties)
			if not self._blocked_class_properties[className:lower()] then
				self._blocked_class_properties[className:lower()] = {}
			end

			for _, property in pairs(properties) do
				self._blocked_class_properties[className:lower()][property:lower()] = true
			end
		end,

		is_class_property_blocked = function(self, className, property)
			if not self._blocked_class_properties[className:lower()] then
				return false
			end

			return self._blocked_class_properties[className:lower()][property:lower()] ~= nil
		end,

		add_blocked_methods = function(self, instance, methods)
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

		is_method_blocked = function(self, method)
			return self._blocked_methods[method] ~= nil
		end,

		get_proxy = function(self, proxy)
			if self._proxy_map[proxy] then
				return self._proxy_map[proxy]
			else
				return nil
			end
		end,
	}

	local environment_data = {
		_environment = {};

		add_native_globals = function(self, names)
			for _, name in pairs(names) do
				local global = getfenv(0)[name]
				assert(global, string.format("Global %s does not exist", name))
				self._environment[name] = global
			end
		end,

		apply_global = function(self, keys, value)
			for _, key in pairs(keys) do
				self._environment[key] = value
			end
		end,

		get_environment = function(self)
			return self._environment
		end,
	}

	local log_data = {
		_data = '';
		_surplus_rows = 0;
		_cap_exceeded = false;

		get_log_string = function(self)
			local surplus_rows = ""
			if self._surplus_rows > 0 then
				surplus_rows = ("\n... (%d more lines)"):format(self._surplus_rows)
			end

			return self._data:sub(1, #self._data - 1) .. surplus_rows
		end,

		add_log = function(self, message_data, message_type)
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
				self._data ..= message .. "\n"
			end
			if #self._data >= max_log_length then
				self._cap_exceeded = true
			end
		end,
	}

	-- [[ Instance Filtering Definitions ]]
	if should_virtualize then

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

		environment_data:apply_global({"game", "Game"}, instance_data:get_wrapped_instance(game):get_proxy())
		environment_data:apply_global({"workspace", "Workspace"}, instance_data:get_wrapped_instance(workspace):get_proxy())
		environment_data:apply_global({"typeof"}, function(value)
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

		get_log_string = function()
			return log_data:get_log_string()
		end

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

		-- Set the execution environment
		execution_env = environment_data:get_environment()
		setfenv(1, execution_env)
	else
		get_log_string = function()
			return log_data:get_log_string()
		end

		local old_print = print
		local old_warn = warn
		local old_error = error

		print = function(...)
			log_data:add_log({...}, Enum.MessageType.MessageInfo)
			old_print(...)
		end
		
		warn = function(...)
			log_data:add_log({...}, Enum.MessageType.MessageWarning)
			old_warn(...)
		end

		error = function(...)
			log_data:add_log({...}, Enum.MessageType.MessageError)
			old_error(...)
		end

		print("LuaVM is disabled for this user, printing debug information:")
		print("User is admin:", user_is_admin)
		print("LuaVMEnabledForAdmins:", vm_enabled_for_admins)
		print("LuaVMEnabledForUser:", should_virtualize)
		print("LuaVMTimeout:", timeout)
		print("LuaVMMaxResultLength:", max_result_length)
		print("LuaVMMaxLogLength:", max_log_length)
		print("LuaVMMaxLogLineLength:", max_log_line_length)
		print("LuaVMEnableLogMessagePrefixes:", enable_log_message_prefixes)
		print("LuaVMBlacklistedClassNames:", FVariable:get_variable("LuaVMBlacklistedClassNames"))
		print("LuaVMBlacklistedClassProperties:", FVariable:get_variable("LuaVMBlacklistedClassProperties"))
		print("LuaVMBlacklistedClassMethods:", FVariable:get_variable("LuaVMBlacklistedClassMethods"))
		print("LuaVMLuaGlobals:", FVariable:get_variable("LuaVMLuaGlobals"))
		print("LuaVMRobloxGlobals:", FVariable:get_variable("LuaVMRobloxGlobals"))
	end
end

local ctx = function() local get_log_string = nil; local FVariable = nil; local execution_env = nil; local max_result_length = nil; local timeout = nil;
{0} 
end

local return_metadata = {}
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
