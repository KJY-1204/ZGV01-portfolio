// 옵션 설정 데이터(GameSettings)의 범위 규칙, 직렬화, 시선 입력 변환을 검증하는 EditMode 테스트다.
using NUnit.Framework;
using UnityEngine;

public class GameSettingsTests
{
    [Test]
    public void Defaults_AreNeutral_AndGraphicsIsHigh()
    {
        GameSettings s = GameSettings.CreateDefault();
        Assert.AreEqual(1f, s.masterVolume);
        Assert.AreEqual(1f, s.effectsVolume);
        Assert.AreEqual(1f, s.voicesVolume);
        Assert.AreEqual(1f, s.footstepsVolume);
        Assert.AreEqual(1f, s.fovScale);
        Assert.AreEqual(1f, s.uiScale);
        Assert.AreEqual(1f, s.mouseSensitivityX);
        Assert.AreEqual(1f, s.mouseSensitivityY);
        Assert.IsFalse(s.invertY);
        Assert.IsTrue(s.vSync);
        Assert.AreEqual(-1, s.fullScreenMode);
        Assert.AreEqual(GameSettings.GraphicsHigh, s.graphicsPreset);
    }

    [Test]
    public void Sanitize_ClampsOutOfRangeValues()
    {
        GameSettings s = new GameSettings
        {
            masterVolume = 3f, effectsVolume = -1f, voicesVolume = 9f, footstepsVolume = 2f,
            fovScale = 9f, uiScale = 0.1f, mouseSensitivityX = -2f, mouseSensitivityY = 99f,
            screenWidth = 1920, screenHeight = 0, fullScreenMode = -7, graphicsPreset = 9,
        };
        s.Sanitize();
        Assert.AreEqual(1f, s.masterVolume);
        Assert.AreEqual(0f, s.effectsVolume);
        Assert.AreEqual(1f, s.voicesVolume);
        Assert.AreEqual(1f, s.footstepsVolume);
        Assert.AreEqual(GameSettings.MaxFovScale, s.fovScale);
        Assert.AreEqual(GameSettings.MinUiScale, s.uiScale);
        Assert.AreEqual(GameSettings.MinSensitivity, s.mouseSensitivityX);
        Assert.AreEqual(GameSettings.MaxSensitivity, s.mouseSensitivityY);
        Assert.AreEqual(0, s.screenWidth, "너비만 있고 높이가 없으면 해상도를 바꾸지 않는다");
        Assert.AreEqual(-1, s.fullScreenMode);
        Assert.AreEqual(GameSettings.GraphicsHigh, s.graphicsPreset);
    }

    [Test]
    public void Sanitize_NegativePreset_BecomesLow()
    {
        GameSettings s = new GameSettings { graphicsPreset = -3 };
        s.Sanitize();
        Assert.AreEqual(GameSettings.GraphicsLow, s.graphicsPreset);
    }

    [Test]
    public void Sanitize_ReplacesNaNWithNeutralValues()
    {
        GameSettings s = new GameSettings { masterVolume = float.NaN, fovScale = float.NaN, uiScale = float.NaN, mouseSensitivityX = float.NaN, mouseSensitivityY = float.NaN, effectsVolume = float.NaN };
        s.Sanitize();
        Assert.AreEqual(1f, s.masterVolume);
        Assert.AreEqual(1f, s.effectsVolume);
        Assert.AreEqual(1f, s.fovScale);
        Assert.AreEqual(1f, s.uiScale);
        Assert.AreEqual(1f, s.mouseSensitivityX);
        Assert.AreEqual(1f, s.mouseSensitivityY);
    }

    [Test]
    public void Json_RoundTrips()
    {
        GameSettings original = new GameSettings
        {
            masterVolume = 0.4f, effectsVolume = 0.5f, voicesVolume = 0.6f, footstepsVolume = 0.7f,
            screenWidth = 1600, screenHeight = 900, fullScreenMode = 3, graphicsPreset = 0, vSync = false,
            fovScale = 1.1f, uiScale = 1.25f, mouseSensitivityX = 1.7f, mouseSensitivityY = 0.6f, invertY = true,
        };
        GameSettings loaded = GameSettings.FromJson(original.ToJson());
        Assert.AreEqual(0.4f, loaded.masterVolume, 0.0001f);
        Assert.AreEqual(0.5f, loaded.effectsVolume, 0.0001f);
        Assert.AreEqual(0.6f, loaded.voicesVolume, 0.0001f);
        Assert.AreEqual(0.7f, loaded.footstepsVolume, 0.0001f);
        Assert.AreEqual(1600, loaded.screenWidth);
        Assert.AreEqual(900, loaded.screenHeight);
        Assert.AreEqual(3, loaded.fullScreenMode);
        Assert.AreEqual(GameSettings.GraphicsLow, loaded.graphicsPreset);
        Assert.IsFalse(loaded.vSync);
        Assert.AreEqual(1.1f, loaded.fovScale, 0.0001f);
        Assert.AreEqual(1.25f, loaded.uiScale, 0.0001f);
        Assert.AreEqual(1.7f, loaded.mouseSensitivityX, 0.0001f);
        Assert.AreEqual(0.6f, loaded.mouseSensitivityY, 0.0001f);
        Assert.IsTrue(loaded.invertY);
    }

    [Test]
    public void FromJson_EmptyOrBroken_GivesDefaults()
    {
        Assert.AreEqual(1f, GameSettings.FromJson(null).masterVolume);
        Assert.AreEqual(1f, GameSettings.FromJson("").mouseSensitivityX);
        Assert.AreEqual(1f, GameSettings.FromJson("{ 이건 json이 아니다").fovScale);
    }

    [Test]
    public void FromJson_MissingFields_KeepDefaults_AndStillSanitizes()
    {
        GameSettings s = GameSettings.FromJson("{\"masterVolume\": 5}");
        Assert.AreEqual(1f, s.masterVolume);
        Assert.AreEqual(1f, s.fovScale);
        Assert.AreEqual(GameSettings.GraphicsHigh, s.graphicsPreset);
        Assert.IsTrue(s.vSync);
    }

    [Test]
    public void ScaleLook_AppliesHorizontalAndVerticalSensitivitySeparately_AndInvert()
    {
        GameSettings s = new GameSettings { mouseSensitivityX = 2f, mouseSensitivityY = 0.5f, invertY = true };
        Vector2 scaled = s.ScaleLook(new Vector2(1f, 4f));
        Assert.AreEqual(2f * GameSettings.LookBaseScale, scaled.x, 0.0001f);
        Assert.AreEqual(-4f * 0.5f * GameSettings.LookBaseScale, scaled.y, 0.0001f);
    }

    [Test]
    public void KeyBindings_SetGetAndReset()
    {
        GameSettings s = new GameSettings();
        Assert.IsNull(s.GetBinding("Craft"));
        s.SetBinding("Craft", "<Keyboard>/g");
        Assert.AreEqual("<Keyboard>/g", s.GetBinding("Craft"));
        s.SetBinding("Craft", "<Keyboard>/h");
        Assert.AreEqual("<Keyboard>/h", s.GetBinding("Craft"));
        Assert.AreEqual(1, s.keyBindings.Count, "같은 id는 하나만 남는다");
        s.SetBinding("Craft", null);
        Assert.IsNull(s.GetBinding("Craft"));
        Assert.AreEqual(0, s.keyBindings.Count);
    }

    [Test]
    public void KeyBindings_RoundTripThroughJson()
    {
        GameSettings s = new GameSettings();
        s.SetBinding("Jump", "<Keyboard>/x");
        s.SetBinding("Lantern", "<Keyboard>/n");
        GameSettings loaded = GameSettings.FromJson(s.ToJson());
        Assert.AreEqual("<Keyboard>/x", loaded.GetBinding("Jump"));
        Assert.AreEqual("<Keyboard>/n", loaded.GetBinding("Lantern"));
        Assert.IsNull(loaded.GetBinding("Craft"));
    }

    [Test]
    public void Sanitize_DropsEmptyAndDuplicateBindings()
    {
        GameSettings s = new GameSettings();
        s.keyBindings.Add(new KeyBindingEntry { id = "Jump", path = "<Keyboard>/a" });
        s.keyBindings.Add(new KeyBindingEntry { id = "", path = "<Keyboard>/b" });
        s.keyBindings.Add(new KeyBindingEntry { id = "Craft", path = "" });
        s.keyBindings.Add(new KeyBindingEntry { id = "Jump", path = "<Keyboard>/c" });
        s.keyBindings.Add(null);
        s.Sanitize();
        Assert.AreEqual(1, s.keyBindings.Count);
        Assert.AreEqual("<Keyboard>/c", s.GetBinding("Jump"));
    }

    [Test]
    public void Clone_CopiesKeyBindingsIndependently()
    {
        GameSettings a = new GameSettings();
        a.SetBinding("Jump", "<Keyboard>/x");
        GameSettings b = a.Clone();
        b.SetBinding("Jump", "<Keyboard>/y");
        b.SetBinding("Craft", "<Keyboard>/z");
        Assert.AreEqual("<Keyboard>/x", a.GetBinding("Jump"));
        Assert.IsNull(a.GetBinding("Craft"));
    }

    [Test]
    public void SensitivityDisplay_DefaultShowsAsHundred_AndRoundTrips()
    {
        Assert.AreEqual(100f, GameSettings.SensitivityToDisplay(GameSettings.CreateDefault().mouseSensitivityX), 0.0001f);
        Assert.AreEqual(1f, GameSettings.DisplayToSensitivity(100f), 0.0001f);
        Assert.AreEqual(2.5f, GameSettings.DisplayToSensitivity(GameSettings.SensitivityToDisplay(2.5f)), 0.0001f);
        // 화면 슬라이더 범위(20~300)가 허용 배율(0.2~3)과 정확히 맞는다.
        Assert.AreEqual(20f, GameSettings.SensitivityToDisplay(GameSettings.MinSensitivity), 0.0001f);
        Assert.AreEqual(300f, GameSettings.SensitivityToDisplay(GameSettings.MaxSensitivity), 0.0001f);
    }

    [Test]
    public void DefaultSensitivity_AppliesTwentyPercentOfRawLook()
    {
        // 기본값(화면 100)이 원래 입력의 0.2배(체감 20)로 적용된다.
        Vector2 scaled = GameSettings.CreateDefault().ScaleLook(new Vector2(10f, -5f));
        Assert.AreEqual(2f, scaled.x, 0.0001f);
        Assert.AreEqual(-1f, scaled.y, 0.0001f);
    }

    [Test]
    public void Clone_IsIndependentCopy()
    {
        GameSettings a = new GameSettings { masterVolume = 0.3f };
        GameSettings b = a.Clone();
        b.masterVolume = 0.9f;
        Assert.AreEqual(0.3f, a.masterVolume);
    }

    [Test]
    public void HiddenHud_DefaultsToAllVisible_AndEachElementTogglesIndependently()
    {
        GameSettings s = GameSettings.CreateDefault();
        foreach (HudElement element in System.Enum.GetValues(typeof(HudElement))) Assert.IsFalse(s.IsHudHidden(element));

        s.SetHudHidden(HudElement.Clock, true);
        s.SetHudHidden(HudElement.Reticle, true);
        Assert.IsTrue(s.IsHudHidden(HudElement.Clock));
        Assert.IsTrue(s.IsHudHidden(HudElement.Reticle));
        Assert.IsFalse(s.IsHudHidden(HudElement.Compass));

        s.SetHudHidden(HudElement.Clock, false);
        Assert.IsFalse(s.IsHudHidden(HudElement.Clock));
        Assert.IsTrue(s.IsHudHidden(HudElement.Reticle));
    }

    [Test]
    public void HiddenHud_RoundTripsThroughJson_AndSurvivesClone()
    {
        GameSettings s = GameSettings.CreateDefault();
        s.SetHudHidden(HudElement.Ammo, true);
        Assert.IsTrue(GameSettings.FromJson(s.ToJson()).IsHudHidden(HudElement.Ammo));
        Assert.IsTrue(s.Clone().IsHudHidden(HudElement.Ammo));
    }
}
