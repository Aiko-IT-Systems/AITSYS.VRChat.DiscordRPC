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
        public void ProjectDetectionHasAnExplanation()
        {
            ProjectContext context = ProjectContext.Resolve();
            Assert.IsNotNull(context);
            Assert.IsFalse(string.IsNullOrWhiteSpace(context.Reason));
            Assert.IsFalse(string.IsNullOrWhiteSpace(context.DisplayName));
        }

        [Test]
        public void SceneStatisticsProduceCompactRotationLines()
        {
            var statistics = new SceneStatistics { ObjectCount = 1234L };

            var lines = statistics.BuildLines(RpcStatFlags.Objects | RpcStatFlags.LastBuildSize, 1572864);

            CollectionAssert.AreEqual(new[] { "1,234 Objects", "Build Size: 1.5 MB" }, lines);
        }

        [Test]
        public void DiscordActivityTextNeverExceedsItsLimit()
        {
            string result = DiscordText.ClampActivityText(new string('x', 200));

            Assert.AreEqual(DiscordText.ActivityTextLimit, result.Length);
        }
    }
}
