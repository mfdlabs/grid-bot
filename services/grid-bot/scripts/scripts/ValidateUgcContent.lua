local argumentsTable = ...

-- This flag enables local debugging. Debug mode will do the following:
-- Expects gameserver.txt to contain an assetId instead of assetHash + checksum
-- Provides output information for the result
local FFlagDebugUGCServerValidation = game:DefineFastFlag("DebugUGCServerValidation", false)

local ScriptContext = game:GetService("ScriptContext")
local HttpService = game:GetService("HttpService")
local CorePackages = game:GetService("CorePackages")
local ContentProvider = game:GetService("ContentProvider")

pcall(function() ContentProvider:SetBaseUrl(argumentsTable.BaseUrl) end)
ScriptContext.ScriptsDisabled = true

assert(CorePackages, "CorePackages is not loaded!")
local UGCValidation = require(CorePackages.UGCValidation)

local function parseBaseUrlInformation(baseUrl)
    -- keep a copy of the base url (https://www.roblox.com/)
    -- append a trailing slash if there isn't one
    if baseUrl:sub(#baseUrl) ~= "/" then
        baseUrl = baseUrl .. "/"
    end

    -- parse out scheme (http, https)
    local _, schemeEnd = baseUrl:find("://")

    -- parse out the prefix (www, kyle, ying, etc.)
    local prefixIndex, prefixEnd = baseUrl:find("%.", schemeEnd + 1)
    local basePrefix = baseUrl:sub(schemeEnd + 1, prefixIndex - 1)

    -- parse out the domain (roblox.com/, sitetest1.robloxlabs.com/, etc.)
    local baseDomain = baseUrl:sub(prefixEnd + 1)

    return baseUrl, basePrefix, baseDomain
end

local function createAssetGameUrl(baseDomain)
	return "https://assetgame." .. baseDomain
end

local _, _, BASE_DOMAIN = parseBaseUrlInformation(argumentsTable.BaseUrl)
local ASSET_GAME = createAssetGameUrl(BASE_DOMAIN)

local ASSET_TYPE_MAP = {
	Hat = Enum.AssetType.Hat,
	HairAccessory = Enum.AssetType.HairAccessory,
	FaceAccessory = Enum.AssetType.FaceAccessory,
	NeckAccessory = Enum.AssetType.NeckAccessory,
	ShoulderAccessory = Enum.AssetType.ShoulderAccessory,
	FrontAccessory = Enum.AssetType.FrontAccessory,
	BackAccessory = Enum.AssetType.BackAccessory,
	WaistAccessory = Enum.AssetType.WaistAccessory,
}

local function validateAsset(assetInfo)
	local assetUrl
	if FFlagDebugUGCServerValidation then
		assetUrl = ASSET_GAME .. string.format("asset/?id=%s", assetInfo.assetId)
	else
		assetUrl = ASSET_GAME .. string.format("asset/?marAssetHash=%s&marChecksum=%s", assetInfo.assetHash, assetInfo.checksum)
	end
	local objects = game:GetObjects(assetUrl)

	local assetTypeEnum = assert(ASSET_TYPE_MAP[assetInfo.assetType], "Invalid Asset Type")

	local success, reasons = UGCValidation.validate(objects, assetTypeEnum, true)
	if success then
		return true, "Success"
	else
		return false, table.concat(reasons, "\n")
	end
end

local assetsToValidate = argumentsTable.Assets
local resultData = {}

for i, assetInfo in ipairs(assetsToValidate) do
	local isValid, validationResult = validateAsset(assetInfo)

	resultData[i] = {
		assetHash = assetInfo.assetHash,
		isValid = isValid,
		validationResult = validationResult
	}
end

local resultJson = HttpService:JSONEncode(resultData)

if FFlagDebugUGCServerValidation then
	print("DEBUG", "resultData", resultJson)
end

return resultJson