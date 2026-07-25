local assetUrl, fileExtension, x, y, baseUrl = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local part = Instance.new("Part")
part.Parent = workspace

local specialMesh = Instance.new("SpecialMesh")
specialMesh.MeshId = assetUrl
specialMesh.Parent = part

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true, --[[crop = ]] true)

return result, requestedUrls