-- Avatar_R15_Standard v1.0.2
-- Pose R6 characters in the normal way.  For R15, have them in the same pose, and raise their arm up if they have gear.
-- Sample params:
--	baseUrl: "http://www.roblox.com/"
--	characterAppearanceUrl: "http://www.roblox.com/Asset/AvatarAccoutrements.ashx?AvatarHash=98925edb8aa60e39ba8a4f0bf8b71d6f&AssetIDs=3372792,9255011,20418682,68258723,158066137,232503325,244097060,248286896,264611665,376530220,376531012,376531300,376531703,376532000,624157131&ResolvedAvatarType=R15&Height=1&Width=0.75&Head=0.95&Depth=0.88"
--	fileExtension: "Png"
-- 	x: 1260
--	y: 1260

local baseUrl, characterAppearanceUrl, fileExtension, x, y = ...

local ThumbnailGenerator = game:GetService('ThumbnailGenerator')
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local player = game:GetService("Players"):CreateLocalPlayer(0)
player.CharacterAppearance = characterAppearanceUrl
player:LoadCharacterBlocking()
ThumbnailGenerator:AddProfilingCheckpoint("PlayerCharacterLoaded")

local function getJointBetween(part0, part1)
	for _, obj in pairs(part1:GetChildren()) do
		if obj:IsA("Motor6D") and obj.Part0 == part0 then
			return obj
		end
	end
end

local function doR15ToolPose(rig)
	local rightShoulderJoint = getJointBetween(rig.UpperTorso, rig.RightUpperArm)
	if rightShoulderJoint then
		rightShoulderJoint.C1 = rightShoulderJoint.C1 * CFrame.new(0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 1, 0):inverse()
	end
end

-- Raise right arm up to hold gear.
local character = player.Character
if character then
    if character:FindFirstChildOfClass("Tool") then
        local humanoid = character:FindFirstChildOfClass("Humanoid")
        if humanoid then
            if humanoid.RigType == Enum.HumanoidRigType.R6 then
                character.Torso['Right Shoulder'].CurrentAngle = math.rad(90)
            elseif humanoid.RigType == Enum.HumanoidRigType.R15 then
				doR15ToolPose(character)
            end
        end
    end
end

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls