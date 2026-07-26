-- Utility module for functions related to loading mannequins for thumbnails

local MannequinUtility = {}

local InsertService = game:GetService("InsertService")

local R6_MANNEQUIN_CONTENT_ID    = "rbxasset://models/Thumbnails/Mannequins/R6.rbxmx"
local R15_MANNEQUIN_CONTENT_ID   = "rbxasset://models/Thumbnails/Mannequins/R15.rbxm"
local RTHRO_MANNEQUIN_CONTENT_ID = "rbxasset://models/Thumbnails/Mannequins/Rthro.rbxm"

local function loadMannequin(contentId)
    local mannequin = InsertService:LoadLocalAsset(contentId)
    mannequin.Humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
    mannequin.Parent = workspace

    return mannequin
end

function MannequinUtility.LoadR15Mannequin()
    return loadMannequin(R15_MANNEQUIN_CONTENT_ID)
end

function MannequinUtility.LoadR6Mannequin()
    return loadMannequin(R6_MANNEQUIN_CONTENT_ID)
end

function MannequinUtility.LoadRthroMannequin()
    return loadMannequin(RTHRO_MANNEQUIN_CONTENT_ID)
end

function MannequinUtility.LoadMannequinForScaleType(scaleType)
    if scaleType == "Classic" then
        return MannequinUtility.LoadR15Mannequin()
    else
        return MannequinUtility.LoadRthroMannequin()
    end
end

function MannequinUtility.RotateMannequin(mannequin, cframe)
    local humanoidRootPart = mannequin:FindFirstChild("HumanoidRootPart")
    if not humanoidRootPart then
        return
    end

    local rootRigAttachment = humanoidRootPart:FindFirstChild("RootRigAttachment")
    if rootRigAttachment then
        rootRigAttachment.CFrame = rootRigAttachment.CFrame * cframe
    end
end

return MannequinUtility