-- Head v1.2.0

local assetUrl, fileExtension, x, y, baseUrl, mannequinId = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

local DFFlagHeadThumbnailMannequins = settings():GetFFlag("HeadThumbnailMannequins")
local FFlagMeshPartHeadOption = settings():GetFFlag("MeshPartHeadOption")

-- Modules
local CreateExtentsMinMax
local MannequinUtility
local ScaleUtility

if DFFlagHeadThumbnailMannequins or FFlagMeshPartHeadOption then
	CreateExtentsMinMax = require(ThumbnailGenerator:GetThumbnailModule("CreateExtentsMinMax"))
	MannequinUtility = require(ThumbnailGenerator:GetThumbnailModule("MannequinUtility"))
	ScaleUtility = require(ThumbnailGenerator:GetThumbnailModule("ScaleUtility"))
end

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local objects = {}
local headScaleType
local mannequin

if FFlagMeshPartHeadOption then
	local HeadMannequin = MannequinUtility.LoadMannequinForScaleType("Classic")
	HeadMannequin.Parent = workspace
	local mcd = HeadMannequin.Humanoid:GetAppliedDescription()
	
	if mcd then
		local HeadId = assetUrl:match("*?id=(%d+)")
		mcd.Head = HeadId
		
		local gray = BrickColor.Gray()
		mcd.HeadColor = Color3.new(gray.r, gray.g, gray.b)
		HeadMannequin.Humanoid:ApplyDescriptionBlocking(mcd)
		if HeadMannequin.Head:IsA("MeshPart") then
			objects[1] = HeadMannequin.Head
		else
			objects[1] = HeadMannequin.Head.Mesh
		end
	end	
	HeadMannequin.Parent = nil
else
	objects = game:GetObjects(assetUrl)
end

ThumbnailGenerator:AddProfilingCheckpoint("ObjectsLoaded")

if DFFlagHeadThumbnailMannequins then
	headScaleType = ScaleUtility.GetObjectsScaleType(objects)
	mannequin = MannequinUtility.LoadMannequinForScaleType(headScaleType)
else
	mannequin = game:GetObjects(baseUrl.. "asset/?id=" .. tostring(mannequinId))[1]
	mannequin.Humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
	mannequin.Parent = workspace
end

ThumbnailGenerator:AddProfilingCheckpoint("MannequinLoaded")

local function addFaceDecal(head)
	if head:FindFirstChild("face") then
		return
	end

	local face = Instance.new("Decal")
	face.Name = "face"
	face.Texture = "rbxasset://textures/face.png"
	face.Parent = head
end

local function replaceMannequinMeshPartHead(meshPartHead, meshHead)
	local newHead = Instance.new("Part")
	newHead.Size = meshPartHead.Size
	newHead.CFrame = meshPartHead.CFrame
	newHead.Color = meshPartHead.Color
	newHead.Name = "Head"

	addFaceDecal(newHead)

	local copiedAttachments = false
	for _, child in pairs(meshHead:GetChildren()) do
		if child:IsA("Vector3Value") and string.find(child.Name, "Attachment") then
			copiedAttachments = true

			local newAttachment = Instance.new("Attachment")
			newAttachment.Name = child.Name
			newAttachment.Position = child.Value
			newAttachment.Parent = newHead
		end
	end

	if not copiedAttachments then
		for _, child in pairs(meshPartHead:GetChildren()) do
			child.Parent = newHead
		end
	end

	meshPartHead:Destroy()
	newHead.Parent = mannequin
end

local function replaceMannequinHeadWithMeshHead()
	for _, obj in pairs(objects) do
		if obj:IsA("Folder") and obj.Name == "R15ArtistIntent" then
			local head = obj.Head
			addFaceDecal(head)
			mannequin.Head:Destroy()
			head.Parent = mannequin
		end
	end
end

local headObject = objects[1]

if headObject:IsA("Folder") then
	replaceMannequinHeadWithMeshHead()
else
	if DFFlagHeadThumbnailMannequins then
		if mannequin.Head:IsA("MeshPart") then
			replaceMannequinMeshPartHead(mannequin.Head, headObject)
		end
	else
		if headObject:IsA("MeshPart") then
			replaceMannequinMeshPartHead(mannequin.Head, headObject)
		else
			mannequin.Head.BrickColor = BrickColor.Gray()
		end
	end

	if not headObject:IsA("MeshPart") then
		if mannequin.Head:FindFirstChild("Mesh") then
			mannequin.Head.Mesh:Destroy()
		end
		headObject.Parent = mannequin.Head
	else
		addFaceDecal(headObject)
		mannequin.Head:Destroy()
		headObject.Parent = mannequin
	end
end

if DFFlagHeadThumbnailMannequins then
	-- Scale mannequin based on the scale type of the Head
	local humanoid = mannequin:FindFirstChild("Humanoid")
	if humanoid then
		ScaleUtility.CreateProportionScaleValues(humanoid, headScaleType)
		humanoid:BuildRigFromAttachments()
	end
else
	for _, child in pairs(mannequin:GetChildren()) do
		if child:IsA("BasePart") and child.Name ~= "Head" then
			child:Destroy()
		end
	end
end

local shouldCrop = false
local extentsMinMax

if DFFlagHeadThumbnailMannequins then
	local focusParts = {
		mannequin:FindFirstChild("Head")
	}

	shouldCrop = #focusParts > 0
	extentsMinMax = CreateExtentsMinMax(focusParts)
end

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true, shouldCrop, extentsMinMax)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls