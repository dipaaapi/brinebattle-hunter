using BrineBattle.Hunting;
using NUnit.Framework;

namespace BrineBattle.Tests
{
    public class CameraTests
    {
        [Test]
        public void ZoomStepsCloserAndFarther()
        {
            Assert.Less(CameraRules.Zoom(20f, -1f), 20f);
            Assert.Greater(CameraRules.Zoom(20f, 1f), 20f);
        }

        [Test]
        public void ZoomStaysInLimits()
        {
            Assert.AreEqual(CameraRules.MinDistance, CameraRules.Zoom(CameraRules.MinDistance, -1f), 1e-4);
            Assert.AreEqual(CameraRules.MaxDistance, CameraRules.Zoom(CameraRules.MaxDistance, 1f), 1e-4);
        }

        [Test]
        public void PitchStaysInLimits()
        {
            Assert.AreEqual(CameraRules.MinPitch, CameraRules.ClampPitch(0f), 1e-4);
            Assert.AreEqual(CameraRules.MaxPitch, CameraRules.ClampPitch(120f), 1e-4);
            Assert.AreEqual(58f, CameraRules.ClampPitch(58f), 1e-4);
        }
    }
}
