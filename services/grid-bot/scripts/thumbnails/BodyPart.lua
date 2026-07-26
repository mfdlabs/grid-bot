-- BodyPart v1.0.6
-- See http://wiki.roblox.com/index.php?title=R15_Compatibility_Guide#Package_Parts for details on how body parts work with R15

local assetUrl, baseUrl, fileExtension, x, y, R6RigUrl, customUrl = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

local DFFlagBodyPartFocusInThreeDThumbnails = settings():GetFFlag("BodyPartFocusInThreeDThumbnails")

local CreateExtentsMinMax
local MannequinUtility
local ScaleUtility

if DFFlagBodyPartFocusInThreeDThumbnails then
	CreateExtentsMinMax = require(ThumbnailGenerator:GetThumbnailModule("CreateExtentsMinMax"))
	MannequinUtility = require(ThumbnailGenerator:GetThumbnailModule("MannequinUtility"))
	ScaleUtility = require(ThumbnailGenerator:GetThumbnailModule("ScaleUtility"))
end

pcall(function() game:GetService('ContentProvider'):SetBaseUrl(baseUrl) end)
game:GetService('ScriptContext').ScriptsDisabled = true

local objects = game:GetObjects(assetUrl)
ThumbnailGenerator:AddProfilingCheckpoint("BodyPartLoaded")

local useR15 = false
local useR15NewNames = false
local bodyPartProportion = "Classic"
local floatMax = math.huge

if DFFlagBodyPartFocusInThreeDThumbnails then
	for _, object in pairs(objects) do
		if object:IsA("Folder") then
			if object.Name == "R15" then
				useR15 = true
			elseif object.Name == "R15ArtistIntent" then
				useR15 = true
				useR15NewNames = true
			end
		end
	end

	bodyPartProportion = ScaleUtility.GetObjectsScaleType(objects)
else
	for _, object in pairs(objects) do
		if object:IsA("Folder") and (object.Name == "R15" or object.Name == "R15ArtistIntent") then
			useR15 = true

			-- Check to see if there are native scale parts in this object
			local partScaleType = object:FindFirstChild("AvatarPartScaleType", true)
			if partScaleType then
				bodyPartProportion = partScaleType.Value
			end
			break
		end
	end
end

local mannequin
if DFFlagBodyPartFocusInThreeDThumbnails then
	if useR15 then
		mannequin = MannequinUtility.LoadMannequinForScaleType(bodyPartProportion)
	else
		mannequin = MannequinUtility.LoadR6Mannequin()
	end
else
	local R15RigUrl = "http://www.roblox.com/asset/?id=516159357"
	for _, object in pairs(objects) do
		if object:IsA("Folder") and object.Name == "R15ArtistIntent" then
			useR15NewNames = true
			if bodyPartProportion == "Classic" then
				R15RigUrl = "http://www.roblox.com/asset/?id=1664543044"
			else
				R15RigUrl = "http://www.roblox.com/asset/?id=2337256345"
			end
			break
		end
	end

	if useR15 then
		mannequin = game:GetObjects(R15RigUrl)[1]
	else
		mannequin = game:GetObjects(R6RigUrl)[1]
	end

	mannequin.Humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
	mannequin.Parent = workspace
end

ThumbnailGenerator:AddProfilingCheckpoint("MannequinLoaded")

game:GetObjects(customUrl)[1].Parent = mannequin

ThumbnailGenerator:AddProfilingCheckpoint("CustomUrlLoaded")

local function addFolderChildren(folder, focusPartNamesOut, focusPartsOut)
	for _, child in pairs(folder:GetChildren()) do
		local existingBodyPart = mannequin:FindFirstChild(child.Name)
		if existingBodyPart then
			existingBodyPart:Destroy()
		end
		child.Parent = mannequin
		table.insert(focusPartNamesOut, child.name)
		table.insert(focusPartsOut, child)
	end
end

local r15FolderName = "R15"
if (useR15 and useR15NewNames) then
	r15FolderName = "R15ArtistIntent"
end

local focusParts = {}
local focusPartNames = {}

for _, object in pairs(objects) do
	if useR15 and object:IsA("Folder") and object.Name == r15FolderName then
		addFolderChildren(object, focusPartNames, focusParts)
	elseif not useR15 and object:IsA("Folder") and object.Name == "R6" then
		addFolderChildren(object, focusPartNames, focusParts)
	elseif not (object:IsA("Folder") and string.find(object.Name, "R15")) then  -- There will now be MULTIPLE R15 Folders. Ignore the ones we didn't search for.
		object.Parent = mannequin
	end
end

local function buildJoint(parentAttachment, partForJointAttachment)
    local jointName = parentAttachment.Name:gsub("RigAttachment", "")
    local motor = partForJointAttachment.Parent:FindFirstChild(jointName)
    if not motor then
        motor = Instance.new("Motor6D")
    end
	motor.Name = jointName

    motor.Part0 = parentAttachment.Parent
    motor.Part1 = partForJointAttachment.Parent

	motor.C0 = parentAttachment.CFrame
    motor.C1 = partForJointAttachment.CFrame

    motor.Parent = partForJointAttachment.Parent
end

-- Builds an R15 rig from the attachments in the parts
local function buildRigFromAttachments(currentPart, lastPart)
	local validSiblings = {}
	for _, sibling in pairs(currentPart.Parent:GetChildren()) do
		-- Don't find matching attachment in the current part being processed.
		-- Don't visit the last part visited again, this would cause an infinite loop.
		if sibling:IsA("BasePart") and sibling ~= currentPart and sibling ~= lastPart then
			table.insert(validSiblings, sibling)
		end
	end

	local function processRigAttachment(attachment)
		for _, sibling in pairs(validSiblings) do
			local matchingAttachment = sibling:FindFirstChild(attachment.Name)
			if matchingAttachment then
				buildJoint(attachment, matchingAttachment)
				buildRigFromAttachments(matchingAttachment.Parent, currentPart)
			end
		end
	end

	for _, object in pairs(currentPart:GetChildren()) do
		if object:IsA("Attachment") and string.find(object.Name, "RigAttachment") then
			processRigAttachment(object)
		end
	end
end

if useR15 then
	if DFFlagBodyPartFocusInThreeDThumbnails then
		local humanoid = mannequin:FindFirstChild("Humanoid")
		if humanoid then
			ScaleUtility.CreateProportionScaleValues(humanoid, bodyPartProportion)
			humanoid:BuildRigFromAttachments()
		end
	else
		-- Build R15 rig
		local humanoidRootPart = mannequin:WaitForChild("HumanoidRootPart")
		humanoidRootPart.CFrame = CFrame.new(Vector3.new(0, 5, 0)) * CFrame.Angles(0, math.pi, 0)
		humanoidRootPart.Anchored = true
		buildRigFromAttachments(humanoidRootPart)
		local humanoid = mannequin:WaitForChild("Humanoid")
		if humanoid then
			local typeObject = humanoid:FindFirstChild("BodyTypeScale")

			if typeObject == nil then
				typeObject = Instance.new("NumberValue")
				typeObject.Name = "BodyTypeScale"
				typeObject.Value = 0
				typeObject.Parent = humanoid
			end

			local proportionObject = humanoid:FindFirstChild("BodyProportionScale")
			if proportionObject == nil then
				proportionObject = Instance.new("NumberValue")
				proportionObject.Name = "BodyProportionScale"
				proportionObject.Value = 0
				proportionObject.Parent = humanoid
			end

			if bodyPartProportion == "ProportionsNormal" then
				typeObject.Value = 1
				proportionObject.Value = 0
			elseif bodyPartProportion == "ProportionsSlender" then
				typeObject.Value = 1
				proportionObject.Value = 1
			end
		end
	end
end

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

local extentsMinMax
local shouldCrop = false

if DFFlagBodyPartFocusInThreeDThumbnails then
	extentsMinMax = CreateExtentsMinMax(focusParts)
	shouldCrop = #focusParts > 0
else
	local focusOnExtents = { minx = floatMax, miny = floatMax, minz = floatMax, maxx = -floatMax, maxy = -floatMax, maxz =  -floatMax }

	local FFlagThumbnailSupportFocusOnPart = settings():GetFFlag("ThumbnailSupportFocusOnPart")
	if FFlagThumbnailSupportFocusOnPart and string.lower(fileExtension) == "png" then
		-- expand focusOnExtents to bound all the part(s) in the focusPartNames table
		if #focusPartNames > 0 then
			for _, focusPartName in pairs(focusPartNames) do
				local focusPart = mannequin:FindFirstChild(focusPartName, --[[recursive = ]] true)
				if focusPart then
					local partPosition = focusPart.Position
					local partRotation = focusPart.Rotation
					local halfPartSize = focusPart.Size / 2.0
					local partCFrame = CFrame.Angles(math.rad(partRotation.x), math.rad(partRotation.y), math.rad(partRotation.z)) + partPosition
					addCornerToBounds(partCFrame, Vector3.new( 1, 1, 1), halfPartSize, focusOnExtents)
					addCornerToBounds(partCFrame, Vector3.new( 1, 1,-1), halfPartSize, focusOnExtents)
					addCornerToBounds(partCFrame, Vector3.new( 1,-1, 1), halfPartSize, focusOnExtents)
					addCornerToBounds(partCFrame, Vector3.new( 1,-1,-1), halfPartSize, focusOnExtents)
					addCornerToBounds(partCFrame, Vector3.new(-1, 1, 1), halfPartSize, focusOnExtents)
					addCornerToBounds(partCFrame, Vector3.new(-1, 1,-1), halfPartSize, focusOnExtents)
					addCornerToBounds(partCFrame, Vector3.new(-1,-1, 1), halfPartSize, focusOnExtents)
					addCornerToBounds(partCFrame, Vector3.new(-1,-1,-1), halfPartSize, focusOnExtents)
					shouldCrop = true
				end
			end
		end
	end

	extentsMinMax = {
		Vector3.new(focusOnExtents["minx"], focusOnExtents["miny"], focusOnExtents["minz"]),
		Vector3.new(focusOnExtents["maxx"], focusOnExtents["maxy"], focusOnExtents["maxz"])
	}
end

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true, --[[crop = ]] shouldCrop, extentsMinMax)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls