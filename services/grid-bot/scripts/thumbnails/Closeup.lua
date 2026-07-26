-- Closeup v1.0.3
-- Used for avatar closeup (aka "headshot")
local baseUrl, characterAppearanceUrl, fileExtension, x, y, quadratic, baseHatZoom, maxHatZoom, cameraOffsetX, cameraOffsetY = ...

local FFlagOnlyCheckHeadAccessoryInHeadShot = game:DefineFastFlag("OnlyCheckHeadAccessoryInHeadShot", false)
local FFlagNewHeadshotLighting = game:DefineFastFlag("NewHeadshotLighting", false)

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

pcall(function() game:GetService('ContentProvider'):SetBaseUrl(baseUrl) end)
game:GetService('ScriptContext').ScriptsDisabled = true

local player = game:GetService("Players"):CreateLocalPlayer(0)
player.CharacterAppearance = characterAppearanceUrl
player:LoadCharacterBlocking()

ThumbnailGenerator:AddProfilingCheckpoint("PlayerCharacterLoaded")

local headAttachments = {}
if FFlagOnlyCheckHeadAccessoryInHeadShot then
    if player.Character:FindFirstChild("Head") then
	    for _,child in pairs(player.Character.Head:GetChildren()) do
		    if child:IsA("Attachment") then
			    headAttachments[child.Name] = true
		    end
	    end
    end
end

local maxDimension = 0

if player.Character then
	-- Remove gear
	for _, child in pairs(player.Character:GetChildren()) do
		if child:IsA("Tool") then
			child:Destroy()
		elseif child:IsA("Accoutrement") then
            local handle = child:FindFirstChild("Handle")
			if handle then
				local attachment = handle:FindFirstChildWhichIsA("Attachment")
                --legacy hat does not have attachment in it and should be considered when zoom out camera
				if not FFlagOnlyCheckHeadAccessoryInHeadShot or not attachment or headAttachments[attachment.Name] then
					local size = handle.Size / 2 + handle.Position - player.Character.Head.Position
					local xy = Vector2.new(size.x, size.y)
					if xy.magnitude > maxDimension then
						maxDimension = xy.magnitude
					end
				end
			end
		end
	end

	-- Setup Camera
	local maxHatOffset = 0.5 -- Maximum amount to move camera upward to accomodate large hats
	maxDimension = math.min(1, maxDimension / 3) -- Confine maxdimension to specific bounds

	if quadratic then
		maxDimension = maxDimension * maxDimension -- Zoom out on quadratic interpolation
	end

	local viewOffset     = player.Character.Head.CFrame * CFrame.new(cameraOffsetX, cameraOffsetY + maxHatOffset * maxDimension, 0.1) -- View vector offset from head

	local yAngle = -math.pi / 16
	if FFlagNewHeadshotLighting then
		yAngle = 0 -- Camera is looking straight at avatar's face.
	end
	local positionOffset = player.Character.Head.CFrame + (CFrame.Angles(0, yAngle, 0).lookVector.unit * 3) -- Position vector offset from head

	local camera = Instance.new("Camera", player.Character)
	camera.Name = "ThumbnailCamera"
	camera.CameraType = Enum.CameraType.Scriptable
	camera.CoordinateFrame = CFrame.new(positionOffset.p, viewOffset.p)
	camera.FieldOfView = baseHatZoom + (maxHatZoom - baseHatZoom) * maxDimension

	if FFlagNewHeadshotLighting then
		-- New lighting setup: we want a light slightly in front of, to the right, and above the character.
		-- Adding Part to be anchor of light. For 3D thumbnails (like full avatar) we should be careful about adding parts as this can affect the bounds.
		local part = Instance.new("Part")
		part.Parent = game.Workspace
		part.Anchored = true
		part.Transparency = 1

		local light = Instance.new("PointLight")
		light.Color = Color3.new(255/255, 255/255, 255/255)
		light.Brightness = 3
		light.Range = 10
		light.Parent = part
		light.Shadows = true

		part.Position = Vector3.new(-5,110,-5)
	end
end

local result, requestedUrls = ThumbnailGenerator:Click(fileExtension, x, y, --[[hideSky = ]] true)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls