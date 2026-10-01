using StellarFramework;
using StellarFramework.Bindable;

namespace HotUpdate
{
    public enum TankArenaPhase
    {
        Active,
        Paused,
        GameOver
    }

    public interface IReadOnlyTankArenaModel : IReadOnlyModel
    {
        IReadOnlyBindableProperty<int> Score { get; }
        IReadOnlyBindableProperty<int> Hull { get; }
        IReadOnlyBindableProperty<int> Wave { get; }
        IReadOnlyBindableProperty<int> Eliminations { get; }
        IReadOnlyBindableProperty<TankArenaPhase> Phase { get; }
    }

    public sealed class TankArenaModel : AbstractModel, IReadOnlyTankArenaModel
    {
        public readonly BindableProperty<int> Score = new BindableProperty<int>(0);
        public readonly BindableProperty<int> Hull = new BindableProperty<int>(100);
        public readonly BindableProperty<int> Wave = new BindableProperty<int>(1);
        public readonly BindableProperty<int> Eliminations = new BindableProperty<int>(0);
        public readonly BindableProperty<TankArenaPhase> Phase =
            new BindableProperty<TankArenaPhase>(TankArenaPhase.Active);

        IReadOnlyBindableProperty<int> IReadOnlyTankArenaModel.Score => Score;
        IReadOnlyBindableProperty<int> IReadOnlyTankArenaModel.Hull => Hull;
        IReadOnlyBindableProperty<int> IReadOnlyTankArenaModel.Wave => Wave;
        IReadOnlyBindableProperty<int> IReadOnlyTankArenaModel.Eliminations => Eliminations;
        IReadOnlyBindableProperty<TankArenaPhase> IReadOnlyTankArenaModel.Phase => Phase;

        public override void Init()
        {
            base.Init();
            Reset();
        }

        public void Reset()
        {
            Score.Value = 0;
            Hull.Value = 100;
            Wave.Value = 1;
            Eliminations.Value = 0;
            Phase.Value = TankArenaPhase.Active;
        }
    }

    public sealed class TankArenaService : AbstractService
    {
        public void ResetMatch()
        {
            GetModel<TankArenaModel>()?.Reset();
        }

        public void RegisterElimination()
        {
            TankArenaModel model = GetModel<TankArenaModel>();
            if (model == null) return;

            int eliminations = model.Eliminations.Value + 1;
            model.Eliminations.Value = eliminations;
            model.Score.Value += 150;
            model.Wave.Value = 1 + eliminations / 5;
        }

        public void ApplyDamage(int damage)
        {
            TankArenaModel model = GetModel<TankArenaModel>();
            if (model == null || model.Phase.Value != TankArenaPhase.Active) return;

            model.Hull.Value = UnityEngine.Mathf.Max(0, model.Hull.Value - UnityEngine.Mathf.Max(0, damage));
            if (model.Hull.Value == 0)
            {
                model.Phase.Value = TankArenaPhase.GameOver;
            }
        }

        public void RepairHull(int amount)
        {
            TankArenaModel model = GetModel<TankArenaModel>();
            if (model == null || model.Phase.Value != TankArenaPhase.Active) return;
            model.Hull.Value = UnityEngine.Mathf.Min(100, model.Hull.Value + UnityEngine.Mathf.Max(0, amount));
        }

        public void SetPhase(TankArenaPhase phase)
        {
            TankArenaModel model = GetModel<TankArenaModel>();
            if (model != null) model.Phase.Value = phase;
        }
    }

    public sealed class TankArenaArchitecture : Architecture<TankArenaArchitecture>
    {
        protected override void InitModules()
        {
            RegisterModel(new TankArenaModel());
            RegisterService(new TankArenaService());
            RegisterService(new TankArenaLocalizationService());
        }
    }
}
