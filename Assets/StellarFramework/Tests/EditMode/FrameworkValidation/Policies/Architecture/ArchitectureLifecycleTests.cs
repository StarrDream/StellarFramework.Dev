using System;
using NUnit.Framework;

namespace StellarFramework.Tests.FrameworkValidation
{
    public sealed class ArchitectureLifecycleTests
    {
        [Test]
        public void InitInvokesInitModulesBeforeInitializingRegisteredModules()
        {
            TestArchitecture app = CreateFreshArchitecture();
            app.Init();

            try
            {
                Assert.That(app.State, Is.EqualTo(ArchitectureState.Initialized));
                Assert.That(app.InitModulesCalled, Is.True);

                TestModel model = app.GetModel<TestModel>();
                TestService service = app.GetService<TestService>();
                Assert.That(model, Is.Not.Null);
                Assert.That(service, Is.Not.Null);
                Assert.That(model.Initialized, Is.True);
                Assert.That(service.Initialized, Is.True);
            }
            finally
            {
                if (app.State == ArchitectureState.Initialized)
                    app.Dispose();
            }
        }

        [Test]
        public void ModelInitFailureRollsBackAndAllowsRetry()
        {
            TestArchitecture app = CreateFreshArchitecture();
            app.FailModelInit = true;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => app.Init());

            Assert.That(exception.Message, Is.EqualTo("Model Init failure."));
            Assert.That(app.State, Is.EqualTo(ArchitectureState.Uninitialized));
            Assert.That(app.RegisteredModel.Initialized, Is.False);
            Assert.That(app.RegisteredModel.Architecture, Is.Null);
            Assert.That(app.RegisteredService.Architecture, Is.Null);

            app.FailModelInit = false;
            app.Init();
            try
            {
                Assert.That(app.State, Is.EqualTo(ArchitectureState.Initialized));
                Assert.That(app.GetModel<TestModel>(), Is.Not.Null);
                Assert.That(app.GetService<TestService>(), Is.Not.Null);
            }
            finally
            {
                if (app.State == ArchitectureState.Initialized)
                    app.Dispose();
            }
        }

        [Test]
        public void ServiceInitFailureRollsBackPreviouslyInitializedModels()
        {
            TestArchitecture app = CreateFreshArchitecture();
            app.FailServiceInit = true;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => app.Init());

            Assert.That(exception.Message, Is.EqualTo("Service Init failure."));
            Assert.That(app.State, Is.EqualTo(ArchitectureState.Uninitialized));
            Assert.That(app.RegisteredModel.Initialized, Is.False);
            Assert.That(app.RegisteredModel.Architecture, Is.Null);
            Assert.That(app.RegisteredService.Initialized, Is.False);
            Assert.That(app.RegisteredService.Architecture, Is.Null);
        }

        [Test]
        public void DisposeCompletesCleanupAndReleasesStaticInstanceWhenDeinitThrows()
        {
            TestArchitecture app = CreateFreshArchitecture();
            app.FailModelDeinit = true;
            app.FailServiceDeinit = true;
            app.Init();

            AggregateException exception = Assert.Throws<AggregateException>(() => app.Dispose());

            Assert.That(exception.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(app.State, Is.EqualTo(ArchitectureState.Disposed));
            Assert.That(app.RegisteredModel.Architecture, Is.Null);
            Assert.That(app.RegisteredService.Architecture, Is.Null);
            Assert.That(TestArchitecture.Interface, Is.Not.SameAs(app));
        }

        private static TestArchitecture CreateFreshArchitecture()
        {
            TestArchitecture app = TestArchitecture.Interface;
            if (app.State == ArchitectureState.Initialized)
                app.Dispose();

            return TestArchitecture.Interface;
        }

        private sealed class TestArchitecture : Architecture<TestArchitecture>
        {
            public bool InitModulesCalled { get; private set; }
            public bool FailModelInit { get; set; }
            public bool FailServiceInit { get; set; }
            public bool FailModelDeinit { get; set; }
            public bool FailServiceDeinit { get; set; }
            public TestModel RegisteredModel { get; private set; }
            public TestService RegisteredService { get; private set; }

            protected override void InitModules()
            {
                InitModulesCalled = true;
                RegisteredModel = new TestModel(FailModelInit, FailModelDeinit);
                RegisteredService = new TestService(FailServiceInit, FailServiceDeinit);
                RegisterModel(RegisteredModel);
                RegisterService(RegisteredService);
            }
        }

        private sealed class TestModel : AbstractModel
        {
            private readonly bool _failInit;
            private readonly bool _failDeinit;

            public TestModel(bool failInit, bool failDeinit)
            {
                _failInit = failInit;
                _failDeinit = failDeinit;
            }

            public bool Initialized { get; private set; }

            public override void Init()
            {
                Initialized = true;
                if (_failInit) throw new InvalidOperationException("Model Init failure.");
            }

            public override void Deinit()
            {
                Initialized = false;
                if (_failDeinit) throw new InvalidOperationException("Model Deinit failure.");
            }
        }

        private sealed class TestService : AbstractService
        {
            private readonly bool _failInit;
            private readonly bool _failDeinit;

            public TestService(bool failInit, bool failDeinit)
            {
                _failInit = failInit;
                _failDeinit = failDeinit;
            }

            public bool Initialized { get; private set; }

            public override void Init()
            {
                Initialized = true;
                if (_failInit) throw new InvalidOperationException("Service Init failure.");
            }

            public override void Deinit()
            {
                Initialized = false;
                if (_failDeinit) throw new InvalidOperationException("Service Deinit failure.");
            }
        }
    }
}
