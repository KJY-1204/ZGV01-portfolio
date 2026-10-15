# Proposal

## Why

기능은 Phase 3 18단계까지 넓게 쌓였지만, 직접 플레이하면 프레임 저하가 가장 거슬린다. 체감되는 상황은 월드 이동·스트리밍 중, 좀비가 많이 모일 때, 건축·거점이 커졌을 때, 멀티플레이 중이다. 지금까지 성능은 "숲에서 약 53fps" 같은 단발 측정뿐이어서 병목이 어디인지, 얼마나 줄여야 하는지 기준이 없다. 추측으로 최적화하지 않고(`rules.md`·`context-notes.md`의 프로파일링 우선 원칙) 먼저 측정하고, 측정으로 확인된 병목만 줄이기 위해 지금 정리한다.

## What Changes

- 네 가지 상황(월드 이동, 좀비 다수, 큰 거점, 멀티플레이)을 반복해서 재현하는 성능 측정 절차를 만든다. 프레임 시간(평균, 상위 1% 최악), CPU 스크립트/렌더/물리 비중, GC 할당을 같은 조건으로 기록한다.
- 측정 결과로 병목 순위를 매기고, 영향이 큰 상위 항목만 고친다. 후보(측정 전 가설이며 확정이 아님):
  - 좀비 한 마리가 가진 매 프레임 `Update`/`LateUpdate` 컴포넌트 약 9개(`ZombieAI`, `ZombieAnimator`, `ZombieAudio`, `ZombieFootsteps`, `ZombieBreathing`, `ZombieHitReaction`, `ZombieStrikeAnimation`, `ZombieShaderFX`, `ZombieSunBurn`)의 비용과 멀리 있는 좀비의 갱신 주기·LOD
  - 좀비 렌더링(스킨드 메시 개수, 머티리얼 인스턴스, 셰이더 효과)과 애니메이터 컬링
  - `WorldSync`의 `FindObjectsByType` 주기 전수 검색, 건축물·전리품이 늘 때의 선형 비용
  - 건축물 수에 비례하는 렌더링·물리 비용(배칭, 콜라이더, 조명 개수)
  - 지형/풀/나무 스트리밍과 멀티플레이 상태 전송 빈도
- 최적화 전후 수치를 같은 절차로 비교해 `context-notes.md`에 남기고, 회귀를 막는 프레임 예산 기준을 정한다.
- 게임 규칙과 서버 권위 구조는 바꾸지 않는다. 시각 품질을 눈에 띄게 낮추는 변경은 하지 않거나 사용자에게 먼저 묻는다.

## Capabilities

### New Capabilities
- `runtime-performance-budget`: 대표 부하 상황별 프레임 예산, 측정 방법, 최적화가 지켜야 하는 동작 보존 조건을 정의한다.

### Modified Capabilities
<!-- 기존 openspec/specs 가 비어 있어 수정 대상 없음 -->

## Impact

- 코드(측정 후 확정): `Assets/_Project/Scripts/AI/`(좀비 컴포넌트), `Networking/WorldSync.cs`와 네트워크 동기화 주기, `Building/`, `World/`, 필요하면 URP·Quality 설정(`ProjectSettings/QualitySettings.asset`은 이미 미커밋 변경이 있어 건드리기 전에 확인한다).
- 측정 보조 코드(프로파일링 마커, 테스트 씬 또는 개발 빌드 인자)가 추가될 수 있다.
- 외부 에셋(`SurvivalEngine`, `Devion Games` 등)은 직접 고치지 않고 프로젝트 측 어댑터로 처리한다.
- 범위 밖: 새 기능, 좀비 AI 행동/난이도 변경, UI 시각 개편, 멀티플레이 신뢰성 개선, 플랫폼별 빌드 최적화.
- 알려진 제약: 에디터가 백그라운드이면 프레임이 거의 진행되지 않으므로 측정 전 `editor_focus` 확인이 필요하고, 에디터 수치는 빌드보다 느리다.
