-- Gear v1.0.3

local assetUrl, fileExtension, x, y, baseUrl = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

for _, object in pairs(game:GetObjects(assetUrl)) do
	object.Parent = workspace
end

ThumbnailGenerator:AddProfilingCheckpoint("ObjectsLoaded")

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true, --[[crop =]] true)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls