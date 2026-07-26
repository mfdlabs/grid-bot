-- Mesh v1.0.2

local assetUrl, fileExtension, x, y, baseUrl = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local part = Instance.new("Part")
part.Parent = workspace

local specialMesh = Instance.new("SpecialMesh")
specialMesh.MeshId = assetUrl
specialMesh.Parent = part

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true, --[[crop = ]] true)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls