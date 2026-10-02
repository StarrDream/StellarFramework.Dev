using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace StellarFramework.Tests.FrameworkValidation
{
    public sealed class OnboardingSurfacePolicyTests
    {
        [Test]
        public void QuickStartPointsUsersToTheTankArenaFrameworkDemo()
        {
            string docPath = Path.Combine(
                Application.dataPath,
                "StellarFramework/FrameworkDoc/00-Overview/快速开始.md");

            string source = File.ReadAllText(docPath);

            Assert.That(source, Does.Contain("FrameworkDemo.unity"));
            Assert.That(source, Does.Not.Contain("UIKit_Playable.unity"));
            Assert.That(source, Does.Not.Contain("ResKit_Playable.unity"));
            Assert.That(source, Does.Not.Contain("FrameworkValidation_Playable.unity"));
        }
    }
}

