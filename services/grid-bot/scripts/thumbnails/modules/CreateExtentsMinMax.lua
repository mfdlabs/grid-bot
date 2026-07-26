-- Utility function for focusing on a selection of parts in a thumbnail

local FLOAT_MAX = math.huge

-- 10% tolerance for meshes that are bigger than the part which contains them
local MESH_SIZE_TOLERANCE_MULTIPLIER = 1.1

local function addToBounds(cornerPosition, focusExtentsOut)
	focusExtentsOut["minx"] = math.min(focusExtentsOut["minx"], cornerPosition.x)
	focusExtentsOut["miny"] = math.min(focusExtentsOut["miny"], cornerPosition.y)
	focusExtentsOut["minz"] = math.min(focusExtentsOut["minz"], cornerPosition.z)
	focusExtentsOut["maxx"] = math.max(focusExtentsOut["maxx"], cornerPosition.x)
	focusExtentsOut["maxy"] = math.max(focusExtentsOut["maxy"], cornerPosition.y)
	focusExtentsOut["maxz"] = math.max(focusExtentsOut["maxz"], cornerPosition.z)
end

local function addCornerToBounds(partCFrame, cornerSelect, halfPartSize, focusExtentsOut)
	local cornerPositionLocal = cornerSelect * halfPartSize
	local cornerPositionWorld = partCFrame * cornerPositionLocal
	addToBounds(cornerPositionWorld, focusExtentsOut)
end

-- Adds a tolerance for meshes in parts
local function getPartSizeBounds(part)
	local mesh = part:FindFirstChildWhichIsA("DataModelMesh")
	if not mesh then
		return part.Size
	end

	return part.Size * MESH_SIZE_TOLERANCE_MULTIPLIER
end

local CORNERS = {
	Vector3.new( 1,  1,  1),
	Vector3.new( 1,  1, -1),
	Vector3.new( 1, -1,  1),
	Vector3.new( 1, -1, -1),
	Vector3.new(-1,  1,  1),
	Vector3.new(-1,  1, -1),
	Vector3.new(-1, -1,  1),
	Vector3.new(-1, -1, -1),
}

local function CreateExtentsMinMax(focusParts)
	local focusOnExtents = {
		minx =  FLOAT_MAX,
		miny =  FLOAT_MAX,
		minz =  FLOAT_MAX,
		maxx = -FLOAT_MAX,
		maxy = -FLOAT_MAX,
		maxz = -FLOAT_MAX
	}

	-- Expand focusOnExtents to bound all the parts in the focusParts table
	for _, focusPart in ipairs(focusParts) do
		if focusPart:IsA("BasePart") then
			local partSize = getPartSizeBounds(focusPart)
			local halfPartSize = partSize * 0.5
			local partCFrame = focusPart.CFrame

			for _, corner in ipairs(CORNERS) do
				addCornerToBounds(partCFrame, corner, halfPartSize, focusOnExtents)
			end
		end
	end

	local extentsMinMax = {
		Vector3.new(focusOnExtents["minx"], focusOnExtents["miny"], focusOnExtents["minz"]),
		Vector3.new(focusOnExtents["maxx"], focusOnExtents["maxy"], focusOnExtents["maxz"])
	}

	return extentsMinMax
end

return CreateExtentsMinMax