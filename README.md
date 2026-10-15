<p align="center"><img src="Assets/_Project/UI/Icons/Logo_ZGV.png" width="120" alt="ZGV01 로고"></p>

<h1 align="center">ZGV01 · 좀비 생존 게임</h1>

<p align="center">
Unity 6 기반 3인칭 좀비 생존 게임 · 싱글 / 협동 멀티플레이<br>
개발과 QA를 함께 맡아 진행한 개인 프로젝트 (2026.09.22 ~ 2026.10.13)
</p>

---

## 이 저장소에 대해

이 저장소는 **포트폴리오 공개용**입니다. 직접 작성한 게임 코드, 자동화 테스트, 셰이더, UI 레이아웃, 개발·QA 기록 문서만 담았습니다.

Asset Store 등 외부 에셋(캐릭터·건물·동물 모델, 사운드, 애니메이션, 서드파티 스크립트)은 라이선스상 재배포할 수 없어서 넣지 않았습니다. 그래서 **이 저장소만으로는 Unity에서 게임을 열어 실행할 수 없습니다.** 실제 개발은 외부 에셋까지 포함된 비공개 저장소에서 진행했습니다.

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6000.3.23f1, URP 17.3 |
| 네트워크 | Netcode for GameObjects 2.13 (Host + Client, 서버 권위) |
| 입력 / UI | Input System 1.20, UI Toolkit (UXML · USS) |
| 테스트 | Unity Test Framework (NUnit) EditMode |
| 협업 도구 | Claude Code (계획 · 체크리스트 · 증거 기반 보고를 강제하는 하네스 방식) |

## 게임 소개

탐험 → 파밍 → 전투 → 제작 → 건축으로 이어지는 생존 루프의 TPS 게임입니다.

- **전투**: 히트스캔 사격, 탄약 · 재장전, 맨손 공격, 근접 · 궁수 좀비
- **생존**: 체력, 허기 · 갈증, 사망 후 침대 부활과 시체 가방
- **제작 · 건축**: 레시피 제작, 장비 장착, 격자 스냅 건축, 건축물 내구도와 붕괴
- **월드**: 낮밤 하늘, 가로등 · 조명, 동물 사냥 · 맹수, 좀비 위협 규칙
- **저장**: 슬롯 3개, 수동 저장과 10분 자동 저장 분리
- **멀티플레이**: 싱글과 같은 게임 규칙을 서버가 실행 (데미지, 아이템, 인벤토리, 건축, AI, 저장)

```text
Input → Request → Authority Validation → State Change → Event/Replication → Presentation
```

## QA 하이라이트

| 287 | 45 | 20 | 6 / 6 |
| :---: | :---: | :---: | :---: |
| EditMode 테스트 케이스 | 원인을 기록한 결함 수정 | 멀티플레이 시험용 실행 인자 | 성능 시나리오 60fps 충족 |

### 테스트 가능한 구조

- 게임 코드가 한 어셈블리에 있어 테스트가 참조할 수 없었습니다. 의존성 없는 규칙만 [`ZGV.Core`](Assets/_Project/Scripts/Core) 어셈블리로 옮겨 테스트할 수 있게 했습니다.
- `Time.deltaTime`을 직접 읽던 생존 수치를 `Tick(dt)`로 바꿔 시간을 주입합니다 ([`SurvivalStats.cs`](Assets/_Project/Scripts/Core/SurvivalStats.cs)).
- 판정을 순수 함수로 분리했습니다: [`SavedPositionRule`](Assets/_Project/Scripts/Core/SavedPositionRule.cs), [`AutoSaveRule`](Assets/_Project/Scripts/Core/AutoSaveRule.cs), [`ZombieThreatRule`](Assets/_Project/Scripts/Core/ZombieThreatRule.cs), [`PendingRequests`](Assets/_Project/Scripts/Core/PendingRequests.cs)

### 자동화 테스트 ([`Tests/EditMode`](Assets/_Project/Tests/EditMode), 31개 파일)

| 영역 | 케이스 | 대표 파일 |
| --- | ---: | --- |
| 인벤토리 · 제작 · 장비 | 68 | `InventoryTests`, `StorageTransferTests`, `EquipmentTests` |
| 좀비 · 동물 AI | 66 | `AnimalBrainTests`, `PredatorBrainTests`, `ZombieThreatRuleTests` |
| 월드 · 환경 · 설정 | 50 | `NightLightScheduleTests`, `GameSettingsTests`, `SkyBlendWeightsTests` |
| 체력 · 생존 · 부활 | 48 | `HealthTests`, `SurvivalStatsTests`, `ReviveTests` |
| 저장 · 자동 저장 | 30 | `SaveDataTests`(실제 옛 세이브로 하위 호환 확인), `SavedPositionRuleTests` |
| 멀티플레이 복제 | 25 | `HealthReplicationTests`, `InventoryReplicaTests`, `PendingRequestsTests` |

### 결함 추적 예시

모든 결함은 **증상 → 재현 → 원인 → 수정 → 같은 조건 재검증** 순서로 [`context-notes.md`](context-notes.md)에 기록했습니다.

- **건축 미리보기가 시점을 따라가지 않음**: `카메라 위치 + 정면 × 3m`로 계산했는데 카메라가 캐릭터 뒤 3.5m에 있어서 결과가 플레이어 중심 반경 0.5m 원이 되었습니다. 기준점을 캐릭터로 바꾸고 규칙 문서에 등록했습니다 ([`rules.md`](rules.md) 26번).
- **깨진 세이브 로드 시 예외**: 테스트를 작성하다 `JsonUtility.FromJson`이 null이 아니라 예외를 던져 null 처리 분기에 도달할 수 없다는 것을 발견했습니다.
- **이어하기 시 지형 아래로 추락**: 옛 맵 좌표를 그대로 적용한 것이 원인이었습니다. 지형 아래 좌표를 판정하는 순수 규칙과 테스트를 추가했습니다.
- **보관함 연속 클릭**: 서버 응답 전 같은 요청이 중복 전송되었습니다. 요청 중 칸 잠금과 1.5초 자동 해제를 넣었습니다.

### 성능 측정

개발 전용 측정 도구 [`PerfProbe`](Assets/_Project/Scripts/Utilities/PerfProbe.cs)를 만들어 개발 빌드(1920×1080, vsync 끔)에서 6개 상황을 쟀습니다.

| 상황 | 평균 프레임 | 평균 fps | 하위 1% |
| --- | ---: | ---: | ---: |
| idle | 8.54ms | 117 | 18.5ms |
| world (이동) | 9.66ms | 104 | 21.3ms |
| base (건축물 300) | 8.85ms | 113 | 19.5ms |
| horde (좀비 40) | 11.45ms | 87 | 24.1ms |
| 멀티 호스트 | 10.49ms | 95 | 24.8ms |
| 멀티 참가자 | 9.78ms | 102 | 19.2ms |

평균은 모두 60fps 예산(16.7ms)을 충족했지만 하위 1% 프레임이 길었습니다. 끊긴 프레임 기록에서 0.75초 간격의 환경광 재계산([`SkyCycle`](Assets/_Project/Scripts/World/SkyCycle.cs))을 찾아 주기를 3초로 늘렸고, 좀비 40마리 상황의 하위 1% 프레임이 24.1ms에서 18.4ms로 줄었습니다.

측정이 틀렸던 경우도 기록했습니다. 에디터 도구 호출 직후 프레임으로 잘못 결론 낸 사례, 측정 흔들림 범위 안의 차이를 병목으로 오인한 사례는 결론과 코드를 되돌리고 대조군을 함께 측정하는 규칙으로 바꿨습니다.

## 폴더 구성

```text
Assets/_Project/
├─ Scripts/      게임 코드 195개 파일 (AI, Combat, Core, Networking, Save, UI, World …)
│  └─ Core/      테스트 대상 순수 규칙 (ZGV.Core 어셈블리)
├─ Tests/        EditMode 테스트 (ZGV.Tests.EditMode 어셈블리)
├─ Shaders/      URP 셰이더 (좀비 피격 · 디졸브, 건축 홀로그램, 물, 하늘, 오염 블렌딩)
└─ UI/           UXML · USS 레이아웃
openspec/        변경 제안 · 설계 문서
```

## 문서

| 문서 | 내용 |
| --- | --- |
| [`DESIGN.md`](DESIGN.md) | 제품 방향, 디자인 기둥, 핵심 루프 |
| [`system.md`](system.md) | 기술 아키텍처와 책임 경계 |
| [`rules.md`](rules.md) | 개발 · 검증 · Git 골든 규칙 26개 |
| [`plan.md`](plan.md) · [`checklist.md`](checklist.md) | 단계별 목표와 실행 체크리스트 |
| [`context-notes.md`](context-notes.md) | 결정, 결함 원인, 측정 기록 (1,300줄+) |
| [`CLAUDE.md`](CLAUDE.md) · [`AGENTS.md`](AGENTS.md) | AI 협업 하네스 설정 |

## 참고

- `ZGVGrimeLit` / `ZGVGrimeSimpleLit` 셰이더는 URP Lit / Simple Lit 셰이더를 복사해 오염 블렌딩을 더한 것입니다.
- 문서 안에 나오는 외부 에셋 이름과 경로는 비공개 개발 저장소 기준입니다.

## 연락처

- GitHub: [KJY-1204](https://github.com/KJY-1204)
- Mail: dbvcxz@naver.com
