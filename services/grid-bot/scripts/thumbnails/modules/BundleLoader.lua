local BundleLoader = {}

local AssetService = game:GetService("AssetService")
local ThumbnailGenerator = game:GetService("ThumbnailGenerator")

local MannequinUtility = require(ThumbnailGenerator:GetThumbnailModule("MannequinUtility"))
local ScaleUtility = require(ThumbnailGenerator:GetThumbnailModule("ScaleUtility"))

local ARTIST_INTENT_FOLDER = "R15ArtistIntent"
local ASSET_URL = "asset/?id="

local function constructAssetUrl(baseUrl, assetId)
    return baseUrl ..ASSET_URL.. tostring(assetId)
end

function BundleLoader.LoadBundleAssets(baseUrl, bundleId)
    local bundleInfo = AssetService:GetBundleDetailsSync(bundleId)

    local contentIdsList = {}
    for _, itemInfo in pairs(bundleInfo.Items) do
		if itemInfo.Type == "Asset" then
		    local assetId = itemInfo.Id
            local assetUrl = constructAssetUrl(baseUrl, assetId)

            contentIdsList[#contentIdsList + 1] = assetUrl
        end
    end

    local objectsList = game:GetObjectsList(contentIdsList)

    local results = {}
    for _, objects in pairs(objectsList) do
        local assetFolder = Instance.new("Folder")

        for _, object in pairs(objects) do
            object.Parent = assetFolder
        end

        results[#results + 1] = assetFolder
    end

    return results
end

local function addPartsToCharacter(character, folder)
    for _, part in pairs(folder:GetChildren()) do
        local existingPart = character:FindFirstChild(part.Name)
        part.Parent = character

        if existingPart then
            existingPart:Destroy()
        end
    end
end

local function addMeshHeadToCharacter(character, mesh)
    local head = character:FindFirstChild("Head")
    if not head then
        return
    end

    local existingMesh = head:FindFirstChild("Mesh")
    if existingMesh then
        existingMesh:Destroy()
    end

	for _, child in pairs(mesh:GetChildren()) do
		if child:IsA("Vector3Value") and string.find(child.Name, "Attachment") then
            local attachment = head:FindFirstChild(child.Name)

            if not attachment then
                attachment = Instance.new("Attachment")
            end
			attachment.Name = child.Name
			attachment.Position = child.Value
			attachment.Parent = head
        end
	end

    mesh.Parent = head
end

local function addFaceToCharacter(character, face)
    local head = character:FindFirstChild("Head")
    if not head then
        return
    end

    local existingFace = head:FindFirstChild("face")
    if existingFace then
        existingFace:Destroy()
    end

    face.Parent = head
end

function BundleLoader.LoadBundleCharacter(baseUrl, bundleId)
    local bundleAssets = BundleLoader.LoadBundleAssets(baseUrl, bundleId)
    local character = MannequinUtility.LoadR15Mannequin()

    local scaleType = ScaleUtility.GetObjectsScaleType(bundleAssets)

    for _, loadedAsset in pairs(bundleAssets) do
        for _, item in pairs(loadedAsset:GetChildren()) do
            if item:IsA("Folder") and item.Name == ARTIST_INTENT_FOLDER then
                addPartsToCharacter(character, item)
            elseif not item:IsA("Folder") then
                if item:IsA("DataModelMesh") then
                    addMeshHeadToCharacter(character, item)
                elseif item:IsA("Decal") then
                    addFaceToCharacter(character, item)
                else
                    item.Parent = character
                end
            end
        end
	end

    local humanoid = character:FindFirstChildOfClass("Humanoid")
    if humanoid then
        ScaleUtility.CreateProportionScaleValues(humanoid, scaleType)
        humanoid:BuildRigFromAttachments()
    end

    return character
end

return BundleLoader