# AGENTS.md

이 파일은 Codex와 다른 코딩 에이전트가 저장소를 탐색하기 위한 짧은 시작점이다. 상세 내용을 이 파일에 계속 추가하지 말고 아래 기준 문서로 이동한다.

## Repository Map

- 게임 제품 방향과 범위는 `DESIGN.md`를 따른다.
- 기술 아키텍처와 시스템 경계는 `system.md`를 따른다.
- 반복 개발 규칙과 골든 원칙은 `rules.md`를 따른다.
- 현재 작업 계획은 `plan.md`를 따른다.
- 진행 상태는 `checklist.md`를 갱신한다.
- 결정과 발견 사항은 `context-notes.md`에 이어서 기록한다.

문서와 실제 프로젝트가 충돌하면 관련 코드, 씬, 프리팹, `ProjectVersion.txt`, `manifest.json`을 직접 확인하고 문서 드리프트를 기록한다.

## Project Intent

Unity 6 기반 TPS 좀비 생존 게임을 만든다. 핵심 루프는 탐험, 자원 파밍, 좀비 전투, 전리품, 제작, 거점 건설, 확장이다. 싱글플레이와 협동 멀티플레이가 같은 게임 규칙을 사용해야 한다.

ARK: Survival Evolved의 생존·파밍·거점 루프와 Palworld의 UI 정보 계층을 참고하되 특정 게임의 표현을 1:1로 복제하지 않는다.

## Before Coding

- 추측하지 말고 실제 작업 파일과 호출 지점을 먼저 읽는다.
- 여러 해석이 가능하면 가정을 적고 필요한 경우 질문한다.
- 더 단순한 해법이 있으면 먼저 제시한다.
- 요청받지 않은 기능과 미래용 추상화를 만들지 않는다.
- 미커밋 변경은 사용자 작업으로 간주하고 관련 없는 변경을 되돌리지 않는다.

비단순 작업에서는 코딩 전에 `plan.md`, `checklist.md`, `context-notes.md`를 만들거나 갱신한다.

## Unity Ground Rules

- 실제 Unity 버전과 패키지는 저장소 설정을 확인한다.
- URP, Input System, UI Toolkit을 프로젝트 기본 방향으로 본다.
- GameObject/MonoBehaviour 멀티플레이는 실제 패키지 상태를 확인한 뒤 Netcode for GameObjects를 우선 검토한다.
- 새 C# 프로젝트 소스 파일 첫 줄에는 역할을 설명하는 한 줄짜리 한국어 주석을 둔다. 생성 파일과 외부 패키지는 제외한다.
- 외부 에셋 폴더를 직접 대규모 수정하지 않는다.
- `.meta` 파일을 잃지 않는다.

## Architecture Invariants

- 입력을 바로 결과에 연결하지 않는다. `Input → Request → Validation → State Change → Presentation` 흐름을 선호한다.
- 게임 결과를 바꾸는 상태 변경 지점을 제한한다.
- ScriptableObject 정적 정의와 런타임 상태·저장 상태를 분리한다.
- Unity InstanceID를 영속 ID로 사용하지 않는다.
- UI, VFX, 카메라는 표현 계층이며 권위 게임 상태를 직접 확정하지 않는다.

멀티플레이에서 서버가 최종 확정해야 하는 대표 상태는 데미지, 사망, 탄약 결과, 전리품, 인벤토리, 제작, 건축, AI, 성장, 월드 저장이다.

카메라, 조준점, UI 애니메이션, 로컬 반동, 예측 총구 화염은 로컬에서 즉시 처리할 수 있다.

## Single + Multiplayer Rule

싱글플레이용 게임 규칙과 멀티플레이용 게임 규칙을 복제하지 않는다. 같은 도메인 규칙을 싱글에서는 로컬 권위가 실행하고 멀티에서는 서버 권위가 실행하도록 구성한다.

원격 플레이어는 로컬 입력을 읽지 않는다. 카메라, AudioListener, 로컬 HUD도 소유자에게만 활성화한다.

## UI Rule

UI Toolkit을 사용할 때 UXML은 구조, USS는 스타일, C#은 상태 연결과 상호작용을 담당한다. 인벤토리·제작·보관함 같은 반복 목록은 ListView를 검토한다. UI가 인벤토리 배열이나 게임 상태 필드를 직접 수정하지 않는다.

## Performance Rule

성능 문제를 기억으로 추측하지 않는다. Unity Profiler, Profile Analyzer, Frame Debugger, Rendering Debugger, 네트워크 시뮬레이션 결과를 먼저 확인한다.

GPU Resident Drawer, GPU 오클루전, VFX Graph, 데칼, APV 같은 기능은 항상 이득이라고 가정하지 않는다.

## Surgical Changes

- 요청과 직접 관련된 줄만 변경한다.
- 주변 코드를 이유 없이 정리하거나 포맷하지 않는다.
- 자신의 변경으로 사용되지 않게 된 코드만 정리한다.
- 기존 스타일과 네이밍을 따른다.
- 다른 문제가 보이면 삭제하지 말고 보고한다.

## Verification

코드를 변경했다면 완료 전에 가장 작은 관련 검증부터 실행한다.

- 순수 로직은 EditMode 테스트를 우선한다.
- 씬, 프리팹, MonoBehaviour 연결은 PlayMode 또는 전용 Test 씬으로 검증한다.
- 네트워크 변경은 가능하면 Host + Client 또는 Multiplayer Play Mode로 검증한다.
- 테스트가 없으면 최소한 Unity 컴파일 오류가 없는지 확인한다.

실패하면 실제 Console 오류와 Stack Trace를 읽고 원인을 확인한 뒤 수정한다. 흔한 해결책을 먼저 적용하지 않는다.

## Git Workflow

한 논리 변경을 완료하고 검증한 뒤 커밋한다. 현재 프로젝트는 GitHub에 커밋 후 push하며 진행하는 흐름을 기본으로 한다.

- 커밋은 한 문장으로 설명 가능한 크기로 나눈다.
- `git status`와 diff를 확인한 뒤 커밋한다.
- 검증 실패 상태를 성공 커밋처럼 취급하지 않는다.
- 원격과 인증이 준비되어 있으면 검증된 커밋을 push한다.
- 강제 push와 히스토리 재작성은 명시 요청 없이는 금지한다.

## Final Reply Evidence

최종 답변에는 실제로 실행한 검사와 결과를 적는다. 실행하지 못한 검증이 있으면 이유와 남은 위험을 적는다. 근거 없이 완료했다고 단정하지 않는다.

## Korean Output

사용자가 한국어로 말하면 한국어로 답한다. 한국어 문장을 콜론으로 끝내지 않는다. 문장 종결은 `.`, `?`, `!`를 사용한다.
