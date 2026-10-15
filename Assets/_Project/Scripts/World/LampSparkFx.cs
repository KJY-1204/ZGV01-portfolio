// 깜빡이는 가로등의 합선 연출(불꽃 파티클과 지직 소리)을 내는 정적 도우미다. 표현만 담당하고 게임 상태를 바꾸지 않는다.
using UnityEngine;

public static class LampSparkFx
{
    private const int SampleRate = 22050;
    private static AudioClip crackle;

    public static void Play(Vector3 position)
    {
        // 불꽃은 탄착 이펙트의 금속 파편을 빌려 아래로 흩뿌린다(프로젝트에 추적되는 에셋).
        ImpactEffectLibrary library = ImpactEffectLibrary.Instance;
        GameObject effect = library != null ? library.GetEffect(ImpactSurface.Metal) : null;
        if (effect != null)
            EffectPool.Spawn(effect, position, Quaternion.FromToRotation(Vector3.forward, Vector3.down));

        EffectPool.PlaySoundAt(Crackle(), position, 0.6f, Random.Range(0.9f, 1.1f));
    }

    // 짧은 잡음 파열 몇 개가 빠르게 잦아드는 소리를 코드로 만든다(오디오 에셋 없이).
    private static AudioClip Crackle()
    {
        if (crackle != null) return crackle;

        float[] samples = new float[(int)(SampleRate * 0.4f)];
        System.Random rng = new System.Random(20261010);
        float[] popTimes = { 0f, 0.06f, 0.11f, 0.19f, 0.27f };
        foreach (float start in popTimes)
        {
            int from = (int)(start * SampleRate);
            int length = (int)(SampleRate * 0.05f);
            for (int i = 0; i < length && from + i < samples.Length; i++)
            {
                float decay = Mathf.Exp(-i / (length * 0.25f));
                samples[from + i] += ((float)rng.NextDouble() * 2f - 1f) * decay;
            }
        }
        for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(samples[i], -1f, 1f) * 0.8f;

        crackle = AudioClip.Create("LampCrackle", samples.Length, 1, SampleRate, false);
        crackle.SetData(samples, 0);
        return crackle;
    }
}
