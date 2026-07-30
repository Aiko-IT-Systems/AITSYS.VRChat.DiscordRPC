using NUnit.Framework;

namespace AITSYS.VRCUnity.DiscordRPC.Tests
{
    public sealed class RpcStateTests
    {
        [TestCase(RpcState.EditMode, "In Edit Mode")]
        [TestCase(RpcState.PlayMode, "In Play Mode")]
        [TestCase(RpcState.BuildAvatar, "Building Avatar")]
        [TestCase(RpcState.UploadAvatar, "Uploading Avatar")]
        [TestCase(RpcState.BuildWorld, "Building World")]
        [TestCase(RpcState.UploadWorld, "Uploading World")]
        public void StateLabelsAreStable(RpcState state, string expected)
        {
            Assert.AreEqual(expected, state.DisplayName());
        }

        [Test]
        public void InstalledSdkDetectionHasAnExplanation()
        {
            ProjectContext.DetectInstalledSdk(out string reason);
            Assert.IsFalse(string.IsNullOrWhiteSpace(reason));
        }
    }
}
