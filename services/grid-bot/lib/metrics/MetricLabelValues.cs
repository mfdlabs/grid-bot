namespace Grid.Bot.Metrics;

/// <summary>
/// The bounded sets of values used for metric labels other than <c>outcome</c>, which is in <see cref="MetricOutcomes"/>.
/// </summary>
/// <remarks>
/// Labels must only ever take values from a small, fixed set. Add a constant here rather than passing an arbitrary string.
/// </remarks>
public static class MetricLabelValues
{
    /// <summary>
    /// Where a request came from.
    /// </summary>
    public static class Source
    {
        /// <summary>
        /// A text command sent in a message.
        /// </summary>
        public const string Message = "message";

        /// <summary>
        /// A slash or context menu command.
        /// </summary>
        public const string Interaction = "interaction";
    }

    /// <summary>
    /// Why a request was refused before it ran.
    /// </summary>
    public static class RejectionReason
    {
        /// <summary>
        /// Maintenance mode is enabled.
        /// </summary>
        public const string Maintenance = "maintenance";

        /// <summary>
        /// The user is blacklisted.
        /// </summary>
        public const string Blacklisted = "blacklisted";
    }

    /// <summary>
    /// The feature protected by a flood checker.
    /// </summary>
    public static class Feature
    {
        /// <summary>
        /// Rendering avatars.
        /// </summary>
        public const string Render = "render";

        /// <summary>
        /// Executing scripts.
        /// </summary>
        public const string Script = "script";
    }

    /// <summary>
    /// The scope of a flood checker that refused a request.
    /// </summary>
    public static class FloodScope
    {
        /// <summary>
        /// Too many requests from everyone.
        /// </summary>
        public const string Global = "global";

        /// <summary>
        /// Too many requests from one user.
        /// </summary>
        public const string PerUser = "per_user";
    }

    /// <summary>
    /// A change to a user's role.
    /// </summary>
    public static class RoleChange
    {
        /// <summary>
        /// Added to the blacklist.
        /// </summary>
        public const string Blacklisted = "blacklisted";

        /// <summary>
        /// Removed from the blacklist.
        /// </summary>
        public const string Unblacklisted = "unblacklisted";

        /// <summary>
        /// Granted privileged access.
        /// </summary>
        public const string Privileged = "privileged";

        /// <summary>
        /// Demoted from administrator.
        /// </summary>
        public const string AdminDemoted = "admin_demoted";

        /// <summary>
        /// Demoted from privileged access.
        /// </summary>
        public const string PrivilegedDemoted = "privileged_demoted";

        /// <summary>
        /// Promoted to administrator.
        /// </summary>
        public const string Promoted = "promoted";
    }

    /// <summary>
    /// What produced a render.
    /// </summary>
    public static class RenderBackend
    {
        /// <summary>
        /// The rbx-thumbnails service.
        /// </summary>
        public const string RbxThumbnails = "rbx_thumbnails";

        /// <summary>
        /// A Grid server.
        /// </summary>
        public const string GridServer = "grid_server";
    }

    /// <summary>
    /// Why a render request was not accepted.
    /// </summary>
    public static class RenderInvalidInput
    {
        /// <summary>
        /// The user ID was not valid.
        /// </summary>
        public const string InvalidId = "invalid_id";

        /// <summary>
        /// The user is banned or does not exist.
        /// </summary>
        public const string BannedUser = "banned_user";
    }

    /// <summary>
    /// Why a script was not accepted.
    /// </summary>
    public static class ScriptRejectedInput
    {
        /// <summary>
        /// The script had no content.
        /// </summary>
        public const string NoContent = "no_content";

        /// <summary>
        /// The script contained non-ASCII characters.
        /// </summary>
        public const string Unicode = "unicode";
    }

    /// <summary>
    /// What was wrong with a script's result.
    /// </summary>
    public static class ScriptResultError
    {
        /// <summary>
        /// The result could not be serialized as JSON.
        /// </summary>
        public const string NonJsonSerializable = "non_json_serializable";

        /// <summary>
        /// The result contained non-ASCII characters.
        /// </summary>
        public const string NonAscii = "non_ascii";
    }

    /// <summary>
    /// A gRPC method.
    /// </summary>
    public static class GrpcMethod
    {
        /// <summary>
        /// The health check.
        /// </summary>
        public const string CheckHealth = "check_health";

        /// <summary>
        /// Setting the maintenance status.
        /// </summary>
        public const string SetStatus = "set_status";
    }

    /// <summary>
    /// Where a script syntax error was found.
    /// </summary>
    public static class SyntaxErrorContext
    {
        /// <summary>
        /// The Grid server reported it in the script's metadata.
        /// </summary>
        public const string GridServerMetadata = "grid_server_metadata";

        /// <summary>
        /// The Grid server raised it as a fault.
        /// </summary>
        public const string GridServerFault = "grid_server_fault";

        /// <summary>
        /// The bot's own parser found it before the script was sent.
        /// </summary>
        public const string PreParser = "pre_parser";
    }

    /// <summary>
    /// What a logged script came from.
    /// </summary>
    public static class LogSource
    {
        /// <summary>
        /// A text command.
        /// </summary>
        public const string Command = "command";

        /// <summary>
        /// An interaction.
        /// </summary>
        public const string Interaction = "interaction";
    }
}
