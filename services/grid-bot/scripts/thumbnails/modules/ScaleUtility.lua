-- Utility module for managing the scale of mannequins and items in thumbnails

local ScaleUtility = {}

local CLASSIC_SCALE = "Classic"
local RTHRO_NORMAL  = "ProportionsNormal"
local RTHRO_SLENDER = "ProportionsSlender"

local function getPartScaleType(part)
    local value = part:FindFirstChild("AvatarPartScaleType")
    if value then
        return value.Value
    end

    return CLASSIC_SCALE
end

function ScaleUtility.GetScaleTypeForAccessory(accessory)
    local handle = accessory:FindFirstChild("Handle")
    if not handle then
        return CLASSIC_SCALE
    end

    return getPartScaleType(handle)
end

function ScaleUtility.GetObjectsScaleType(objects)
    for _, object in pairs(objects) do
        local partScaleType = object:FindFirstChild("AvatarPartScaleType", --[[ recursive = ]] true)
        if partScaleType then
            return partScaleType.Value
        end
    end
end

local function getOrCreateScaleValue(humanoid, name, default)
    local scaleValue = humanoid:FindFirstChild(name)
    if scaleValue then
        return scaleValue
    end

    scaleValue = Instance.new("NumberValue")
    scaleValue.Name = name
    scaleValue.Value = default
    scaleValue.Parent = humanoid

    return scaleValue
end

function ScaleUtility.CreateProportionScaleValues(humanoid, scaleType)
    local bodyTypeValue = getOrCreateScaleValue(humanoid, "BodyTypeScale", 0)
    local bodyProportionValue = getOrCreateScaleValue(humanoid, "BodyProportionScale", 0)

    if scaleType == RTHRO_NORMAL then
        bodyTypeValue.Value = 1
        bodyProportionValue.Value = 0
    elseif scaleType == RTHRO_SLENDER then
        bodyTypeValue.Value = 1
        bodyProportionValue.Value = 1
    end
end

return ScaleUtility