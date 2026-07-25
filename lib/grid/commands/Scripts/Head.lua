local assetUrl, fileExtension, x, y, baseUrl, mannequinId = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")

local DFFlagHeadThumbnailMannequins = settings():GetFFlag("HeadThumbnailMannequins")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local objects = {}
local headScaleType
local mannequin

objects = game:GetObjects(assetUrl)

mannequin = game:GetObjects(baseUrl.. "asset/?id=" .. tostring(mannequinId))[1]
mannequin.Humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
mannequin.Parent = workspace

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

for _, child in pairs(mannequin:GetChildren()) do
	if child:IsA("BasePart") and child.Name ~= "Head" then
		child:Destroy()
	end
end

local shouldCrop = false

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true, shouldCrop)

return result, requestedUrls