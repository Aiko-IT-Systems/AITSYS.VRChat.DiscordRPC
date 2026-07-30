using NUnit.Framework;

namespace AITSYS.VRCUnity.DiscordRPC.Tests
{
    public sealed class RpcStateTests
    {
        [TestCase((int)RpcState.EditMode, "In Edit Mode")]
        [TestCase((int)RpcState.PlayMode, "In Play Mode")]
        [TestCase((int)RpcState.BuildAvatar, "Building Avatar")]
        [TestCase((int)RpcState.UploadAvatar, "Uploading Avatar")]
        [TestCase((int)RpcState.BuildWorld, "Building World")]
        [TestCase((int)RpcState.UploadWorld, "Uploading World")]
        public void StateLabelsAreStable(int state, string expected)
        {
            Assert.AreEqual(expected, ((RpcState)state).DisplayName());
        }

        [Test]
        public void InstalledSdkDetectionHasAnExplanation()
        {
            ProjectContext.DetectInstalledSdk(out string reason);
            Assert.IsFalse(string.IsNullOrWhiteSpace(reason));
        }
    }
}
