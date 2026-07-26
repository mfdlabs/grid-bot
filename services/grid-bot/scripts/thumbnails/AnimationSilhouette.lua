-- AnimationSilhouette.lua
-- Generates a Silhouette of a character doing the animation in the color requested

local assetUrl, baseUrl, x, y, silhouetteColor = ...

local ThumbnailGenerator = game:GetService("ThumbnailGenerator")
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailScriptStarted")

local BundleLoader = require(ThumbnailGenerator:GetThumbnailModule("BundleLoader"))

pcall(function() game:GetService("ContentProvider"):SetBaseUrl(baseUrl) end)
game:GetService("ScriptContext").ScriptsDisabled = true

local emoteAnim = game:GetObjects(assetUrl)[1]

local bundleId = 401 -- Default is Alexandra Ninniflip
local bundleIdValue = emoteAnim:FindFirstChild("ThumbnailBundleId")
if bundleIdValue and bundleIdValue:IsA("NumberValue") then
    bundleId = bundleIdValue.Value
end

-- Default keyframe to use in thumbnail is middle keyframe
local thumbnailKeyframeNumber
local thumbnailKeyframeValue = emoteAnim:FindFirstChild("ThumbnailKeyframe")
if thumbnailKeyframeValue and thumbnailKeyframeValue:IsA("NumberValue") then
    thumbnailKeyframeNumber = thumbnailKeyframeValue.Value
end

local thumbnailZoom = 1
local thumbnailZoomValue = emoteAnim:FindFirstChild("ThumbnailZoom")
if thumbnailZoomValue and thumbnailZoomValue:IsA("NumberValue") then
    thumbnailZoom = thumbnailZoomValue.Value
end

local fieldOfView = 20
local fieldOfViewValue = emoteAnim:FindFirstChild("ThumbnailFieldOfView")
if fieldOfViewValue and fieldOfViewValue:IsA("NumberValue") then
    fieldOfView = fieldOfViewValue.Value
end

local verticalOffset = 0
local verticalOffsetValue = emoteAnim:FindFirstChild("ThumbnailVerticalOffset")
if verticalOffsetValue and verticalOffsetValue:IsA("NumberValue") then
    verticalOffset = verticalOffsetValue.Value
end

local horizontalOffset = 0
local horizontalOffsetValue = emoteAnim:FindFirstChild("ThumbnailHorizontalOffset")
if horizontalOffsetValue and horizontalOffsetValue:IsA("NumberValue") then
    horizontalOffset = horizontalOffsetValue.Value
end

local rotationDegrees = 0
local thumbnailRotationValue = emoteAnim:FindFirstChild("ThumbnailCharacterRotation")
if thumbnailRotationValue and thumbnailRotationValue:IsA("NumberValue") then
	rotationDegrees = thumbnailRotationValue.Value
end

local bundleCharacter = BundleLoader.LoadBundleCharacter(baseUrl, bundleId)
ThumbnailGenerator:AddProfilingCheckpoint("BundleCharacterLoaded")

local r, g, b = unpack(silhouetteColor:split("/"))
local silhouetteColor3 = Color3.fromRGB(tonumber(r), tonumber(g), tonumber(b))

local overrideColor3Value = emoteAnim:FindFirstChild("ThumbnailSilhouetteColor")
if overrideColor3Value and overrideColor3Value:IsA("Color3Value") then
    silhouetteColor3 = overrideColor3Value.Value
end

local KeyframeSequenceProvider = game:GetService("KeyframeSequenceProvider")

local kfs = KeyframeSequenceProvider:GetKeyframeSequence(emoteAnim.AnimationId)
local emoteKeyframes = kfs:GetKeyframes()

ThumbnailGenerator:AddProfilingCheckpoint("KeyframesLoaded")

local function getJointBetween(part0, part1)
	for _, obj in pairs(part1:GetChildren()) do
		if obj:IsA("Motor6D") and obj.Part0 == part0 then
			return obj
		end
	end
end

local function applyPose(character, poseKeyframe)
	local function recurApplyPoses(parentPose, poseObject)
		if parentPose then
			local joint = getJointBetween(character[parentPose.Name], character[poseObject.Name])
			joint.C1 = joint.C1 * poseObject.CFrame:inverse()
        end

		for _, subPose in pairs(poseObject:GetSubPoses()) do
			recurApplyPoses(poseObject, subPose)
		end
	end

	for _, poseObj in pairs(poseKeyframe:GetPoses()) do
		recurApplyPoses(nil, poseObj)
	end
end

local thumbnailKeyframe
if thumbnailKeyframeNumber then
    -- Check that the index provided as the keyframe number is valid
    if thumbnailKeyframeNumber > 0 and thumbnailKeyframeNumber <= #emoteKeyframes then
        thumbnailKeyframe = emoteKeyframes[thumbnailKeyframeNumber]
    else
        thumbnailKeyframe = emoteKeyframes[math.ceil(#emoteKeyframes/2)]
    end
else
    thumbnailKeyframe = emoteKeyframes[math.ceil(#emoteKeyframes/2)]
end

if rotationDegrees ~= 0 then
	local rootPose = thumbnailKeyframe:GetPoses()[1]
	if rootPose then
		local upperTorsoPose = rootPose:GetSubPoses()[1]
		if upperTorsoPose then
			upperTorsoPose.CFrame = upperTorsoPose.CFrame * CFrame.Angles(0, math.rad(rotationDegrees), 0)
		end
	end
end

applyPose(bundleCharacter, thumbnailKeyframe)

local function getCameraOffset(fov, extentsSize)
	local xSize, ySize, zSize = extentsSize.X, extentsSize.Y, extentsSize.Z

	local maxSize = math.sqrt(xSize^2 + ySize^2 + zSize^2)
	local fovMultiplier = 1 / math.tan(math.rad(fov) / 2)

    local halfSize = maxSize / 2
	return halfSize * fovMultiplier
end

local function zoomExtents(model, lookVector, thumbnailCamera)
    local modelCFrame = model:GetModelCFrame()

    local position = modelCFrame.p
	position = position + Vector3.new(horizontalOffset, -verticalOffset, 0)

	local extentsSize = model:GetExtentsSize()
    local cameraOffset = getCameraOffset(thumbnailCamera.FieldOfView, extentsSize)

    local zoomFactor = 1 / thumbnailZoom
    cameraOffset = cameraOffset * zoomFactor

    local cameraRotation = thumbnailCamera.CFrame - thumbnailCamera.CFrame.p
	thumbnailCamera.CFrame = cameraRotation + position + (lookVector * cameraOffset)
end

local function createThumbnailCamera(model)
    local modelCFrame = model:GetModelCFrame()
	local lookVector = modelCFrame.lookVector

	local humanoidRootPart = model:FindFirstChild("HumanoidRootPart")
	if humanoidRootPart then
		lookVector = humanoidRootPart.CFrame.lookVector
	end

    local thumbnailCamera = Instance.new("Camera")
    thumbnailCamera.Name = "ThumbnailCamera"
    thumbnailCamera.Parent = model

    thumbnailCamera.FieldOfView = fieldOfView
    thumbnailCamera.CFrame = CFrame.new(modelCFrame.p + (lookVector * 5), modelCFrame.p)
    thumbnailCamera.Focus = modelCFrame

    zoomExtents(model, lookVector, thumbnailCamera)
end

createThumbnailCamera(bundleCharacter)

local result, requestedUrls = game:GetService("ThumbnailGenerator"):ClickSilhouette(x, y, silhouetteColor3)
ThumbnailGenerator:AddProfilingCheckpoint("ThumbnailGenerated")

return result, requestedUrls