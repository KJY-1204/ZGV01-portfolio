# CLAUDE.md

이 파일은 Claude Code가 세션 시작 시 읽는 프로젝트 브리핑이다. 장문의 규칙을 여기에 복제하지 말고 저장소의 기준 문서를 탐색한다.

## Read First

작업과 관련된 범위만 점진적으로 읽는다.

1. 제품 목표와 기능 범위는 `DESIGN.md`.
2. 기술 아키텍처와 책임 경계는 `system.md`.
3. 개발·검증·Git 골든 원칙은 `rules.md`.
4. 현재 작업 목표는 `plan.md`.
5. 할 일과 완료 상태는 `checklist.md`.
6. 결정과 리스크는 `context-notes.md`.

실제 Unity 프로젝트의 코드와 설정이 문서보다 최신일 수 있다. 관련 파일과 `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`을 확인한 뒤 판단한다.

## Project Brief

Unity 6 기반 3인칭 좀비 생존 게임이다. 플레이어는 월드를 탐험하고 자원을 파밍하며 좀비와 싸워 전리품을 얻고, 제작과 건축으로 거점을 확장한다. 싱글플레이와 협동 멀티플레이를 모두 목표로 한다.

ARK: Survival Evolved의 생존·자원·거점 루프와 Palworld의 UI 정보 계층을 참고하되 표현을 그대로 복제하지 않는다. Division 스타일 자료는 TPS 전투와 아키텍처 참고 자료이며 커버 슈터를 그대로 구현하는 것이 목표가 아니다.

## Think Before Coding

- 모르는 것을 추측하지 않는다.
- 여러 해석이 있으면 선택 근거를 적고 필요한 경우 질문한다.
- 간단한 해결책이 충분하면 복잡한 프레임워크를 만들지 않는다.
- 실제 파일과 근처 호출 지점을 읽지 않고 수정하지 않는다.
- 미커밋 변경을 사용자 작업으로 간주하고 덮어쓰지 않는다.

비단순 작업 전에 `plan.md`, `checklist.md`, `context-notes.md`를 만들거나 갱신한다.

## Core Architecture

게임 결과를 다음 흐름으로 생각한다.

```text
Input → Request → Authority Validation → State Change → Event/Replication → Presentation
```

싱글플레이와 멀티플레이의 게임 규칙을 따로 복제하지 않는다. 싱글에서는 로컬 권위, 멀티에서는 서버 권위가 같은 도메인 규칙을 실행한다.

멀티플레이에서 서버 권위로 확정하는 대표 값은 데미지, 사망, 탄약 결과, 아이템 획득, 드랍 RNG, 인벤토리, 제작, 건축, AI, 성장, 저장이다.

로컬 카메라, 조준점, UI, 화면 흔들림, 예측 총구 화염은 클라이언트 표현이다.

## Data Rules

- 정적 게임 정의는 ScriptableObject를 우선 검토한다.
- 런타임 상태와 저장 상태를 ScriptableObject 에셋에 직접 기록하지 않는다.
- 영속 데이터와 네트워크 메시지는 안정적인 DefinitionId와 WorldId를 사용한다.
- Unity InstanceID를 저장 키로 사용하지 않는다.
- UI가 도메인 상태 필드를 직접 변경하지 않는다.

## Unity Rules

- 신규 입력은 Input System을 기준으로 한다.
- UI Toolkit 사용 시 UXML, USS, C# 책임을 분리한다.
- 반복 데이터 UI는 ListView와 런타임 바인딩을 검토한다.
- UI 바인딩 converter를 무겁게 만들지 않는다.
- URP/VFX 최적화는 Profiler와 Rendering Debugger 결과를 근거로 한다.
- 외부 에셋을 직접 개조하기보다 프로젝트 측 어댑터를 우선한다.

새 C# 프로젝트 파일 첫 줄에는 역할을 설명하는 한 줄짜리 한국어 주석을 둔다. 생성 파일과 외부 패키지는 제외한다.

## Simplicity And Surgical Edits

- 요청하지 않은 기능을 추가하지 않는다.
- 한 번만 쓰는 추상화를 만들지 않는다.
- 관련 없는 코드 정리와 리포맷을 하지 않는다.
- 자신의 변경으로 생긴 미사용 코드만 제거한다.
- 기존 네이밍과 스타일을 따른다.

## Testing

코드를 만졌다면 완료 전에 실제 검증을 한다.

- 계산과 규칙은 EditMode 테스트.
- GameObject와 씬 통합은 PlayMode 또는 Test 씬.
- 네트워크는 Host + Client 또는 지원되는 멀티 인스턴스 테스트.
- 테스트가 없으면 최소한 Unity 컴파일을 확인한다.

오류가 나면 Console과 Stack Trace를 읽고 재현한 뒤 수정한다. 오류 키워드만 보고 흔한 패치를 적용하지 않는다.

## Git

한 논리 변경을 검증한 뒤 semantic commit을 만든다. 이 프로젝트는 검증된 커밋을 GitHub에 push하며 진행하는 흐름을 기본으로 한다.

- 커밋 전 `git status`와 diff를 확인한다.
- 커밋 메시지는 변경 목적이 한 문장으로 드러나게 쓴다.
- 원격 저장소 또는 인증이 준비되지 않았으면 push를 억지로 시도하지 않고 상태를 보고한다.
- 강제 push, reset으로 사용자 변경 제거, 히스토리 재작성은 명시 요청 없이는 하지 않는다.

## Context Maintenance

작업 중 새 결정이 생기면 `context-notes.md`에 이유와 함께 기록한다. 반복되는 실수는 채팅에서만 교정하지 말고 `rules.md`, 테스트, 검증 도구 중 적절한 위치로 승격한다.

문서가 너무 커지면 `CLAUDE.md`에 세부 내용을 추가하지 말고 주제별 기준 문서로 이동한다.

## Final Response

최종 답변에는 변경한 파일, 실행한 검증 명령 또는 검사, 결과, 남은 위험을 적는다. 실제로 실행하지 않은 테스트를 실행했다고 말하지 않는다.

사용자가 한국어로 대화하면 한국어로 답하고 한국어 문장을 콜론으로 끝내지 않는다.
