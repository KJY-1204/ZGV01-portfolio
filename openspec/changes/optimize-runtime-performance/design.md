# Design

## Context

동기와 범위는 `proposal.md`, 요구사항은 `specs/runtime-performance-budget/spec.md`를 따른다. 코드에서 확인한 현재 상태는 다음과 같다.

- 좀비 한 마리에 `Update`/`LateUpdate`를 가진 프로젝트 컴포넌트가 9개 붙어 있다(`ZombieAI`, `ZombieAnimator`, `ZombieAudio`, `ZombieFootsteps`, `ZombieBreathing`, `ZombieHitReaction`, `ZombieStrikeAnimation`, `ZombieShaderFX`, `ZombieSunBurn`). 거리나 가시성에 따른 갱신 단계 조절이 있는지는 아직 확인하지 않았다.
- `ZombieAppearance`, `ZombieShaderFX`, `ZombieSunBurn`은 `GetComponentsInChildren`으로 렌더러를 모으고(스킨드 메시와 머티리얼 인스턴스 수에 영향), `WorldSync`는 `FindObjectsByType`으로 동물·좀비·전리품을 주기적으로 전수 검색한다.
- 기존 성능 측정은 "에디터 숲 안 약 53fps" 같은 단발 수치뿐이다. 에디터가 백그라운드이면 프레임이 거의 진행되지 않는 함정이 있다(`context-notes.md`).
- 사용 가능한 도구: Unity Editor MCP(`get_performance_stats`, `eval`, `editor_play`, `capture_game_view`), 개발 빌드 실행 인자(`-mp-*` 시험 코드 패턴), EditMode 테스트(`ZGV.Core` 순수 규칙).
- `ProjectSettings/QualitySettings.asset`에 미커밋 변경이 있다. 사용자 작업으로 간주하고 덮어쓰지 않는다.

## Goals / Non-Goals

**Goals:**
- 네 가지 상황을 한 명령으로 반복 재현·기록하는 가벼운 측정 수단을 만든다.
- 측정에서 가장 큰 비중인 병목 2~3개만 고쳐 상황별 프레임 예산을 지킨다.
- 판단 로직(거리별 갱신 단계)은 `ZGV.Core` 순수 규칙으로 두어 EditMode로 검증한다.

**Non-Goals:**
- 플랫폼별 빌드 최적화, 새 렌더 기능(GPU Resident Drawer 등) 도입 자체를 목표로 삼지 않는다(측정에서 이득이 확인될 때만 검토).
- 외부 에셋 소스 수정, 좀비 행동 규칙 변경, 네트워크 프로토콜 구조 변경.

## Decisions

1. **측정 먼저, 최적화는 나중(1단계 산출물 = 병목 순위표).** 네 상황 각각을 개발 빌드 인자(예: `-perf-scenario=horde|base|world|mp`)로 자동 실행하고, 일정 시간 동안 프레임 시간 분포와 `ProfilerRecorder` 마커(스크립트, 렌더, 물리, GC)를 로그로 남긴다.
   - 대안: 에디터 Profiler 창을 손으로 보는 방식. 재현성이 없고 전후 비교가 어려워 기각. 에디터 `get_performance_stats`는 보조로만 쓴다.
2. **예산 수치는 첫 측정 후 확정.** 지금 임의의 fps를 박지 않는다. 기준 환경(이 PC, 개발 빌드)과 상황별 예산(예: 목표 프레임 시간과 상위 1% 한도)은 1단계 결과와 함께 `context-notes.md`에 기록하고 필요하면 spec을 갱신한다.
   - 대안: 처음부터 60fps 같은 고정 목표. 병목과 기준 환경을 모르고 정하면 맞지 않을 수 있어 기각.
3. **좀비 다수 병목은 "거리 기반 갱신 단계(LOD of logic)"를 우선 검토.** 플레이어와의 거리·가시성에 따라 먼 좀비의 부가 컴포넌트(숨쉬기, 발소리, 셰이더 FX, 햇빛 화상 검사, 오디오)를 낮은 주기로 돌리거나 끈다. 단계 결정은 `ZGV.Core`의 순수 규칙(거리 → 단계)으로 만들고, 컴포넌트는 그 결과만 읽는다. 서버 권위 결과(데미지, 사망, 전리품)와 AI 상태 전이는 멀리서도 같은 규칙으로 계속 돌린다.
   - 대안: 좀비 컴포넌트를 하나의 매니저 루프로 통합. 효과는 크지만 변경 범위가 커서 측정에서 `Update` 호출 오버헤드 자체가 지배적일 때만 채택.
4. **렌더 병목이면 애니메이터 컬링·그림자·머티리얼 인스턴스 정리를 먼저.** 이미 있는 `updateWhenOffscreen`/컬링 모드 조정과 충돌하지 않게 `IndoorView` 등의 예외를 확인하고 바꾼다. 시각 품질이 달라질 수 있는 항목(그림자 거리, 풀·나무 밀도)은 spec 요구대로 사용자 승인 또는 캡처 비교를 거친다.
5. **전수 검색은 캐시/등록 방식으로.** `WorldSync`의 `FindObjectsByType` 반복은 측정에서 비중이 확인될 때만, 생성/파괴 시 등록하는 목록으로 바꾼다. 단독 사용이어서 별도 추상화는 만들지 않는다.
6. **멀티플레이는 "전송 빈도와 크기"를 측정.** 상태 보고·동기화 RPC 주기(체력 2초 등)를 늘리거나 변경분만 보내는 개선은 측정으로 호스트/참가자 CPU 또는 대역이 병목일 때만 한다. 서버 권위 규칙은 바꾸지 않는다.
7. **한 항목 = 한 커밋.** 항목마다 전후 수치, EditMode, 싱글과 호스트+참가자 콘솔 예외 0건을 확인한 뒤 semantic commit 한다(`CLAUDE.md` Git 원칙). 개선이 없으면 되돌린다.

## Risks / Trade-offs

- [에디터 수치와 빌드 수치가 다르다] → 예산 판정은 개발 빌드 기준, 에디터는 상대 비교용으로만 사용
- [에디터가 백그라운드이면 프레임이 안 돈다] → 측정 시작 시 `Time.frameCount` 증가를 확인하고 아니면 무효 처리(spec 시나리오)
- [거리 기반 갱신 때문에 좀비가 가까이 올 때 튀거나 동작이 달라진다] → 단계 전환에 여유 거리(히스테리시스)를 두고 근접 시 즉시 최고 단계로 복귀, 전환 직후 동작을 플레이로 확인
- [시각 품질 저하] → 사용자 승인/캡처 비교 없이는 품질 설정을 낮추지 않는다
- [측정 코드가 제품 빌드에 남는다] → 개발 빌드 전용(`DEVELOPMENT_BUILD`/인자)으로 한정
- [미커밋 `QualitySettings.asset` 변경과 충돌] → 품질 설정을 건드리기 전에 diff를 확인하고 사용자에게 알린다
- [병목이 외부 에셋 내부(Gaia/MicroVerse 지형 등)에 있다] → 직접 수정하지 않고 설정·어댑터로만 대응하고, 안 되면 한계로 기록

## Open Questions

- (확정) 목표는 60fps, 평균 프레임 시간 16.7ms 이하(사용자 결정 2026-10-13). 기준 환경은 이 PC의 개발 빌드이고 에디터 수치는 상대 비교용이다.
- 좀비 다수 상황의 "기준 수"(예: 30/60/100마리) — 1단계에서 실제 스폰 상한을 확인한 뒤 결정
