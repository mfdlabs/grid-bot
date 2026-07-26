-- TexturePack v1.0

local baseUrl, texturePackId = ...

local RCCTexturePackGenerator = game:GetService("RCCTexturePackGenerator")
RCCTexturePackGenerator:AddProfilingCheckpoint("RCCTexturePackGeneratorScriptStarted")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local result, requestedUrls = RCCTexturePackGenerator:Process(baseUrl, texturePackId)
RCCTexturePackGenerator:AddProfilingCheckpoint("RCCTexturePackGeneratorFinished")

return result, requestedUrls
