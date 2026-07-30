namespace AITSYS.VRCUnity.DiscordRPC
{
    internal enum RpcState
    {
        EditMode,
        PlayMode,
        BuildAvatar,
        UploadAvatar,
        BuildWorld,
        UploadWorld
    }

    internal static class RpcStateExtensions
    {
        internal static string DisplayName(this RpcState state)
        {
            switch (state)
            {
                case RpcState.EditMode:
                    return "In Edit Mode";
                case RpcState.PlayMode:
                    return "In Play Mode";
                case RpcState.BuildAvatar:
                    return "Building Avatar";
                case RpcState.UploadAvatar:
                    return "Uploading Avatar";
                case RpcState.BuildWorld:
                    return "Building World";
                case RpcState.UploadWorld:
                    return "Uploading World";
                default:
                    return "In Unity";
            }
        }
    }
}
