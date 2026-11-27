using NUnit.Framework;
using RAV.Scripts.UI.Helpers;

namespace RAV.Editor.Tests
{
    public sealed class StandardLoadingOverlayEditModeTests
    {
        [Test]
        public void LoadingRequestsAreIndependentAndIdempotent()
        {
            StandardLoadingOverlay.LoadingRequest first = null;
            StandardLoadingOverlay.LoadingRequest second = null;

            try
            {
                first = StandardLoadingOverlay.BeginRequest();
                second = StandardLoadingOverlay.BeginRequest();

                Assert.IsTrue(StandardLoadingOverlay.IsOpen);

                first.End();
                Assert.IsTrue(StandardLoadingOverlay.IsOpen);

                first.End();
                Assert.IsTrue(StandardLoadingOverlay.IsOpen);

                second.Dispose();
                Assert.IsFalse(StandardLoadingOverlay.IsOpen);
            }
            finally
            {
                first?.Dispose();
                second?.Dispose();
            }
        }

        [Test]
        public void SceneLoadingSuppressionCanBeReleasedForMenuRequests()
        {
            StandardLoadingOverlay.LoadingRequest request = null;

            try
            {
                request = StandardLoadingOverlay.BeginRequest();
                Assert.IsTrue(StandardLoadingOverlay.IsOpen);

                StandardLoadingOverlay.SetSceneLoadingSuppressed(true);
                Assert.IsFalse(StandardLoadingOverlay.IsOpen);

                StandardLoadingOverlay.SetSceneLoadingSuppressed(false);
                Assert.IsTrue(StandardLoadingOverlay.IsOpen);
            }
            finally
            {
                StandardLoadingOverlay.SetSceneLoadingSuppressed(false);
                request?.Dispose();
            }
        }
    }
}
