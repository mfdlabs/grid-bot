local characterAppearanceUrl, baseUrl, fileExtension, x, y = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local player = game:GetService("Players"):CreateLocalPlayer(0)
player.CharacterAppearance = characterAppearanceUrl
player:LoadCharacterBlocking()

-- Raise up the character's arm if they have gear.
if player.Character then
	for _, child in pairs(player.Character:GetChildren()) do
		if child:IsA("Tool") then
			player.Character.Torso["Right Shoulder"].CurrentAngle = math.rad(90)
			break
		end
	end
end

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true)

return result, requestedUrls