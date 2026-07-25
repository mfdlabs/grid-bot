local assetUrl, fileExtension, x, y, baseUrl, mannequinId = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local mannequin = game:GetObjects(baseUrl.. "asset/?id=" .. tostring(mannequinId))[1]
mannequin.Humanoid.DisplayDistanceType = Enum.HumanoidDisplayDistanceType.None
mannequin.Parent = workspace

local pants = game:GetObjects(assetUrl)[1]
pants.Parent = mannequin

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true)

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