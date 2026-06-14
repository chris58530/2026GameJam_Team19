using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程式合成音效產生器。
/// 以波形數學即時合成 AudioClip，無需任何外部音檔或網路資源。
/// 供 SoundManager 在「找不到已綁定的 AudioClip」時作為備援使用。
///
/// 支援的音效名稱見 <see cref="HasDefinition"/> / Generate 內的 switch。
/// 未定義的名稱會回傳一個通用的短促 click，確保永遠有聲音。
/// </summary>
public static class ProceduralAudio
{
    public const int SampleRate = 44100;

    private enum Wave { Sine, Square, Triangle, Saw, Noise }

    // 常用音高 (Hz)
    private const float C4 = 261.63f, D4 = 293.66f, E4 = 329.63f, F4 = 349.23f, G4 = 392.00f, A4 = 440.00f, B4 = 493.88f;
    private const float C5 = 523.25f, D5 = 587.33f, E5 = 659.25f, F5 = 698.46f, G5 = 783.99f, A5 = 880.00f, B5 = 987.77f;
    private const float C6 = 1046.50f, E6 = 1318.51f, G6 = 1567.98f;

    private static readonly System.Random Rng = new System.Random(19);

    /// <summary>是否為已明確定義的音效名稱（非通用備援）。</summary>
    public static bool HasDefinition(string name)
    {
        switch (name)
        {
            case "Jump":
            case "Land":
            case "Die":
            case "Fail":
            case "KeyPickup":
            case "LevelClear":
            case "ButtonPress":
            case "ButtonRelease":
            case "GateOpen":
            case "GateClose":
            case "PlatformMove":
            case "DoorLocked":
            case "UIClick":
            case "Pause":
            case "Resume":
            case "SlotTick":
            case "SlotReveal":
            case "CardSelect":
            case "DrawStart":
            case "TitleBGM":
            case "GameBGM":
            case "BossBGM":
                return true;
            default:
                return false;
        }
    }

    /// <summary>是否為 BGM（需要 loop）。</summary>
    public static bool IsBGM(string name)
    {
        return name == "TitleBGM" || name == "GameBGM" || name == "BossBGM";
    }

    /// <summary>
    /// 依名稱合成並回傳 AudioClip。未定義名稱回傳通用 click。
    /// </summary>
    public static AudioClip Generate(string name)
    {
        switch (name)
        {
            // ── 玩家動作 ──────────────────────────────
            case "Jump":           return BuildJump();
            case "Land":           return BuildLand();

            // ── 結果回饋 ──────────────────────────────
            case "Die":            return BuildDie();
            case "Fail":           return BuildFail();
            case "LevelClear":     return BuildLevelClear();

            // ── 收集 / 機關 ───────────────────────────
            case "KeyPickup":      return BuildKeyPickup();
            case "GateOpen":       return BuildGate(true);
            case "GateClose":      return BuildGate(false);
            case "PlatformMove":   return BuildPlatformMove();
            case "DoorLocked":     return BuildDoorLocked();

            // ── UI ────────────────────────────────────
            case "UIClick":        return BuildUIClick();
            case "ButtonPress":    return BuildButton(true);
            case "ButtonRelease":  return BuildButton(false);
            case "Pause":          return BuildTwoNote(name, A5, D5);
            case "Resume":         return BuildTwoNote(name, D5, A5);

            // ── 抽卡 / 拉霸 ───────────────────────────
            case "SlotTick":       return BuildSlotTick();
            case "SlotReveal":     return BuildSlotReveal();
            case "CardSelect":     return BuildTwoNote(name, E5, A5);
            case "DrawStart":      return BuildDrawStart();

            // ── BGM (loop) ────────────────────────────
            case "TitleBGM":       return BuildTitleBGM();
            case "GameBGM":        return BuildGameBGM();
            case "BossBGM":        return BuildBossBGM();

            default:               return BuildGenericClick(name);
        }
    }

    // ──────────────────────────────────────────────────────────
    //  SFX 定義
    // ──────────────────────────────────────────────────────────

    // 跳躍：快速上升方波，俐落帶點 8-bit 感
    private static AudioClip BuildJump()
    {
        float[] buf = NewBuffer(0.16f);
        AddTone(buf, 0f, 0.16f, 300f, 720f, Wave.Square, 0.35f, 0.005f, 0.10f);
        return Make("SFX_Jump", buf);
    }

    // 落地：低頻悶響 + 短噪音
    private static AudioClip BuildLand()
    {
        float[] buf = NewBuffer(0.16f);
        AddTone(buf, 0f, 0.14f, 160f, 70f, Wave.Sine, 0.5f, 0.002f, 0.12f);
        AddTone(buf, 0f, 0.05f, 200f, 200f, Wave.Noise, 0.25f, 0.001f, 0.04f);
        return Make("SFX_Land", buf);
    }

    // 死亡：下降方波，帶點不祥
    private static AudioClip BuildDie()
    {
        float[] buf = NewBuffer(0.55f);
        AddTone(buf, 0f, 0.55f, 440f, 90f, Wave.Square, 0.32f, 0.005f, 0.5f);
        AddTone(buf, 0f, 0.55f, 220f, 45f, Wave.Triangle, 0.18f, 0.005f, 0.5f);
        return Make("SFX_Die", buf);
    }

    // 失敗：兩段下行刺耳鋸齒 (G4 → C4)
    private static AudioClip BuildFail()
    {
        float[] buf = NewBuffer(0.6f);
        AddTone(buf, 0.0f, 0.28f, G4, G4, Wave.Saw, 0.28f, 0.005f, 0.22f);
        AddTone(buf, 0.3f, 0.30f, C4, C4 * 0.94f, Wave.Saw, 0.30f, 0.005f, 0.28f);
        return Make("SFX_Fail", buf);
    }

    // 通關：上行琶音 C-E-G-C 小號鳴奏
    private static AudioClip BuildLevelClear()
    {
        float[] buf = NewBuffer(0.85f);
        AddTone(buf, 0.00f, 0.16f, C5, C5, Wave.Square, 0.28f, 0.004f, 0.14f);
        AddTone(buf, 0.14f, 0.16f, E5, E5, Wave.Square, 0.28f, 0.004f, 0.14f);
        AddTone(buf, 0.28f, 0.16f, G5, G5, Wave.Square, 0.28f, 0.004f, 0.14f);
        AddTone(buf, 0.42f, 0.40f, C6, C6, Wave.Square, 0.32f, 0.004f, 0.38f);
        AddTone(buf, 0.42f, 0.40f, E6, E6, Wave.Triangle, 0.16f, 0.004f, 0.38f);
        return Make("SFX_LevelClear", buf);
    }

    // 撿鑰匙：明亮的雙音上升叮咚
    private static AudioClip BuildKeyPickup()
    {
        float[] buf = NewBuffer(0.28f);
        AddTone(buf, 0.0f, 0.10f, E5, E5, Wave.Triangle, 0.32f, 0.003f, 0.09f);
        AddTone(buf, 0.08f, 0.20f, B5, B5, Wave.Triangle, 0.34f, 0.003f, 0.18f);
        AddTone(buf, 0.08f, 0.20f, G6, G6, Wave.Sine, 0.12f, 0.003f, 0.18f);
        return Make("SFX_KeyPickup", buf);
    }

    // 閘門開 / 關：機械感的掃頻 + 少量噪音
    private static AudioClip BuildGate(bool open)
    {
        float[] buf = NewBuffer(0.4f);
        float fStart = open ? 150f : 420f;
        float fEnd = open ? 420f : 150f;
        AddTone(buf, 0f, 0.38f, fStart, fEnd, Wave.Saw, 0.26f, 0.01f, 0.3f);
        AddTone(buf, 0f, 0.38f, 0f, 0f, Wave.Noise, 0.06f, 0.01f, 0.3f);
        return Make(open ? "SFX_GateOpen" : "SFX_GateClose", buf);
    }

    // 平台移動：低頻嗡鳴帶輕微抖動
    private static AudioClip BuildPlatformMove()
    {
        float[] buf = NewBuffer(0.35f);
        AddTone(buf, 0f, 0.35f, 90f, 90f, Wave.Square, 0.2f, 0.02f, 0.06f, tremoloHz: 14f, tremoloDepth: 0.4f);
        AddTone(buf, 0f, 0.35f, 45f, 45f, Wave.Sine, 0.18f, 0.02f, 0.06f);
        return Make("SFX_PlatformMove", buf);
    }

    // 門上鎖：低沉的雙下擊 thunk（否定感）
    private static AudioClip BuildDoorLocked()
    {
        float[] buf = NewBuffer(0.32f);
        AddTone(buf, 0.00f, 0.12f, 180f, 110f, Wave.Square, 0.34f, 0.002f, 0.1f);
        AddTone(buf, 0.15f, 0.14f, 150f, 90f, Wave.Square, 0.34f, 0.002f, 0.12f);
        return Make("SFX_DoorLocked", buf);
    }

    // UI 點擊：短而清脆
    private static AudioClip BuildUIClick()
    {
        float[] buf = NewBuffer(0.06f);
        AddTone(buf, 0f, 0.05f, 900f, 900f, Wave.Triangle, 0.3f, 0.001f, 0.045f);
        return Make("SFX_UIClick", buf);
    }

    // 按鈕壓下 / 放開：短 click，壓下下行、放開上行
    private static AudioClip BuildButton(bool press)
    {
        float[] buf = NewBuffer(0.08f);
        float fStart = press ? 700f : 500f;
        float fEnd = press ? 420f : 760f;
        AddTone(buf, 0f, 0.07f, fStart, fEnd, Wave.Square, 0.28f, 0.001f, 0.06f);
        return Make(press ? "SFX_ButtonPress" : "SFX_ButtonRelease", buf);
    }

    // 通用雙音（暫停 / 繼續）
    private static AudioClip BuildTwoNote(string name, float f1, float f2)
    {
        float[] buf = NewBuffer(0.26f);
        AddTone(buf, 0.0f, 0.11f, f1, f1, Wave.Triangle, 0.3f, 0.003f, 0.1f);
        AddTone(buf, 0.11f, 0.13f, f2, f2, Wave.Triangle, 0.3f, 0.003f, 0.12f);
        return Make("SFX_" + name, buf);
    }

    // 拉霸滾輪每格的「噠」聲：極短、清脆的機械 tick
    private static AudioClip BuildSlotTick()
    {
        float[] buf = NewBuffer(0.05f);
        AddTone(buf, 0f, 0.04f, 1100f, 820f, Wave.Square, 0.3f, 0.001f, 0.03f);
        AddTone(buf, 0f, 0.012f, 0f, 0f, Wave.Noise, 0.12f, 0.001f, 0.01f);
        return Make("SFX_SlotTick", buf);
    }

    // 抽卡揭曉：明亮的和弦衝擊 + 上揚閃光感
    private static AudioClip BuildSlotReveal()
    {
        float[] buf = NewBuffer(0.55f);
        // 和弦疊音 (C5-E5-G5)
        AddTone(buf, 0.0f, 0.45f, C5, C5, Wave.Square, 0.22f, 0.004f, 0.4f);
        AddTone(buf, 0.0f, 0.45f, E5, E5, Wave.Square, 0.20f, 0.004f, 0.4f);
        AddTone(buf, 0.0f, 0.45f, G5, G5, Wave.Triangle, 0.18f, 0.004f, 0.4f);
        // 上揚閃光掃頻
        AddTone(buf, 0.0f, 0.30f, G5, C6 * 1.5f, Wave.Sine, 0.14f, 0.004f, 0.26f);
        return Make("SFX_SlotReveal", buf);
    }

    // 抽卡開始：短促的上行提示音
    private static AudioClip BuildDrawStart()
    {
        float[] buf = NewBuffer(0.2f);
        AddTone(buf, 0f, 0.18f, C4, G4, Wave.Triangle, 0.3f, 0.004f, 0.15f);
        return Make("SFX_DrawStart", buf);
    }

    // 未定義名稱的通用備援 click
    private static AudioClip BuildGenericClick(string name)
    {
        float[] buf = NewBuffer(0.07f);
        AddTone(buf, 0f, 0.06f, 600f, 480f, Wave.Square, 0.25f, 0.002f, 0.055f);
        return Make("SFX_Generic_" + name, buf);
    }

    // ──────────────────────────────────────────────────────────
    //  BGM 定義（皆為可 loop 的短旋律）
    // ──────────────────────────────────────────────────────────

    // 標題：平靜上行的琶音循環
    private static AudioClip BuildTitleBGM()
    {
        float bpm = 96f;
        float beat = 60f / bpm;
        float[] notes = { C4, E4, G4, B4, C5, B4, G4, E4 };
        float len = beat * notes.Length;
        float[] buf = NewBuffer(len);
        for (int i = 0; i < notes.Length; i++)
        {
            float t = i * beat;
            AddTone(buf, t, beat * 0.95f, notes[i], notes[i], Wave.Triangle, 0.16f, 0.02f, beat * 0.6f);
            AddTone(buf, t, beat * 0.95f, notes[i] * 0.5f, notes[i] * 0.5f, Wave.Sine, 0.10f, 0.02f, beat * 0.6f);
        }
        return MakeLoop("BGM_Title", buf);
    }

    // 遊戲中：稍快、輕快的循環旋律 + 低音
    private static AudioClip BuildGameBGM()
    {
        float bpm = 120f;
        float beat = 60f / bpm;
        float[] mel = { E4, G4, A4, G4, E4, D4, E4, G4, A4, C5, B4, A4, G4, E4, D4, C4 };
        float[] bass = { A4 * 0.25f, A4 * 0.25f, F4 * 0.25f, F4 * 0.25f,
                         G4 * 0.25f, G4 * 0.25f, C4 * 0.5f, C4 * 0.5f };
        float len = beat * mel.Length;
        float[] buf = NewBuffer(len);
        for (int i = 0; i < mel.Length; i++)
        {
            float t = i * beat;
            AddTone(buf, t, beat * 0.9f, mel[i], mel[i], Wave.Square, 0.14f, 0.01f, beat * 0.5f);
        }
        float bassBeat = len / bass.Length;
        for (int i = 0; i < bass.Length; i++)
        {
            float t = i * bassBeat;
            AddTone(buf, t, bassBeat * 0.95f, bass[i], bass[i], Wave.Triangle, 0.18f, 0.01f, bassBeat * 0.7f);
        }
        return MakeLoop("BGM_Game", buf);
    }

    // Boss：低沉緊張的循環，半音與快速脈動
    private static AudioClip BuildBossBGM()
    {
        float bpm = 140f;
        float beat = 60f / bpm;
        float[] mel = { A4, A4, C5, A4, A4, B4, A4, G4 };
        float len = beat * mel.Length;
        float[] buf = NewBuffer(len);
        // 持續低音脈動
        int pulses = mel.Length * 2;
        float pulse = len / pulses;
        for (int i = 0; i < pulses; i++)
        {
            float t = i * pulse;
            AddTone(buf, t, pulse * 0.8f, 55f, 55f, Wave.Saw, 0.22f, 0.005f, pulse * 0.5f);
        }
        for (int i = 0; i < mel.Length; i++)
        {
            float t = i * beat;
            AddTone(buf, t, beat * 0.9f, mel[i], mel[i], Wave.Square, 0.13f, 0.005f, beat * 0.4f);
        }
        return MakeLoop("BGM_Boss", buf);
    }

    // ──────────────────────────────────────────────────────────
    //  合成核心
    // ──────────────────────────────────────────────────────────

    private static float[] NewBuffer(float seconds)
    {
        return new float[Mathf.Max(1, Mathf.CeilToInt(seconds * SampleRate))];
    }

    /// <summary>
    /// 將一段音調加總寫入緩衝區（支援頻率掃描、包絡、顫音）。
    /// </summary>
    private static void AddTone(
        float[] buf, float startSec, float durSec,
        float freqStart, float freqEnd, Wave wave,
        float amp, float attackSec, float decaySec,
        float tremoloHz = 0f, float tremoloDepth = 0f)
    {
        int start = Mathf.RoundToInt(startSec * SampleRate);
        int count = Mathf.RoundToInt(durSec * SampleRate);
        if (count <= 0) return;

        int attack = Mathf.Max(1, Mathf.RoundToInt(attackSec * SampleRate));
        int decay = Mathf.Max(1, Mathf.RoundToInt(decaySec * SampleRate));

        double phase = 0.0;
        for (int i = 0; i < count; i++)
        {
            int idx = start + i;
            if (idx < 0) continue;
            if (idx >= buf.Length) break;

            float p = (float)i / count;
            float freq = Mathf.Lerp(freqStart, freqEnd, p);
            phase += 2.0 * Mathf.PI * freq / SampleRate;

            float s = Sample(wave, (float)phase);

            // 包絡：線性 attack，之後依 decay 指數衰減
            float env;
            if (i < attack)
                env = (float)i / attack;
            else
                env = Mathf.Exp(-(float)(i - attack) / decay);

            // 顫音
            float trem = 1f;
            if (tremoloHz > 0f && tremoloDepth > 0f)
                trem = 1f - tremoloDepth * 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * tremoloHz * i / SampleRate));

            buf[idx] += s * amp * env * trem;
        }
    }

    private static float Sample(Wave wave, float phase)
    {
        switch (wave)
        {
            case Wave.Sine:
                return Mathf.Sin(phase);
            case Wave.Square:
                return Mathf.Sin(phase) >= 0f ? 1f : -1f;
            case Wave.Triangle:
            {
                float t = (phase / (2f * Mathf.PI)) % 1f;
                if (t < 0f) t += 1f;
                return 4f * Mathf.Abs(t - 0.5f) - 1f;
            }
            case Wave.Saw:
            {
                float t = (phase / (2f * Mathf.PI)) % 1f;
                if (t < 0f) t += 1f;
                return 2f * t - 1f;
            }
            case Wave.Noise:
                return (float)(Rng.NextDouble() * 2.0 - 1.0);
            default:
                return 0f;
        }
    }

    private static AudioClip Make(string name, float[] samples)
    {
        Normalize(samples, 0.85f);
        var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip MakeLoop(string name, float[] samples)
    {
        Normalize(samples, 0.6f);
        // 對首尾各做極短交叉淡化，降低 loop 接縫的爆音
        int fade = Mathf.Min(samples.Length / 20, SampleRate / 50);
        for (int i = 0; i < fade; i++)
        {
            float k = (float)i / fade;
            samples[i] *= k;
            samples[samples.Length - 1 - i] *= k;
        }
        var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void Normalize(float[] samples, float target)
    {
        float peak = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float a = Mathf.Abs(samples[i]);
            if (a > peak) peak = a;
        }
        if (peak <= 0.0001f) return;
        float gain = target / peak;
        for (int i = 0; i < samples.Length; i++)
            samples[i] *= gain;
    }
}
