using System;
using StellarFramework;
using StellarFramework.Localization;

namespace HotUpdate
{
    public sealed class TankArenaLocalizationService : AbstractService
    {
        private static readonly string[] Keys =
        {
            "tank.title",
            "tank.subtitle",
            "tank.score",
            "tank.wave",
            "tank.hull",
            "tank.objective",
            "tank.move",
            "tank.mode_auto",
            "tank.mode_manual",
            "tank.fire_hint_auto",
            "tank.fire_hint_manual",
            "tank.game_over",
            "tank.result",
            "tank.restart",
            "tank.resume",
            "tank.paused",
            "tank.pickup",
            "tank.phase_ready",
            "tank.package"
        };

        private static readonly string[] English =
        {
            "IRON VANGUARD",
            "SECTOR 07  /  NEBULA REACH",
            "SCORE",
            "WAVE",
            "HULL",
            "HOLD THE LINE  ·  ELIMINATE HOSTILES",
            "MOVE HULL",
            "AUTO",
            "MANUAL",
            "AUTO AIM  ·  TAP FOR MANUAL",
            "RIGHT PAD AIM  ·  HOLD TO FIRE",
            "TANK DISABLED",
            "FINAL SCORE  {score}   ·   ELIMINATIONS  {kills}",
            "DEPLOY AGAIN",
            "RESUME",
            "PAUSED",
            "HULL RESTORED  +25",
            "SECTOR STABLE",
            "PACKAGE"
        };

        private static readonly string[] Chinese =
        {
            "星域突围",
            "第 07 区  /  星云边界",
            "得分",
            "波次",
            "装甲",
            "守住阵线  ·  击退来袭敌军",
            "移动车体",
            "自动",
            "手动",
            "自动瞄准  ·  点击切换手动",
            "右侧摇杆瞄准  ·  按住开火",
            "坦克已损毁",
            "最终得分  {score}   ·   击毁  {kills}",
            "再次出击",
            "继续作战",
            "暂停",
            "装甲恢复  +25",
            "区域稳定",
            "热更包"
        };

        private static readonly LocaleId EnglishLocale = LocaleId.From("en-US");
        private static readonly LocaleId ChineseLocale = LocaleId.From("zh-CN");

        public LocalizationService Text { get; private set; }

        public override void Init()
        {
            base.Init();
            LocalizationTable english = CreateTable(EnglishLocale, English);
            LocalizationTable chinese = CreateTable(ChineseLocale, Chinese);
            var catalog = new LocalizationCatalog(new[] { english, chinese });
            Text = new LocalizationService(
                catalog,
                EnglishLocale,
                new LocalizationFallbackPolicy(new[] { EnglishLocale }));
        }

        public string Get(string key)
        {
            return Text.GetRequired(LocalizationKey.From(key));
        }

        public bool ToggleLanguage()
        {
            LocaleId next = Text.CurrentLocale == EnglishLocale ? ChineseLocale : EnglishLocale;
            return Text.SetLocale(next, out string error) ||
                   LogLocaleFailure(error);
        }

        private static LocalizationTable CreateTable(LocaleId locale, string[] values)
        {
            var entries = new LocalizationEntry[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                entries[i] = new LocalizationEntry(LocalizationKey.From(Keys[i]), values[i]);
            }
            return new LocalizationTable(locale, entries);
        }

        private static bool LogLocaleFailure(string error)
        {
            LogKit.LogError("[TankArena] Locale switch failed: " + (error ?? "unknown error"));
            return false;
        }
    }
}
