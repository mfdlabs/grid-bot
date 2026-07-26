local Hat = {}

function Hat.Validate(loadedObjects)
	if #loadedObjects ~= 1 then
		return false, "InvalidStructure"
	end

	local instance = loadedObjects[1]

	if not instance:IsA("Accoutrement") then
		return false, "InvalidStructure"
	end

	-- TODO: Implement proper asset validation here
	local isValid = math.random() > 0.5
	local validationResult = isValid and "Success" or "TestingInvalid"

	return isValid, validationResult
end

return Hat