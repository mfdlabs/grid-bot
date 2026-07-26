-- Pants v1.0.2

local assetUrl, fileExtension, x, y, baseUrl, mannequinId = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local mannequin = game:GetObjects(baseUrl.. "asset/?id=" .. tostring(mannequinId))[1]
mannequin.Humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
mannequin.Parent = workspace

ThumbnailGenerator:AddProfilingCheckpoint("MannequinLoaded")

local pants = game:GetObjects(assetUrl)[1]
pants.Parent = mannequin

ThumbnailGenerator:AddProfilingCheckpoint("ObjectsLoaded")

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

local DFFlagThrowErrorWhenRequestedURLFailed = settings():GetFFlag("ThrowErrorWhenRequestedURLFailed")
if DFFlagThrowErrorWhenRequestedURLFailed then
    local ContentProvider = game:GetService("ContentProvider")
    local failedRequests = ContentProvider:GetFailedRequests()
    if #failedRequests > 0 then
        local failedRequestString = "Asset failed to be requested:"
        for _,failedString in pairs(failedRequests) do
            failedRequestString = failedRequestString.." "..failedString
        end
        error(failedRequestString)
    end
end

return result, requestedUrls