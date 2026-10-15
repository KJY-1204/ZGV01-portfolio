// 오염/이끼 블렌딩 공용 함수다. 알베도와 매끄러움을 월드 좌표 기준으로 바꾼다: 바닥에 튄 흙, 세로 빗물 줄무늬, 위를 향한 면에 낀 이끼.
// 효과 속성(_GrimeColor 등)은 입력 복사본(ZGVGrime*Input.hlsl)의 UnityPerMaterial 안에 선언돼 있다.
#ifndef ZGV_GRIME_INCLUDED
#define ZGV_GRIME_INCLUDED

// 그래픽 품질 단계(GraphicsPresets가 넣는 전역 값): 0 높음, 1 중간, 2 낮음. 재질 속성이 아니라서 상수 버퍼 밖에 둔다.
float _ZGV_Cut;

float GrimeHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float GrimeNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = GrimeHash(i), n100 = GrimeHash(i + float3(1, 0, 0));
    float n010 = GrimeHash(i + float3(0, 1, 0)), n110 = GrimeHash(i + float3(1, 1, 0));
    float n001 = GrimeHash(i + float3(0, 0, 1)), n101 = GrimeHash(i + float3(1, 0, 1));
    float n011 = GrimeHash(i + float3(0, 1, 1)), n111 = GrimeHash(i + float3(1, 1, 1));
    return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y), lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
}

void ApplyGrime(inout half3 albedo, inout half smoothness, float3 positionWS, half3 normalWS)
{
    // 낮음은 오염/이끼를 아예 건너뛴다.
    if (_ZGV_Cut >= 1.5 || (_GrimeAmount <= 0.001h && _MossAmount <= 0.001h)) return;
    bool simple = _ZGV_Cut >= 0.5;

    // 물체 원점(바닥에 놓인다고 본다) 위 높이다.
    float height = positionWS.y - GetObjectToWorldMatrix()._m13;
    float noiseLow = GrimeNoise(positionWS * _GrimeNoiseScale);
    // 중간은 두 번째 잡음과 빗물 줄무늬를 줄인다.
    float noiseHigh = simple ? noiseLow : GrimeNoise(positionWS * (_GrimeNoiseScale * 3.1) + 7.7);

    // 바닥 근처에 튄 흙: 높이 경계를 잡음으로 들쭉날쭉하게 한다.
    half splash = 1.0h - smoothstep(0.0h, _GrimeHeight, height + (noiseLow - 0.5) * _GrimeHeight * 0.9);
    // 빗물 줄무늬: 가로 방향은 촘촘하고 세로 방향은 길게 늘인 잡음. 세로 면에만 낀다.
    half streak = 0.0h;
    if (!simple)
    {
        float streakNoise = GrimeNoise(float3((positionWS.x + positionWS.z) * 3.5, positionWS.y * 0.3, 1.7));
        streak = smoothstep(0.55, 0.85, streakNoise) * saturate(1.0h - abs(normalWS.y) * 1.5h) * _StreakAmount;
    }
    half grime = saturate((splash * 0.9h + streak) * _GrimeAmount * (0.6h + 0.8h * noiseHigh));
    albedo = lerp(albedo, albedo * _GrimeColor.rgb, grime);
    smoothness *= 1.0h - grime * 0.6h;

    // 이끼: 위를 향한 면과 바닥 근처에 잡음 덩어리로 낀다.
    half up = saturate(normalWS.y);
    half patch = smoothstep(0.42, 0.7, noiseLow * 0.6 + noiseHigh * 0.4);
    half moss = saturate((up * up * 1.3h + splash * 0.5h) * patch) * _MossAmount;
    albedo = lerp(albedo, _MossColor.rgb * (0.7h + 0.6h * noiseHigh), moss);
    smoothness *= 1.0h - moss * 0.8h;
}

#endif
