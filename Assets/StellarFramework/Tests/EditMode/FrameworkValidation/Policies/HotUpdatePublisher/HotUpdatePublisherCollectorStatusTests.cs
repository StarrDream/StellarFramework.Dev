using NUnit.Framework;
using StellarFramework.Editor.HotUpdatePublisher;

namespace StellarFramework.Tests.FrameworkValidation
{
    public sealed class HotUpdatePublisherCollectorStatusTests
    {
        [TearDown]
        public void TearDown()
        {
            HotUpdatePublisherCollectorStatus.Provider = null;
        }

        [TestCase(null)]
        [TestCase("")]
        public void Check_ReportsMissingBusinessPackage(string packageName)
        {
            HotUpdatePublisherCollectorStatus status = HotUpdatePublisherCollectorStatus.Check(packageName);

            Assert.That(status.IsReady, Is.False);
            Assert.That(status.Message, Does.Contain("请先在“概览”填写 YooAsset 业务资源包名"));
        }

        [Test]
        public void Check_RejectsVerificationPackageBeforeConsultingProvider()
        {
            bool providerCalled = false;
            HotUpdatePublisherCollectorStatus.Provider = _ =>
            {
                providerCalled = true;
                return new HotUpdatePublisherCollectorStatus(true, "unexpected");
            };

            HotUpdatePublisherCollectorStatus status =
                HotUpdatePublisherCollectorStatus.Check("StellarHotUpdateVerification");

            Assert.That(status.IsReady, Is.False);
            Assert.That(status.Message, Does.Contain("Verification"));
            Assert.That(providerCalled, Is.False);
        }

        [Test]
        public void Check_ExplainsUnavailableYooAssetAdapter()
        {
            HotUpdatePublisherCollectorStatus.Provider = null;

            HotUpdatePublisherCollectorStatus status =
                HotUpdatePublisherCollectorStatus.Check("BusinessPackage");

            Assert.That(status.IsReady, Is.False);
            Assert.That(status.Message, Does.Contain("YooAsset 编辑器扩展尚未加载"));
        }
    }
}
