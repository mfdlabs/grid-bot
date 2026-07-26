-- Hat v1.1.0

local assetUrl, fileExtension, x, y, baseUrl = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

local DFFlagHatThumbnailMannequins = settings():GetFFlag("HatThumbnailMannequins")

-- Modules
local CreateExtentsMinMax
local MannequinUtility
local ScaleUtility

if DFFlagHatThumbnailMannequins then
    CreateExtentsMinMax = require(ThumbnailGenerator:GetThumbnailModule("CreateExtentsMinMax"))
    MannequinUtility = require(ThumbnailGenerator:GetThumbnailModule("MannequinUtility"))
    ScaleUtility = require(ThumbnailGenerator:GetThumbnailModule("ScaleUtility"))
end

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local accoutrement = game:GetObjects(assetUrl)[1]
ThumbnailGenerator:AddProfilingCheckpoint("ObjectsLoaded")

local handle

if DFFlagHatThumbnailMannequins then
    local accoutrementScaleType = ScaleUtility.GetScaleTypeForAccessory(accoutrement)
    local mannequin = MannequinUtility.LoadMannequinForScaleType(accoutrementScaleType)

    ThumbnailGenerator:AddProfilingCheckpoint("MannequinLoaded")

    -- Rotate mannequin for back accessories
    handle = accoutrement:FindFirstChild("Handle")
    if handle and handle:FindFirstChild("BodyBackAttachment") then
        MannequinUtility.RotateMannequin(mannequin, CFrame.Angles(0, math.pi, 0))
    end

    -- Scale mannequin based on accoutrement scale type
    local humanoid = mannequin:FindFirstChild("Humanoid")
    if humanoid then
        ScaleUtility.CreateProportionScaleValues(humanoid, accoutrementScaleType)
        humanoid:BuildRigFromAttachments()
    end

    accoutrement.Parent = mannequin
else
    accoutrement.Parent = workspace
end

local focusParts = {}
local extentsMinMax

if DFFlagHatThumbnailMannequins then
    if handle then
        focusParts[#focusParts + 1] = handle

        local connectedParts = handle:GetConnectedParts()
        for _, part in pairs(connectedParts) do
            focusParts[#focusParts + 1] = part
        end
    end

    extentsMinMax = CreateExtentsMinMax(focusParts)
end

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true, --[[crop =]] true, extentsMinMax)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls