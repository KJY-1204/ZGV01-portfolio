# Context Notes

## 2026-09-22 초기 결정

- 프로젝트 목표는 Unity 6 기반 3인칭 좀비 생존 게임이다. 핵심 루프는 탐험, 자원 파밍, 좀비 전투, 전리품 획득, 귀환, 제작, 건축, 확장으로 잡는다.
- ARK: Survival Evolved에서는 생존·자원·기지 건설의 장르적 루프를 참고하고, Palworld에서는 UI의 정보 계층과 조작 흐름을 참고한다. 특정 게임의 아트, 아이콘, 레이아웃을 1:1로 복제하지 않는다.
- 기존 Division 계열 자료는 커버 슈터 전체를 그대로 도입하는 용도가 아니라 TPS 입력, 카메라, 사격, AI, 데이터 분리, 멀티플레이 권위 구조를 참고하는 용도로 사용한다.
- 싱글플레이와 멀티플레이가 서로 다른 게임 로직을 갖지 않도록 한다. 게임 결과를 바꾸는 상태 변경은 공통 권위 경로에 모으고, 싱글플레이는 로컬 권위로, 멀티플레이는 서버 권위로 같은 규칙을 실행한다.
- 카메라, 조준점, 즉시 피드백, UI 애니메이션은 로컬 표현 계층에 둔다. 데미지, 사망, 탄약 확정, 아이템 획득, 드랍 RNG, 제작, 건축, 저장, AI 최종 판단은 권위 계층이 확정한다.
- UI는 UI Toolkit을 기본으로 보고 UXML/USS와 런타임 데이터 바인딩을 사용한다. 인벤토리, 보관함, 제작 목록처럼 반복 데이터는 ListView를 우선 검토한다.
- 정적 게임 정의는 ScriptableObject로 관리하되 런타임 상태와 저장 상태를 ScriptableObject에 직접 기록하지 않는다.
- 대형 월드는 처음부터 전체 오픈월드를 구현하지 않는다. 작은 수직 슬라이스를 먼저 완성하고, 이후 additive scene 또는 Addressables 기반 스트리밍 경계를 도입한다.
- VFX와 URP 기능은 시각 품질보다 프로파일링 결과를 우선한다. GPU Resident Drawer, GPU 오클루전, VFX Graph 기능은 프로젝트 조건과 타깃 플랫폼에서 실제 이득을 확인한 뒤 활성화한다.
- Git 작업은 한 논리 단위마다 검증 후 커밋하고 푸시하는 흐름을 기본으로 한다. 강제 푸시와 사용자 변경 덮어쓰기는 금지한다.

## 2026-09-22 소스 분석 반영

- `Unity 6 TPS 장르의 Division 클론 프로젝트 기초편.pdf`에서는 플레이 루프 우선 개발, 입력과 캐릭터 역할 분리, 기능별 폴더, Test/Prototype 씬, ScriptableObject 데이터화 원칙을 반영했다.
- `Unity 6 TPS 장르의 Division 클론 프로젝트 중급편.pdf`에서는 장비·드랍·인벤토리·스탯·AI 구조와 싱글/멀티 전환을 고려한 상태 분리 원칙을 반영했다. 커버 시스템 자체는 현재 게임 핵심 요구가 아니므로 필수 범위에서 제외했다.
- `Unity 6 TPS 장르의 Division 클론 프로젝트 고급편.pdf`에서는 입력을 요청으로 분리하고 서버가 검증·실행·동기화하는 구조, 상태 변경 진입점 제한, 로컬/원격 플레이어 분리, 서버 권위 AI 원칙을 반영했다.
- `UI_Toolkit_FINAL-en_us-ko_kr-CR-C_251105_Feedback_imp.pdf`에서는 UXML/USS 분리, 런타임 데이터 바인딩, ListView, 가벼운 converter, UI 데이터 소스 중간 계층과 UI 성능 원칙을 반영했다.
- `URP_Cookbook_ko-KR_final.pdf`에서는 GPU Resident Drawer, GPU 오클루전, 데칼, APV와 렌더링 디버거를 참고했다. 기능 활성화를 규칙으로 강제하지 않고 실제 프로파일링 후 선택하도록 정리했다.
- `Unity_The_Game_Designer_Playbook_eBook_ko_KR.pdf`에서는 프로토타입 우선, ScriptableObject를 통한 데이터 분리, Input System, 플레이 테스트와 타깃 디바이스 검증 원칙을 반영했다.
- `VFX_Artist_Guide_FINAL-ko_kr.pdf`에서는 VFX Graph 이벤트, bounds/capacity, 출력 비용, Profiler/Frame Debugger 기반 최적화 원칙을 반영했다.
- `_소문난 명강의_레트로의 유니티 6 게임 프로그래밍 에센스(3판) - 이제민(레트로).pdf`는 텍스트 추출이 되지 않아 전자책 페이지 이미지를 직접 확인했다. 6부의 탑다운 슈터 좀비 서바이버에서 적 AI, UI, NavMesh, Raycast, 인터페이스·이벤트 구조를, 7부의 네트워크 게임에서 Netcode for GameObjects, RPC, 위치 동기화 학습 구성을 참고했다. 현재 프로젝트는 탑다운 시점이 아니라 TPS이므로 개념만 가져왔다.
- `Unity_Multiplayer_101_ko-KR.pdf`에서는 네트워크 토폴로지, 권한 서버, NetworkVariable과 RPC의 역할, PlayerInput 소유권, 지연과 테스트 구조를 반영했다.
- 기존 `AGENTS.md`와 `CLAUDE.md`의 Think Before Coding, Simplicity First, Surgical Changes, Plan/Checklist/Context Notes, 테스트, semantic commit, 오류 로그 우선 원칙을 유지하면서 프로젝트 특화 규칙과 문서 맵으로 압축했다.

## 2026-09-22 웹 교차 검증

- OpenAI의 Harness Engineering 글에서 `AGENTS.md`를 백과사전이 아니라 저장소 지식의 맵으로 두고, 계획과 문서를 버전 관리하며, 기계적인 경계와 검증 피드백 루프를 강화하는 원칙을 확인했다.
- Anthropic의 CLAUDE.md 가이드에서 프로젝트 지침은 짧고 구체적으로 유지하고 큰 규칙은 주제별 문서로 나누는 방향을 확인했다.
- Unity 6 공식 문서에서 Netcode for GameObjects, Multiplayer Services, UI Toolkit 런타임 데이터 바인딩과 ListView, Input System, URP GPU 오클루전 관련 현재 기능을 확인했다.

## 2026-09-22 문서 하네스 검증

- `AGENTS.md`와 `CLAUDE.md`를 각각 약 100줄의 시작점 문서로 압축하고 상세 원칙은 `DESIGN.md`, `system.md`, `rules.md`로 분리했다.
- 필수 파일 존재 여부와 문서 간 참조를 정적 검사했고 모두 통과했다.
- 이 작업 공간에는 실제 Unity 프로젝트 저장소와 `.git` 정보가 없으므로 Unity 컴파일, Test Framework, 커밋, push는 수행하지 않았다.

## 2026-09-23 저장소 인프라 정리 (Phase A)

- 실제 Unity 프로젝트에 `.git`이 없고 `Assets`에는 자체 게임 코드 없이 에셋스토어 구매 패키지(SurvivalEngine, Devion Games 계열, Kinemation, Survivalist StarterAssets, ar_buildings 등)만 약 10GB 쌓여 있는 상태를 확인했다.
- `rules.md` 21절 기준으로 `.gitignore`를 작성했다. `Library`, `Temp`, `Logs`, `UserSettings`, IDE가 재생성하는 `*.csproj`/`*.sln`/`*.slnx`는 제외하고 `ProjectSettings`, `Packages`, 문서, `.vscode`, `.vsconfig`는 추적한다.
- `Assets` 폴더를 커밋 범위에 넣을지 사용자에게 확인했다. 10GB 규모라 원격에 그대로 push하면 GitHub 개별 파일 100MB 제한에 걸릴 가능성이 크고, LFS 없이 커밋하면 히스토리 재작성 없이는 되돌리기 어렵다는 점을 근거로 세 가지 선택지(그대로 커밋 / Git LFS 도입 / 압축 백업 후 제외)를 제시했다.
- 사용자는 "에셋들을 전부 압축한 뒤 에셋을 제외하고 커밋 후 푸시한다"를 선택했다. 이에 따라 `Assets` 전체를 `tar -czf ../ZGV01-Assets-backup-20260923.tar.gz Assets`로 프로젝트 상위 폴더(`/Users/mr5/Desktop/26_09_16/NZS/`)에 백업하고, `.gitignore`에 `/Assets/`와 백업 압축 파일 패턴(`*-Assets-backup-*.tar.gz`, `*-Assets-backup-*.zip`)을 추가했다.
- **이유**: 자체 게임 코드가 아직 없는 상태에서 대용량 서드파티 에셋을 무분별하게 커밋하면 저장소가 비대해지고, 실제로 어떤 패키지를 채택할지 결정되지 않은 채 이력에 남는다. 압축 백업은 원본 손실 없이 저장소 크기 문제를 임시로 분리하는 방법이다.
- **후속 조치 필요**: 채택할 에셋 패키지가 정해지면 `.gitignore`의 `/Assets/` 줄을 걷어내고, 채택된 패키지만 개별적으로 추적하거나 Git LFS를 재검토한다. 그 전까지 `Assets` 실제 폴더(원본)는 디스크에 그대로 남아 있고 Unity 에디터는 정상 동작한다 — 삭제된 것이 아니라 git 추적에서만 제외된 상태다.
- Assets 제외 초기 커밋(`e27eb61`, `chore: Unity 프로젝트 저장소 초기화 (Assets 제외)`)을 `main` 브랜치에 만들었다.
- 사용자가 `gh auth login`으로 GitHub 로그인(계정 `KJY-1204`)을 완료한 뒤, `gh repo create ZGV01 --private --source=. --remote=origin`으로 private 저장소를 생성하고 `git push -u origin main`으로 두 커밋(`e27eb61`, `ffec453`)을 push했다. 원격은 `https://github.com/KJY-1204/ZGV01`이다.
- Assets 압축 백업(`ZGV01-Assets-backup-20260923.tar.gz`, 프로젝트 상위 폴더)은 push 시점에도 백그라운드에서 계속 진행 중이었다. push 자체는 Assets를 포함하지 않으므로 백업 완료를 기다릴 필요는 없었다.

## 2026-09-23 Phase B 기반 만들기

### 발견한 기존 컴파일/런타임 문제와 조치

- **`com.unity.ai.navigation@2.0.14` 패키지 캐시 손상**: `Library/PackageCache`의 `NavMeshLink.cs`가 옛 네임스페이스(`UnityEngine.AI`)로, `NavMeshLink.deprecated.cs`는 새 네임스페이스(`Unity.AI.Navigation`)를 기대하는 상태로 불일치해 컴파일이 깨져 있었다. 손상된 캐시 폴더를 삭제하고 `package_resolve`로 재추출해 해결했다(Library는 버전관리 대상이 아니라 git diff 없음).
- **`Assets/ar_buildings`**: 설치되지 않은 `com.unity.xr.arfoundation`/`arsubsystems`/`remote-config`를 참조해 컴파일 에러 19건을 유발했다. 좀비 서바이벌 게임과 무관한 AR 건축 배치 데모라 사용자 승인을 받아 `../ZGV01-quarantine/`(프로젝트 밖)으로 이동했다. 삭제는 아니므로 필요하면 원복 가능하다.
- **`Assets/Survivalist/StarterAssets/Editor/StarterAssetsDeployMenu.cs`**: Cinemachine 2.x용 `using Cinemachine;`을 쓰는데 설치된 패키지는 3.1.7(네임스페이스 `Unity.Cinemachine`)이라 컴파일이 깨졌다. `CinemachineVirtualCamera`/`CinemachineBrain` 모두 3.x에도 `Unity.Cinemachine` 네임스페이스에 존재함을 확인하고 `using` 줄만 `Unity.Cinemachine`으로 수정했다. **재임포트 위험**: `Assets/Survivalist`는 아직 채택 여부가 결정되지 않아 git에서 제외돼 있으므로, 이 벤더 파일 수정은 git에 잡히지 않는다. 이 패키지를 재설치/재임포트하면 원본(깨진 버전)으로 되돌아가니, 다시 컴파일 에러가 나면 이 노트를 보고 같은 한 줄을 다시 고치면 된다.
- **`Devion Games/Triggers`의 `TriggerRaycaster`**: `[RuntimeInitializeOnLoadMethod]`로 모든 씬에 자동 생성되는데 레거시 `UnityEngine.Input.mousePosition`을 호출한다. 프로젝트의 Active Input Handling이 "Input System Package (New)" 전용(`activeInputHandler: 1`)이라 Play Mode 진입 시 `InvalidOperationException`이 발생했다. `ProjectSettings/ProjectSettings.asset`의 `activeInputHandler`를 `2`(Both)로 바꿔 레거시 Input과 새 Input System을 함께 허용하도록 했다. **이 설정은 보통 에디터 재시작이 있어야 완전히 적용된다** — 이번 세션에서는 재시작 후 재검증을 하지 못했으므로 다음 세션에서 `Test_Camera`/`Test_Input` Play Mode를 다시 확인해야 한다.

### Unity Editor MCP 도구 신뢰성 문제 (중요)

이번 세션에서 `mcp__unity-editor-mcp__create_folder`와 `create_script`가 **실제로는 아무 것도 만들지 않고 그럴듯한 GUID가 담긴 성공 응답만 반환**하는 것을 발견했다. `Assets/_Project` 폴더 39개와 `InputDebugProbe.cs` 스크립트를 이 도구들로 "생성"했지만 실제 디스크에는 없었다. `eval`로 `UnityEditor.AssetDatabase`를 직접 호출하는 방식은 신뢰할 수 있어서 이 방식으로 다시 만들었다.

또한 세션 중간에 `editor_status`가 보고하는 `projectPath`와 `Application.dataPath`가 한때 이 프로젝트(`.../NZS/ZGV01`)가 아니라 `/Users/mr5/Desktop/26_09_16/Zombie-Survival`라는 **완전히 다른 프로젝트**를 가리켰다가 다시 `ZGV01`로 돌아오는 것을 확인했다. 사용자의 로컬 머신에 Unity 에디터가 두 개 열려 있고 MCP 브리지가 그 중 하나에 비결정적으로 연결되는 것으로 추정된다.

**이유**: 이 상태에서 도구의 성공 응답만 믿으면 엉뚱한 프로젝트를 고치거나, 아무 효과 없는 작업을 완료로 착각할 위험이 크다.

**적용한 대응**: 이후 모든 Unity 측 생성/수정 작업은 `eval`로 `UnityEngine.Application.dataPath`가 `.../NZS/ZGV01/Assets`인지 먼저 확인한 뒤 진행했고, 각 결과를 Bash로 실제 디스크 파일 존재 여부까지 재확인했다.

**후속 조치 필요**: 사용자가 `Zombie-Survival` 프로젝트를 연 에디터 창이 남아있다면 닫아서 MCP가 항상 `ZGV01`에만 연결되게 해야 한다. 그 전까지 Unity MCP 도구의 "성공" 응답은 `Application.dataPath` 확인 없이 신뢰하지 않는다.

### Phase B 결과물

- `Assets/_Project` 폴더 구조 (Art/Audio/Data/Prefabs/Scenes/Scripts/UI/VFX/Materials/Tests, system.md 4절과 동일).
- `Assets/_Project/Scripts/Items/ItemDefinition.cs`: `StableId`, `DisplayName`, `Icon`, `Category`, `MaxStack` 필드를 가진 첫 Data Definition. system.md의 `BaseProperties`는 실제 사용처가 정해지지 않아 지금은 넣지 않았다 — WeaponDefinition 등 구체적인 정의가 필요할 때 각자 필드로 추가한다.
- `Assets/_Project/Scripts/Input/InputDebugProbe.cs`: PlayerInput의 Send Messages 콜백으로 각 액션 값을 로그로 남기는 `Test_Input` 전용 디버그 컴포넌트.
- `InputSystem_Actions.inputactions`: Player 맵에 Aim/Reload/Inventory/BuildMode 액션 추가, 기존 Attack은 Fire로 개명(바인딩 유지).
- `Assets/_Project/Scenes/Test/Test_Input.unity`: `InputTestRig` GameObject에 `PlayerInput`(actions=InputSystem_Actions, map=Player, SendMessages) + `InputDebugProbe`.
- `Assets/_Project/Scenes/Test/Test_Camera.unity`: `Main Camera`(+`CinemachineBrain`), `TPS_TestTarget`(캡슐), `PlayerFollowCamera`(`CinemachineCamera`+`CinemachineOrbitalFollow`+`CinemachineRotationComposer`, Follow/LookAt=TPS_TestTarget).

## 2026-09-23 Unity 에디터 재시작 후 Play Mode 재검증

- `EditorApplication.OpenProject`로 ZGV01 에디터를 재시작했다. 재시작 후에도 `Application.dataPath`가 정상적으로 `.../NZS/ZGV01/Assets`를 가리켜 이번에는 다중 프로젝트 연결 문제 없이 진행했다.
- `Test_Camera`: Play Mode 진입 시 이전에 있던 `UnityEngine.Input` 레거시 예외가 더 이상 발생하지 않았다(Active Input Handling=Both 적용 확인). 게임 뷰 캡처로 `CinemachineOrbitalFollow`+`CinemachineRotationComposer`가 타겟을 정상적으로 프레이밍하는 것도 확인했다.
- `Test_Input`: Play Mode 진입 시 `DevionGames.TriggerRaycaster.Initialize()`에서 `NullReferenceException`이 새로 발견됐다. 원인은 이 씬에 Main Camera가 없어 `Camera.main`이 null이었기 때문이다(`TriggerRaycaster.cs:26`, `[RuntimeInitializeOnLoadMethod]`가 씬 시작 시 `Camera.main`에 자동으로 컴포넌트를 붙이려 시도). Main Camera GameObject를 씬에 추가해 해결했다.
- **일반화된 교훈**: Devion Games Triggers 패키지가 Assets에 있는 한, Play Mode로 진입하는 모든 씬은 `MainCamera` 태그가 붙은 카메라를 반드시 포함해야 한다. 앞으로 만드는 테스트/프로토타입 씬에도 이 제약이 적용된다.
- 재검증 후 남아있는 유일한 콘솔 에러는 에디터 시작 시점의 `DevionGames.WriteInputManager` 정적 생성자 오류(seeded, Editor-only, Play Mode와 무관)뿐이다. 이건 legacy Input Manager 축 정의 확인 로직이 이 프로젝트의 `InputManager.asset`과 안 맞아서 나는 것으로 보이며, Editor 콘솔에만 나타나고 게임 동작에는 영향이 없어 이번 단계에서는 그대로 둔다.

### 남은 위험

- Unity Editor MCP의 다중 프로젝트 연결 문제가 재발할 수 있으니 다음 세션에서도 `Application.dataPath` 확인을 습관화한다.
- 에디터 시작 시점의 `DevionGames.WriteInputManager` cctor 오류는 원인 미해결 상태로 남아있다(Editor 콘솔 노이즈 수준, 기능 영향 없음 확인).

Phase B 완료 — 사용자가 `Test_Input`에서 WASD 8방향 + Fire 입력이 Console에 정상적으로 찍히는 것을 직접 확인했다.

## 2026-09-23 Phase 1 시작 — 캐릭터 컨트롤러 채택

- Phase 1(TPS 전투 수직 슬라이스)을 시작하기 전, 캐릭터 이동/카메라 기반을 처음부터 새로 만들지 `Assets/Survivalist`의 Starter Assets ThirdPersonController를 어댑터로 채택할지 사용자에게 확인했다. `StarterAssetsInputs.cs`가 이미 새 Input System(`OnMove`/`OnLook`/`OnJump`/`OnSprint`, PlayerInput Send Messages)으로 동작하는 것을 미리 확인하고 근거로 제시했다.
- 사용자는 **Survivalist StarterAssets 채택**을 선택했다. 이에 따라 `.gitignore`에서 `Assets/Survivalist`를 풀어 git 추적을 시작했다(217MB, 50MB 넘는 개별 파일 없음 확인 — LFS 불필요).
- **이유**: 검증된 이동/애니메이션/카메라 타겟 연결(`PlayerArmature.prefab`에 `Animator`+`CharacterController`+`ThirdPersonController`+`StarterAssetsInputs`+`PlayerInput`+`BasicRigidBodyPush`가 이미 온전히 구성돼 있음을 확인)을 재사용하는 것이 처음부터 이동감을 튜닝하는 것보다 합리적이다. system.md의 계층 구조(Domain/Authority는 이 컨트롤러를 몰라도 되게 유지)는 그대로 지킨다 — 사격·데미지·좀비 로직은 이 벤더 스크립트에 의존하지 않고 별도 시스템으로 만든다.
- `PlayerArmature.prefab`의 `PlayerInput.actions`가 원래 벤더 자체 `Assets/Survivalist/StarterAssets/InputSystem/StarterAssets.inputactions`를 참조하고 있어서, 우리 `Assets/InputSystem_Actions.inputactions`(Fire/Aim/Reload/Inventory/BuildMode 포함)로 재연결했다. `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`으로 안전하게 수정하고 디스크에 반영된 것을 확인했다.
- `Assets/_Project/Scenes/Test/Test_Combat.unity`를 만들어 바닥, 디렉셔널 라이트, `PlayerArmature` 인스턴스, `PlayerCameraRoot`를 Follow/LookAt으로 삼는 Cinemachine 카메라(`CinemachineCamera`+`CinemachineOrbitalFollow`+`CinemachineRotationComposer`)를 배치했다. Play Mode 진입 시 에러 없이 캐릭터가 정상 렌더링되고 카메라가 뒤에서 프레이밍하는 것을 스크린샷으로 확인했다.
- **후속 확인 필요**: 실제 WASD/마우스 조작으로 이동·회전이 되는지는 사람이 직접 확인해야 한다(자동 입력 시뮬레이션 불가, `rules.md` 16절 참고).
- **다음 단계**: 사격(Fire → 히트스캔 → 데미지), 기본 좀비 AI, 드랍/획득, 저장을 system.md 6~12절 흐름대로 순서대로 연결한다.

## 2026-09-23 마우스 룩 미작동 버그 수정

- 사용자가 `Test_Combat`에서 마우스를 움직여도 화면(카메라 시점)이 회전하지 않는다고 보고했다.
- 원인: `ThirdPersonController.CameraRotation()`은 마우스 Look 입력으로 `CinemachineCameraTarget`(`PlayerCameraRoot`)의 **transform.rotation을 직접** 갱신하는 방식이다(Cinemachine 자체 입력축을 쓰지 않음). 그런데 내가 카메라 리그에 붙인 `CinemachineOrbitalFollow`는 Follow 타겟의 회전을 전혀 읽지 않고 **자기 자신의 독립적인 궤도 각도**로만 카메라를 위치시키는 컴포넌트라서, `PlayerCameraRoot`가 아무리 회전해도 카메라에는 반영되지 않았다.
- Starter Assets 원작이 쓰는 방식은 Body = `CinemachineThirdPersonFollow`(구 "3rd Person Follow")로, 이건 Follow 타�터의 회전을 그대로 읽어 어깨걸이 오프셋(ShoulderOffset)만큼 카메라를 배치한다 — 별도 Aim 컴포넌트가 필요 없다.
- `Test_Combat.unity`의 `PlayerFollowCamera`에서 `CinemachineOrbitalFollow`+`CinemachineRotationComposer`를 제거하고 `CinemachineThirdPersonFollow`(ShoulderOffset=(0.4,0.2,0), VerticalArmLength=0.2, CameraSide=1, CameraDistance=3.5)로 교체했다.
- **검증 방법과 함정**: Play Mode에 진입한 뒤 리플렉션으로 `_cinemachineTargetYaw`를 90으로 직접 바꿔 카메라가 따라 도는지 확인하려 했는데, 처음엔 아무 변화가 없었다. 원인은 버그가 아니라 **Unity 에디터가 백그라운드(포커스 밖)에 있어 `Time.frameCount`가 150초 동안 3프레임만 진행**된 것이었다 — `editor_focus`로 에디터를 전면으로 가져오자 프레임이 정상 속도로 돌기 시작했고, 그제서야 카메라 회전이 실제로 반영되는 것을 확인했다. 앞으로 Play Mode 결과를 스크린샷/상태로 검증할 때는 먼저 `Time.frameCount`가 실제로 증가하고 있는지 확인하고, 필요하면 `editor_focus`를 먼저 호출한다.
- `Test_Camera.unity`는 정적 캡슐을 바라보기만 해서 이 버그가 겉으로 드러나지 않았지만 같은 `CinemachineOrbitalFollow` 구성을 쓰고 있다. 지금은 그대로 두었지만(용도상 카메라 패키지 동작 확인용이라 문제 없음), **실제 캐릭터를 회전시키는 카메라를 새로 만들 때는 항상 `CinemachineThirdPersonFollow`(또는 대상 회전을 읽는 Body)를 쓰고 `CinemachineOrbitalFollow`를 그대로 복사하지 않는다.**
- 사용자가 수정 후 직접 확인: "감도가 느린거뺴면 괜찮네" — 마우스 룩은 정상 동작하고, 감도(sensitivity) 수치 튜닝만 남았다. 사용자 요청으로 감도 조절은 지금 하지 않고 후순위로 미뤘다가, 저장/불러오기 작업 전에 먼저 처리했다(아래 참고).

## 2026-09-23 마우스 감도 튜닝

- 원인: `ThirdPersonController.CameraRotation()`이 `_input.look.x * Time.deltaTime`로 매 프레임 값을 깎는데, Look 액션의 마우스 바인딩(`<Pointer>/delta`)은 이미 그 프레임의 픽셀 이동량(델타)이라 프레임률만큼 다시 나누는 효과가 나서 체감 감도가 매우 낮았다.
- `ThirdPersonController`/`StarterAssetsInputs`는 벤더 코드라 직접 고치지 않고, `Assets/InputSystem_Actions.inputactions`의 Look 액션 중 `<Pointer>/delta` 바인딩에만 Input System `scaleVector2(x=30,y=30)` 프로세서를 추가했다. 게임패드 바인딩(`<Gamepad>/rightStick`)은 건드리지 않았다 — 그쪽은 아날로그 스틱이라 `Time.deltaTime` 곱셈이 원래 의도대로 동작한다.
- 배율 30은 대략 60fps 기준으로 `Time.deltaTime`(~0.0167) 감쇠를 상쇄하도록 고른 시작값이다. 프레임률에 따라 완벽히 보정되진 않지만(고정 상수라 가변 deltaTime을 정확히 취소할 수 없음) 1차 근사로는 충분하다. 사용자가 다시 느리거나 빠르다고 하면 이 값만 조절하면 된다.

## 2026-09-23 Fire → 히트스캔 → 데미지 연결

- system.md 6절 사격 흐름(로컬 입력 → Fire 요청 → 권위 검증 → 히트 판정 → 데미지 적용 → 확정 이벤트)에 따라 싱글플레이 로컬 권위로 최소 구현을 추가했다. 아직 네트워크 계층이 없으므로 "권위 검증"은 이 컴포넌트 자체가 유일한 권위로 동작한다(문서 3.3절 "싱글플레이에서는 같은 검증 규칙을 로컬에서 실행한다"에 부합).
- `Assets/_Project/Scripts/Combat/Health.cs`: 체력 변경의 유일한 진입점(`ApplyDamage`, `Heal`)을 제공하는 순수 도메인 컴포넌트. `Damaged`/`Died` 이벤트만 공개하고 필드를 직접 노출하지 않는다(rules.md 3절).
- `Assets/_Project/Scripts/Combat/HitscanWeapon.cs`: `PlayerInput`의 `OnFire(InputValue)` 콜백으로 카메라 정면 레이캐스트를 쏘고, 맞은 대상의 `Health.ApplyDamage`를 호출한다. 클라이언트가 데미지를 직접 확정하는 게 아니라 이 한 경로로만 데미지가 발생하게 했다(rules.md 5절 대비 미래 서버 권위 이전 시 이 메서드만 서버로 옮기면 됨).
- `Assets/_Project/Scripts/Combat/HealthDebugPrinter.cs`: Health 이벤트를 콘솔에 출력하는 테스트 전용 표현 컴포넌트(도메인과 분리, `InputDebugProbe`와 같은 패턴).
- `PlayerArmature.prefab`에 `HitscanWeapon`을 추가했다(`PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`로 안전하게 수정). 프리팹에 추가한 컴포넌트는 씬의 기존 인스턴스에도 자동으로 반영됨을 확인했다(`Test_Combat`의 플레이어 인스턴스에 별도 작업 없이 `HitscanWeapon`이 붙어 있었다).
- `Test_Combat.unity`에 `TrainingDummy`(캡슐 + `Health` + `HealthDebugPrinter`)를 배치했다.
- **검증**: 리플렉션으로 `Fire()`를 직접 호출해 데미지 적용을 1차 확인했고, 이후 콘솔 로그의 스택 트레이스(`NativeInputSystem:NotifyUpdate` 경로)로 사용자가 실제 마우스 클릭 4번으로 `TrainingDummy`(HP 100 → 75 → 50 → 25 → 0, "died" 로그)를 직접 처치한 것까지 확인했다.
- **후속 확인 필요**: Play Mode 자동 검증 중 한 번은 HP 읽기가 75에서 멈춰 보였는데, 이는 실제 버그가 아니라 사용자가 동시에 조작 중이던 것과 내 eval 호출 타이밍이 겹쳐 생긴 스냅샷 차이였다(로그 타임스탬프로 확인). 실제 게임 로직에는 문제가 없다.

## 2026-09-23 기본 좀비 AI 연결

- system.md 9절/DESIGN.md 6절에 따라 `Assets/_Project/Scripts/AI/ZombieAI.cs`를 추가했다. 상태는 Idle/Chase/Attack/Dead 4개로 최소 구성했다(Wander/Investigate/Stagger는 아직 필요성이 검증되지 않아 보류 — DESIGN.md 6절 예시 상태 중 일부만 우선 구현).
- 감지(`detectionRange` 내 거리 체크) → 추적(`NavMeshAgent.SetDestination`) → 공격(`attackRange` 내에서 쿨다운마다 `Health.ApplyDamage`) → 사망(`Health.Died` 이벤트로 agent/collider 비활성화)을 분리했다. 좀비도 `Health`를 재사용해 데미지 진입점을 하나로 유지한다(rules.md 3절).
- 플레이어가 데미지를 받을 수 있어야 하므로 `PlayerArmature.prefab`에 `Health`+`HealthDebugPrinter`를 추가하고 태그를 `Player`로 설정했다. 좀비는 `GameObject.FindGameObjectWithTag("Player")`로 타겟을 찾는다 — 나중에 플레이어가 여러 명(멀티플레이)이 되면 이 탐색 방식은 재검토가 필요하다.
- `Test_Combat.unity`의 `Floor`에 `Unity.AI.Navigation.NavMeshSurface`를 추가하고 `BuildNavMesh()`로 구웠다. `Zombie`(캡슐, 초록색, `NavMeshAgent`+`Health`+`HealthDebugPrinter`+`ZombieAI`)를 배치했다.
- **검증**: Play Mode에서 실시간으로 좀비가 플레이어를 쫓아가 1.2초 간격으로 10데미지씩 공격해 HP 100→0(로그 10회)까지 깎고 "PlayerArmature died"가 찍히는 것을 확인했다. 반대로 좀비의 `Health.ApplyDamage(200)`을 호출해 `IsDead=True`, `NavMeshAgent.enabled=False`, `Collider.enabled=False`로 정상 정지하는 것도 확인했다.
- **알아둘 점 — 씬 파일이 바이너리로 저장됨**: `NavMeshSurface.BuildNavMesh()`로 구운 NavMesh 데이터가 씬에 포함되면서 `Test_Combat.unity`가 (프로젝트의 `Force Text` 설정과 무관하게) 바이너리로 저장됐다. Unity가 구운 NavMesh처럼 본질적으로 바이너리인 데이터를 포함한 씬을 이런 식으로 직렬화하는 것은 알려진 정상 동작이다. 앞으로 이 씬은 git diff가 텍스트로 보이지 않는다 — 리뷰가 필요하면 Unity 에디터로 열어서 확인해야 한다.
- **다음 단계**: 좀비 사망 → 드랍 테이블 → 인벤토리 획득 연결. `Health.Died` 이벤트를 좀비 쪽에서 구독해 드랍 스폰으로 이어주는 별도 컴포넌트(예: `ZombieLootDropper`)를 추가하는 방향을 검토한다.

## 2026-09-23 드랍 → 인벤토리 획득 연결

- system.md 7~8절 흐름대로 최소 구현을 추가했다. UI가 아직 없으므로 이번 단계는 "획득해서 인벤토리에 저장되는지"까지만 검증하고, 화면 표시는 UI Toolkit 작업을 시작할 때 별도로 다룬다.
- `Assets/_Project/Scripts/Items/DropTable.cs`: 가중치 기반 드랍 테이블 ScriptableObject (`Entry[] {item, weight, minQuantity, maxQuantity}`, `TryRoll`).
- `Assets/_Project/Scripts/Inventory/ItemStack.cs`, `Inventory.cs`: 런타임 슬롯 리스트와 `Add`/`Remove`만 우선 구현했다. system.md 7.2절의 Move/Split/Merge/Swap은 UI·제작 작업이 실제로 필요해질 때 추가한다(현재는 미사용 코드를 만들지 않기 위해 보류).
- `Assets/_Project/Scripts/Items/LootPickup.cs`: 트리거 콜라이더 + 키네마틱 Rigidbody로 플레이어 접촉을 감지해 `Inventory.Add` 호출 후 자기 자신을 파괴한다.
- `Assets/_Project/Scripts/AI/ZombieLootDropper.cs`: `Health.Died`를 구독해 `DropTable.TryRoll` 결과로 `LootPickup` 프리팹을 스폰한다.
- 데이터 에셋: `Assets/_Project/Data/Items/Item_ScrapMetal.asset`(첫 아이템, 카테고리 Resource, MaxStack 99), `Assets/_Project/Data/DropTables/DropTable_Zombie_Basic.asset`(1~3개 확률 드랍). `Assets/_Project/Prefabs/World/LootPickup.prefab`도 함께 만들었다. 전부 `SerializedObject`로 private 필드를 채워서 생성했다(런타임 API는 캡슐화 유지, 에디터 툴링에서만 직접 접근).
- `PlayerArmature.prefab`에 `Inventory`+`InventoryDebugPrinter`를 추가했다. `Test_Combat`의 `Zombie`에 `ZombieLootDropper`를 붙이고 드랍 테이블/루트 프리팹을 연결했다.
- **검증**: Play Mode에서 사용자가 실제로 좀비를 쏴 죽이는 것("Zombie took 25 damage" → "Zombie died", 스택 트레이스가 `NativeInputSystem:NotifyUpdate` 경로라 실제 클릭임을 확인)과, 전리품을 주워 인벤토리에 "고철 조각 x2"가 저장되는 것("PlayerArmature picked up 2x 고철 조각")까지 콘솔 로그로 확인했다.
- **후속 필요**: 인벤토리/드랍 결과를 화면에 보여주는 UI Toolkit 작업, 자원 채집·제작 연결. `Inventory`의 Move/Split/Merge/Swap은 그 작업들과 함께 추가한다.

## 2026-09-23 싱글플레이 저장/불러오기 연결

- system.md 12절에 따라 정적 정의(ScriptableObject) / 런타임 상태(컴포넌트) / 저장 DTO(순수 클래스)를 분리했다.
- `Assets/_Project/Scripts/Save/SaveData.cs`: `GameSaveData`(schemaVersion 포함) / `PlayerSaveData` / `ItemStackSaveData` — 전부 일반 C# 클래스, MonoBehaviour나 ScriptableObject가 아니다.
- `Assets/_Project/Scripts/Save/GameSaveSystem.cs`: `JsonUtility`로 `Application.persistentDataPath/save_default.json`에 저장/로드. 아이템은 `ItemDefinition` 에셋 참조 대신 `StableId` 문자열로 저장하고, 로드 시 `ItemDatabase`로 되찾는다(rules.md 7절 "영속성에는 안정적인 ID 사용").
- **이름 충돌**: 처음 클래스명을 `SaveSystem`으로 지었다가 `Assets/SurvivalEngine/Scripts/Data/SaveSystem.cs`(전역 네임스페이스)와 충돌해 컴파일 에러가 났다. 벤더 파일을 건드리지 않고 우리 쪽을 `GameSaveSystem`으로 이름을 바꿔 해결했다. `ItemDatabase`도 이름이 겹치는 벤더 클래스(`DevionGames.InventorySystem.ItemDatabase`)가 있었지만 그쪽은 네임스페이스가 있어 실제 충돌은 없었다 — **새 전역 클래스를 만들 때는 미리 `grep -rl "class <이름>" Assets`로 벤더 패키지와 이름이 겹치는지 확인한다.**
- `Assets/_Project/Scripts/Items/ItemDatabase.cs`: `Resources.Load<ItemDatabase>("ItemDatabase")`로 자기 자신을 찾는 정적 접근자를 가진 조회용 에셋. 그래서 `Assets/_Project/Resources/ItemDatabase.asset` 경로에 둬야 한다(Resources 폴더 이름은 고정).
- `Assets/_Project/Scripts/Save/SaveLoadDebugController.cs`: F5=저장, F9=불러오기. 정식 저장/불러오기 UI가 생기기 전까지의 테스트용 진입점이다.
- `Inventory.cs`에 `Clear()`를 추가했다(불러오기 시 기존 아이템 위에 중복 추가되지 않도록).
- `PlayerArmature.prefab`에 `SaveLoadDebugController`를 추가했다.
- **검증**(Play Mode, 리플렉션으로 직접 `GameSaveSystem.Save`/`Load` 호출): ① 위치(12, -0.01, 4.85)·인벤토리(고철 조각 x7)를 저장한 뒤 다른 위치로 이동하고 인벤토리를 비운 상태에서 불러오기 → 정확히 복원됨. ② 체력 30으로 저장 → 100까지 회복 → 불러오기 → 정확히 30으로 복원됨.
- **알려진 한계**: 플레이어가 죽은(`IsDead=true`) 상태에서 저장한 뒤 불러와도 체력은 되살아나지 않는다(`Health.Heal`이 `IsDead`일 때 무시하도록 만들어져 있음). 리스폰/부활 흐름이 아직 없어서 의도적으로 범위 밖에 뒀다 — 게임오버·리스폰 시스템을 만들 때 함께 다룬다.
- Phase 1(TPS 전투 수직 슬라이스) 완료: 이동/카메라/사격, 좀비 감지·추적·공격·사망, 드랍·획득, 저장/불러오기까지 DESIGN.md 15절 완료 조건을 (UI 제외) 충족했다. 남은 건 인벤토리 등을 화면에 보여주는 UI Toolkit 작업이며, Phase 2(생존과 거점 루프) 또는 UI 작업 중 어느 쪽을 먼저 할지는 사용자와 상의해서 정한다.

## 2026-09-23 UI Toolkit HUD 연결

- system.md 13절 구조(Gameplay State → UI Data Source → Runtime Binding → UXML → USS)와 rules.md 8절(UI가 게임 상태를 직접 수정하지 않는다)을 따랐다. `HudController`는 `Health`/`Inventory`의 이벤트를 구독해 표시만 하고, 역방향으로 도메인 상태를 건드리지 않는다.
- `Assets/_Project/UI/UXML/Hud.uxml`: 체력 라벨(`health-label`)과 인벤토리 패널(`inventory-panel` + `ListView`)로 구성. `<Style src="project://database/...">`로 USS를 연결했다.
- `Assets/_Project/UI/USS/Hud.uss`: 좌상단 체력 패널, 우상단 인벤토리 패널의 최소 스타일(반투명 배경, 흰 글씨).
- `Assets/_Project/Scripts/UI/HudController.cs`: `Health.Damaged`/`Died`, `Inventory.ItemAdded` 이벤트로 라벨/ListView를 갱신한다. `ListView.itemsSource`는 `Inventory.Stacks`(런타임 실제로는 `List<ItemStack>`)를 그대로 연결한다 — UI가 직접 슬롯을 만들지 않고 도메인 컬렉션을 읽기만 한다. Tab 키(Keyboard 직접 폴링)로 인벤토리 패널을 토글한다. `PlayerInput`의 SendMessage 방식은 같은 GameObject에 있는 컴포넌트에만 전달되는데 HUD는 플레이어와 다른 오브젝트라 이 방식을 쓰지 않았다 — 나중에 UI 매니저나 이벤트 버스가 생기면 재검토한다.
- `Assets/_Project/UI/Themes/PanelSettings_Hud.asset`: 기본 테마 스타일시트 없이 만들었다. 우리 UI는 커스텀 USS로 배경/글자색을 전부 직접 지정하므로 Unity 기본 런타임 테마가 없어도 정상 렌더링되는 것을 확인했다.
- `Test_Combat.unity`에 `UIDocument`+`HudController`를 가진 `HUD` GameObject를 추가했다. `HudController`는 `Player` 태그로 플레이어를 찾는다(Phase B `ZombieAI`와 같은 패턴 — 씬에 플레이어가 하나뿐이라는 전제, 멀티플레이 때 재검토 필요).
- **검증**: Play Mode 스크린샷으로 체력 라벨이 좀비 공격에 따라 실시간으로 갱신되는 것("70 / 100" → "0 / 100")과, 아이템을 추가하고 Tab으로 열었을 때 "고철 조각 x5"가 정확히 표시되는 것을 확인했다. 한글 표시도 별도 폰트 설정 없이 정상 렌더링됐다.
- **후속 필요**: 사망/재장전/드랍 알림 같은 나머지 HUD 요소(DESIGN.md 12.1절), 건축·제작 UI, 인벤토리 Move/Split/Merge/Swap 조작 UI는 해당 기능이 실제로 생길 때 추가한다.

## 2026-09-23 Phase 2 시작 — 자원 채집

- system.md 10절 구조(ResourceNodeDefinition/ResourceNodeState)대로 정적 정의(`ResourceNodeDefinition`: yieldItem/yieldPerGather/maxYield/respawnSeconds)와 런타임 상태(`ResourceNode`: remainingYield/respawnAt)를 분리했다.
- `ResourceGatherer`는 Interact 액션(원래 Player 맵에 Hold 인터랙션으로 이미 정의돼 있던 것)으로 주변(`OverlapSphere`)의 `ResourceNode`를 찾아 채집한다.
- 첫 채집 아이템 "나무"(`Item_Wood.asset`, Resource 카테고리)를 추가하고 `ItemDatabase`에도 등록했다(저장/불러오기에서 stableId로 복원 가능하게).
- `Test_Combat`에 `Tree`(원기둥, 갈색) 노드를 배치했다.
- **검증**: Play Mode에서 `ResourceNode.TryGather`를 6번 호출해 5번 성공(인벤토리에 "나무 x5" 누적) 후 6번째는 고갈로 실패하는 것을 확인했다. 재생(20초 후 `remainingYield` 복구)은 코드 검토로는 단순하고 명확하지만 실시간 20초 대기까지는 하지 않았다 — 필요하면 다음에 실측한다.
- **다음 단계**: 제작(RecipeDefinition으로 나무+고철 조각 → 기초 자재), 그다음 그 기초 자재로 간단한 건축.

## 2026-09-23 제작 시스템 연결

- `Inventory.GetQuantity(item)`를 추가해 제작 가능 여부를 재료 차감 없이 미리 확인할 수 있게 했다.
- `RecipeDefinition`(입력 재료 배열 + 결과물), `CraftingSystem`(`CanCraft`로 전부 확인한 뒤에만 `TryCraft`가 실제로 차감·지급 — 일부만 차감되는 상황 방지)을 추가했다.
- 첫 레시피: "기초 자재 제작" = 나무 x2 + 고철 조각 x1 → 기초 자재 x1(`BuildingMaterial` 카테고리). 이 "기초 자재"는 다음 단계인 건축의 재료로 재사용할 계획이다.
- `HudController`에 제작 패널(C로 토글, `RecipeDefinition[]` 직렬화 필드, ListView + 제작 버튼)을 추가했다. 버튼 클릭은 `Button.clickable = new Clickable(...)`로 매 바인딩마다 교체해서 ListView 재사용 시 클릭 핸들러가 중복 등록되지 않게 했다.
- **검증**: Play Mode에서 ① `CraftingSystem.TryCraft`를 직접 호출해 나무/고철 조각이 정확히 소모되고 기초 자재 1개를 얻는 것, ② 재료가 없을 때 `TryCraft`가 `false`를 반환하고 아무것도 바뀌지 않는 것, ③ 인벤토리·제작 패널을 동시에 열어 실제 화면에 재료 수량과 "제작" 버튼이 올바르게 표시되는 것을 스크린샷으로 확인했다. 버튼을 실제 마우스 클릭 이벤트로 누르는 것까지는 테스트하지 않았다(표준 UI Toolkit Button/Clickable 패턴이라 별도 검증 없이 신뢰함) — 사람이 직접 클릭해서 확인해줘야 한다.
- **다음 단계**: 기초 자재를 소비하는 간단한 건축(벽 또는 보관함 1종) 배치 시스템.

## 2026-09-23 간단한 건축 시스템 연결

- system.md 11절 흐름(Select → Ghost Preview → Snap/Rotate → Build Request → Authority Validation → Resource Transaction → Spawn → Save)대로 회전/스냅은 생략하고 최소 구현했다. `BuildableDefinition`(buildableId/displayName/prefab/costs), `BuildableInstance`(StableWorldId는 `Guid.NewGuid()`로 생성, BuildableId 보관 — system.md "최소한 StableWorldId, BuildableId, Transform, Health, Ownership" 중 저장/네트워크 연동 전이라 Health/Ownership은 아직 안 만듦), `BuildingSystem`(고스트 스폰/추적, 자원 확인, 설치)을 추가했다.
- **입력 충돌 처리**: `BuildMode`와 `Fire` 액션이 같은 플레이어 오브젝트의 `HitscanWeapon`과 `BuildingSystem` 양쪽에 `OnFire`로 전달되는 문제가 있었다. system.md는 이런 경우 Action Map 분리를 권장하지만, 이번 단계에서는 `HitscanWeapon`이 `BuildingSystem.BuildModeActive`를 확인해 건축 모드 중에는 발사하지 않도록 하는 실용적인 방법을 택했다. **후속 개선 여지**: 제대로 하려면 건축 모드 진입 시 Player 액션맵을 Build 전용 맵으로 전환하는 방식으로 바꾼다.
- **프리팹에 런타임 Material을 직접 대입하면 저장되지 않는다** — 중요한 함정: `new Material(...)`을 만들어 `renderer.sharedMaterial`에 대입한 뒤 `PrefabUtility.SaveAsPrefabAsset`으로 저장하면 그 재질 참조가 `fileID: 0`(없음)으로 날아간다. 씬에 직접 놓는 오브젝트(`EditorSceneManager.SaveScene`)는 임베디드 Material이 정상 저장되지만, 프리팹은 그렇지 않았다. 벽 고스트가 마젠타(재질 없음 기본색)로 나온 걸 보고 발견했다. **해결**: 재질을 `AssetDatabase.CreateAsset(mat, "Assets/.../M_xxx.mat")`으로 먼저 독립 에셋으로 저장한 뒤 그 에셋을 프리팹에 연결해야 한다. 앞으로 프리팹에 커스텀 색을 넣을 때는 항상 이 순서를 따른다.
- 고스트 프리뷰는 콜라이더만 끄고 반투명 처리는 이번에는 넣지 않았다(재질 관련 위 함정과 겹쳐서 최소 기능으로 유지) — 필요하면 URP Lit 셰이더의 `_BaseColor`와 Surface Type(Transparent) 프로퍼티를 직접 설정하는 별도 자산으로 후속 작업한다.
- 첫 건축물: "간이 벽"(`Buildable_Wall`, 큐브, 기초 자재 x3).
- **검증**(Play Mode): 기초 자재 3개 지급 → BuildMode 진입 → 고스트가 정상 색상(마젠타 아님)으로 표시 → 설치 → 기초 자재 3→0 차감, 실제 설치물이 별도 GUID(WorldId)와 `buildableId="wall_basic"`을 갖는 것, 고스트(콜라이더 꺼짐)와 실제 설치물이 서로 다른 오브젝트로 공존하는 것을 확인했다.
- **후속 필요**: 회전/스냅 조작, 배치 위치 유효성 검증(경사·충돌 검사 — 지금은 카메라 정면 레이캐스트로 바닥 높이만 맞춘다), 건축물 저장/불러오기(`GameSaveSystem`에 아직 미포함), Ownership/Health 필드.

Phase 2(생존과 거점 루프) 완료: 채집 → 제작 → 건축까지 DESIGN.md Phase 2 목표를 모두 연결했다.

## 2026-09-23 건축 폴리시 1차 — 그리드 스냅 + 회전

- 사용자가 "포트나이트처럼 벽 여러 개를 소켓 스냅으로 빠르게 짓게" 하는 방향을 제안했다. DESIGN.md 9.3절("초기에는 복잡한 구조 안정도나 연결 그래프를 만들지 않는다")과 이 게임이 ARK류 생존 거점이지 포트나이트류 전투 건축 대결이 목표가 아니라는 점을 근거로, 전체 소켓 스냅 시스템은 지금 하지 않고 그리드 스냅 + 회전만 먼저 하는 절충안을 제안했다. 사용자가 "처음부터 크게 하면 버그가 많이 발생하니" 이 절충안에 동의했다 — **이 판단 근거를 기억한다**: 벽/바닥/경사로 등 조각 타입이 여러 개 생기기 전까지는 소켓 스냅을 미룬다.
- `BuildingSystem`에 `gridSize`(기본 2m, 벽 폭과 일치)와 `rotationStep`(90도) 필드를 추가했다. 고스트 위치는 매 프레임 `Mathf.Round(x/gridSize)*gridSize`로 격자에 맞추고, Q/E로 `ghostYaw`를 90도씩 돌린다. `UpdateGhostPosition`을 `UpdateGhostTransform`으로 이름을 바꿔 위치+회전을 함께 처리하게 했다.
- **검증**: Play Mode에서 플레이어를 (5.3, -0.01, 7.1)로 이동시키고 고스트를 갱신하니 정확히 격자에 맞는 (6, 0, 6)으로 스냅됐고, `ghostYaw=90` 설정 후 고스트와 실제 설치물 모두 회전 90도가 그대로 반영되는 것을 확인했다.
- **다음 폴리시**: 고스트 반투명(지난번 URP Material 프리팹 저장 함정을 피해서 `_Surface`/`_BaseColor` 알파를 코드로 직접 설정), 건축물 저장/불러오기(`GameSaveSystem`에 `BuildableInstance` 목록 포함), 그 다음이 소켓 스냅.

## 2026-09-23 건축 고스트가 마우스를 따라가지 않던 버그 수정

- 사용자 리포트: "제작 키를 눌렀을 때 고스트가 플레이어 옆에 나타나고 마우스를 움직여도 이동을 안 하고 플레이어를 따라다닌다."
- **원인**: `UpdateGhostTransform`이 `카메라 위치 + 카메라 정면 * placeDistance`로 목표 지점을 계산했다. 3인칭 `CinemachineThirdPersonFollow` 카메라는 플레이어 뒤 `CameraDistance`(3.5m)만큼 떨어져 있고, `placeDistance`는 3m였다. 카메라 위치를 기준으로 정면 3m를 투사하면 `플레이어 위치 - 정면방향 * (3.5 - 3) = 플레이어 위치 - 정면방향 * 0.5`가 되어, 실제로는 **플레이어를 중심으로 반경 0.5m짜리 원**을 그릴 뿐이었다 — 마우스로 시점을 돌려도 고스트가 거의 안 움직이는 것처럼 보인 이유다.
- **수정**: 기준점을 카메라 위치가 아니라 **플레이어 위치**(`transform.position`)로 바꿨다. `카메라가 보는 방향(cam.transform.forward)`은 그대로 쓰되, 그 방향으로 플레이어에서 `placeDistance`만큼 투사하면 카메라 리그의 거리 설정과 무관하게 항상 정확히 시점 방향으로 나간다.
- **검증**: Play Mode에서 `_cinemachineTargetYaw`를 0°/90°로 바꿔가며 확인 — yaw 0°에서는 고스트가 플레이어보다 Z축으로 +2.8m 앞에, yaw 90°에서는 X축으로 +3.2m 앞에 나타나 정확히 시점 방향을 따라 이동하는 것을 확인했다.
- **교훈**: 3인칭 카메라 기준으로 "앞쪽 지점"을 계산할 때는 카메라 위치가 아니라 캐릭터(피벗) 위치에서 카메라가 보는 방향으로 투사해야 카메라 리그의 오프셋/거리 튜닝에 영향받지 않는다. `HitscanWeapon`의 사격 레이캐스트는 카메라 위치 기준이 맞다(총알은 실제로 카메라 위치에서 나가는 것처럼 보여야 하니까) — 이번 것과 성격이 다른 계산이라 그쪽은 그대로 둔다.

## 2026-09-23 고스트 반투명 처리

- 지난번 "프리팹에 런타임 Material을 대입하면 저장되지 않는다"는 함정은 `PrefabUtility.SaveAsPrefabAsset`에 국한된 문제였다. 고스트는 `Instantiate()`로 매번 새로 만드는 런타임 전용 인스턴스라 디스크에 저장할 일이 없으므로 이번엔 해당하지 않는다 — 그냥 `renderer.sharedMaterial`에 새 Material을 대입해도 된다.
- URP Lit 셰이더는 legacy `_Color`가 아니라 `_BaseColor`를 쓰고, Surface Type을 Transparent로 바꾸려면 `_Surface`/`_Blend`/`_SrcBlend`/`_DstBlend`/`_ZWrite`를 함께 설정하고 `renderQueue`도 Transparent로 바꿔야 한다(단순히 `.color.a`만 바꾸면 여전히 불투명하게 렌더링된다).
- `SetGhostVisual`에서 고스트의 모든 Renderer에 반투명 재질(알파 0.45)을 적용하도록 했다.
- **검증**: Play Mode에서 고스트를 카메라 정면으로 옮겨 스크린샷을 찍으니 뒤에 있는 좀비(초록 캡슐)와 나무(갈색 원기둥)가 벽을 통해 비쳐 보이는 것을 확인했다. 재질 프로퍼티도 `_Surface=1`, `_BaseColor.a=0.45`, `renderQueue=3000`으로 정확히 설정된 것을 확인했다.
- **다음 단계**: 건축물 저장/불러오기 연동.

## 2026-09-23 건축물 저장/불러오기 연동

- `BuildableDatabase.cs`(`ItemDatabase`와 같은 패턴 — `Resources.Load`로 자기 자신을 찾는 정적 접근자, `buildableId`로 `BuildableDefinition` 조회)를 추가하고 `Assets/_Project/Resources/BuildableDatabase.asset`에 `Buildable_Wall`을 등록했다.
- `SaveData.cs`에 `BuildableSaveData`(worldId/buildableId/position/rotationY)와 `GameSaveData.buildables` 목록을 추가했다. 건축물은 플레이어 소유가 아니라 월드 상태라 `PlayerSaveData`가 아니라 `GameSaveData` 최상위에 둔다.
- `GameSaveSystem.Save`는 씬의 모든 `BuildableInstance`를 찾아 직렬화한다. `Load`는 먼저 기존 `BuildableInstance`를 전부 파괴한 뒤(중복 스폰 방지, `Inventory.Clear()`와 같은 원칙) 저장된 목록대로 `BuildableDatabase`에서 프리팹을 찾아 다시 스폰한다.
- `BuildableInstance`에 `Restore(buildableId, worldId)`를 추가했다 — `Initialize`는 새 GUID를 만들지만 `Restore`는 저장된 WorldId를 그대로 복원해야 하므로 별도 메서드로 분리했다.
- **검증**: Play Mode에서 벽을 설치 → 저장(JSON에 `buildables` 배열로 정확히 기록됨) → 씬에서 파괴 → 불러오기 → 같은 위치·같은 WorldId로 정확히 재스폰되는 것을 확인했다.

## 2026-09-23 HUD 홀로그램 + 노이즈 재도색

- 사용자 요청: "홀로그램 느낌나는데 노이즈가 낀 느낌"으로 디자인. USS는 실제 블러/글로우/그라디언트를 지원하지 않아서, 절차적으로 생성한 텍스처 오버레이 + 색상 교체 + 런타임 플리커로 그 인상을 근사했다.
- **팔레트 교체**: `:root` 변수(`--hud-ink/--hud-muted/--hud-surface/--hud-surface-soft/--hud-edge/--hud-accent/--hud-warm`)와 파일 전체에 하드코딩돼 있던 올리브/카키/호박색 rgba 리터럴을 전부 시안(`rgb(80,235,255)` 계열)과 마젠타(`rgb(255,90,200)` 계열)로 바꿨다. 레이아웃(크기·위치·패딩·폰트 크기)은 전혀 건드리지 않고 색상만 교체했다.
- **스캔라인/노이즈 오버레이**: `Hud.uxml`에 `hologram-scanlines`/`hologram-noise`/`hologram-glitch-bar` 3개의 `picking-mode="Ignore"` 풀스크린 오버레이를 hud-root 맨 끝(=가장 위 레이어)에 추가했다. `HudController`가 시작할 때 `Texture2D`를 코드로 생성한다 — 스캔라인은 1×240px 텍스처에 3px 간격으로 알파를 채운 정적 패턴, 노이즈는 96×96 텍스처에 픽셀마다 무작위 알파를 채우고 0.07초마다 재생성하는 코루틴으로 "TV 정전기" 느낌을 낸다. `-unity-background-image-tint-color`로 흰 알파 텍스처를 시안 톤으로 물들였다.
- **플리커/글리치**: `FlickerRoutine`이 2.5~6초 간격으로 hud-root 전체의 `style.opacity`를 짧게 떨어뜨렸다 복구한다(홀로그램이 깜빡이는 느낌). `GlitchBarRoutine`은 3~7초 간격으로 화면 임의 높이에 얇은 시안 바를 짧게 번쩍인다.
- 오버레이가 hud-root 전체(화면 전체)를 덮기 때문에 게임 월드까지 스캔라인/노이즈가 걸쳐 보인다 — 이게 오히려 "바이저를 통해 보는 홀로그램 UI"처럼 읽혀서 의도한 효과와 잘 맞았다(스크린샷으로 확인). 만약 게임 월드는 그대로 두고 UI 패널에만 효과를 주고 싶다면 오버레이를 hud-root가 아니라 각 패널 안쪽에 개별로 넣는 방향으로 바꿔야 한다.
- **검증**: Play Mode 스크린샷 두 장을 시간차를 두고 찍어 노이즈 텍스처가 실제로 매 프레임 바뀌는 것을 확인했다. 컴파일 에러 0건, 체력바(80/100)도 여전히 정확히 반영됨을 확인했다.
- **후속 조정 여지**: 스캔라인/노이즈 강도(`scanlineOpacity`=0.16, `noiseOpacity`=0.07)와 플리커 빈도는 `HudController`의 인스펙터 필드라 취향에 따라 바로 조절 가능하다.

## 2026-09-23 홀로그램 효과를 UI 패널 안쪽으로 한정 + 미래풍 디테일

- 사용자 피드백: "화면 전체가 아니라 UI만 해달라는 거였는데 설명을 너무 줄여서 했나보네. UI를 미래적으로 디자인 해주고 UI에만 노이즈 해줘." 직전 구현은 hud-root(화면 전체)에 스캔라인/노이즈/글리치 바를 올려서 3D 게임 월드까지 덮고 있었다.
- **수정**: `Hud.uxml`에서 화면 전체 오버레이 3개(`hologram-scanlines`/`hologram-noise`/`hologram-glitch-bar`)를 제거했다. 대신 `HudController.SetupHologramPanels`가 지정한 패널 이름 목록(`mission-banner`, `compass-bar`, `objective-panel`, `inventory-panel`, `crafting-panel`, `hotbar-panel`, `combat-panel`, `control-hints`, `survivor-card`)을 찾아 각 패널의 **자식**으로 스캔라인·노이즈 오버레이를 코드로 붙인다. `position:absolute; inset 0`인 자식은 UI Toolkit에서 부모 박스 기준으로 배치되므로 딱 그 패널 크기만큼만 덮는다 — hud-root 전체가 아니라.
- 텍스처(스캔라인 1장, 노이즈 1장)는 한 번만 생성해 모든 패널이 공유한다. 노이즈 재생성 코루틴이 텍스처를 갱신하면 그걸 참조하는 모든 패널 오버레이가 한 번에 같이 갱신된다.
- 글리치 효과도 화면 전체를 가로지르는 바 대신, 무작위로 고른 패널 하나의 `style.opacity`를 짧게 떨어뜨렸다 되돌리는 방식으로 바꿔 "그 패널 하나만 신호가 끊기는" 것처럼 보이게 했다(`GlitchRoutine`).
- **미래적 디테일 추가**: 각 패널에 시안색 L자 코너 브래킷(`.hud-corner-tl/tr/bl/br`, 테두리 2면만 그린 8~10px 사각형)을 4개씩 붙였다 — SF 게임 HUD에서 흔한 "타겟팅 프레임" 모티프. 또한 대부분 패널의 `border-radius`를 12~18px에서 3~4px로 줄여 부드러운 카드형에서 각진 기술적 형태로 바꿨다(단, `objective-band`/`hotbar-slot`처럼 원래도 대각선으로 깎인 모양이나 원형 아이콘 배지는 그대로 유지).
- **검증**: Play Mode 스크린샷으로 3D 게임 월드(하늘, 캐릭터, 좀비)에는 더 이상 스캔라인/노이즈가 없고, 모든 HUD 패널 안쪽에만 스캔라인 무늬와 코너 브래킷이 보이는 것을 확인했다. 컴파일도 정상(`compileFailed=false`).
- **참고**: Play Mode 진입 직후 콘솔에 `noiseOverlay`/`glitchBar` 관련 CS0103 에러가 잠깐 보였는데, 타임스탬프를 보니 이건 편집 중간 단계에서 발생했다가 이미 사라진 과거 캐시 항목이었다(`groundTruth.compilationFailed=false`로 실시간 상태는 정상이었고, 스크린샷에도 새 디자인이 정상 렌더링됨). 이 콘솔 도구가 종종 실시간 필터링에서 오래된 항목을 섞어 보여주는 것은 이전에도 발견한 특성이다 — `groundTruth` 필드나 실제 화면 결과로 교차 확인하는 습관을 유지한다.

## 2026-09-23 나침반 회전 버그 수정 + 패널 모서리 정돈

- 사용자 리포트: "12시쪽위의 나침판은 안움직이는 버그가 있고 UI에서 도형 배치가 삐뚤한것들을 정돈해줘."
- **나침반 버그**: 기존 `compass-bar`는 W/N/E 라벨과 눈금이 고정된 flex 배치라 완전히 장식용이었다 — 실제 시점과 무관하게 항상 같은 모양이었다. `Hud.uxml`을 `compass-viewport`(overflow:hidden으로 잘라내는 창) + `compass-strip`(그 안에서 좌우로 움직이는 긴 눈금 띠) 구조로 바꿨다.
  - `HudController.BuildCompassStrip()`가 -180°~540°(총 720도, 카메라가 어느 방향을 보든 여유 있게 커버) 범위를 15도 간격 눈금 + 45도 간격 8방위(N/NE/E/SE/S/SW/W/NW) 라벨로 한 번만 생성한다.
  - `UpdateCompass()`가 매 프레임 `Camera.main.transform.eulerAngles.y`(카메라가 보는 방향 = 조준 방향)를 읽어 그 각도에 해당하는 눈금이 뷰포트 중앙(고정된 `compass-pointer` 밑)에 오도록 `compassStrip.style.left`를 계산한다. 벤더 `ThirdPersonController`의 내부 필드에 리플렉션으로 접근하지 않고 카메라 자체를 읽어서 결합도를 낮췄다.
  - **검증**: Play Mode에서 카메라 yaw를 45.2도로 뒀을 때 계산된 스트립 위치가 공식과 일치했고, yaw를 158도로 바꿨을 때 뷰포트 중앙에 가장 가까운 라벨이 정확히 "S"(180도)로 방위각 차이만큼 오프셋된 것을 확인했다.
- **모서리/테두리 정돈**: 지난 커밋에서 추가한 코너 브래킷이 기존의 "한쪽만 두꺼운 강조 테두리"(`survivor-card`의 왼쪽 3px, `combat-panel`의 오른쪽 3px)나 "대각선으로 깎인 비대칭 모서리"(`mission-marker`, `hotbar-slot`)와 겹치면서 프레임이 삐뚤어 보이는 원인이 됐다.
  - `survivor-card`/`combat-panel`: 한쪽만 두꺼운 강조 테두리를 제거하고 4면 균일한 `hud-edge` 테두리로 통일 — 강조는 이제 코너 브래킷이 담당한다.
  - `objective-panel`: 왼쪽 1px만 있던 테두리를 4면 통일된 테두리로 바꾸고, 코너 브래킷은 `hud-corner-warm`(마젠타)로 구분해 이 패널의 정체성(경고/목표)을 유지했다.
  - `mission-marker`: `9px 4px 9px 4px` 대각선 컷 사각형 → `50%` 원형으로 바꿔 다른 아이콘 배지(`survivor-emblem`, `objective-icon`)와 형태 언어를 통일했다.
  - `hotbar-slot`/`hotbar-slot-active`: `10px 4px 10px 4px` 비대칭 대각선 모서리 → 균일한 `4px`로 통일.
  - `compass-bar`/`control-hints`는 세로 폭이 얇아서(18~30px) 10px 코너 브래킷을 달면 브래킷이 패널을 거의 다 차지해버리므로 코너 브래킷은 제외하고 스캔라인/노이즈만 유지했다.
- **검증**: Play Mode 스크린샷으로 모든 패널이 균일한 얇은 테두리 + 코너 브래킷 조합으로 통일된 것과, 컴파일 에러 0건(`groundTruth`)을 확인했다.

## 2026-09-23 원형 아이콘 중앙 정렬 + 텍스트 가독성 개선

- 사용자 리포트: "UI의 원 안의 도형이나 문자가 원 중심에 있지 않아 수정해주고, 글자 가독성을 높여줘."
- **원인**: `survivor-emblem`/`team-slot-emblem`/`mission-marker`/`objective-icon`/`hotbar-icon` 원형 배지들은 부모에 `justify-content:center; align-items:center`가 있어 라벨 "박스"는 중앙에 오지만, 라벨 자체의 기본 `-unity-text-align`이 중앙이 아니어서(기본값은 좌상단 계열) 박스 안의 실제 글자/기호는 정중앙에서 벗어나 보였다.
- **수정**: `Hud.uss`에 `.survivor-emblem-text, .team-slot-symbol, .mission-marker-text, .objective-icon-text, .hotbar-symbol { width:100%; height:100%; margin:0; -unity-text-align: middle-center; }`를 추가했다. 라벨이 원 전체 영역을 차지하게 만든 뒤 그 안에서 텍스트를 정중앙 정렬하므로, 부모 크기가 달라져도 항상 도형/원의 중심에 온다.
- **가독성**: UI Toolkit의 `text-shadow`(offset/blur/color 지원)를 확인하고 `.hud-root Label, .hud-root Button { text-shadow: 0 1px 2px rgba(0,0,0,0.9); }`을 전역으로 추가해 스캔라인·노이즈가 깔린 반투명 배경 위에서도 글자 테두리가 살아나게 했다. 보조 텍스트 색상(`--hud-muted`)도 `rgb(103,178,199)` → `rgb(122,196,216)`로 살짝 밝혀 대비를 보강했다.
- **검증**: Play Mode 스크린샷으로 생존자 엠블럼("S"), 동료 슬롯("+"), 미션 마커("i"), 퀵슬롯 아이콘(★◆⌂+)이 모두 원 정중앙에 오는 것과, 본문 텍스트에 그림자가 적용돼 더 또렷하게 보이는 것을 확인했다.

## 2026-09-23 무기 아이콘 개선

- 사용자 리포트: "오른쪽의 weapon system 창에 총 그림이 너무 단순해. 그리고 분홍색이니 그래픽이 깨진 거 같은 느낌이 나니깐 흰색으로 해줘."
- 기존 `weapon-glyph`는 사각형 3개(개머리판/총열/손잡이)만으로 만들어져 있었고 색이 `--hud-warm`(마젠타)라, 실제로 텍스처 누락 시 나는 마젠타와 비슷해 "그래픽이 깨졌다"는 인상을 줄 만했다.
- 색을 `rgb(235, 245, 248)`(흰색에 가까운 아이보리)로 바꾸고, 부품을 7개(개머리판/리시버/조준경/총열/총구/손잡이/탄창)로 늘렸다. 손잡이와 탄창은 USS `rotate` 프로퍼티(12deg/18deg)로 비스듬하게 기울여 총 실루엣에 가깝게 만들었다.
- **검증**: Play Mode 스크린샷으로 흰색 소총 실루엣(개머리판-리시버-조준경-총열-총구가 위쪽 한 줄, 손잡이·탄창이 비스듬히 아래로)이 또렷하게 보이는 것을 확인했다.

## 2026-09-23 HUD 시각 재구성

- 사용자 참고 화면은 화면 가장자리에 상태·목표·조작 정보를 배치하고, 중앙 게임플레이 영역을 비우는 TPS HUD의 정보 계층을 보여준다. 이를 특정 게임의 아이콘·명칭·레이아웃을 그대로 복제하는 대신, ZGV01의 청록/주황 강조색과 생존 게임 용어로 새 UI Toolkit HUD에 반영했다.
- 실제로 연결된 상태는 `Health`, `Inventory`, `CraftingSystem`뿐이다. 좌측 생존자 카드의 체력 숫자/ProgressBar는 `HudController`가 `Health`에서 읽어 갱신한다. 인벤토리와 제작 ListView·토글 키는 기존 동작을 유지한다.
- 우측의 작전 목표, 상단 중앙 목표, 우하단 전투 안내는 현재 도메인에 퀘스트·탄약 모델이 없다는 점을 명시적으로 반영한 안내성 텍스트다. 향후 퀘스트/장비 상태가 생기면 별도의 표시 전용 데이터 소스를 통해 교체해야 하며 HUD가 그 상태를 직접 확정하면 안 된다.
- **정적 검증**: `xmllint --noout Hud.uxml`, USS 중괄호 균형, 금지된 구조 선택자 검사, `git diff --check`를 모두 통과했다. Unity CLI는 설치된 에디터와 Pipeline 연결 인스턴스를 찾지 못해 실제 UI Toolkit 재임포트, C# 컴파일, Play Mode 렌더링은 이 환경에서 확인하지 못했다.
- 후속 시각 조정에서는 참고 화면과 더 가까운 실루엣을 위해 상단 나침반, 좌측 세로 팀 상태 카드, 주황색 헤더를 가진 우측 다단 목표 트래커, 하단 좌측 빠른 행동 휠, 하단 우측 장비 카드로 재구성했다. 현재 파티/탄약 도메인은 없으므로 동료 영역은 빈 슬롯, 장비 영역은 조작 안내로 표시한다.
- 사용자가 여전히 참고 이미지보다 단순하다고 알려 추가로 팀 슬롯 설명, 목표 행 아이콘과 진행 트랙, 다단 퀵 슬롯을 보강했다. 좁은 화면에서 조작 힌트와 겹치지 않도록 퀵 슬롯 폭을 제한했다.

## 2026-09-23 HUD 오류 수정

- 사용자 에디터가 보고한 CS0311/CS1061은 `HudController`가 `ProgressBar`로 Q 조회하고 `highValue`/`value`를 설정하는 변경에서 발생했다. 해당 컨트롤 의존을 제거하고 UXML의 VisualElement 트랙 안에 채움 VisualElement를 두어 실제 Health 비율(0–100%)로 너비를 갱신하도록 바꿨다. Unity 문서는 UI Toolkit ProgressBar를 `UnityEngine.UIElements`의 VisualElement 계층 요소로 안내한다. 그러나 현 프로젝트의 컴파일 환경에서 받은 오류를 우선 반영해 가장 단순한 전용 트랙 구현으로 교체했다.
- `DevionGames.WriteInputManager.AxisDefined`는 `SerializedProperty.Next`로 `m_Axes`를 순회하다 stringValue를 배열/복합 프로퍼티에 조회하여 예외를 냈다. 요소별 `GetArrayElementAtIndex`와 `FindPropertyRelative("m_Name")`를 사용하도록 고쳤고, 자식 필드 조회도 `FindPropertyRelative`로 바꿨다. 공급자 파일 한 곳의 API 호환 수정이며 광범위한 에셋 변경은 없다.
- **검증 한계**: 코드/USS/UXML 정적 확인은 가능하지만 이 실행 환경에는 Unity Editor가 없어 컴파일과 Play Mode 진입은 재현하지 못했다. 사용자가 Unity에서 재임포트 후 결과를 확인해야 한다.
- **(다른 세션에서 이어받아 검증)** Unity Editor MCP로 재컴파일·Play Mode를 직접 확인했다. `compileFailed=false`, 콘솔 에러 0건, 스크린샷으로 체력바(80/100 정확히 반영)·나침반·팀 상태·작전 목표·퀵슬롯·전투 안내가 모두 정상 렌더링되는 것을 확인했다.
- **재임포트 위험**: `Assets/Devion Games/Third Person Controller/Scripts/Editor/WriteInputManager.cs` 수정은 `Assets/Devion Games`가 아직 채택되지 않아 git 추적 밖이라 커밋되지 않는다. 이 패키지를 재설치/재임포트하면 원본(깨진 버전)으로 되돌아가 `AxisDefined`의 SerializedProperty 순회 예외가 다시 날 수 있다 — 그때는 `GetArrayElementAtIndex`/`FindPropertyRelative("m_Name")`로 배열 원소를 순회하도록 다시 고치면 된다(Survivalist Cinemachine 네임스페이스 수정과 같은 패턴, 위 Phase B 노트 참고).

## 2026-09-23 HUD 패널 배치 보정

- 빠른 행동 휠을 UXML/USS에서 제거했다.
- 좌측 팀 상태 폭을 240px에서 204px로 줄이고 카드/슬롯 내부 여백과 슬롯 배지를 축소했다. 우측 현재 목표 폭을 330px에서 286px로 줄이고 아이콘, 설명 글자와 행 간격도 축소했다.
- 인벤토리 또는 제작 패널을 열면 해당 VisualElement에 `BringToFront()`를 호출해 이후 형제 요소가 창 위를 덮지 않게 했다. 두 패널을 동시에 열 경우 가장 나중에 연 창을 위에 둔다.
- 추가 사용자 피드백을 반영해 `hotbar-panel`을 왼쪽 아래(22px 여백)로 옮기고, 슬롯 안에 별도 원형 아이콘 베젤을 넣었다. 생존자/목표/인벤토리/제작/전투 프레임과 상단 배지에 라운드 코너를 적용해 네모 패널이 반복되는 인상을 줄였다.
- 밀리터리 스타일 피드백에 따라 색상을 청록에서 올리브 드랩·카키·황색 명령 강조로 바꾸고 FIELD OPERATIONS/SQUAD STATUS/FIELD ORDERS/FIELD KIT 식별 라벨을 추가했다. Unity 에디터 연결 없이도 텍스트 자산을 직접 고칠 수 있지만 Game 뷰에서 화면 크기에 따른 최종 외형을 실시간 확인할 수는 없다.

## 2026-09-23 ar_buildings 격리 재발

- 이전 세션에서 `Assets/ar_buildings`를 `../ZGV01-quarantine/`로 옮겼던 조치(위 Phase B 노트)가 프로젝트를 이 머신(`E:\Unity\ZGV01`)으로 옮기는 과정에서 되돌아와 있었다. 같은 위치(`E:\Unity\ZGV01-Assets-backup-20260923.tar.gz` 등)의 Assets 백업으로 복원하면서 격리 이전 상태가 같이 살아난 것으로 추정된다.
- 증상은 이전과 동일: `com.unity.xr.arfoundation`/`arsubsystems`/`remote-config` 미설치로 컴파일 에러 9건(`Bottom_btns_control.cs`, `Device_check.cs`, `Main_control.cs`).
- `Assets`는 `.gitignore`로 대부분 추적 밖이라 `git status`/`git diff`로는 이 드리프트가 보이지 않는다. 재발 여부는 Unity 콘솔/재컴파일 결과로만 확인 가능하다.
- 같은 방식으로 재조치: `Assets/ar_buildings`(+ `.meta`)를 `E:\Unity\ZGV01-quarantine\`로 이동. 삭제가 아니므로 필요하면 원복 가능. `recompile` 결과 `compilationFailed: false`, 에러 0건으로 확인했다.
- **후속**: Assets 백업 압축본(`ZGV01-Assets-backup-20260923.tar.gz`, `Unity Assets 1~4.zip`)으로 다시 복원할 일이 생기면 이 항목이 또 재발할 수 있음을 유의한다.

## 2026-09-23 HUD가 아예 안 보이던 문제(진짜 원인)

- 사용자 리포트: "UI가 망가진거 같은데 복구가능해?"
- Play Mode 콘솔에서 `No Theme Style Sheet set to PanelSettings PanelSettings_Hud, UI will not render properly` (`UIDocument:OnEnable`) 경고를 발견했다.
- `PanelSettings_Hud.asset`의 `themeUss`는 GUID(`c20c4adc7c32847ad834523250c0d0ec`)를 참조하고 있었지만 `AssetDatabase.GUIDToAssetPath`가 빈 경로를 반환했다 — 프로젝트에 있어야 할 Unity 기본 런타임 테마(`UnityDefaultRuntimeTheme.tss`) 파일 자체가 디스크에 없는 상태였다.
- `UnityEditor.UIElements.PanelSettingsCreator.GetFirstThemeOrCreateDefaultTheme()`(에디터가 Panel Settings 생성 메뉴에서 내부적으로 쓰는 바로 그 메서드)를 eval로 직접 호출해 기본 테마를 재생성하고 `PanelSettings_Hud.themeStyleSheet`에 연결했다.
- **재발 방지**: 새로 생성된 테마 파일은 처음에 `Assets/UI Toolkit/UnityThemes/...`에 생겼는데, 이 경로는 `.gitignore`의 `/Assets/*` 규칙에 걸려 추적 밖이었다. 그대로 두면 `ar_buildings`와 같은 패턴(로컬 Assets 백업/복원 시 재발)이 반복될 것이므로, `AssetDatabase.MoveAsset`으로 추적 대상인 `Assets/_Project/UI/Themes/UnityDefaultRuntimeTheme.tss`로 옮기고 참조가 유지되는지 확인했다(GUID 보존).
- **스크린샷 도구 관련 발견**: `mcp__unity-editor-mcp__capture_game_view`/`screenshot` 두 MCP 도구 모두 UI Toolkit의 Screen Space Overlay 패널을 전혀 캡처하지 못한다(테스트로 루트에 불투명 빨간 배경을 씌워도 안 보임). 카메라 렌더 텍스처 기반 캡처로 추정되며, 이후 세션에서 HUD 렌더링을 눈으로 확인하려면 이 두 도구 대신 `UnityEngine.ScreenCapture.CaptureScreenshot(path)`를 `eval`로 직접 호출해 실제 백버퍼를 캡처해야 한다. 이전 세션 노트의 "스크린샷으로 HUD 정상 렌더링 확인" 기록들은 이 한계를 몰랐을 수 있어 재검토가 필요할 수 있다.
- **검증**: `Health` 로직은 이 문제와 무관하게 처음부터 정상 동작 중이었다(콘솔에 데미지/사망 로그가 계속 찍힘). 테마 수정 후 `ScreenCapture.CaptureScreenshot`로 나침반·팀 상태·목표·무기·퀵슬롯이 모두 정상 렌더링되는 것과 체력 100/100이 정확히 표시되는 것을 확인했다.

## 2026-09-23 HUD가 화면 크기에 비례해 스케일되도록 설정

- 사용자 요청: "화면 크기에 따라 UI비율이 따라가게 설정해줘".
- `PanelSettings_Hud.asset`의 `m_ScaleMode`가 `1`(Constant Physical Size, DPI 기준 물리 크기 고정)로 돼 있어서 창/해상도가 바뀌어도 HUD가 화면에 대해 상대적으로 커지거나 작아지지 않는 상태였다. 이를 `2`(Scale With Screen Size)로 바꿨다.
- `m_ReferenceResolution`(1200x800)은 기존 HUD 레이아웃이 이미 이 기준으로 잡혀 있어 그대로 뒀다. `m_ScreenMatchMode`(0=MatchWidthOrHeight)도 유지하되, 가로/세로 중 한쪽만 기준으로 삼으면 레퍼런스와 크게 다른 종횡비(세로로 긴 창 등)에서 다른 쪽 축이 과도하게 늘어날 수 있어 `m_Match`를 `0`(가로 기준)에서 `0.5`(가로/세로 균형)로 조정했다.
- **검증**(Unity Editor MCP 연결 확인, `Application.dataPath`가 `E:\Unity\ZGV01\Assets`인지 먼저 확인 후 진행): `eval`로 `AssetDatabase.LoadAssetAtPath<PanelSettings>`를 읽어 `scaleMode=ScaleWithScreenSize, match=0.5, refRes=(1200,800)`로 저장된 것을 확인했다. `Test_Combat` Play Mode에서 Game View를 1280x720(레퍼런스에 근접)과 640x360(레퍼런스의 절반)으로 바꿔가며 `capture_game_view(source="screen")`로 비교한 결과, 두 해상도에서 HUD 각 패널이 화면에서 차지하는 상대적 비율(가로/세로 위치·크기)이 동일하게 유지되는 것을 스크린샷으로 확인했다. 매우 넓은 해상도(2560x1080)는 Game View 커스텀 사이즈 등록/선택까지는 됐지만 `Screen.width/height`가 실제로 그 값을 반영하지 않는 에디터 스크립팅 한계로 직접 검증하지 못했다 — 필요하면 실제 빌드된 플레이어 창 크기 조절로 재검증한다.
- 이 프로젝트는 아직 UIDocument/PanelSettings가 HUD 하나뿐이라(`grep -rl PanelSettings Assets/_Project` 결과 1건) 다른 화면에는 영향 없다. 추후 메뉴/설정 등 새 UI 화면을 추가할 때 별도 PanelSettings를 쓸 경우 이번과 같은 스케일 설정을 적용할지 검토가 필요하다.

## 2026-09-23 HUD 텍스트/조준점 가시성 개선

- 사용자 요청: "UI글자 랑 조준점 가시성을 올려줘".
- 텍스트: `.hud-root Label, .hud-root Button`에 있던 텍스트 그림자를 조금 더 진하게 키우고(`text-shadow: 0 1px 3px rgba(0,0,0,0.95)`), `-unity-text-outline-width: 1px` + `-unity-text-outline-color: rgba(0,0,0,0.9)`를 추가했다. 그림자만으로는 밝은 하늘색 같은 배경 위에서 옅은 색 글자가 여전히 묻혀 보이는데, 글자 SDF 외곽선은 배경 밝기와 무관하게 대비를 만들어줘서 그림자보다 근본적인 해결책이다. `PanelSettings_Hud`가 기본 런타임 테마(SDF 폰트)를 쓰고 있어 별도 폰트 에셋 교체 없이 바로 적용됐다.
- 조준점(`reticle-*`): 기존엔 옅은 시안색 채움(라인)과 테두리만 있는 속이 빈 원(코어)이라 밝은 배경에서 거의 안 보였다. 라인/코어 모두 밝은 흰색(옅은 시안 틴트) 채움 + 검은 테두리(1~2px) 조합으로 바꿔 어떤 배경에서도 대비가 나오게 했다. UI Toolkit의 박스 모델이 border-box라 테두리를 추가해도 전체 십자선 크기는 거의 그대로 유지된다(안쪽 채움 폭만 유지하고 바깥으로 1px씩 확장).
- 부가로 상단 미션 배너(`--hud-surface-soft`, mission-banner 전용) 배경 불투명도를 0.5 → 0.64로 살짝 올려 그 안의 텍스트 대비를 보강했다. 다른 패널(`--hud-surface` 등)은 이미 충분히 어두워 손대지 않았다.
- **검증**: `AssetDatabase.Refresh()` 후 콘솔에 USS 파싱 관련 경고/에러가 없는 것을 확인했다(신규 에러 없음, 기존 `ar_buildings`/`KatsBits` 관련 에러는 무관한 기존 이슈). `Test_Combat` Play Mode에서 `capture_game_view(source="screen")`로 밝은 하늘 배경 위 HUD를 캡처해 라벨 글자와 조준점이 이전보다 뚜렷하게 보이는 것을 스크린샷으로 확인했다.

## 2026-09-23 텍스트 외곽선 완화 + 체력 연동 노이즈

- 사용자 피드백: "조준점은 괜찮은데 글자가 가시성이 너무높으니 잘안보인다 그리고 노이즈 효과는 체력이 40%남았을때 부터 나타나고 체력이 낮아질수록 심해지게 하자".
- **텍스트 외곽선 과다 문제**: 직전 세션에서 넣은 `-unity-text-outline-width: 1px`가 7~11px대 작은 한글 라벨에서는 외곽선이 글자 두께에 비해 너무 두꺼워 획끼리 뭉쳐 보이는 역효과를 냈다(가독성 "너무 높임" → 오히려 잘 안 보임). `Hud.uss`에서 `-unity-text-outline-width`를 `1px → 0.35px`, `-unity-text-outline-color` 알파를 `0.9 → 0.7`, `text-shadow`도 `0 1px 3px 0.95 → 0 1px 2px 0.85`로 되돌려 얇은 보조 대비만 남겼다. 조준점(reticle) 쪽은 피드백대로 그대로 뒀다.
- **체력 연동 노이즈**: `HudController.cs`의 패널별 노이즈 오버레이가 지금까지는 `noiseOpacity`(0.07) 고정값으로 항상 표시됐다. 이를 체력 비율에 연동시켰다 — `maxNoiseOpacity`(기본 0.3, 체력 0%일 때 최대 강도), `noiseHealthThreshold`(기본 0.4 = 40%)를 추가하고, `DecoratePanel`이 만드는 각 노이즈 `VisualElement`를 `noiseOverlays` 배열로 보관한 뒤 `RefreshHealth()`에서 `UpdateNoiseIntensity(healthRatio)`를 호출해 `Mathf.InverseLerp(threshold, 0, healthRatio) * maxNoiseOpacity`로 매 데미지/회복마다 불투명도를 갱신한다. 체력 40% 초과에서는 0(안 보임), 40%→0%로 갈수록 선형으로 강해진다.
- 노이즈 텍스처 자체의 프레임별 랜덤 갱신(`NoiseRoutine`, 0.07초 간격)은 그대로 유지된다 — 이번 변경은 "보이는 정도(불투명도)"만 체력에 연동한 것이고 애니메이션 자체는 항상 돌아간다.
- 스캔라인(`scanlineOpacity`)과 코너 브래킷, 플리커/글리치 코루틴은 사용자가 노이즈만 언급했으므로 손대지 않았다.
- 필드 `noiseOpacity`(고정 불투명도 의미)를 `maxNoiseOpacity`(체력 0%일 때 최대 강도 의미)로 이름을 바꿨다 — 의미가 근본적으로 바뀌어서(상수 → 상한값) 이름을 유지하면 오해 소지가 크다고 판단했다. 변경 전 씬의 `HudController` 직렬화 값이 코드 기본값(0.07)과 동일한 것을 `SerializedObject`로 먼저 확인해 인스펙터 오버라이드가 사라질 위험이 없음을 확인한 뒤 진행했다.
- **검증**(Play Mode, 좀비를 `SetActive(false)`로 꺼서 변수 통제): `Health.ApplyDamage`를 단계적으로 호출해 100%→45%(외곽선 임계값 초과, noiseOpacity=0.000) → 35%(임계값 아래 진입, 0.038) → 20%(0.150) → 0%(0.300, `maxNoiseOpacity`와 일치)까지 `noiseOverlays[0].resolvedStyle.opacity` 값을 직접 읽어 선형 램프가 정확히 동작하는 것을 확인했다. 이후 재생 모드에서 80/100 상태 스크린샷(노이즈 없음, 텍스트가 이전보다 얇고 또렷함)과 0/100 상태 스크린샷(패널 전체에 뚜렷한 정적 노이즈)도 비교 확인했다. 컴파일은 `recompile_status`로 `completed, failed:false` 확인.

## 2026-09-23 HUD 폰트 크기 확대

- 사용자 피드백: "UI의 글씨가 작으면 알아보기 힘들어".
- `Hud.uss`의 모든 `font-size` 선언(31곳, 7px~21px 범위)을 각각 +2px 했다. 균일하게 +2px를 적용하면 가장 작던 7~9px대 라벨(퀵슬롯 상태, 핫바 번호/캡션 등)이 비율상 가장 크게(약 25~29%) 커지고, 이미 충분히 크던 21px(생존자 엠블럼 글자) 등은 상대적으로 조금만(약 10%) 커져서 기존 시각적 위계는 유지하면서 가장 읽기 힘들던 텍스트를 우선적으로 개선했다.
- **ListView 행 높이 보정**: 인벤토리/제작 목록의 `ListView.fixedItemHeight`가 기존 폰트(14px) 기준으로 고정돼 있어(`inventoryList`=24, `recipeList`=28), 라벨 폰트가 16px로 커지면 행 안에서 텍스트가 잘릴 위험이 있었다. `HudController.cs`에서 `inventoryList.fixedItemHeight`를 24→32, `recipeList.fixedItemHeight`를 28→34로 올려 새 폰트 크기에 맞췄다. `ListView`는 고정 행 높이 밖의 오버플로를 자동으로 처리해주지 않으므로 이 보정 없이는 실제로 텍스트가 잘렸을 것이다(Play Mode로 실측 확인).
- **제작 패널·퀵슬롯 패널 겹침 발견 및 수정**: 폰트 확대로 제작 패널(`crafting-panel`)의 내용 높이가 늘어나면서, 고정된 `bottom: 88px` 위치에서는 그 아래 있는 퀵슬롯 패널(`hotbar-panel`, `bottom: 22px`)과 약 39px 겹치는 것을 `VisualElement.worldBound`로 실측해 발견했다. `crafting-panel`의 `bottom`을 88px → 144px로 올려 최종적으로 약 17px 여유 간격이 생기도록 조정했다(둘 다 `bottom` 기준 절대 위치라 값을 올릴수록 화면 위쪽으로 이동해 간격이 벌어진다). 이 조정을 하지 않았다면 사용자가 요청한 "글씨 키우기"가 오히려 패널 겹침이라는 새 문제를 만들 뻔했다.
- **검증 중 발견한 함정**: Play Mode를 유지한 채로 `AssetDatabase.ImportAsset(..., ForceUpdate)`로 USS를 반복 핫리로드하면서 같은 eval 호출 안에서 곧바로 `worldBound`를 측정하면 그 프레임의 레이아웃 재계산이 아직 반영되지 않아 이전 값이 나온다(레이아웃은 다음 프레임에 갱신됨) — reimport와 측정을 반드시 별도의 eval 호출로 나눠야 정확한 값을 얻는다. 또한 같은 세션에서 USS를 여러 번 연속 핫리로드하니 스캔라인/노이즈 텍스처·테마 스타일이 일시적으로 깨져 보이는 렌더링 아티팩트가 발생했는데, Play Mode를 재시작(정지 후 재생)하자 디스크에 저장된 최종 상태 그대로 깨끗하게 렌더링됐다 — 이건 반복적인 라이브 핫리로드 테스트 방식 자체의 부작용이며 실제 코드/에셋 문제가 아니다. 앞으로 USS를 여러 번 조정해야 할 때는 마지막에 Play Mode를 재시작해서 디스크 기준 최종 상태를 확인하는 절차를 습관화한다.
- **검증**: `recompile_status`로 `HudController.cs` 컴파일 성공 확인. Play Mode 재시작 후(좀비 비활성화, 고철 조각 x5·나무 x12 지급, 인벤토리·제작 패널 모두 열기) 스크린샷으로 전 패널의 글자가 커지고 또렷해진 것, 인벤토리/제작 목록 행이 잘리지 않는 것, 제작 패널과 퀵슬롯 패널 사이 간격이 확보된 것을 확인했다. `hotbarRect.y - (craftRect.y + craftRect.height)`가 약 17px로 양수(겹치지 않음)인 것도 수치로 확인했다. 콘솔에는 기존 무관한 경고 외 새 에러/경고 없음.

## 2026-09-23 체력 40% 이전엔 스캔라인까지 완전히 제거

- 사용자 피드백: "일단 체력이 40%가 되기전에는 UI화면이 노이즈 없이 깔끔하고 선명해야되".
- 직전 세션에서 체력 연동을 붙인 건 패널 안쪽의 "그레인 노이즈" 텍스처 오버레이(`noiseOverlays`)뿐이었다. 그런데 각 패널에는 그와 별개로 스캔라인 텍스처 오버레이가 `scanlineOpacity`(0.16) 고정값으로 체력과 무관하게 항상 표시되고 있었고, `FlickerRoutine`/`GlitchRoutine` 코루틴(HUD 전체 또는 무작위 패널을 짧게 깜빡이는 "신호 끊김" 연출)도 체력과 무관하게 계속 동작하고 있었다. 텍스트 위에 항상 겹쳐 있는 스캔라인 줄무늬와 이따금의 깜빡임 때문에, 그레인 노이즈가 0이어도 화면이 "완전히 깔끔"하지는 않았다 — 이번 요청은 이 부분을 짚은 것으로 판단했다.
- `HudController.cs`에서 스캔라인도 노이즈와 동일한 체력 임계값(`noiseHealthThreshold`, 40%) 로직을 타도록 통합했다: `scanlineOpacity`(고정값) 필드를 `maxScanlineOpacity`(체력 0%일 때 최대 강도)로 의미를 바꾸고, `DecoratePanel`이 만드는 스캔라인 엘리먼트도 초기 불투명도 0에서 시작하도록 했다. `noiseOverlays`와 같은 패턴으로 `scanlineOverlays` 배열에 참조를 모아두고, `RefreshHealth()`에서 호출하던 `UpdateNoiseIntensity`를 `UpdateHologramIntensity`로 이름을 바꿔 노이즈와 스캔라인 오버레이 모두를 같은 `t`(0~1, 40%→0%를 0→1로 매핑) 값으로 함께 갱신한다.
- `FlickerRoutine`/`GlitchRoutine`도 매 반복마다 `currentHealthRatio`(새로 추가한 필드, `RefreshHealth`에서 매번 갱신)를 확인해 체력이 40% 이상이면 그 회차의 깜빡임 효과를 건너뛴다 — 대기 타이머 자체는 계속 돌지만(체력이 다시 떨어졌을 때 곧바로 반응하도록) 정상 체력에서는 어떤 시각적 효과도 트리거되지 않는다.
- 필드 이름 변경(`scanlineOpacity` → `maxScanlineOpacity`) 전에 `SerializedObject`로 씬의 실제 값이 코드 기본값(0.16)과 같은지 확인해 인스펙터 오버라이드 유실 위험이 없음을 재확인한 뒤 진행했다(직전 `noiseOpacity`→`maxNoiseOpacity` 변경 때와 같은 절차).
- **검증**(Play Mode, 좀비 비활성화로 변수 통제): `noiseOverlays[0]`/`scanlineOverlays[0]`의 `resolvedStyle.opacity`를 단계적으로 측정 — 100%/45%(둘 다 0.000) → 35%(노이즈 0.038, 스캔라인 0.020) → 0%(노이즈 0.300=`maxNoiseOpacity`, 스캔라인 0.160=`maxScanlineOpacity`)로 두 오버레이가 정확히 같은 비율로 함께 나타나는 것을 확인했다. Play Mode를 재시작해 디스크 기준 최종 상태로 정상 체력 스크린샷을 다시 캡처해, 모든 패널에서 스캔라인 줄무늬·그레인 노이즈가 전혀 보이지 않고 텍스트가 선명한 것을 시각적으로도 확인했다. `recompile_status`로 컴파일 성공 확인, 콘솔에 새 에러/경고 없음.

## 2026-09-23 텍스트 외곽선/그림자를 완전히 제거

- 사용자 피드백: "UI의 작은 글씨는 가시성을 없에봐 글씨가 흐려서 못알아보겠어".
- 두 세션 전에 넣은 `.hud-root Label, .hud-root Button`의 `text-shadow`(블러 2px) + `-unity-text-outline-width`(0.35px)를 한 번 얇게 줄였는데도, 작은 폰트(9~13px대)에서는 이 흐림 효과 자체가 여전히 글자를 뭉개는 원인이었다. 두 속성이 붙은 규칙 블록 전체를 삭제해 텍스트 렌더링을 SDF 폰트 기본 상태(외곽선·그림자 없음)로 되돌렸다.
- 대부분의 HUD 텍스트가 이미 어둡고 불투명한 패널 배경(`--hud-surface` 등) 위에 있어 별도 그림자/외곽선 없이도 대비가 충분하다고 판단했다. 폰트 크기도 직전 세션에 전체적으로 키워둔 상태라 가독성을 위한 보조 장치 없이도 기본 렌더링만으로 충분할 것으로 보고 제거하는 쪽을 선택했다.
- **검증**: `AssetDatabase.Refresh()` 후 컴파일/USS 파싱 에러 없음 확인. Play Mode 재시작(좀비 비활성화) 후 스크린샷으로 모든 패널의 글자가 흐림 없이 또렷하게 렌더링되는 것을 확인했다.

## 2026-09-23 Phase 2 콘텐츠 폭 확장 — 정제 자재, 바닥, 튼튼한 벽

- 사용자가 멀티플레이(Phase 3)는 미루고 "2번부터"(레시피·건축물 종류 추가)를 선택했다. 그 전에 DESIGN.md가 예시로 든 "보관함"을 이번에 실제로 기능하게 만들지 물었고, 사용자는 컨테이너 인벤토리+UI가 필요한 별도 작업이라는 설명에 동의해 "이번엔 제외, 바닥·벽 종류만 추가"를 선택했다.
- **콘텐츠 설계**: 재료 흐름에 단계를 하나 더 넣었다 — 나무+고철 조각 → 기초 자재(기존) → 기초 자재+고철 조각 → **정제 자재**(신규, `Item_RefinedMaterial`/`Recipe_RefinedMaterial`). 정제 자재를 고급 건축물의 재료로 써서 자연스러운 티어 진행을 만들었다. 신규 건축물은 `바닥`(Buildable_Floor, 기초 자재 x2, 얇은 큐브 스케일 2x0.2x2)과 `튼튼한 벽`(Buildable_ReinforcedWall, 정제 자재 x2, 기존 벽보다 두꺼운 큐브 스케일 2x2x0.35) — DESIGN.md 9.3절이 언급한 "바닥, 벽"의 최소 조합을 채웠다. 프리팹은 기존 `M_Buildable_Wall.mat`을 그대로 재사용했다(형태 차이로 구분 가능해 새 재질을 만들지 않음 — 스코프 최소화).
- **BuildingSystem 구조 변경(필수)**: 기존 `BuildingSystem`은 `BuildableDefinition buildable` 단일 필드만 있어 여러 건축물을 선택할 방법이 없었다. 이 변경 없이는 새 건축물을 추가해도 실제로 지을 수 없으므로, 콘텐츠 추가에 필수적인 인프라로 판단해 같이 진행했다: `buildables` 배열 + `selectedIndex` + 숫자 키(1~9, `Keyboard.current[Key]` 인덱서로 `buildables.Length`만큼만 반응)로 선택하는 방식으로 바꿨다. 선택 UI는 새로 만들지 않고(범위 제외 대상과 같은 이유로 이번 스코프 밖) `Debug.Log($"[Build] 선택: ...")`로만 피드백한다 — 기존 코드베이스가 이미 쓰던 "전용 UI 없는 기능은 콘솔 로그로 검증" 패턴(HealthDebugPrinter 등)을 따랐다.
- **세이브 시스템 정합성**: `ItemDatabase`/`BuildableDatabase`(Resources 싱글턴, stableId/buildableId로 역참조하는 조회용 에셋)에 새 아이템·건축물을 등록하지 않으면 저장된 세이브 파일을 불러올 때 "unknown id" 경고와 함께 조용히 스킵된다. `GameSaveSystem.cs`/`SaveData.cs` 자체는 완전히 제네릭(정의 목록을 몰라도 동작)해서 코드 변경이 전혀 필요 없었다 — 데이터베이스 등록만으로 저장/불러오기가 새 콘텐츠까지 자동으로 커버됐다.
- **테스트 스크립트 작성 시 교훈(재확인)**: `SelectBuildable`/`TryPlace`/`SetBuildMode`를 한 eval 호출 안에서 연달아 호출했더니, `Destroy()`가 즉시 적용되지 않고 다음 프레임까지 지연되는 특성 때문에 고스트 오브젝트가 실제로는 아직 살아있는 상태에서 `GameSaveSystem.Save`가 그걸 같이 저장해버렸다(빈 buildableId 항목 발생, 이후 Load에서 "unknown buildable id ''" 경고 + 중복 스폰). 각 단계를 별도의 eval 호출로 나눠(MCP 왕복 자체가 자연스러운 프레임 경계 역할을 함) 다시 테스트하니 문제없이 정확한 개수로 배치/저장/복원됐다. 이전 세션에서도 비슷한 "프레임 경계" 문제를 겪은 적이 있는데(카메라 회전 검증), 여러 상태 변경 함수를 한 번에 몰아서 reflection 호출할 때 특히 주의해야 한다는 걸 다시 확인했다.
- **검증**(Play Mode, 좀비 비활성화, 각 단계를 별도 eval 호출로 분리): ① `CraftingSystem.TryCraft`로 정제 자재 제작 — 기초 자재 2개+고철 조각 2개 정확히 소모, 정제 자재 1개 획득 확인. ② 숫자 키 선택(리플렉션으로 `SelectBuildable(0/1/2)` 호출) 전환 시 콘솔에 "[Build] 선택: 바닥" / "[Build] 선택: 튼튼한 벽" 로그 확인. ③ 벽/바닥/튼튼한 벽 순서로 설치 — 기초 자재 10→5(벽 3+바닥 2), 정제 자재 11→9(튼튼한 벽 2)로 정확히 차감되고 서로 다른 프리팹(Buildable_Wall/Floor/ReinforcedWall)으로 스폰되는 것을 확인. ④ 빌드 모드 종료 후 `GameSaveSystem.Save`→기존 3개 파괴→`Load` — 3종 모두 정확한 buildableId(`wall_basic`/`floor_basic`/`wall_reinforced`)와 새 worldId로 복원, 콘솔에 "unknown buildable id" 경고 없음. ⑤ HUD 제작 패널을 열어 "기초 자재 제작"/"정제 자재 제작" 두 레시피가 모두 표시되고, 인벤토리에 벽+바닥+튼튼한 벽이 배치된 월드를 스크린샷으로 시각 확인. `recompile_status`로 `BuildingSystem.cs` 컴파일 성공 확인.

## 2026-09-23 건축 프리팹을 KatsBits/Medieval Fortification/FurnishedCabin 실물 에셋으로 교체

- 사용자 요청: "건축에 사용될 프리펩은 FurnishedCabin, KatsBits-fantasy-base, Medieval Fortification 에셋을 사용할거야".
- **역할 분담 확인**: 세 패키지가 성격이 너무 달라서(KatsBits=2m 그리드 모듈형 벽/바닥/기둥/문, Medieval Fortification=4m급 대형 타워·게이트·성벽, FurnishedCabin=침대·소파 등 가구+통짜 캐빈 셸 2종) 바로 작업하면 헛수고할 위험이 커 먼저 물었다. 사용자가 선택한 답: **계층형**(기초=KatsBits, 고급/방어=Fortification, 장식=FurnishedCabin) + KatsBits는 7가지 색 변형 중 **plain**만 사용.
- **용량/추적 범위**: 세 패키지 총합 약 453MB(개별 파일 100MB 초과 없음 확인, GitHub 하드 리밋 안전). `.gitignore`에 세 패키지를 개별적으로 화이트리스트했고, KatsBits는 `Prefabs/Walls_plain/`만 남기고 `Walls/`, `Walls_green/`, `Walls_grey/`, `Walls_orange/`, `Walls_red/`, `Walls_yellow/`는 계속 무시하도록 상위 `!.../** ` 뒤에 더 구체적인 무시 규칙을 추가했다(`git add -n`으로 실제로 0건만 걸리는 것을 사전 확인). Survivalist 때와 같은 패턴(대형 벤더 패키지를 선택적으로 git 추적)이라 별도 커밋(`chore: 건축 프리팹 소스 3종 채택`)으로 먼저 올렸다.
- **실측 없이 바로 벤더 프리팹을 못 쓰는 이유**: 세 프리팹(KatsBits `Wall.Plain.2U`, KatsBits `Paving.Slabs [2x2]`, Fortification `Fortification_Wall`)을 원점에 인스턴스화해 `Renderer.bounds`를 직접 측정했다.
  - `Wall.Plain.2U`: center=(0,1,0), size=(2,2,0.7) — 피벗이 이미 "바닥 중앙"이라 우리 그리드 배치 로직(레이캐스트로 땅 높이를 구해 `transform.position`에 바로 대입)과 완벽히 맞는다. 폭도 정확히 2m라 `gridSize=2`와 일치.
  - `Paving.Slabs [2x2]`: center=(0,0,0), size=(2,0,2) — 완전 평면, 피벗도 정중앙. 역시 그대로 맞는다.
  - `Fortification_Wall`: center=(0.01, 1.90, 0.68), size=(4.11, 4.02, 1.61) — 피벗이 원점 기준으로 크게 치우쳐 있고 크기도 2m 그리드보다 훨씬 크다(원래 시야로는 "돌 성벽"일 거라 예상했는데, 실제로 배치해 보니 끝이 뾰족한 **목재 방책(팔리세이드)** 형태였다 — 좀비 서바이벌 방어벽 테마에 오히려 더 잘 맞는 우연한 결과).
- **래퍼 프리팹 패턴**: 벤더 프리팹을 직접 수정하지 않고(rules.md 13), `Assets/_Project/Prefabs/Building/`의 기존 3개 프리팹(Buildable_Wall/Floor/ReinforcedWall)을 "빈 루트 + 자식으로 벤더 프리팹을 `PrefabUtility.InstantiatePrefab`로 중첩 인스턴스 참조 + 측정된 바운즈로 계산한 자식 로컬 오프셋(피벗을 바닥-중앙으로 재정렬) + 루트에 바운즈 크기의 `BoxCollider` + `BuildableInstance`" 구조로 다시 만들었다. **기존 프리팹 파일 경로(=GUID)를 그대로 덮어써서 저장**했기 때문에 `BuildableDefinition` 에셋의 `prefab` 참조, `BuildableDatabase` 등록, 씬의 `BuildingSystem.buildables` 배열 등 다른 파일은 전혀 건드릴 필요가 없었다(변경 후 `BuildableDefinition.Prefab == 새 프리팹` True로 확인).
- **부수 효과 1 — URP 머티리얼 자동 업그레이드**: 세 패키지 모두 Built-in RP 기준으로 제작된 에셋이라, 프리팹을 처음 인스턴스화하자 Unity가 관련 머티리얼(`_MainTex`/`_Color` 등 레거시 슬롯을 URP의 `_BaseMap`/`_BaseColor`와 동기화)을 자동 업그레이드하며 20여 개 `.mat` 파일이 디스크에서 수정됐다. `git diff`로 각 변경이 정말 셰이더 호환성 동기화뿐인지 확인했다(예: `Fortification_Wall.mat`의 `_MainTex`가 `fileID: 0`(누락) → 실제 텍스처 참조로 채워짐 — 오히려 버그 수정에 가까움). 콘솔에 핑크/셰이더 누락 관련 에러·경고가 없는 것도 확인했다. 이 자동 업그레이드 없이는 URP 프로젝트에서 이 머티리얼들이 마젠타로 깨져 보였을 것이므로 필요한 변경으로 판단해 그대로 커밋에 포함했다.
- **부수 효과 2 — PlayerArmature.prefab 필드 드리프트 발견**: `git status`에서 `Assets/Survivalist/Prefab/PlayerArmature.prefab`이 함께 수정된 것을 보고 원인을 조사했다. 두 가지였다:
  1. `BuildingSystem.buildable`(구 필드) → `buildables`(신규 배열) 리네임을 지난 세션에서 **씬 인스턴스에만** `SerializedObject`로 적용하고 프리팹 에셋 자체는 갱신하지 않아서, 프리팹을 다시 저장(`PrefabUtility.SaveAsPrefabAsset`류 작업이 도메인 경유로 프리팹을 재직렬화)하자 필드가 기본값(빈 배열)으로 드러났다. `PrefabUtility.LoadPrefabContents`로 프리팹 자체의 `buildables` 배열도 씬과 동일한 3종(Wall/Floor/ReinforcedWall)으로 맞춰 저장했다 — 이걸 안 고쳤으면 새 플레이어 인스턴스(멀티플레이 등)가 빈 건축 목록으로 스폰되는 잠재 버그였다.
  2. `ThirdPersonController.GroundLayers`가 `513`(bit 0 + bit 9) → `1`(bit 0만)로 바뀌어 있었다. `ProjectSettings/TagManager.asset`은 전혀 변경되지 않았고, 직접 확인해보니 레이어 9는 애초에 이름도 없고(`LayerMask.LayerToName(9)`가 빈 문자열) 씬의 `Floor` 오브젝트도 항상 레이어 0(Default)이었다 — 즉 bit 9는 원래부터 쓰이지 않던 값이었고, 현재 값(bit 0만)으로도 지면 판정에 영향이 없다. Play Mode에서 `Grounded=True`를 리플렉션으로 직접 확인해 실제로 문제없음을 검증했다. 이 변경이 정확히 언제/왜 발생했는지는 특정하지 못했지만(이번 세션 어느 시점의 정상적인 에디터 동작으로 추정), 기능적으로 무해하다는 걸 확인했으므로 되돌리지 않고 그대로 뒀다.
- **후속 과제로 남긴 것**: FurnishedCabin 가구를 실제 건축 가능한 콘텐츠로 연동하는 작업, Fortification류 대형 조각과 KatsBits 2m 그리드 사이의 스케일 불일치를 다루는 방법(별도 그리드 크기 도입 또는 "특수 대형 구조물"로 취급) — 둘 다 이번 스코프에서 의도적으로 제외했다.
- **검증**: `AssetDatabase.Refresh()` 후 콘솔에 새 컴파일/셰이더 에러 없음 확인(펜딩 벤더 데모 씬 관련 에러는 전부 기존에도 있던 무관한 것 — `wall_2U`/`arch_cnr_L` 등은 KatsBits 자체 데모 씬이 우리가 무시하기로 한 색상 변형을 참조해서 나는 것으로, 우리가 실제로 여는 씬이 아니라 영향 없음). Play Mode에서 벽/바닥/튼튼한 벽을 각각 배치해 실물 메시·텍스처가 정상 렌더링되고(회색 석벽, 석재 바닥 타일, 목재 방책) 바닥에 파묻히거나 뜨지 않는 것을 스크린샷으로 확인했다. `git status`로 최종 변경 파일 목록이 의도한 범위(래퍼 프리팹 3개, PlayerArmature.prefab, 벤더 머티리얼 자동 업그레이드분)와 정확히 일치하는 것을 확인했다.

## 2026-09-23 건축 UI를 Palworld류 탭+리스트 메뉴로 전환

- 사용자 요청: "건축키를 누르면 Palworld 게임의 건축 UI처럼 탭 기반의 리스트 UI 구조의 창이 나와 선택할수있게 하자". 숫자 키(1/2/3) + 콘솔 로그였던 건축물 선택 방식을 실제 UI로 바꾸는 작업이다. DESIGN.md 12.3절(카테고리별 건축물 목록, 요구 재료 표시)을 실제로 구현했다.
- **카테고리 설계**: `BuildableDefinition`에 `BuildableCategory`(Wall/Floor) 필드를 추가했다. 재질 티어(기초/정제)가 아니라 조각 타입(벽/바닥) 기준으로 묶었다 — Palworld류 건축 UI와 KatsBits 자체 폴더 구조(Walls/Ground)가 이 방식이고, "튼튼한 벽"도 결국 벽이라 같은 "벽" 탭 안에서 재질 업그레이드로 보여주는 게 더 자연스럽다고 판단했다. 벽=Wall(간이 벽, 튼튼한 벽), 바닥=Floor(바닥) 두 탭으로 시작하지만, 나중에 Door/Roof/Decoration 등으로 늘어나도 `BuildableCategory`에 값만 추가하면 된다.
- **BuildingSystem을 UI에서 조회·조작 가능하게 확장**: `Buildables`/`SelectedIndex` 공개, `SelectBuildable(BuildableDefinition)` 공개 오버로드(리스트의 "선택" 버튼이 호출), `BuildModeChanged`/`BuildableSelected` 이벤트(HudController가 구독해 패널 개폐·하이라이트 갱신). 기존 숫자 키 1~9 단축키는 유지했다(보조 수단으로 남겨둠, 제거하지 않음).
- **커서 잠금 토글(필수 발견 사항)**: `StarterAssetsInputs.cs`가 `cursorLocked=true` 기본값으로 커서를 항상 잠가 놓는데, 이 프로젝트 어디에도 상황에 따라 커서를 풀어주는 코드가 없었다 — 즉 지금까지 인벤토리/제작 패널의 버튼들도 실제 마우스로는 클릭이 안 되는 상태였을 가능성이 높다(이전 세션 노트에도 "사람이 직접 클릭해서 확인해줘야 한다"고만 적혀 있었다). 건축 메뉴가 진짜로 "선택할 수 있게" 하려면 최소한 건축 모드 중에는 커서를 풀어야 해서, `BuildingSystem.SetBuildMode()`에서 건축 모드 on/off에 맞춰 `Cursor.lockState`/`Cursor.visible`을 직접 토글하도록 추가했다(인벤토리/제작 패널의 커서 잠금 문제는 이번 요청 범위 밖이라 손대지 않았다 — 필요하면 후속 작업으로 분리).
- **UI Toolkit Button 클릭이 Fire와 동시에 처리되는 문제**: 이 프로젝트는 UGUI EventSystem 없이 UIDocument 런타임 패널만 쓴다. Input System의 `Fire` 액션(좌클릭)은 UI 피킹과 무관하게 항상 발동하므로, 건축 메뉴의 "선택" 버튼을 클릭하면 그 클릭이 동시에 `BuildingSystem.OnFire`로도 들어가 고스트를 배치해버릴 위험이 있었다. `RuntimePanelUtils.ScreenToPanel` + `panel.Pick()`으로 "포인터가 피킹 가능한 UI 위에 있는지"를 직접 판정하는 `IsPointerOverInteractiveUI()`를 추가해 `OnFire`에서 그 경우 배치를 건너뛰게 했다(UGUI의 `EventSystem.IsPointerOverGameObject()`에 해당하는 UI Toolkit 방식).
- **검증 중 발견한 진짜 버그 — 조준점(reticle)이 항상 피킹 가능했다**: 화면 정중앙(50%,50%)에서 `panel.Pick()`을 테스트했더니 `reticle-core`가 잡혔다. `Hud.uxml`의 reticle 관련 5개 VisualElement(reticle 루트 + 라인 4개 + 코어) 어디에도 `picking-mode="Ignore"`가 없었던 것 — 지금까지는 아무 것도 `panel.Pick()`을 쓰지 않아서 드러나지 않았을 뿐, 이 조준점은 처음부터 화면 중앙에서 클릭을 "먹는" 상태였다. 전부 `picking-mode="Ignore"`로 고쳤다. 이 수정이 없었다면 건축 모드에서 화면 중앙 근처를 조준할 때마다(가장 흔한 경우) Fire가 항상 막혔을 것이다.
- **테스트 방법론 — 실제 마우스 좌표 주입**: `Mouse.current.position`은 읽기 전용이라 단순 대입이 안 되고, `RuntimePanelUtils.ScreenToPanel`은 스크린→패널 변환만 제공해 역방향(패널→스크린) 함수가 없었다(`PanelToScreen`은 존재하지 않음, 컴파일 에러로 확인). `Screen.width / root.layout.width`로 실제 스케일 팩터(이 세션 테스트 환경에서는 Game View 816x596 vs 레퍼런스 1200x800 기준 약 0.7125)를 직접 계산해 패널 좌표를 스크린 좌표로 역변환하고, `InputSystem.QueueStateEvent(mouse, new MouseState{position=...})` + `InputSystem.Update()`로 실제 Input System에 마우스 위치를 주입해 검증했다. 이 방식으로 "버튼 위에서는 배치 차단, 빈 화면에서는 정상 배치"를 자원 차감/오브젝트 개수 변화로 직접 확인했다 — `ClickEvent.GetPooled()`로 합성 이벤트를 만들어 `SendEvent`하는 방식은 `Button.clicked`를 안정적으로 트리거하지 못해 폐기했다(재현 안 되는 SelectedIndex 변화가 한 번 관찰됐는데, 이후 동일 조건에서 재현되지 않아 테스트 아티팩트로 결론지었다 — 실제 코드 버그는 아닌 것으로 판단).
- **후속 필요**: 실제 사람이 마우스로 "선택" 버튼과 탭을 직접 클릭해 조작감을 확인해야 한다(자동화로 검증한 것은 로직 정확성과 Pick 기반 가드 동작이지, 진짜 클릭 이벤트 자체의 전체 파이프라인은 아니다). 인벤토리/제작 패널도 지금까지 커서 잠금 때문에 마우스로 못 눌렀을 가능성이 있다는 점을 사용자에게 알려야 한다.
- **검증**: `recompile_status`로 `BuildableDefinition.cs`/`BuildingSystem.cs`/`HudController.cs` 컴파일 성공 확인(도중 `UnityEngine.Cursor`와 `UnityEngine.UIElements.Cursor` 이름 충돌 에러가 나서 완전한 이름으로 고쳤다). Play Mode에서 B키로 패널이 열리고 벽/바닥 탭과 비용("기초 자재 x3" 등)이 정확히 표시되는 것, `currentBuildCategory`를 바닥으로 바꿨을 때 리스트가 정확히 필터링되는 것, `SelectBuildable(정의)` 호출 시 선택 하이라이트(`build-row-selected`)가 해당 행으로 옮겨가는 것을 스크린샷과 리플렉션으로 확인했다. Fire 가드는 위에 설명한 실제 마우스 좌표 주입 방식으로 배치 차단/허용 양쪽을 수치로 확인했다.

## 2026-09-23 인벤토리·제작·건축 패널을 중앙 아이콘 그리드로 재구성

- 사용자 요청: "인벤토리 창, 제작창, 건축창을 중앙으로 옮기고 크기고 키운다음 격자형태로 아이템 아이콘으로 정렬되게하고 아이템 아이콘 우측 하단에는 수량이 표시되고 마우스를 올리면 살세 툴팁이 나오게 해줘". 직전 세션의 탭+리스트 건축 메뉴를 포함해 세 패널 전부를 텍스트 행에서 아이콘 그리드로 갈아엎는 작업이다.
- **아이콘 소스**: `ItemDefinition`은 이미 `Sprite icon` 필드가 있었지만(비어있음) `BuildableDefinition`엔 없어서 추가했다. `RecipeDefinition`은 새 필드를 만들지 않고 `OutputItem.Icon`을 재사용했다 — 레시피는 결국 "그 아이템을 만드는 행위"라 결과물 아이콘을 그대로 쓰는 게 자연스럽고 중복 데이터도 피할 수 있다. 실제 아이콘 스프라이트는 하나도 없는 상태라(이 프로젝트는 지금까지 실사 아이콘 대신 survivor-emblem류 "이니셜 원형 배지"를 계속 써왔다), 아이콘이 없으면 이름 앞 1~2글자를 원형 배지에 넣는 폴백으로 처리해 지금 당장도 보기 흉하지 않게 하고 나중에 실제 아이콘이 생기면 코드 변경 없이 자동으로 교체되게 했다.
- **ListView → 수동 래핑 그리드**: 현재 데이터 양(아이템 몇 종, 레시피 2개, 건축물 3개)이 가상화가 필요 없을 만큼 작아서, `ListView`(세로 가상 리스트) 대신 `ScrollView` + `flex-direction:row; flex-wrap:wrap;` 컨테이너에 셀을 직접 `Clear()`/`Add()`하는 방식을 택했다. `GridView`(UI Toolkit 실험적 API) 존재 여부는 확인하지 않고 더 단순하고 버전 의존성이 없는 이 방법으로 바로 갔다 — 아이템 수가 수백 단위로 늘어나면 그때 가상화를 재검토한다.
- **중앙 정렬 버그와 원인**: `.center-panel`에 `left/top:50%` + 음수 margin(절반 크기)으로 중앙 정렬을 걸었는데, 처음엔 `max-height:520px`를 썼다가 내용이 짧아 실제 렌더 높이가 훨씬 작아지면서 위쪽으로 치우쳐 보였다(중앙 정렬 공식이 "고정 크기"를 전제로 하기 때문). `height:520px`(고정)로 바꿨는데도 안 고쳐져서 봤더니, 예전부터 있던 `.utility-panel`의 `max-height:342px`가 여전히 살아있어 새 `height:520px`를 강제로 342까지 눌러 잡고 있었다(CSS의 max-height가 height보다 우선 적용되는 것과 같은 동작). `.utility-panel`에서 `width`/`max-height`를 아예 빼고 크기 결정을 전부 `.center-panel`에 맡기도록 정리해서 해결했다. `resolvedStyle.height`를 직접 찍어보며(171 → 342 → 520) 원인을 좁혀갔다.
- **커서 잠금/AnyPanelOpen 통합**: 직전 세션에서 건축 모드 전용으로 넣었던 커서 잠금 토글을 인벤토리/제작 패널까지 확장해야 했다(그리드 클릭과 호버 툴팁이 커서가 잠겨있으면 애초에 작동할 수 없으므로 이번 요청을 만족시키는 데 필수). `HudController`에 정적 프로퍼티 `AnyPanelOpen`과 `UpdatePointerLock()`을 추가해 인벤토리/제작/건축 세 상태를 한 곳에서 계산하고 커서 잠금·표시를 일관되게 맞췄다. 세 패널은 서로 배타적으로 열리게 했다(하나를 열면 나머지 둘을 자동으로 닫음) — 안 그러면 다 같은 화면 중앙 자리에 겹쳐서 뜨기 때문에 이번 요청(중앙 배치)을 제대로 구현하려면 사실상 필수였다. `BuildingSystem`에 `ExitBuildMode()` 공개 메서드를 추가해 HudController가 건축 모드도 함께 닫을 수 있게 했다.
- **크래프팅 셀 클릭이 동시에 사격으로 처리되는 문제(새로 발견)**: 건축 모드 때와 같은 문제가 제작 패널에도 있었다 — `HitscanWeapon.OnFire`는 `BuildingSystem.BuildModeActive`만 확인하고 인벤토리/제작 패널이 열려있는지는 전혀 몰랐다. 지금까지는 커서가 항상 잠겨있어 실질적으로 드러나지 않았지만, 이번에 커서를 풀면서 실제로 클릭 시 사격이 같이 나갈 수 있는 상태가 됐다. `HudController.AnyPanelOpen`(정적 프로퍼티, HUD는 하나뿐이라 정적으로 접근해도 무방하다고 판단)을 `HitscanWeapon.OnFire`에서 확인하도록 한 줄 추가해 막았다.
- **자동화 한계 재확인**: `InputSystem.QueueStateEvent`로 마우스 좌표를 주입해도 UI Toolkit의 `PointerEnterEvent`/`PointerMoveEvent` 디스패치 자체는 트리거되지 않았다(패널의 `Pick()` 판정은 즉시 반영되지만, 실제 포인터 이벤트 라우팅은 에디터 Game 뷰의 실제 OS 포커스/렌더 루프에 더 깊이 걸려있는 것으로 보인다). 그래서 툴팁의 `ShowTooltip`/`PositionTooltip`은 리플렉션으로 직접 호출해 렌더링 자체(텍스트, 배경, BringToFront)만 검증했고, 실제 호버로 툴팁이 뜨는지는 검증하지 못했다. 이전 세션의 "사람이 직접 클릭해서 확인해야 한다" 한계와 같은 종류다.
- **검증**: `recompile_status`로 `BuildableDefinition.cs`/`BuildingSystem.cs`/`HitscanWeapon.cs`/`HudController.cs` 컴파일 성공 확인. Play Mode에서 인벤토리(나무/고철/기초/정제 4종, 수량 배지 x12/x7/x5/x2)·제작(기초/정제, 결과 수량 x1)·건축(벽 탭에 간이/튼튼, 선택 하이라이트) 세 그리드가 모두 화면 정중앙(680x520, `resolvedStyle.height` 확인)에 정확히 렌더링되는 것을 스크린샷으로 확인했다. 인벤토리를 연 상태에서 제작을 열면 인벤토리가 자동으로 닫히는 것, `Craft()` 직접 호출로 나무/고철이 정확히 차감되고 기초 자재가 늘어나는 것, 건축 모드가 아닌 인벤토리만 열린 상태에서도 `AnyPanelOpen=True`가 되어 사격이 막히는 조건이 성립하는 것, 모든 패널을 닫으면 커서가 다시 잠기는 것을 확인했다. 콘솔에 새 에러 없음.

## 2026-09-23 git에 안 올라가는 벤더 에셋 전체 백업 압축

- 사용자 요청: "일단 깃허브에 안올라가는 에셋들을 전부 압축을 하자".
- `git check-ignore`로 `Assets/` 최상위 항목을 전수 분류했다. 완전히 무시되는 최상위 폴더 24개(All In One - Heightmaps, Animal pack deluxe v2, Character Controller, Creep Horror Creature, Devion Games, Gaia User Data, JMO Assets, KUBIKOS-Animated Cube Mini Animals 2, Kinemation, LoafbrrAssets, MicroVerse-Extras, Mine, NewPunch, PolyOne, PostApocalypseGunsDemo, RPG_FPS_game_assets_industrial, Rowlan, SurvivalEngine, TirgamesAssets, TutorialInfo, Ultimate Animal Sounds, Vefects, Zombie, ZombieHorrorPackageFree, rezus13_assets)와 느슨한 파일 2개(AnimalPackDeluxe.textClipping, Readme.asset), 그리고 `KatsBits-fantasy-base`(대부분 추적됨) 안에서 일부만 무시되는 6개 색상 변형 프리팹 폴더(Walls/Walls_green/Walls_grey/Walls_orange/Walls_red/Walls_yellow)를 확인했다. `FurnishedCabin`/`Medieval Fortification`/`Resources`/`Scenes`/`Settings`/`Survivalist`/`_Project`는 하위에 무시되는 파일이 전혀 없어(각각 `git status --porcelain --ignored`로 확인) 백업 대상에서 제외했다.
- 합계 약 9.8GB(각 폴더 `du -sh`로 실측). `SurvivalEngine`/`Devion Games`/`Kinemation`은 지금도 씬에서 실제로 참조·동작 중인 벤더 패키지라(Triggers, WriteInputManager, Cinemachine 어댑터 등) **압축은 백업일 뿐 원본을 지우지 않았다** — Phase A 때와 같은 원칙(`tar -czf`만 하고 `--remove-files`는 쓰지 않음). 압축 후 `Assets/Devion Games` 등 원본 폴더가 디스크에 그대로 있는 것과 `git status`가 압축 전후로 변화 없는 것(ProjectSettings의 기존 무관한 드리프트 제외)을 확인했다.
- 각 항목의 `.meta` 파일도 함께 포함했다(폴더 자체의 `.meta`는 `Assets/` 바로 밑에 형제 파일로 있고 이것도 `/Assets/*` 블랭킷 규칙에 걸려 무시 대상이라 같이 넣지 않으면 나중에 복원 시 GUID가 깨진다).
- 저장 위치는 Phase A 백업(`ZGV01-Assets-backup-20260923.tar.gz`)과 같은 규칙으로 **프로젝트 폴더 바로 위**(`E:\Unity\ZGV01-Assets-untracked-backup-20260923.tar.gz`)에 뒀다 — 저장소 안에 두지 않아 Unity가 에셋으로 인식할 위험이 없고, `.gitignore`의 `*-Assets-backup-*` 패턴과도 무관하게 애초에 git 추적 범위 밖이다(파일명에 "untracked"가 끼어 있어 그 패턴과 정확히 일치하진 않지만, 저장소 밖에 있어 상관없다).
- 압축은 백그라운드로 실행했다(9.8GB라 몇 분 이상 걸릴 것으로 예상해 포그라운드 타임아웃을 피했다). 완료 후 `tar -tzf`로 압축 파일을 실제로 나열해(16,062개 항목) 무결성을 확인했다. 최종 압축 크기는 7.3GB — 대부분 PNG/FBX/WAV 등 이미 압축된 포맷이라 gzip으로 크게 줄지는 않았다.
- **후속 참고**: 이 벤더 패키지들을 실제로 채택할지 결정되면(예: 좀비 몬스터 애셋, 총기 애셋 등) 원본이 디스크에 그대로 있으니 바로 `.gitignore`에서 풀어 추적을 켜면 된다. 반대로 확실히 안 쓸 패키지는 이 백업이 있으니 로컬 `Assets`에서 지워 디스크 공간(약 9.8GB)을 회수해도 안전하다 — 단, `SurvivalEngine`/`Devion Games`/`Kinemation`은 현재 씬이 실제로 참조하므로 삭제하면 컴파일이 깨진다.

## 2026-10-06 원격 main 동기화 후 Unity 복구

- 로컬 폴더(`.git` 없음, 초기 원본)를 `KJY-1204/ZGV01` main(`07ce4b1`)에 `git reset --hard`로 맞췄다. untracked 외부 에셋은 그대로다.
- **`com.unity.ai.navigation@2.0.14` 캐시 재손상**: 옛 `Library`에 `NavMeshLink.cs`(2020, 1.x)와 `.deprecated.cs`(2.0.14)가 섞여 있었다. Reimport All로는 `PackageCache`가 재생성되지 않으므로 `Library/PackageCache/com.unity.ai.navigation@*` 폴더를 삭제하고 `package_resolve` 후 `recompile`로 해결했다. 같은 에러가 다시 나면 같은 절차를 쓴다.
- **`Assets/ar_buildings` 재격리**: 로컬 옛 복사본에 다시 존재해 AR 패키지 누락 컴파일 에러 9건을 냈다. 삭제하지 않고 `D:\work\ZGV01-quarantine\`로 이동했다(git 비추적).
- 미해결(차단 아님): `Assets/Character Controller/.../Controls.inputactions`의 래퍼 코드 생성이 기존 `Controls.cs`를 덮어쓰지 못한다는 에디터 예외, `LightingData` 비호환 경고(라이팅 재베이크 필요), SurvivalEngine의 Floor 레이어 이름 경고.

## 2026-10-06 패널 열림 중 카메라 고정, 건축 메뉴/모드 분리

- 인벤토리/제작/건축 메뉴가 열려 있는 동안 `HudController.UpdatePointerLock()`이 플레이어 `StarterAssetsInputs.cursorInputForLook`을 끄고 `look`을 0으로 만든다. 외부 에셋(`Assets/Survivalist`)은 수정하지 않고 프로젝트 쪽에서만 처리했다(같은 어셈블리라 `StarterAssets` 네임스페이스 참조 가능). 게임패드 오른쪽 스틱 시선도 함께 꺼진다.
- 건축 모드(`BuildModeActive`)와 메뉴 열림(`BuildMenuOpen`)을 분리했다. 메뉴에서 항목을 고르면 메뉴만 닫히고 건축 모드(고스트, 설치)는 유지된다. B키는 모드 꺼짐 -> 모드+메뉴 켜기, 메뉴 닫힘 -> 메뉴 다시 열기, 메뉴 열림 -> 모드 끄기 순으로 동작한다.
- 이에 따라 `BuildingSystem.BuildModeChanged` 이벤트는 쓰는 곳이 없어져 `BuildMenuChanged`로 대체했다. 커서 잠금 해제는 `HudController`가 메뉴 열림 기준으로만 처리한다. `AnyPanelOpen`은 이제 "메뉴 열림" 기준이라 건축 모드 중 메뉴가 닫혀 있어도 false이며, 사격 억제는 `HitscanWeapon`의 `BuildModeActive` 검사가 계속 맡는다.

## 2026-10-06 건축 설치 규칙 보강과 툴팁 잔류 버그

- `BuildingSystem.TryPlace`에 두 가지를 추가했다. 플레이어가 사망(`Health.IsDead`)했으면 설치하지 않고, 고스트가 놓인 칸(격자 반 칸 이내, 높이 무시)에 같은 종류(`BuildableCategory`)의 건축물이 이미 있으면 설치하지 않는다. 벽과 바닥처럼 종류가 다르면 같은 칸에 겹쳐 지을 수 있다. 고스트도 프리팹에서 복제된 `BuildableInstance`를 가지므로 검사에서 제외한다.
- 툴팁 잔류 원인: 항목을 클릭해 패널이 닫히면 포인터가 그 칸 위에 있던 채로 요소가 사라져 `PointerLeaveEvent`가 오지 않았다. `HudController.UpdatePointerLock()`에서 패널 상태가 바뀔 때마다 `HideTooltip()`을 호출하도록 했다(인벤토리/제작창을 키로 닫을 때 같은 문제도 함께 해소된다).
- 알려진 한계: 고스트 높이 계산이 설치된 건축물 위도 지면으로 잡아(`groundMask = ~0`) 벽 위에 바닥이 높게 얹힐 수 있다. 또 `GameSaveSystem`이 `FindObjectsByType<BuildableInstance>`로 고스트까지 훑는지는 확인하지 않았다. 둘 다 이번 범위 밖이라 손대지 않았다.

## 2026-10-06 맨손 공격 구현 방식

- 모션 클립이 없고 아바타가 휴머노이드라 변환 커브 클립은 무시된다. 근육 커브 클립 + 외부 에셋 Animator 수정 대신, `Animator.GetBoneTransform(HumanBodyBones.*)`로 오른팔/상체 본을 찾아 `LateUpdate`에서 플레이어 기준 축(`transform.right/up`)으로 월드 회전을 곱하는 방식을 썼다. 본 로컬 축에 의존하지 않고 외부 에셋을 수정하지 않는다.
- 곡선 값은 시각 확인으로 정했다. 기본 포즈에 팔꿈치가 이미 조금 굽어 있어서 타격 순간 팔꿈치는 -35도(더 펴기)여야 팔이 곧게 뻗는다. 곡선은 직렬화되므로 값을 바꾸면 컴포넌트를 다시 붙이거나 프리팹의 값을 직접 고쳐야 한다(코드 기본값만 바꿔서는 기존 직렬화 값이 유지된다).
- 공격 시작 시 캐릭터를 카메라 yaw 방향으로 돌린다. 판정은 캐릭터 앞쪽 구체(`OverlapSphere`)이며 한 번의 공격에서 대상당 한 번만 맞는다. 로컬 권위 컴포넌트이므로 멀티플레이 때는 서버 권위 판정으로 옮겨야 한다.

## 2026-10-06 맨손 공격 기본 키를 좌클릭으로 변경

- 사용자 요청으로 `Punch`의 기본 바인딩을 `<Mouse>/leftButton`으로 했다(F와 게임패드 오른쪽 숄더는 보조로 유지). 좌클릭은 `Fire`(히트스캔 사격, 건축 설치)도 쓰므로 선택지를 제시했고, 사용자는 "둘 다 좌클릭(겹침 허용)"을 선택했다. 따라서 좌클릭 한 번에 사격 즉시 피해(25)와 약 0.18초 뒤 맨손 타격(10)이 모두 들어간다.
- 맨손 공격은 패널 열림/건축 모드/사망 중에는 시작하지 않으므로 건축 설치 클릭에는 끼어들지 않는다.
- 무기 장착/해제 개념이 생기면 이 겹침을 풀어야 한다(맨손일 때만 Punch, 무기를 들면 Fire).

## 2026-10-06 무기 장착 시스템 구현 방식

- **에셋 조사**: `PolyOne/Free Gun`은 총 모델 5종(+스카이박스)뿐이고 애니메이션이 없다. 프로젝트의 다른 총 클립인 Vexa `GunShot*`는 Generic 리그라 휴머노이드 캐릭터에 쓸 수 없고, Devion 휴머노이드 클립은 활/검 자세뿐이다. Kinemation은 SightsAligner 에디터 도구뿐이다. 그래서 모델만 쓰고 자세/반동은 절차적으로 만들었다.
- **총 모델은 본에 붙이지 않는다**: 총은 `WeaponPivot`이라는 별도 오브젝트로 두고 매 `LateUpdate`에 어깨 중점 기준으로 위치/회전을 계산해 놓는다(내려 든 자세와 조준 자세를 블렌드, 카메라 조준점을 바라봄). 양손은 2본 IK(`SolveArm`)로 총의 손잡이/덮개 지점에 맞추고, 손가락이 향할 방향으로 손목을 돌린다. 휴머노이드 본은 `HumanBodyBones`로 찾아 본 이름/로컬 축에 의존하지 않는다. 외부 에셋(`Survivalist` Animator 컨트롤러)은 수정하지 않았다.
- **모델 총구 방향 함정**: 렌더 이미지로 방향을 판단할 때 Unity 카메라의 `right`가 −Z를 볼 때 −X임을 놓쳐 처음에 총이 뒤를 향했다. 실제 총구는 소총(MP5/AK/M16/Barrett)이 모델 −X, 권총(FN Five-seven)이 +Z이며, `WeaponDefinition.modelEuler`(소총 Y 90, 권총 0)로 총구가 +Z가 되게 정렬한다.
- **좌클릭 겹침 해소**: 앞서 허용했던 "좌클릭에 사격과 맨손 공격이 함께 나감"은 무기 장착 개념이 생기면서 풀었다. 맨손이면 `UnarmedPunch`만, 총을 들면 `HitscanWeapon`만 반응한다. `HitscanWeapon`의 `damage`/`range` 직렬화 필드는 `WeaponDefinition`으로 옮겨 제거했다. 판정은 여전히 카메라 레이 기준이라 총구 위치와 시각적으로 어긋날 수 있다.
- **조준 중 캐릭터 회전**: 조준(우클릭) 또는 사격 후 1.5초 동안 `LateUpdate`에서 캐릭터를 카메라 yaw로 돌린다. 기본 컨트롤러(`ThirdPersonController`)의 이동 방향 회전 뒤에 덮어쓰는 방식이라 컨트롤러 코드는 건드리지 않았다.
- **PolyOne 추적**: `WeaponDefinition`이 PolyOne 프리팹을 GUID로 참조하므로 `.gitignore`에 `/Assets/PolyOne`을 풀어 추적한다(약 3MB). 다른 PC에서 에셋이 비어도 깨지지 않게 하려는 것이다.
- **테스트 환경 메모**: 사용자가 같은 에디터에서 직접 플레이 중이면 주입 입력과 실제 입력이 섞여 판정 로그가 흔들린다. 또 플레이 모드가 프레임 2에서 멈추면(드물게 발생) 플레이를 껐다 켜면 해결된다.

## 2026-10-06 탄약과 재장전 구조

- 데이터 규칙에 따라 정적 값(탄약 종류, 탄창 크기, 재장전 시간, 탄창 위치)은 `WeaponDefinition` 에셋에, 런타임 값(탄창 속 탄, 재장전 진행)은 `WeaponEquipment`에 둔다. 정의 에셋에 현재 탄수를 기록하지 않는다. 예비 탄약은 별도 카운터를 만들지 않고 기존 인벤토리의 `Ammo` 아이템 수량을 그대로 쓴다(저장 시스템과 정합).
- 사격 한 발의 흐름: `HitscanWeapon.TryFire` -> `WeaponEquipment.TryConsumeRound`(재장전 중이거나 빈 탄창이면 거부, 빈 탄창이면 재장전 시작) -> 발사. 재장전 완료 시 `Inventory.Remove`로 필요한 만큼만 차감한다.
- 재장전 중에는 조준 자세를 풀고(총을 내림) 왼손이 `magazinePoint`로 내려갔다 오는 곡선(`ReloadHandCurve`)을 쓴다. `magazinePoint` 값은 초기 추정치라 후속 단계에서 눈으로 다듬는다.
- HUD는 `WeaponEquipment.StatusChanged` 이벤트로만 갱신한다(장착/탄 소모/재장전/탄약 줍기). 매 프레임 폴링하지 않는다.
- 테스트 메모: 사용자가 같은 에디터에서 플레이 중이면 주입한 수량 검증이 흔들린다. 수량 계산은 한 번의 `eval` 호출 안에서 모아 확인했다.

## 2026-10-06 연사 눌림 해제 누락 버그와 렌더 검증 요령

- **연사 고착**: `HitscanWeapon`이 `OnFire(InputValue)`의 `isPressed`를 `fireHeld`에 저장해 자동 무기 연사에 썼는데, 눌림 해제 이벤트를 놓치면(`fireHeld`=true, 실제 마우스는 눌리지 않음) 계속 발사해 탄창이 줄었다. 실제 눌림 상태를 직접 읽는 `PlayerInput.actions["Fire"].IsPressed()`로 바꿨다. 이벤트로 "누름 시작"만 쓰고 "누르고 있는 중" 판정은 액션 상태를 읽는 방식이 안전하다.
- **스킨드 메시 렌더 함정**: 같은 프레임 안에서 본 회전을 바꾸고 `Camera.Render()`를 부르면 메시는 이전 프레임 포즈로 그려진다(수치상 손은 목표에 닿아 있는데 화면에는 안 닿아 보임). 자세 확인용 렌더는 `EditorApplication.update` 콜백으로 몇 프레임 기다린 뒤 찍어야 한다.
- **테스트 요령**: 같은 에디터에서 사용자가 플레이 중이면 카메라 피치와 사격 입력이 계속 바뀐다. 자세 렌더 전에 `ThirdPersonController.LockCameraPosition = true`와 피치 0으로 고정한다. 응답 오류가 난 `eval`도 실제로는 실행됐을 수 있어서(콜백이 중복 등록돼 두 상태기계가 장비를 번갈아 바꿈) 오류 뒤에는 재시도 전에 플레이 모드를 껐다 켠다.

## 2026-10-06 무기는 제작 후 장비창에서 장착 (규칙 변경)

- 사용자 지시로 "총을 바로 쓸 수 있는" 구조를 바꿨다. 시작은 맨손이고, 총은 인벤토리 아이템이라 제작창에서 만들어야 하며, 장비창(V)에서 장착해야 쓸 수 있다. 무기 정의(`WeaponDefinition`)가 인벤토리 아이템(`Item`)을 참조하고 `WeaponEquipment.catalog`(구 `loadout`, `FormerlySerializedAs`로 이름 변경)가 아이템 -> 무기 정의 조회에 쓰인다.
- 장착 조건은 인벤토리에 그 무기 아이템이 있을 것(`Owns`), 건축 모드/사망 중이 아닐 것이다. 장착해도 아이템은 인벤토리에 그대로 남는다(장착 슬롯은 참조일 뿐). 이 때문에 무기 아이템이 인벤토리에서 사라지는 경로(버리기/해체)가 생기면 `WeaponEquipment`가 자동 해제하도록 처리해야 한다.
- 장비창은 `HudController`의 기존 패널 패턴(`SetXOpen`/`SetXOpenInternal` 상호 배타)을 그대로 따랐다. 패널이 네 개가 되어 `HudController`가 비대해졌다. 다섯 번째 패널을 추가하기 전에 패널 열림 관리를 별도 컴포넌트로 분리하는 것이 좋다.
- 테스트 요령: `Keyboard.wasPressedThisFrame`으로 읽는 키(Tab/C/V)는 `InputSystem.Update()`를 직접 부르면 감지되지 않는다. 이벤트를 `QueueStateEvent`로 큐에만 넣고 다음 프레임에 게임 루프가 처리하게 해야 한다(PlayerInput 액션 메시지는 직접 `Update()`해도 된다).

## 2026-10-06 손가락 모양 구현 (HumanPoseHandler)

- 본의 로컬 축을 몰라도 되도록 휴머노이드 근육 값으로 손가락만 접는다. `HumanPoseHandler(avatar, root)`로 이번 프레임 포즈를 읽어 손가락 근육만 쥔 값으로 섞고 다시 쓴다(`WeaponEquipment.ApplyFingers`, 팔 IK보다 먼저 실행). 읽고 되쓴 결과가 몸/루트 위치를 바꾸지 않는 것을 수치로 확인했다.
- **루트 함정**: 핸들러의 루트는 Animator가 붙은 `PlayerArmature`가 아니라 아바타 스켈레톤 루트(`Geometry/SK_Military_Survivalist`, FBX 루트)여야 한다. `PlayerArmature`를 넘기면 `SetHumanPose`가 아무 본도 바꾸지 않는다(오류 없이 무시됨). 엉덩이 본에서 위로 올라가며 스킨 메시를 가진 가장 낮은 조상을 루트로 찾는다.
- 근육 값은 정규화된 -1(접힘)~1(펴짐)이고 이 아바타의 편 손가락은 대략 0.7이다. 쥐는 정도(0~1)를 `Lerp(0.7, -0.95)`로 바꿔 쓰고 1/2/3번 관절에 0.8/1/0.9 비율을 곱한다. 엄지는 별도 범위(0.3~-0.7)다.
- IK 목표 보정: 팔 IK는 손목 위치를 목표에 맞추므로 손바닥 중심은 총 손잡이/덮개보다 손가락 방향으로 약 6cm 앞서게 된다. 목표를 `palmOffset`만큼 손가락 반대 방향으로 당긴다.

## 2026-10-06 장착 상태 저장과 집어넣기 키

- 저장 스키마는 필드만 추가했고(`schemaVersion` 1 유지) `JsonUtility`가 없는 필드를 기본값으로 읽으므로 옛 세이브와 호환된다. 구조가 바뀌어 호환이 깨지는 변경이 생기면 그때 `schemaVersion`을 올린다.
- 불러오기 순서가 중요하다. 장착 조건이 "인벤토리에 그 무기 아이템이 있을 것"이라 `Inventory` 복원 -> `WeaponEquipment.RestoreState` 순서여야 한다. 복원 시 건축 모드/사망 상태에 대한 `TryEquip` 제한은 적용하지 않고 소유 여부만 확인한다.
- 마지막으로 든 무기(`lastWeapon`)도 저장한다. 집어넣은 상태로 저장했다가 불러와도 H로 같은 총을 다시 꺼낼 수 있다.
- 탄창 속 탄수도 같이 저장한다. 저장하지 않으면 저장/불러오기가 재장전 없이 탄창을 가득 채우는 편법이 된다.
- 사용자 지시로 시작 탄약 지급은 만들지 않는다(예비 탄약은 좀비 드롭으로만 얻는다).

## 2026-10-06 WarFX 이펙트 채택

- 에셋 폴더 이름은 `JMO Assets/WarFX`다. WarFX 프리팹의 셰이더/머티리얼/텍스처/`AutoDestruct` 스크립트는 WarFX 폴더가 아니라 `Devion Games/Third Person Controller/Example/Particles/Impact`에 들어 있어서(WarFX가 이 경로의 GUID를 참조) 두 곳을 함께 추적해야 한다. 쓰는 프리팹의 의존성 계산으로 필요한 범위를 정했고, `.gitignore`에 `_Effects/MuzzleFlashes`, `_Effects/Bullet Impacts`, `Mesh`, 그리고 Devion의 `Particles/Impact` 폴더만 풀었다(약 17MB, 335개 파일). 폭발/불/연기/데모/모바일 폴더는 계속 무시한다.
- **프리팹 회전을 덮어쓰지 말 것**: WarFX 총구 화염은 루트에 Y 270도가 들어 있어 그 방향이 +Z(총구 정면)다. 인스턴스 후 `localRotation`을 초기화하면 화염이 옆으로 뻗는다. 탄착 이펙트는 `Quaternion.FromToRotation(Vector3.forward, normal)`로 세운다(WarFX 데모와 같은 방식).
- **파티클 검증 방법**: `Camera.Render()`를 수동으로 부르면 파티클이 그려지지 않는다(파티클 렌더 데이터가 플레이어 루프 안에서 갱신되기 때문). 이펙트는 `capture_game_view`(메인 카메라)로 슬로모션(`Time.timeScale` 0.03~0.1)을 걸고 확인한다. 카메라 구도가 필요하면 `CombatCameraController`를 끄고 `CinemachineThirdPersonFollow` 값을 직접 바꾼다.
- 표면 종류는 `ImpactSurfaceMarker`로 오브젝트에 표시하고 표식이 없으면 콘크리트다. 건축물 프리팹과 광석/나무 같은 월드 오브젝트에 표식을 달아 주면 표면별 이펙트가 정확해진다(현재는 테스트 씬의 Floor=흙, Tree=나무만 표시했다).

## 2026-10-06 전투 효과음

- `PostApocalypseGunsDemo`는 총마다 `1p`(1인칭, 스테레오), `3p`(3인칭, 모노), `far`(원거리) 변형을 준다. 이 게임은 3인칭이라 플레이어 본인의 총소리에는 `3p`를 썼다. `far`는 다른 사람/좀비가 낸 소리를 멀리서 들을 때 쓸 수 있다.
- 발사음은 AutoGun(자동소총) 하나를 MP5/AK/M16이 같이 쓴다(모델별 차이를 소리로 두지 않았다). 총마다 다른 소리가 필요하면 에셋이 추가될 때 `WeaponDefinition.fireSounds`만 바꾸면 된다.
- 빈 탄창 클릭은 Devion Inventory 예제의 UI `Click.wav`를 임시로 썼다(총기 전용 소리 에셋이 없어서).
- 소리 출처 에셋(약 10MB)을 `.gitignore`에서 풀었다: `PostApocalypseGunsDemo` 전체, Devion `Example/Sounds`, Devion Inventory의 `Click.wav`.
- 살점 피격음은 `AudioSource.PlayClipAtPoint`로 낸다(임시 오브젝트가 소리가 끝나면 사라진다). 풀링 단계에서 이 부분을 풀로 옮긴다.
- 오디오 검증은 귀로 들을 수 없어서 `AudioSource.isPlaying`/`clip`/`volume`/`pitch`를 프레임마다 읽는 방식으로 했다. 시점(진행 45% 등)은 `EditorApplication.update` 콜백에서 시간을 기록해 확인했다.

## 2026-10-06 이펙트 풀링 구조

- 풀링 대상은 탄착 이펙트(WarFX 프리팹)와 일회성 피격음이다. `EffectPool.Spawn(prefab, pos, rot)`가 쉬는 인스턴스를 꺼내 쓰고, 풀 실행기(`DontDestroyOnLoad` 오브젝트)가 프레임마다 수명이 지난 것을 비활성화해 큐로 돌려보낸다. 프리팹별 수명은 파티클 `startDelay + duration + startLifetime`의 최댓값을 한 번 측정해 캐시한다(0.5~15초).
- **스크립트 끄기**: WarFX 프리팹의 `AutoDestruct`(Devion 예제 스크립트)는 입자가 끝나면 `Destroy`한다. 풀 인스턴스는 만들 때 모든 `MonoBehaviour`를 `enabled=false`로 꺼 두고 비활성으로 대기시킨다. 이펙트 프리팹에 파티클 외 동작을 하는 스크립트가 생기면 이 가정이 깨지므로 그때는 풀링 대상에서 빼거나 개별 처리해야 한다.
- 상한 정책: 프리팹당 활성 32개를 넘으면 가장 오래된 활성 이펙트를 끊고 재활용한다. 총알 구멍(약 10초 유지)이 연사로 무한히 쌓이는 것을 이 상한이 막는다. 값이 크면 흔적이 오래 보이고 작으면 구멍이 빨리 사라진다.
- 반환 순서 가정: 한 프리팹의 반환 시간은 생성 순서와 같아서 활성 목록의 앞에서부터만 확인한다(프리팹별 수명이 일정하기 때문). 인스턴스별로 수명이 달라지면 정렬 구조가 필요하다.
- 도메인 리로드를 끄고 플레이하는 설정에서도 이전 판의 풀이 남지 않게 실행기 생성 시 풀을 비운다. `CreatedCount`/`ActiveCount`/`IdleCount`는 검증을 위해 공개한 관찰용 값이다.

## 2026-10-06 표면 타격음, 좀비 소리, 발소리

- 소리 데이터는 `SoundGroup`(클립 변형 + 음량 + 피치 범위) 구조로 통일했다. 좀비 소리 묶음과 발소리가 같은 구조를 쓴다. 타격음은 표면마다 항목에 같은 의미의 필드를 둔다(이펙트 프리팹과 한 곳에서 관리).
- 긴 클립에서 일부만 쓰는 방법: `AudioSource.time`으로 시작하고 `SetScheduledEndTime(dspTime + 길이/피치)`로 끊는다. 피치가 바뀌면 같은 구간의 실제 재생 시간이 달라지므로 길이를 피치로 나눈다.
- 좀비 소리는 `ZombieAI` 안에서 재생하지 않고 `Alerted`/`Attacked` 이벤트와 `Health.Damaged`/`Died`를 구독하는 별도 컴포넌트(`ZombieAudio`)가 낸다. AI 규칙과 표현을 분리했다. 사망 소리는 다른 소리에 끊기지 않도록 사망 후에는 다른 소리를 내지 않는다.
- **발소리 판정은 발목 높이를 쓴다**: 이 프로젝트의 걷기/달리기 클립에는 발소리 애니메이션 이벤트가 없고 `ThirdPersonController`에도 발소리 코드가 없다. 발목 높이(걷기 0.13~0.33, 달리기 0.14~0.68)에 문턱을 두는 방식이며, 문턱은 클립 실측값에서 정했다. 클립이 바뀌면 다시 재서 `plantHeight`/`liftHeight`를 맞춰야 한다.
- **검증 환경 한계**: MCP로 플레이할 때는(에디터 창이 가려지는 등) 플레이어 루프가 Animator의 본을 갱신하지 않아 발 높이가 계속 같은 값으로 읽힌다(`Animator`의 상태 시간은 진행되는데 본이 안 움직임). 그래서 발소리 판정은 `Animator.Update(dt)`를 직접 호출하며 `PlayerFootsteps.LateUpdate`를 같이 호출하는 방식으로 검증했다. 실제 플레이에서의 발소리 타이밍(발이 닿는 순간과의 일치)은 에디터에서 직접 확인이 필요하다.
- 소리 에셋 약 7MB(곰 포효/곰 공격/고릴라/침팬지/황소 울음 폴더)와 `Metal Drop.wav`를 `.gitignore`에서 풀었다.

## 2026-10-06 플레이어 Animator 중복 문제

- **증상**: 걷거나 점프해도 캐릭터 본이 전혀 움직이지 않았다(엉덩이/척추/팔/다리 모두). Animator 상태 시간(`normalizedTime`)은 진행되고 파라미터(`Speed=2`)와 클립 가중치도 맞았지만 본 변환이 대기 자세로 고정돼 있었다. `Animator.Update(dt)`를 직접 부르면 정상적으로 움직여서 그래프 자체는 문제가 없었다.
- **진단 과정**: 내 컴포넌트를 모두 끄고, 5개를 지워 보고, `HumanPoseHandler`를 해제해도 그대로였다(내 변경이 원인이 아님). 같은 모델+컨트롤러로 새 캐릭터를 만들면 정상 재생돼 환경 문제도 아니었다. 플레이어 안의 Animator를 나열하자 루트와 자식 모델 두 개가 모두 켜져 있었고, 자식을 끄자 애니메이션이 살아났다.
- **교훈**: 한 계층에서 같은 본을 쓰는 Animator는 하나만 둔다. FBX 모델 인스턴스에는 Animator가 함께 붙어 오므로, 모델을 부모 Animator 아래에 둘 때는 자식 Animator를 지워야 한다. 순서에 따라 보이다 말다 하므로 "예전엔 됐는데"가 가능하다.
- **검증 요령**: 애니메이션이 의심되면 본 위치 변화 폭(걷는 동안 캐릭터 로컬 좌표의 최대-최소)을 재면 된다. 정상이면 엉덩이 0.07m, 발 앞뒤 0.9m 정도 변한다.
- 앞서 발소리 검증 한계로 적었던 "MCP 플레이 중 본이 갱신되지 않는다"는 환경 한계가 아니라 이 중복 Animator 때문이었다. 이제 발소리도 실제 걷기 중에 `PlayerFootsteps`가 동작해야 하므로 에디터에서 한 번 확인한다.

## 2026-10-06 바닥 재질별 발소리

- 재질 판별은 발밑 `RaycastAll`(위 0.5m에서 아래 1.5m, 자기 콜라이더 제외)로 첫 표면의 `ImpactSurfaceMarker`를 읽는다. 타격음과 같은 표식을 재사용하므로 건축물/자원에 표식만 붙이면 발소리도 따라온다.
- 발소리 데이터는 `FootstepSoundSet`으로 분리해 플레이어와 좀비가 각자 세트를 쓰게 했다. 재질 소리는 별도 AudioSource로 겹쳐 피치가 서로 영향을 주지 않는다.
- 검증 교훈: 런타임에 만든 콜라이더는 `Physics.SyncTransforms()` 전에는 레이에 안 잡힌다.

## 2026-10-06 좀비 발소리

- 좀비는 뼈대가 없는 캡슐이라 발 높이 방식을 못 쓰고 이동 거리(보폭 1.1m)로 걸음을 센다. 나중에 좀비 모델과 애니메이션이 들어오면 애니메이션 이벤트나 `PlayerFootsteps` 방식으로 바꿀 수 있고, 재질 판별과 재생(`FootstepSounds`)은 그대로 쓴다.
- 좀비 소리는 3D(선형 감쇠 2~25m)로 두어 멀리 있는 좀비 소리가 작게 들리고, 걸음마다 소스 2개(기본/재질)를 쓴다.

## 2026-10-06 좀비 모델 채택

- **결정**: 걷는 좀비는 NewPunch Shirtless Zombie(휴머노이드), 변이체는 Creep Horror Creature(Generic, 클립 13개 포함)로 정했다. 외부 에셋은 고치지 않고 `ZombieAssetBuilder`가 컨트롤러/프리팹을 어댑터로 만든다.
- **용량**: 쓰는 파일만 추적한다(NewPunch 약 90MB, Creep는 `tex` 한 벌 포함 약 70MB). Creep의 tex2/tex3 색상 변형과 데모 씬은 제외했으므로 Creep2/3 머티리얼과 프리팹은 클론에서 텍스처가 비어 보인다(사용하지 않음).
- **Creep 클립**: 원본 루프가 꺼져 있어서 복사본에 루프를 켰다. 이동용 Walk1/Walk2 외에 달리기 클립이 없어 빠른 이동은 Walk2로 대신한다.
- **걷는 좀비 사망**: 사망 클립이 없어 모델을 뒤로 눕히는 임시 연출(`tipOverOnDeath`)이다. 클립이 생기면 이 옵션을 끄고 Dead 상태를 추가한다.

## 2026-10-06 궁수 좀비

- **착안**: 걷는 좀비는 공격 클립이 없었는데, 프로젝트에 있는 Devion 활 애니메이션(휴머노이드)을 쓰면 원거리 공격이라는 개성과 함께 해결된다.
- **구조**: 공격 판정은 `ZombieAI`에 두고 화살 생성/발사는 `ZombieRangedAttack`, 비행/피격은 `ZombieProjectile`로 나눴다. 근접 좀비는 컴포넌트가 없으면 기존 방식 그대로다.
- **함정 1 - 복사한 클립의 이벤트**: Devion 클립에 `OnEndUse` 같은 이벤트가 있어서 받는 컴포넌트가 없으면 매 재생마다 오류가 난다. 외부 클립을 복사할 때는 이벤트 목록을 확인하고 지운다.
- **함정 2 - 프리팹 덮어쓰기**: 중첩 프리팹(Creep1)의 Animator 컨트롤러를 지정한 뒤 같은 경로로 `SaveAsPrefabAsset`을 다시 하면 컨트롤러 지정이 사라졌다. 그래서 빌더는 프리팹을 지우고 새로 저장한다(씬 인스턴스는 다시 배치해야 한다).
- **활 크기**: `Bow.fbx` 루트 스케일 100은 단위 변환 흔적이고 메시는 이미 1.37m다. 회전만 옮긴다.

## 2026-10-06 궁수 시야 판정

- **시야 판정 위치**: 시야는 `ZombieAI.CanSee`에 두고 원거리 좀비에만 적용했다(근접 좀비의 발견 방식은 바꾸지 않았다). 시야각은 공격 진입에만 쓰고, 공격 중에는 목표를 바라보므로 가림 여부만 본다.
- **이동 표현**: `agent.Move`로 하는 좌우/후퇴 이동은 `agent.velocity`에 잡히지 않아 애니메이터 속도를 위치 변화량으로 계산한다. 근접 좀비도 같은 방식이다.
- **검증 함정**: 일반 큐브 벽은 NavMesh를 막지 않아 좀비가 그대로 통과한다. 시야 차단 시험에는 `NavMeshObstacle`(carving)을 붙인 벽을 쓴다. 플레이어가 죽은 상태에서는 `Heal`로 되살릴 수 없으니 시험 전에 확인한다.

## 2026-10-06 프리팹 덮어쓰기 참조 소실(재발)

- 같은 경로에 `SaveAsPrefabAsset`을 다시 하면 컨트롤러뿐 아니라 메시/머티리얼 참조도 사라질 수 있다. 빌더 산출물은 항상 `SavePrefab`으로 지우고 새로 저장하고, 빌드 뒤에는 참조(NULL 여부)를 직접 확인한다. 씬 인스턴스는 다시 배치해야 한다.

## 2026-10-06 활 방향 검증 방법

- 활 방향은 눈으로 맞추면 틀리기 쉬워서 메시 정점으로 시위 쪽을 구한 뒤(양 끝 정점이 몰린 쪽) 목표 방향과의 내적으로 확인했다. 측정은 좀비가 목표를 바라보게 돌려 놓고 `Animator.Play`+`Update(0)`로 포즈를 고정해서 한다(비활성 창에서는 플레이 루프가 멈춰 있어 시간에 의존하는 측정이 틀린다).

## 2026-10-06 생존 수치 범위 결정

- `DESIGN.md`는 허기·갈증·체온 등 다수의 생존 게이지를 확정 요구사항으로 보지 않고 범위 통제 대상에 올려 두었다. 사용자가 허기/갈증부터 시작하라고 직접 요청했으므로 이 둘만 만든다. 체온, 질병, 수면은 만들지 않는다. 관리 부담이 크다고 느껴지면 감소 속도를 낮추거나 끌 수 있게 데이터(`SurvivalStatsConfig`)로 뺀다.
- 멀티플레이에서 허기/갈증 감소와 소비는 서버 권위 규칙이 되므로, UI와 분리된 도메인 컴포넌트에 두고 UI는 이벤트만 구독한다.

## 2026-10-06 허약 상태 구현 방식

- 외부 StarterAssets 컨트롤러를 고치지 않으려고 입력 정리 어댑터(`SurvivalMovementLimiter`)가 컨트롤러보다 먼저 `jump`/`sprint`를 지운다. 컨트롤러가 입력을 읽는 Update보다 앞서야 해서 `DefaultExecutionOrder(-50)`을 쓴다.
- 허약 기준(30%)은 HUD 경고 색 기준과 하나로 합쳤다(`lowThreshold`). 단계별로 나눌 필요가 생기면 그때 구간 데이터로 확장한다.
- 검증 함정: Play 직후 첫 eval은 항상 타임아웃되고, 그 뒤에야 플레이 루프가 돈다(그 전에는 `Time.frameCount`가 2에 멈춰 있다). 루프가 안 도는 것으로 보이면 타임아웃된 eval을 한 번 더 보낸 뒤 확인한다.

## 2026-10-06 건축 조각 선정

- 에셋 조사 결과 KatsBits 벽/바닥 조각에는 콜라이더가 없고, 문/소품 프리팹은 벽 높이(2m)의 절반 크기(문 1m 높이)라 문은 2배로 키워 아치(폭 2m) 틀에 맞춘다. 래퍼가 콜라이더와 오프셋을 책임진다.
- FurnishedCabin의 옷장/주방은 에셋 자체 상호작용 스크립트와 애니메이터가 붙어 있어 이번에는 쓰지 않았다(정적 가구만 채택). 난로 같은 1~2개 콜라이더 가구만 쓴다.
- 문은 열리지 않으면 벽과 다를 바 없어서 Interact 키로 여닫는 작은 컴포넌트를 둔다. 인벤토리 같은 다른 시스템과는 연결하지 않는다.

## 2026-10-06 건축 조각 추가 결과

- 도어/아치 조사: KatsBits `Wall.Arch`는 한쪽 기둥만 있는 조립용 조각이고 `Prop.Door.*`는 씬에서 렌더되지 않았다. 문은 아치 세트를 조립하는 별도 작업으로 미뤘다.
- 격자: 2m 격자에서 4m 벽은 두 칸을 덮으므로 정의별 `snapSize`(4m)를 두었다. 가구는 0.5m. 같은 칸 판정도 정의의 스냅 크기 절반을 쓴다.
- 래퍼 콜라이더: 얇은 바닥 조각은 밟을 수 있게 표면 아래 10cm 두께 박스를 쓴다. 계단과 가구/방어 조각은 벤더 메시 콜라이더를 그대로 쓴다.
- 검증 함정: 바닥 평면은 원점 근처에만 있어서 멀리(80,85 등) 테스트하면 캐릭터가 떨어진다.

## 2026-10-06 문 구현에서 배운 것

- 벤더(KatsBits) 프리팹 루트에는 임의의 위치(-19,0,-13)와 스케일 100이 저장돼 있다. 래퍼에서 `InstantiatePrefab`한 뒤 `localPosition`을 원점으로 맞추고 스케일은 벤더 값(100)에 배율을 곱해야 한다. 스케일을 1로 덮어쓰면 100분의 1 크기가 된다(앞서 문이 안 보였던 원인).
- 아치(`Wall.Arch`)는 단면 메시다. 한쪽에서만 온전히 보이므로 문틀은 두 장을 맞댄다. 개구부 꼭대기가 원본 크기에서 1.75m라 캡슐 1.83m 플레이어가 통과하지 못하는 것을 캡슐 캐스트로 찾았다(레이 캐스트는 통과해서 못 찾는다).

## 2026-10-06 사다리

- 감시탑(`Fortification_Tower_Front`) 구조: 아랫부분은 막혀 있고 y=3.0에 발판, 좌우(±x)가 열려 있다. 그래서 사다리는 탑 옆면에 기대 세우고 3.0m 높이에서 안쪽으로 올라선다. 같은 에셋의 데모 씬은 사다리를 벽 뒤편 통로 쪽에 세웠다.
- 오르기는 컨트롤러를 끄고 위치를 직접 옮기는 방식이다(StarterAssets를 수정하지 않는다). 사다리 메시 콜라이더 때문에 CharacterController가 걸리지 않도록 오르는 동안에는 CC도 끈다.

## 2026-10-06 음식·물 노드

- 새 시스템을 만들지 않고 기존 `ResourceNode`를 재사용했다. 채집이 `Physics.OverlapSphere`로 찾은 콜라이더의 `GetComponent<ResourceNode>()`를 보므로(부모를 보지 않는다) 노드는 콜라이더와 같은 오브젝트에 둔다.
- 노드를 겹쳐 놓으면 먼저 잡히는 노드만 채집된다(물통이 기존 나무와 겹쳐 나무가 나왔다). 배치할 때 겹치지 않게 한다.
- 열매/물통/상자 수치는 허기·갈증 감소(허기 17분, 갈증 10분)에 맞춰 잡았다. 플레이해 보고 조절한다.

## 2026-10-06 스폰 시스템 결정

- 스폰 위치/수 결정은 `ZombieSpawner`(도메인 컴포넌트) 한 곳에 모았다. 멀티플레이에서는 이 컴포넌트를 서버에서만 켜고, 클라이언트는 스폰된 좀비를 복제로 받는다.
- 시체 수명은 `Died` 구독 후 `Destroy(go, 시간)`으로 처리한다. 좀비 로직이 이미 사망 시 에이전트/콜라이더를 끄므로 시체가 길을 막지 않는다. 풀링은 성능 문제가 보일 때 도입한다.
- 먼 좀비를 지우고 다른 곳에 다시 스폰하는 방식은 좀비 수(성능)를 플레이어 주변으로 제한하기 위한 것이다. 지우는 거리(90m)가 스폰 구역(40m)보다 커야 막 스폰된 좀비가 바로 지워지지 않는다.

## 2026-10-06 상호작용 안내 구조

- E 입력을 한 곳(`InteractionFocus`)에서 받아 가장 가까운 닿는 대상 하나에만 보낸다. 새 상호작용 대상(예: 상자 열기)은 `InteractionFocus`에 후보 수집/문구/실행을 추가하면 된다. 대상 컴포넌트는 활성 목록(`Active`)을 노출해 매 프레임 `FindObjectsByType`을 피한다.
- 건축 고스트(미리보기)는 `BuildableInstance.WorldId`가 비어 있어서 상호작용 대상에서 제외한다.
- 테두리는 `Graphics.DrawMesh` + 역면 외곽선 셰이더다. 셰이더는 컴포넌트의 직렬화 필드로 참조해 빌드에 포함되게 한다(Shader.Find 금지). 속이 빈 잎 카드 메시는 단색으로 채워지므로 반투명으로 그린다.
- 검증 함정: 일반 캡처 도구는 같은 프레임에 제출된 `DrawMesh` 결과를 담지 못한다. 임시 카메라로 `Update`/`LateUpdate`를 호출한 같은 평가에서 `Camera.Render()`해야 테두리가 보인다. `InputValue`는 코드로 만들 수 없어서 입력 핸들러는 `Activate()`처럼 공개 메서드로 분리해야 테스트할 수 있다.

## 2026-10-06 채집 소리 구조

- 소리 데이터는 노드 정의(`gatherSounds`), 재생은 `ResourceNodeAudio`로 나눴다. `ResourceNode`는 `Gathered` 이벤트만 알리고 소리를 모른다(멀티플레이에서 서버는 이벤트 없이, 클라이언트만 재생하게 바꾸기 쉽다).
- 프리팹을 지우고 다시 만드는 빌더는 GUID가 바뀌어 씬에 배치한 인스턴스를 끊는다. 정의 값만 바꿀 때는 정의 전용 메서드(`ApplyDefinitions`)를 쓴다.

## 2026-10-06 자원 노드 소멸 연출

- 소멸은 "삭제"가 아니라 렌더러/콜라이더를 끄는 일시 숨김이다. 노드 컴포넌트를 계속 켜 둬야 재생 시간을 셀 수 있고, 저장/네트워크 복제에서도 같은 오브젝트를 유지할 수 있다.
- `Depleted`는 숨기기 전에 알린다. 표현 컴포넌트가 사라지기 전의 렌더러 경계로 연기 위치를 계산하기 때문이다.
- 이름이 소리 전용이던 `ResourceNodeAudio`를 소리/연기를 모두 맡는 `ResourceNodeFeedback`으로 바꿨다.

## 2026-10-06 근접 좀비 추가

- 공격 클립이 없는 모델은 뼈를 직접 돌리는 절차적 모션으로 해결한다(플레이어 맨손 공격과 같은 방식, LateUpdate에서 Animator 뒤에 덧씌움). 스킨 메시가 한 프레임에 한 번 갱신되므로 같은 평가 안에서 `Camera.Render`로는 포즈를 볼 수 없다. 모션 값은 뼈 방향으로 수치 확인하고, 모양은 포즈를 오래 머물게 한 뒤 실제 게임 화면을 캡처해서 확인한다.
- 스폰 표/컨트롤러가 참조하는 프리팹은 지우고 다시 만들면 GUID가 바뀌어 끊긴다. 새 종류는 새 빌더 메서드로 새 에셋만 만든다.
- 비활성 오브젝트는 `GameObject.Find`로 찾을 수 없다. 테스트에서 껐다 켜야 할 오브젝트는 `FindObjectsByType(FindObjectsInactive.Include, ...)`로 찾는다.

## 2026-10-06 좀비 외형 변형

- 색 변형은 머티리얼 복제 없이 `MaterialPropertyBlock`을 쓴다(머티리얼 인스턴스가 늘지 않는다). 대신 SRP 배처가 렌더러별로 끊기지만 좀비 수(최대 12)에서는 문제없다.
- 크기 변형은 모델 자식만 키우고 캡슐은 비율대로 맞춘다. 루트를 키우면 에이전트/발소리 거리 등 다른 값과 어긋난다. 크기와 능력치는 연결하지 않았다(필요하면 `ZombieAppearance.Scale`을 읽어 체력/피해에 반영한다).
- 시험 함정: 분포 측정을 NavMesh 밖에서 하면 에이전트 생성 경고가 쏟아진다(코드 문제 아님). 측정은 NavMesh 안이나 에이전트를 끈 상태에서 한다.

## 2026-10-06 에셋 이전 준비

- 다른 컴퓨터에서 clone하면 `.gitignore`가 막은 에셋(약 9GB)은 없다. 게임에 필요한 것이 모두 추적되는지 확인하려고 추적 파일의 `guid:` 참조(YAML, UXML의 `guid=`)를 모아, 추적 중인 `.meta`/패키지/내장 GUID에 없는 것을 찾는 스크립트를 썼다. 새 에셋을 채택해 `.gitignore`를 풀 때는 의존하는 머티리얼/텍스처/셰이더/스크립트까지 같이 풀어야 한다(프리팹 의존성은 `AssetDatabase.GetDependencies`로 확인한다).
- 최상위 에셋 폴더의 `.meta`(예: `Assets/Devion Games.meta`)는 추적하지 않는다. 폴더 GUID는 참조되지 않아 다른 컴퓨터에서 새로 만들어져도 문제없다.
- 압축은 `zip` 도구가 없어 파이썬 `zipfile`로 파트 분할했다. 이미 압축된 텍스처/오디오가 대부분이라 압축률은 낮다.

## 2026-10-07 전투 후속 정리

- Play Mode에서 키보드 입력을 주입해 검증하려면 에디터에 포커스가 없을 때 `InputSystem.settings.backgroundBehavior = IgnoreFocus`와 `InputSystem.EnableDevice(키보드)`가 필요하다. 아니면 PlayerInput이 입력을 받지 않아 "막힘" 시험이 가짜로 통과한다. 반드시 같은 입력의 대조군(막지 않는 경우)을 함께 확인한다. 눌림과 뗌은 서로 다른 호출로 나눠 보낸다(같은 프레임에 합쳐지면 눌림이 사라진다). 시험 중 플레이어가 좀비에게 죽으면 `reloading`이 `Update`에서 지워지니 사망 여부도 확인한다.
- 추적하지 않는 에셋이 컴파일을 막으면 지우지 말고 프로젝트 밖으로 옮긴다(`D:\work\ZGV01_assets_moved\`).

## 2026-10-07 건축 격자(Edge/Vertex)

- 벽 길이(2m)와 칸 크기(2m)가 같으면 벽 중심을 칸 중심에 두면 끝이 서로 맞물리지 못한다(직각 모서리가 불가능). 그래서 바닥=칸 중심, 벽=칸 경계, 기둥/이음=꼭짓점으로 격자 위상을 나눴다. 새 조각은 `snapMode`만 정하면 된다(긴 변이 로컬 x 방향이어야 한다).
- 벤더 이음 조각(T/X)은 원점이 이음매라서 경계 중심으로 옮기면 어긋난다. 빌더의 `keepOrigin`으로 x,z를 그대로 둔다.
- 시험 함정: 임시 카메라 렌더로는 원근 때문에 맞물림을 눈으로 판단하기 어렵다. 렌더러 경계를 수치로 비교한다.

## 2026-10-07 쌓기와 지붕

- 고스트 높이를 "맞은 표면의 높이"로 잡으면 건축물 위에 올라타 층이 제멋대로 정해진다. 지면 높이를 건축물을 건너뛰고 구한 뒤 층 번호 * 층 높이를 더하는 방식으로 바꿨다. 층 번호는 플레이어 발 높이(자동) + R/F(수동)로 정한다.
- 지붕은 별도 메시 없이 바닥 조각 재사용이다. 평면 한 장 메시는 뒷면이 안 그려져서 X축 180도 복사본을 같은 평면에 겹친다. 위층 바닥과 지붕을 구분하는 것은 분류와 "1층 이상" 규칙뿐이다.
- 문처럼 이미 만들어진 프리팹을 고칠 때는 지우고 만들지 말고 같은 경로에 `SaveAsPrefabAsset`으로 덮어써 GUID를 유지한다.
- 테스트로 `GameSaveSystem.Save`를 호출하면 실제 세이브 파일(`save_default.json`)이 시험 상태로 덮어써진다. 시험 전에 백업하거나 시험 후 사용자에게 알린다.

## 2026-10-07 좀비와 건축물(NavMesh)

- 런타임에 설치한 건축물은 NavMesh를 바꾸지 않는다. NavMeshAgent는 콜라이더에 막히지 않으므로 건축물마다 carving NavMeshObstacle이 필요하다. 문처럼 열고 닫히는 것은 장애물을 껐다 켠다(완전히 닫혔을 때만 켠다).
- 목표가 닫힌 문 뒤에 있으면 `pathStatus`가 `PathPartial`이 된다. 이 신호로 "문을 열러 간다"를 판단하고, 가장 가까운 문의 자기 쪽 앞 지점으로 SamplePosition해서 이동한다(문 자체는 carve된 영역이라 도착 불가).
- 근접 공격이 벽을 통과하던 문제도 같이 막았다(시야 확인). 새 공격 방식을 만들 때도 벽 너머 판정을 확인한다.

## 2026-10-07 건축물 체력

- 건축물에 `Health`를 붙이지 않는다. `Health`는 총알/주먹/화살이 모두 `GetComponentInParent<Health>()`로 찾고 `ImpactEffects`가 살점 이펙트로 분류하므로 붙이면 플레이어가 자기 기지를 쏴서 부수고 피 이펙트가 난다. 구조물은 `BuildableHealth`로 분리하고 좀비가 명시적으로 `ApplyDamage`를 호출한다.
- 좀비의 "무엇을 부술까"는 목표까지의 경로가 `PathPartial`일 때 정한다: 목표 근처 닫힌 문 우선, 없으면 경로 끝점(`pathEndPosition`) 옆의 가장 가까운 구조물. 길이 끊기는 이유가 건축물이라는 가정이므로 지형 때문에 끊기는 경우 3.5m 안에 건축물이 없으면 아무것도 하지 않는다.

## 2026-10-07 피해 표시와 무너짐 연출

- 건축물 색 변화는 재질을 복제하지 않고 MaterialPropertyBlock으로 재질 슬롯마다 `_BaseColor`를 곱한다. 슬롯별 원래 색을 처음에 저장해 두고 곱한다(문처럼 여러 재질을 가진 모델 때문).
- 파티클을 코드로 만들 때 재질은 Shader.Find 대신 이미 추적 중인 이펙트(탄착 이펙트의 연기 재질)에서 빌려 쓴다. 비활성 상태에서 컴포넌트를 붙여 모듈 설정 뒤에 켜야 자동 재생 설정이 먼저 돌지 않는다.
- 파괴는 "즉시 파괴"가 아니라 `BuildableDebris.Crumble(instance, delay)` 한 길로 모았다(좀비가 부술 때, 받침을 잃을 때, 플레이어가 철거할 때). 지연이 있는 동안에도 부서진 것으로 표시(`IsBroken`)해서 좀비가 다시 대상으로 삼지 않는다.

## 2026-10-07 파편 성능

- 시각 효과가 한 프레임에 몰리면 끊긴다: 오브젝트 풀(미리 채움) + 프레임당 생성 예산 + 전체 상한 세 가지를 같이 쓴다. 풀을 첫 사용 시점이 아니라 그 시스템이 처음 필요해지는 앞 시점(첫 건축물 생성)에 여러 프레임에 걸쳐 채운다.
- 겹쳐서 생성되는 물리 파편은 전용 레이어로 서로의 충돌을 꺼야 한다. 레이어 추가는 `ProjectSettings/TagManager.asset`(이번에 Debris=8)이고 충돌 무시는 코드(`Physics.IgnoreLayerCollision`)에서 한다.
- 측정할 때는 호출한 프레임의 할당(스크립트 컴파일 등 도구 비용)이 섞이지 않게 다음 프레임에 동작을 일으키고, 평상시 기준선(평균 프레임 시간과 프레임당 할당)을 먼저 잰다. 원인이 의심되는 부분은 A/B(예: 장애물 제거, 단순 Destroy)로 분리한다. 이번에는 파편이 아니라 NavMesh carve가 끊김의 주범이었다.

## 2026-10-07 측정 방법의 함정 (에디터 도구 호출 직후 프레임)

- Unity 에디터 도구(MCP eval 등)를 호출한 직후 몇 프레임은 스크립트 컴파일/JIT로 45~100ms씩 튄다. 그 프레임에 게임 동작을 일으켜 재면 동작의 비용으로 오인한다. 파편/NavMesh 성능 측정이 이것 때문에 한 번 틀렸고 `NavMesh carve가 주범`이라는 잘못된 결론까지 갔다.
- 성능 측정 규칙: (1) 동작을 호출 후 수백 프레임 뒤에 실행해 도구 부담이 가라앉은 뒤 잰다. (2) 반드시 **아무 일도 하지 않는 동작(대조군)** 을 같은 방식으로 재서 기준 잡음을 확인한다. (3) 의심 원인은 A/B로 분리하되, 대조군과 같은 조건에서 비교한다. 최적화 효과를 주장하기 전에 최적화 전/후를 같은 조건으로 잰다.
- 측정 결과를 문서에 쓰기 전에 대조군이 없으면 "잠정"이라고 표시한다.

## 2026-10-07 최적화는 측정이 먼저

- 파편 풀/프레임 예산/레이어 같은 최적화는 문제를 (대조군과 함께) 측정으로 확인한 뒤에 넣는다. 이번에는 오염된 측정을 근거로 넣었다가 되돌렸다. 단순한 구현(`CreatePrimitive`로 파편 생성)이 이 규모(건축물 20개 동시 붕괴)에서 충분했다. 풀링을 다시 넣는 기준은 측정된 프레임 끊김이다.


## 2026-10-07 사용자 결정: 카메라와 멀티플레이 시점

- **카메라**: 숄더 카메라 + 우클릭 조준으로 가기로 했다. 확인해 보니 이미 `CombatCameraController`(2026-10-06, 어깨 오프셋 0.75, 조준 시 거리 2/FOV 32)로 구현돼 있어서 새로 만들지 않았다. 남은 일은 사람이 직접 플레이하며 값(오프셋, 거리, FOV)을 다듬는 것이다.
- **멀티플레이(Phase 3)**: 싱글 콘텐츠를 더 다듬은 뒤에 시작한다. 그때까지 스폰 결정 등 도메인 규칙을 서버 권위로 옮길 수 있는 구조를 유지한다(`system.md` 기준).

## 2026-10-07 총구 화염 상시 출력과 조준 고정

- **화염**: WarFX 화염 파티클이 `loop=True`인데 `NotifyFired`에서 `Play`만 하고 멈추지 않아 한 번 쏘면 계속 나왔다. `flashTimer`가 끝날 때 `Stop(StopEmitting)`을 호출한다.
- **조준 고정**: `PlayerInput`이 SendMessages 모드라 버튼 액션은 `performed`(누름)만 메시지로 오고 `canceled`(뗌)는 Value 타입 액션에만 온다(`PlayerInput.cs` `OnActionTriggered`). `OnAim(value.isPressed)`는 해제를 못 받아 `aimHeld`가 true로 굳었다. `WeaponEquipment`가 `Update`에서 `Aim` 액션의 `IsPressed()`를 직접 읽는다. 다른 버튼 액션을 "누르고 있는 동안" 의미로 쓸 때도 같은 함정이 있으니 `On...` 메시지 대신 액션을 폴링한다.
- **빛 무한 깜빡임**: 화염 프리팹의 Light에 벤더 `WFX_LightFlicker`가 붙어 있어 `Start`부터 0.05초마다 `Light.enabled`를 계속 뒤집는다. 장착 시 빛을 꺼 두어도 이 스크립트가 다시 켠다. 외부 에셋을 고치지 않고 `BuildMuzzleFlash`에서 이 컴포넌트를 비활성화하고, 빛은 `NotifyFired`가 0.06초만 켠다. 벤더 이펙트 프리팹을 쓸 때는 파티클뿐 아니라 붙어 있는 스크립트도 확인한다.
- **몸에서 터지는 빛**: 화염 프리팹의 점광원(강도 12.4, 범위 2m)이 몸통에서 0.7m인 총구에 있어 몸통과 손이 통째로 밝아졌다. 총구 앞 0.3m로 내밀고 강도 4, 범위 1.2m로 줄였다(`FlashLightForwardOffset/Intensity/Range`). 값은 수치 기준이며 실제 화면에서 눈으로 맞춰야 한다.
- **화염이 안 보임**: 위의 반복 중지 수정 뒤 한 발에 화염이 거의 안 보였다. WarFX `WFX_MF 4P RIFLE2` 프리팹은 버스트 없이 초당 10개(`rateOverTime`)를 수명 0.05초로 내는 "사격 중 계속 켜 두는" 반복 이펙트라, 0.06초만 재생하면 입자가 평균 0.6개뿐이다. 자동 방출을 끄고 발사마다 `Emit(startLifetime 0.08초, 1개)`로 직접 내보낸다. 벤더 이펙트를 단발로 쓸 때는 방출 방식(버스트/rate)과 수명부터 확인한다.

## 2026-10-07 시작 아이템(StartingItems)

- 테스트 때마다 총과 탄약을 Play Mode에서 직접 넣어야 해서 `StartingItems`(`Start`에서 `Inventory.Add`)를 만들고 `Test_Combat` 플레이어에 붙였다. 목록은 씬에 직렬화돼 있어 에디터에서 바꾼다. 플레이어는 프리팹 인스턴스라 컴포넌트는 프리팹이 아니라 이 씬에만 있다(다른 씬에 영향 없음).
- `Add`를 쓰므로 획득 알림(`ItemPickedUp`)은 뜨지 않는다. 자동 불러오기가 없고 불러오기(F9)는 인벤토리를 덮어쓰므로 세이브와 충돌하지 않는다. 지급하는 총은 장착하지 않는다(장비창에서 장착).
- 씬이 바이너리 직렬화라 git diff로 씬 변경을 검토할 수 없다. 씬을 고친 뒤에는 Play Mode로 결과를 확인한다.

## 2026-10-07 총 손 위치 다듬기

- **원인**: 조준 시 총 기준점이 어깨 중심 + `aimOffset`(0.15, -0.06, 0.40)이라 M16/M82의 왼손 목표(덮개 앞쪽)가 팔 길이(0.573m)를 넘었다. `SolveArm`이 거리를 클램프해 팔이 완전히 펴진(펴짐 1.00) 채 손이 목표에 못 닿고 덮개 끝에 걸쳤다. 왼손 엄지도 곧게 서 있었다.
- **수정**: 왼손 목표(`leftGrip.z`)를 어깨에서 팔 길이의 93% 안으로 당겼다(M16 0.26→0.08, AK 0.20→0.165, M82 0.07→-0.03, MP5 유지). 권총은 총을 앞으로 미는 양(`holdForward`)을 0.20→0.11로 줄였다. 왼손 엄지 접힘은 0.45→0.8(`PlayerArmature.prefab`).
- **방법**: 눈으로만 맞추지 말고 (1) 손 본과 IK 목표의 거리 오차, (2) `거리(어깨, 손) / (위팔+아래팔)`로 팔 펴짐을 잰다. 새 총을 추가할 때는 `leftGrip`을 정한 뒤 오차 0, 펴짐 0.95 이하인지 확인한다. 임시 카메라로 `Camera.Render()` 후 PNG를 저장해 옆/위에서 확대해 본다(조준 상태에서 `Mouse` 상태 이벤트로 우클릭 주입, 장착 직후 한 프레임 뒤에 측정).
- 오른팔 펴짐은 0.32~0.41로 많이 접혀 있다. 총 손잡이 위치가 정해져 있고 총을 앞으로 밀면 왼손이 안 닿아서 그대로 뒀다. 어색해 보이면 `aimOffset`(전 총 공통)을 조정해야 한다.

## 2026-10-07 소총이 흰색으로 보이던 문제

- **원인**: PolyOne Free Gun의 소총 4종(M16A1/AK-47/MP5/M82A1) 프리팹은 FBX에 내장된 기본 재질(`Props_Gun_M`, 텍스처 없음, 흰색)을 쓴다. 권총(FN57)만 정식 재질 `Props_FreeGun_M`(`T_PropsFreeGun_BaseColor`)을 쓴다. 같은 아틀라스 UV라서 정식 재질을 입히면 정상으로 나온다.
- **수정**: 외부 에셋은 고치지 않고 `WeaponDefinition.modelMaterial`(비우면 프리팹 재질 그대로)을 추가했다. `WeaponEquipment.BuildWeaponObjects`가 모델 렌더러에만 적용한다(화염은 제외). 소총 4종 정의에 `Props_FreeGun_M`을 지정했다. 새 총 모델을 가져올 때 흰색이면 같은 방법을 쓴다.

## 2026-10-07 카메라 값 조정

- **문제**: 조준(거리 2, 어깨 0.7)에서 캐릭터의 등/배낭이 화면 왼쪽 1/3을 크게 가리고 총은 팔에 가려 총구 끝만 보였다. 무장 상태는 캐릭터가 왼쪽으로 쏠렸다.
- **조정**: 무장 어깨 0.75→0.85, 높이 0.2→0.3, 거리 3.2→3.4. 조준 어깨 0.7→0.9, 높이 0.15→0.25, 거리 2→2.5, FOV 32→34. 캐릭터가 작아지고 월드가 더 보인다. 값은 `PlayerArmature.prefab`(직렬화)과 `CombatCameraController` 기본값 두 곳에 같이 적는다(코드 기본값만 바꾸면 이미 직렬화된 프리팹 값이 이긴다).
- **확인 방법**: `Camera.main.targetTexture`에 임시 RenderTexture를 걸고 `Render()`해서 PNG로 저장하면 UI 없이 게임 카메라 화면을 비교할 수 있다. 조준은 `Mouse` 상태 이벤트로 우클릭을 주입한다.

## 2026-10-07 오른팔 접힘(조준 시 상체 비틀기)

- **원인**: 조준하면 몸통이 정면을 향하고 총은 어깨 중심 앞 0.40m에 놓여 오른손(손잡이)이 오른쪽 어깨에서 0.2m도 안 떨어졌다. 오른팔 펴짐이 0.32~0.41(팔을 심하게 접음), 왼팔은 0.92~1.00(거의 일자). 총을 앞으로 밀면 왼손이 안 닿아서 오른쪽만 고칠 수 없었다.
- **수정**: 조준할 때 상체(`chest`)를 오른쪽으로 `chestAimYaw`(30도)만큼 튼다. 총 쥔 오른쪽 어깨가 뒤로 빠지고 왼쪽 어깨가 앞으로 나와 오른팔은 펴지고(0.46~0.53) 왼팔은 여유가 생긴다(0.71~0.78). 머리는 같은 각도만큼 반대로 돌려 목표를 계속 보게 한다(`head`). 총 기준점은 캐릭터 방향(`yaw`) 기준이라 상체가 틀어져도 조준선은 그대로다.
- 권총은 오른팔이 0.94로 펴져 `holdForward`를 0.11→0.04로 줄였다(오른 0.84/왼 0.70).
- **함정**: 에디터가 백그라운드일 때는 디스크에서 직접 고친 프리팹/에셋을 읽지 않는다. 파일을 직접 고쳤다면 `AssetDatabase.Refresh`를 부르거나 컴파일을 일으킨 뒤 값을 확인한다. 새 직렬화 필드를 추가한 뒤 기본값을 바꾸면 열려 있는 씬이 이전 기본값을 들고 있을 수 있다. 프리팹에 값을 명시한다.

## 2026-10-07 근접 좀비 공격 타이밍

- **측정**: 걷는 좀비를 플레이어 앞 1.3m에 두고 공격 시작(`Attacked`)부터 프레임별로 손의 앞쪽 뻗음(`Dot(손-루트, forward)`), 피해(`Health.Damaged`) 시각을 쟀다. 기존 곡선은 손이 +0.15초에 가장 멀리 뻗고(0.56m) 피해는 +0.33초에 들어가 팔이 이미 0.42m로 되돌아오는 중이었다(0.18초 어긋남).
- **수정**: 피해 시점(`attackHitDelays` 0.33초 = 곡선 시간 0.55 × 지속 0.6초)은 두고 휘두르기 곡선을 다시 짰다. 머리 위로 들어(0.3, 어깨 120도, 팔꿈치 35도) 앞으로 내려치며 팔이 수평으로 쭉 펴지는 순간(0.55, 어깨 88도, 팔꿈치 0)에 상체를 가장 숙인다. 걷는/달리는 좀비 프리팹과 `ZombieStrikeAnimation` 기본값을 같이 바꿨다. 프리팹에 곡선이 직렬화돼 있어서 코드 기본값만 바꾸면 반영되지 않는다.
- **검증**: 새 프리팹에서 손이 가장 멀리 뻗는 순간 +0.332초(0.62m)와 피해 +0.332초가 일치한다.
- **남은 것**: 좀비 루트에서 목표까지 1.3m일 때 손이 0.6m밖에 안 뻗어 손과 몸 사이에 공간이 남는다. `attackRange`(1.7m)와 피해 확인 거리(`attackRange * 1.3` = 2.2m)가 팔 길이에 비해 크다. 게임 난이도에 영향이 있어 바꾸지 않았다. 변이체(애니메이션 클립)는 이번에 확인하지 않았다.
- **사용자 결정(2026-10-07)**: 근접 좀비 공격 사거리(`attackRange` 1.7m, 피해 확인 `attackRange * 1.3`)는 적당하다고 판단해 바꾸지 않는다. 손과 몸 사이에 남는 공간은 의도된 난이도로 본다.

## 2026-10-07 조준 중 이동 미끄러짐

- **원인**: 컨트롤러는 입력 방향으로 이동시키는데 조준 중 `WeaponEquipment`이 캐릭터를 카메라 방향으로 돌려 놓아, 옆/뒤로 가도 전진 걷기 클립만 재생돼 발이 미끄러졌다(다리 방향 오차 옆 약 90도, 뒤 180도). 이동 클립은 Idle/Walk/Run뿐이다.
- **수정**: (1) 조준 중 루트 방향을 `카메라 yaw + 이동 입력 각도`(±`legTurnLimit` 70도)로 직접 정한다. 기본 컨트롤러도 매 프레임 루트를 돌리므로 보정(Slerp)으로는 줄다리기가 돼 77도/63도 같은 어정쩡한 값에 걸렸다. 조준 중에는 `rootYaw`를 직접 쥐고 대입한다. (2) 다리가 조준 방향에서 벗어난 만큼 척추(40%)와 가슴(60%)을 반대로 비틀어 상체/총/손은 카메라 yaw 기준으로 둔다(`yaw`는 루트가 아니라 카메라 기준). (3) 뒤로 갈 때(입력 110도 초과, 70도 미만에서 해제하는 히스테리시스)는 다리를 정면에 두고 걷기를 거꾸로 재생한다.
- **함정**: `Animator.speed`는 음수가 안 된다(경고 후 0으로 처리돼 다리가 얼어붙는다). 거꾸로 재생은 이동 상태의 속도 배수가 `MotionSpeed` 파라미터인 점을 이용해 그 값을 음수로 덮는다. 컨트롤러가 매 프레임 `MotionSpeed`를 쓰므로 `AimLegReverse`(실행 순서 100)가 그 뒤 `Update`에서 덮는다. 실행 순서를 `WeaponEquipment`에 걸면 `LateUpdate` 순서(카메라, IK)가 바뀌므로 별도 컴포넌트로 뒀다.
- **검증**: 에디터가 백그라운드이면 플레이 루프가 멈춘다(프레임 2에서 정지). 포커스를 주거나 `set_autotick`을 확인한다. 조준 입력 주입이 불안정해서 `lastFireTime`을 매 프레임 갱신해 조준 상태(IsAiming)를 유지하고 `StarterAssetsInputs.move`를 시간표대로 바꿨다. 결과(평균): 앞 다리오차 0도, 오른쪽/왼쪽 20도, 뒤 0도(거꾸로 재생), 총-카메라 오차 2도.

## 2026-10-07 손가락 방향과 손목 롤

- **문제**: 플레이 카메라에서 왼손이 핸드가드를 쥐지 못하고 아래에서 주먹을 쥔 채 손가락만 반대편으로 곧게 튀어나와 보였다. 손가락이 총신 방향(+Z)으로 향한 채 접히면 총신을 감싸는 게 아니라 주먹이 된다. 핸드가드를 감싸려면 손가락이 총신을 가로질러야 한다.
- **방법**: `leftFingerDirection`/`leftHandRoll` 조합을 훅에서 프레임마다 바꾸며 PNG를 저장해(스윕) 비교했다. 롤 0/90/180/270 중 90도가 아래에서 위로 감싸는 C자 그립이었다(180은 손이 아래로 접힘, 270은 주먹). 오른손도 같은 방식으로 비교했고 0도가 가장 자연스러웠다(90/270은 손이 꺾임).
- **적용**: `leftFingerDirection` (0.2,0,1)→(1,0.2,0.1), `leftHandRoll` 0→90(`PlayerArmature.prefab`과 코드 기본값). 손목 목표가 `palmOffset * 손가락 방향`만큼 이동하므로 5종 모두에서 손 위치 오차와 팔 펴짐을 다시 쟀다: 오차 0, 왼팔 0.74~0.82.
- **한계**: 권총은 왼손이 오른손 위로 겹쳐 보인다(받쳐 쥔 형태). 총마다 다른 손가락 방향이 필요하면 `WeaponDefinition`에 왼손 방향/롤을 추가해야 한다. 오른손 방아쇠 손가락은 안쪽에서 보이지 않아 확인하지 못했다.

## 2026-10-09 EditMode 테스트 어셈블리 도입

- **배경**: 게임 코드가 전부 `Assembly-CSharp`라서 테스트 어셈블리가 참조할 수 없다. 전체를 asmdef로 쪼개는 건 범위가 크다.
- **결정**: 의존성 없는 순수 계산만 `Assets/_Project/Scripts/Core`(`ZGV.Core`, 자동 참조)로 옮기고 `Tests/EditMode`(`ZGV.Tests.EditMode`)에서 참조한다. 첫 대상은 `SurvivalMeter`(.meta 이동으로 GUID 유지). `Assembly-CSharp`는 `ZGV.Core`를 자동 참조하므로 기존 코드는 바뀌지 않는다.
- **How to apply**: 새 순수 규칙(유니티 컴포넌트 비의존)은 `Scripts/Core`에 두고 EditMode 테스트를 붙인다. MonoBehaviour 의존 코드는 PlayMode/Test 씬으로 검증한다.

## 2026-10-09 Health·SurvivalStats Core 이전과 테스트

- `Health`, `SurvivalStats`, `SurvivalStatsConfig`, `ConsumableDefinition`을 `Scripts/Core`(`ZGV.Core`)로 옮겼다(.meta 함께 이동, GUID 유지).
- **리팩터링 1건**: `SurvivalStats.Update`의 본문을 `public void Tick(float dt)`로 추출하고 `Update`는 `Tick(Time.deltaTime)`만 부른다. 동작은 같고, EditMode에서 `Time.deltaTime`을 제어할 수 없어 시간을 직접 넣기 위한 것이다. `ApplyEmptyPenalty`도 dt를 인자로 받는다.
- `HealthTests`(8), `SurvivalStatsTests`(15)를 추가했다(총 47개). 공통 도우미는 `TestUtil`(Awake 호출, 직렬화 필드 설정).
- **How to apply**: 시간에 의존하는 규칙은 `Time.deltaTime`을 직접 읽지 말고 dt를 받는 메서드로 빼 두면 EditMode로 검증할 수 있다.

## 2026-10-09 저장/불러오기 검증과 깨진 세이브 처리

- `SaveData.cs`(순수 데이터)를 `Scripts/Core`로 옮기고 `SaveDataTests`(8)를 추가했다. 2026-09-23에 실제로 저장된 세이브를 픽스처로 써서 옛 세이브 하위 호환(생존 없음, 무기 없음, 문 닫힘, 내구도 음수=가득, 시각 음수=변경 안 함)을 고정했다.
- **Play Mode 왕복 검증**: 위치/회전/체력/허기·갈증/인벤토리 16칸, 건축물(문 열림·방향·내구도·월드 ID·위치)이 저장→변경→불러오기 후 모두 일치. 옛 세이브(2칸 인벤토리, 건축물 3개)도 정상 로드되고 허기는 건드리지 않는다.
- **버그 수정**: `JsonUtility.FromJson`은 깨진 JSON에서 null이 아니라 `ArgumentException`을 던진다. 그래서 `Load`의 `data == null` 분기가 도달 불가였고 깨진 세이브가 예외로 터졌다. `ArgumentException`을 잡아 오류 로그 + `false` 반환으로 바꿨다. 파싱이 상태 변경보다 먼저라 게임 상태는 변하지 않는다(확인함).
- **함정**: 검증 중 사용자의 `save_default.json`을 덮어쓰게 되므로 먼저 백업하고 끝나면 `cmp`로 복원을 확인했다. 세이브 경로: `%LocalAppData%Low/DefaultCompany/ZGV01/`.
- 남은 것: `GameSaveSystem` 자체(경로/컴포넌트 의존)는 EditMode 테스트가 없다. 저장 직렬화를 순수 함수로 빼면 가능하다.

## 2026-10-09 .gitattributes 도입(줄바꿈 통일)

- **원인**: 시스템 Git이 `core.autocrlf=true`이고 `.gitattributes`가 없어, 저장소는 LF인데 작업본은 CRLF로 체크아웃됐다(추적 파일 약 2490개). Unity는 YAML/.meta를 LF로 쓰므로 `ProjectSettings` 등이 내용 변경 없이 `M`으로 계속 표시됐다(해시는 동일).
- **적용**: `* text=auto eol=lf`로 텍스트는 작업본도 LF로 통일하고, png/jpg/tga/exr/wav/ogg/mp3/pdf는 `binary`로 둔다. FBX는 ASCII/바이너리가 섞여 있어 명시하지 않고 자동 판별에 맡긴다(`binary`로 지정하면 기존 ASCII FBX blob이 바뀐다).
- **재정규화 범위**: `Assets/Survivalist/StarterAssets`의 CRLF 파일 11개가 LF로 바뀐다(줄바꿈만, 내용 변경 없음 확인).
- **How to apply**: 다른 컴퓨터는 작업본에 변경이 없으면 `git pull`만 하면 된다. 가짜 `M`이 남으면 `git add --renormalize .`로 정리한다(내용이 같은 파일의 인덱스만 갱신되며 작업본은 바뀌지 않는다). `git reset --hard`나 `git rm --cached -r .` 같은 작업본을 덮어쓰는 명령은 미커밋 변경을 잃을 수 있어 쓰지 않는다.

## 2026-10-09 요리·정수 범위와 방식 결정

- **사용자 결정**: 요리·정수·사냥 중 요리+정수만 먼저 한다. 방식은 모닥불 건축물을 설치하고 그 근처에서 제작창으로 조리·정수한다.
- **이유**: 사냥은 동물 에셋·AI가 필요하고 추적 중인 동물 에셋이 없다(Animal pack은 추적 제외 예제). 모닥불 근처 제작은 기존 제작·건축·소비 시스템 위에서 새 UI 없이 가능하고 거점 루프와 맞는다. `DESIGN.md` 5.3은 추가 생존 요소를 검증 후 넣도록 하므로 새 수치는 만들지 않고 기존 허기·갈증 회복 경로만 넓힌다.
- **설계 메모**: 현재 물통은 깨끗한 물병을 바로 주므로 정수에는 오염된 물 공급원(웅덩이)이 필요하다. `Heal`이 음수를 무시하므로 오염 물의 체력 감소는 `TryConsume`에서 처리한다.

## 2026-10-09 요리·정수 구현 결과

- **구조**: `CraftingStation`(종류, 반경)이 활성 목록을 정적으로 들고 `IsNear(type, position)`으로 판정한다. `RecipeDefinition.requiredStation`이 None이 아니면 `CraftingSystem.CanCraft`가 플레이어 위치와 작업대 거리를 본다. 규칙은 모두 `ZGV.Core`이고 EditMode 테스트가 있다(전체 67개).
- **데이터**: `ZGV01/Survival/Build Cooking And Purification`(`CookingAssetBuilder`)이 아이템 3종, 레시피 3종, 모닥불, 웅덩이를 만들고 DB/플레이어 프리팹/열린 씬의 HUD·건축 목록에 등록한다. 여러 번 실행해도 기존 프리팹은 다시 만들지 않는다(GUID 유지). 프리팹 모양을 바꿀 때는 `LoadPrefabContents`로 그 자리에서 고친다.
- **함정 1(백업)**: 저장/불러오기 검증은 사용자의 `save_default.json`을 덮어쓴다. 먼저 백업하고 끝나면 복원한다.
- **함정 2(렌더 확인)**: 고갈한 웅덩이는 숨겨져 렌더에 안 나온다. 외형 확인은 고갈되지 않은 노드로 한다. 파티클은 에디터가 포커스를 가져야 프레임이 흐른다(`editor_focus`).
- **함정 3(건축물 저장)**: 모닥불은 일반 건축물이라 세이브 기존 경로(`BuildableInstance`)로 저장된다. 불러온 뒤 `OnEnable`로 작업대가 다시 등록되는 것을 확인했다.
- **밸런스 메모**: 열매 12→구운 열매 20, 통조림 35→데운 통조림 50(+체력 5), 더러운 물은 갈증 15에 체력 -6, 정수는 더러운 물 2 + 나무 1 → 물병 2. 연료로 나무 1을 쓴다. 플레이해 보고 조절한다.

## 2026-10-09 사냥 범위와 동물 에셋 결정

- **사용자 결정**: 다음 콘텐츠는 사냥. `Animal pack deluxe v2`를 동물 에셋으로 쓴다.
- **에셋 조사**: 팩은 2.2GB, 동물 약 48종(프리팹), 대부분 Idle/Walk/Run/Eat/Die 클립이 있고 일부는 Attack이 있다. 컨트롤러에는 파라미터가 없고 상태 이름(`idle A`, `walk`, `run`, `eat`, `die`)만 있어서 `CrossFade(상태 이름)`으로 구동해야 한다. 휴머노이드가 아니라 Generic 리그다.
- **채택 3종과 용량**: 돼지 31.8MB, 양 54.0MB, 롱혼 소 54.6MB(합 약 140MB, 대부분 TGA 텍스처). 팩 전체를 추적하면 2.2GB라 의존 파일만 허용한다.
- **설계 근거**: 좀비 스폰·전리품 패턴(`ZombieSpawner`, `ZombieLootDropper`, `LootPickup`)을 재사용하되 좀비 전용 코드(시야 가림 검사 등)는 끌어오지 않는다. 판정(`Health`, 콜라이더)은 기존 시스템이 그대로 처리한다. 동물은 반격하지 않아 위험 요소를 늘리지 않는다.

## 2026-10-09 사냥 구현 결과

- **구조**: `AnimalBrain`(순수, `PreyState` Idle/Wander/Flee/Dead)이 시간과 위협 거리만으로 전이를 정하고, `AnimalAI`가 NavMeshAgent 이동과 `Animator.CrossFadeInFixedTime(상태 이름)`을 맡는다. `AnimalDefinition`(데이터)이 속도·경계 거리·고기·상태 이름을 갖는다. 전이 규칙 테스트 24개.
- **이름 충돌**: `SurvivalEngine`(벤더)에 전역 `AnimalState`가 있어 처음 이름을 `PreyState`로 바꿨다. 새 전역 타입 이름은 벤더 에셋과 겹치는지 먼저 확인한다(컴파일 오류가 `CS0117`/`CS0266`로 나오고 두 정의의 출처가 메시지에 찍힌다).
- **애니메이터**: 벤더 컨트롤러에 파라미터가 없고 상태 이름(`idle A`, `idle v1`, `walk`, `run`, `eat`, `die`)이 모델마다 다르다. 빌더가 컨트롤러에서 키워드로 상태 이름을 찾아 정의에 적는다.
- **판정**: 래퍼 프리팹 루트에 `BoxCollider`+`Health`+`ImpactSurfaceMarker(SoftBody)`를 둬서 기존 사격(`GetComponentInParent<Health>`)과 맨손이 그대로 먹힌다. 사망하면 collider와 agent를 끄고 시체를 90초 뒤 치운다.
- **에셋 추적**: `.gitignore`에 동물 3종 의존 파일 44개만 허용했다(자동 생성). 새 동물을 더하려면 의존 파일 목록(`AssetDatabase.GetDependencies`)으로 같은 방식의 규칙을 만든다. 폴더 규칙은 경로 인덱스를 `parts[2]`부터 쓴다(처음 `parts[3]`으로 해서 폴더 규칙이 빠졌었다).
- **검증 팁**: 플레이어를 동물 근처로 옮길 때 `CharacterController`를 껐다 켠다. `NavMeshAgent.Warp`로 동물을 옮긴다.

## 2026-10-09 동물 소리

- **구성**: `AnimalAudioSet`(데이터: 평소 울음/경계/피격/사망/발소리 `SoundGroup`, 보폭, 울음 간격)과 `AnimalAudio`(3D `AudioSource` 두 개: 목소리와 발소리). `ZombieAudio`/`ZombieAudioSet` 구조를 그대로 따랐다. `AnimalAI`가 `StateChanged` 이벤트를 내보내고 `AnimalAudio`가 구독한다.
- **규칙**: 도망치기 시작하면 경계 소리, 맞으면 피격 소리(쿨다운 0.5초), 죽으면 사망 소리를 낸다. 방금 맞았으면 0.6초 동안 경계 소리를 생략해 겹치지 않게 한다. 비명은 재생 중인 소리를 끊고 낸다. 평소 울음은 대기/배회 중이고 다른 소리가 없을 때만 낸다. 발소리는 평면 이동 거리가 보폭을 넘을 때마다 한 번이다(제자리에서는 나지 않는다).
- **소리 매핑**: 돼지(Pig Snorts/Squeals/Footsteps), 양(Sheep baas/footsteps, 피치로 경계 1.1~1.3·피격 1.2~1.4·사망 0.7~0.85을 구분), 소(Cow moos 평소, Bull Moo 경계/피격/사망, Large Animal Footsteps 발소리). 음량·피치·간격은 임시 값이다.
- **검증 방법**: 소리를 직접 들을 수 없어서 `EditorApplication.update`에서 `AudioSource.isPlaying`의 상승 에지를 기록해(결과는 `AppDomain.SetData`로 다음 eval에 전달) 어떤 상황에서 어떤 피치로 재생되는지 확인했다.
- **함정**: `PlayOneShot`은 `source.clip`을 채우지 않으므로 재생 중인 클립 이름은 읽을 수 없다. 피치는 소스 전체에 걸리므로 재생 중인 소리를 끊을 때 `Stop()` 후 피치를 바꾼다.

## 2026-10-09 육식 맹수 범위 결정

- **사용자 결정**: 동물 확장 중 반격은 육식 맹수에만 적용한다(초식은 도주 유지). 좀비 애니메이션은 패스.
- **채택**: 표범과 호랑이. 팩에는 개과(워허운드, 코르소 등)도 있지만 "맹수"에 맞는 대형 고양잇과 2종으로 시작한다. 두 모델 모두 `attack A/B`, `roar` 상태가 있다.
- **설계 근거**: 초식의 `AnimalBrain`에 추격/공격을 섞으면 규칙이 흐려져서 `PredatorBrain`을 따로 둔다. 모델/속도/고기/상태 이름은 `AnimalDefinition`을 상속해 재사용한다. 피해는 좀비처럼 `Health.ApplyDamage`로 직접 준다.

## 2026-10-09 육식 맹수 구현 결과와 발견한 문제

- **구조**: `PredatorBrain`(순수, Idle/Wander/Chase/Attack/Dead + `AttackLanded` 이벤트)과 `PredatorDefinition : AnimalDefinition`(속도·고기·상태 이름 재사용 + 감지/공격 수치), `PredatorAI`(NavMeshAgent 추격, 공격 타격 순간에 사거리(여유 1.4배) 안이면 `Health.ApplyDamage`). 소리는 `IAnimalVoiceSource`(Alarmed/Attacked/Motion/CanCallIdly)로 `AnimalAudio`를 초식·맹수가 공유한다. 맹수의 포효는 "경계" 묶음, 공격은 `attack` 묶음이다.
- **중요한 발견(벤더 컨트롤러)**: `Animal pack deluxe v2`의 애니메이터 컨트롤러에는 조건 없는 자동 전환이 있다(예: `walk`가 83%에서 `idle A`로, 표범 `run`이 40%에서 `die`로, 양 `run`이 42%에서 `walk`로). `CrossFade`로 지정한 동작이 곧 바뀌어서 상태와 애니메이션이 어긋났다(초식 동물도 같은 문제였다). 해결: 같은 상태·클립을 가지고 전환이 없는 프로젝트 컨트롤러(`Data/Animals/Controllers/Anim_*.controller`)를 만들어 래퍼 프리팹의 애니메이터에 덮어씌운다(`UseOwnController`). 새 동물을 추가할 때 같은 방식을 쓴다. 확인 방법: 모든 동물의 (상태, 재생 중 애니메이션) 쌍의 누적 시간표를 만들면 어긋남이 한눈에 보인다.
- **밸런스 메모**: 표범(체력 60, 달리기 5.8, 피해 12, 쿨다운 2.2, 감지 15), 호랑이(체력 120, 달리기 5.0, 피해 18, 쿨다운 2.0, 감지 18). 플레이어 전력 질주가 약 5.3이라 표범은 따돌릴 수 없고 호랑이도 거의 못 따돌린다(포기는 40m 밖에서 8초 연속이라 맹수를 앞질러야 한다). 서 있기만 하면 몇 방에 죽는다. 난이도는 플레이해 보고 조절한다.
- **시험 팁**: 시험 중 플레이어가 죽지 않게 `EditorApplication.update`에서 체력을 유지시킨다. 텔레포트할 때 바닥이 없는 곳으로 보내면 플레이어가 추락해 `CharacterController.Move called on inactive controller` 오류가 수백 개 쌓인다(시험 도구 탓, 게임 오류 아님). 게임 시간이 평가 호출보다 느리게 흘러서 몇 초 기다리려면 `Start-Sleep`을 쓴다.

## 2026-10-09 가죽과 저장 결정, 맹수 확장 보류

- **사용자 결정**: 맹수의 무리·매복·야간 전용·건축물 공격은 나중으로 미룬다. 가죽과 저장을 한다.
- **가죽 쓰임**: 장비 시스템이 없어 방어구는 만들 수 없다. 응급 치료품 붕대(체력 +35)로 쓰임을 준다(가죽 2 → 붕대 1). 임시 밸런스.
- **저장 해석**: "저장"을 동물·맹수 저장으로 해석했다(좀비와 함께 지금까지 유일하게 저장하지 않던 살아 있는 개체). 행동 상태는 저장하지 않고 불러온 동물은 대기에서 시작한다.

## 2026-10-09 가죽과 동물 저장 구현 결과

- **가죽**: `AnimalDefinition`에 `hideItem/hideMin/hideMax`와 `RollHide`를 더했다. 드랍은 `AnimalLoot.Drop`이 고기와 가죽을 각각 `LootPickup`으로 ±0.25m 떨어뜨려 놓는다(초식 `AnimalAI`와 맹수 `PredatorAI`가 공유, 각자 있던 `DropMeat`는 제거). 쓰임은 붕대(가죽 2 → 붕대 1, 체력 +35, 작업대 없음)다. `ItemSpec.resource`(일반 자원)와 `RecipeSpec.anywhere`(작업대 없음) 옵션을 빌더에 더했다.
- **저장**: 살아 있는 동물과 맹수의 종류 id·위치·방향·체력을 `GameSaveData.animals`에 담고 `hasAnimals`로 옛 세이브와 구분한다(false면 지금 있는 동물을 그대로 둔다, true면 시체까지 치우고 목록대로 다시 만든다). 프리팹은 `AnimalDatabase`(Resources, `AnimalAssetBuilder`가 채움)에서 id로 찾고, 체력은 이벤트 없이 `Health.RestoreState`로 복원해 피격 도주가 일어나지 않게 한다. 불러온 동물은 `AnimalSpawner.Owns`로 맡은 스포너를 찾아 `Adopt`해서 살아 있는 수에 센다(안 하면 최대 수를 넘겨 더 만든다).
- **저장하지 않는 것**: 행동 상태(불러오면 대기), 시체, 떨어진 전리품, 좀비.
- **관찰**: 불러온 직후 스포너의 보충 타이머가 만료돼 있으면 최대 수 미만일 때 곧바로 한 마리가 새로 생길 수 있다(정상).

## 2026-10-09 창문 변형과 고스트 올라타기 재확인

- **고스트 올라타기**: 처음에 "남은 문제"라고 봤지만 이미 해결돼 있었다(`FindGroundHeight`가 건축물·플레이어 콜라이더를 건너뛰고 층 높이는 층 번호 × `levelHeight`로 정한다). 낡은 후속 항목 두 줄만 갱신되지 않았다. 후속 목록을 쓸 때는 완료된 뒤 같은 항목을 닫았는지 확인한다.
- **창문**: 벽 모델(2m/4m 폭, 높이 2m, 원점은 폭의 가운데·바닥)을 세로로(그리고 기둥은 가로로도) 줄여 쌓고 사이를 비운 구조다. `WindowSpec`(벽, 폭, 아랫단 높이, 윗단 시작 높이, 기둥 폭)으로 변형을 추가한다. 개구부 치수는 콜라이더 경계로 확인한다.
- **격자**: 넓은 창(4m)은 긴 벽(`wall_long`)처럼 `Vertex` 스냅이라 홀수 좌표(예: 1,3)에 놓이고 두 칸의 가장자리에 걸친다. 좁은 창은 `Edge`.
- **시험 팁**: 시험 중 플레이어가 죽으면 건축 상태가 `Dead`가 돼 설치가 막힌다. 이동 직후에는 고스트가 한 프레임 뒤에 갱신되므로 `TryPlace`는 다음 호출에서 한다. 사용자 세이브는 백업했다가 복원했다.

## 2026-10-09 장비 시스템과 건축 추가 범위 결정

- **사용자 결정**: 건축 추가 4항목(창살·여닫이 창, 지붕 변형, 문 종류·격자 정리, 방어 건축)과 장비 시스템(방어구 머리·몸통·다리 3칸, 피해 감소만, 외형 없음)을 한다.
- **조사**: 아치 문은 이미 `Edge` 스냅이라 "격자 정리"는 확인만 한다. Kats에 철문 문짝(`Prop.Gate.Iron.L/R`)과 쇠창살 문(`Prop.Gate.Portcullis`)이 있다. 지붕은 평평한 슬래브뿐이라 기울여서 변형을 만든다.
- **장비 설계 근거**: 플레이어가 받는 피해는 모두 `Health.ApplyDamage`로 들어온다(좀비 `ApplyDamage(float)`, 맹수 `ApplyDamage(amount, dir, headshot)`, 화살). 감소율을 `Health`에 두면 한 곳에서 처리된다. 굶주림·오염(`SurvivalStats`)은 방어구가 막아 주면 이상하므로 `ApplyTrueDamage`로 분리한다.

## 2026-10-10 건축 추가 구현 결과

- **구조**: 모두 `BuildableAssetBuilder`에서 만든다. 창살/덧창은 `WindowSpec.infill`(None/Bars/Shutter), 문 종류는 `DoorVariant`(철문은 아치+쇠창살 양문, 나무 문은 아치+`Prop.Door.Static` 외문짝), 지붕은 `BuildRoofPrefab`(경사/박공), 방어 건축은 큐브로 조립(바리케이드는 통 박스 하나, 철책은 창살마다 박스).
- **함정**: 창살 문짝(`Prop.Gate.Iron`)은 가운데에 가로 살이 있어 개구부 중간 높이에서는 틈이 없다(레이 시험 때 y를 여러 개 재야 한다). `BuildableNavBlocker`는 문이 아닌 조각의 콜라이더마다 장애물을 만들어서 철책은 11개, 창살 창은 4개가 생긴다. 문제가 되면 철책 하나로 합친다.
- **한계(후속)**: 박공 지붕 양끝은 열려 있다(삼각 마감 없음). 경사 지붕 박스 콜라이더가 기울인 모서리에서 0.05m쯤 삐져나온다. 여닫이 창/문은 `BuildableDoor`를 그대로 써서 열고 닫을 때 좀비 길 막힘 규칙도 문과 같다.
- **시험 팁**: 건축 시험은 `BuildingSystem`의 `ghost`/`ghostLevel` 비공개 필드를 설정하고 `GetPlacementStatus`/`TryPlace`를 리플렉션으로 호출하면 조준 없이 된다(같은 프레임 안에서 끝내야 고스트가 덮어써지지 않는다). 세이브는 백업했다가 복원했다.

## 2026-10-10 보관함과 상호작용 UX 구현 결과

- **조사에서 낡은 항목 발견**: 체크리스트의 "제작 재료 부족 알림 없음"과 "사다리 프롬프트 없음"은 이미 구현돼 있었다(`HudController.Craft`, `InteractionFocus`의 사다리 대상). 후속 목록을 쓸 때 구현이 먼저 들어간 것을 닫지 않는 문제가 또 있었다. 새 후속을 적기 전에 코드를 확인한다.
- **보관함 구조**: 용량 규칙은 `Inventory.MaxSlots`/`MaxAddable`과 `StorageTransfer.Move`(순수, `ZGV.Core`)에 두고, `StorageContainer`는 건축물에 붙어 자기 `Inventory`와 칸 수, 저장 복원, 파괴 시 드랍을 맡는다. `Inventory.Add`는 상한을 검사하지 않으므로(플레이어는 무제한) 보관함에 넣을 때는 반드시 `StorageTransfer`를 거친다. 서버 권위로 옮길 때 요청 지점은 `StorageTransfer.Move` 한 곳이다.
- **파괴 시 드랍**: 부서짐, 받침 붕괴, 철거가 모두 `BuildableDebris.Crumble`을 지나므로 거기서 `DropContents`를 부른다(여러 번 불려도 한 번만 떨어진다). 가구는 내구도가 없어 좀비가 부수지는 못하고 철거나 받침 붕괴로만 사라진다.
- **HUD 갱신 변경**: 플레이어 인벤토리 패널 갱신을 `ItemAdded`에서 `Modified`로 바꿨다(보관함으로 옮겨 줄어드는 경우를 잡으려고).
- **길게 누르기 링**: `Interact`는 Hold(기본 0.4초) 입력이라 `InteractionFocus`가 `IsPressed`로 시간을 따로 잰다. 대상이 있을 때 누르기 시작한 경우만 세고, 완료되면 놓을 때까지 0으로 둔다(사다리를 잡은 직후 링이 다시 차오르지 않게).
- **발견한 버그**: `LadderClimber.TryGrab`이 지면 높이에서 `climbHeight=0`으로 시작해 첫 프레임에 "맨 아래까지 내려옴"으로 놓아졌다(플레이어 y가 -0.01이면 재현). 시작 높이를 최소 0.1m로 고쳤다.
- **시험 팁**: Play Mode에서 `Keyboard.current`에 `InputSystem.QueueStateEvent(kb, new KeyboardState(Key.E))`로 실제 키 입력을 흉내 낼 수 있다. 눌린 동안 여러 번 `eval`을 호출해 진행을 읽고 빈 `KeyboardState()`를 보내 뗀다. 패널의 칸 클릭은 `ClickEvent.GetPooled()`에 `target`을 지정해 `SendEvent`하면 `RegisterCallback<ClickEvent>` 핸들러가 불린다(Shift 여부는 이 방법으로 못 보내서 1개 이동은 `MoveStorageItem` 직접 호출로 봤다).

## 2026-10-10 사망 후 부활 흐름 결정과 발견

- **기본 규칙(사용자 결정 전 임시)**: `DESIGN.md`에 플레이어 사망 규칙이 없어서 가볍게 시작했다. 인벤토리/장비를 잃지 않고, 시작 위치에서 체력 100%, 허기·갈증 50%로 부활한다. 패널티(시체 가방, 아이템 드랍)와 리스폰 지점(침대 등)은 나중에 `PlayerRespawn`과 `Health.Revive` 위에 얹는다.
- **발견**: 플레이어는 죽어도 이동 입력이 막히지 않았다(스타터 에셋 `ThirdPersonController`는 `Health`를 모른다). `PlayerRespawn`이 사망 때 컴포넌트를 끄고 부활 때 켠다. 이동 외에 `IsDead`를 보는 곳(사격, 맨손 공격, 건축, 상호작용, 사다리)은 `Revive`로 자동으로 풀린다.
- **쓰러짐 연출**: 모델 루트(`Geometry`, `Skeleton`) 둘을 같은 각도(-85°)로 기울인다. 애니메이터가 이 둘 위의 루트는 건드리지 않아 유지된다. 카메라 루트는 건드리지 않아서 시점은 서 있던 높이에 남고, 쓰러진 모델은 화면 아래로 벗어난다(캡처로 확인). 죽는 순간 카메라 연출은 후속.
- **`Health.RestoreState`는 죽은 개체를 되살리지 않는다**(저장 불러오기용이라 의도). 죽은 채 F9를 누르면 `SaveLoadDebugController`가 먼저 `ReviveInPlace`를 부른다. 저장 파일이 없으면 되살리지 않는다(`GameSaveSystem.HasSave`).
- **시험 팁**: 게임 시간이 평가 호출보다 느리게 흘러서 부활 지연(2.5초)을 기다릴 때는 `Start-Sleep`을 쓴다. Enter는 `KeyboardState(Key.Enter)`로 보낸다.

## 2026-10-10 침대 부활 지점과 시체 가방 결정과 구현 결과

- **사용자 결정**: 침대에 상호작용하면 부활 위치로 지정, 침대가 부서지면 새 침대를 설치하고 상호작용하기 전까지 시작 위치에서 부활, 사망 패널티는 시체 가방으로 확정(이전의 "아이템 유지" 임시 규칙을 대체).
- **침대**: `RespawnPoint`(침대 프리팹에 부착)는 `Removed` 이벤트를 `BuildableDebris.Crumble`에서만 낸다(부서짐, 받침 붕괴, 철거가 모두 지나는 한 곳). 저장 불러오기가 건축물을 `Destroy`로 지울 때는 알리지 않아서 불러오기가 "침대가 사라졌다" 알림을 만들지 않는다. 마지막으로 상호작용한 침대 하나만 지정된다(여러 개 선택 UI 없음). 부활 위치는 침대 콜라이더 경계에서 앞(+Z) 0.7m다.
- **불러오기 함정**: 불러오기는 옛 건축물을 `Destroy`(프레임 끝에 삭제)하고 같은 프레임에 새로 만들어서 같은 `worldId`의 `RespawnPoint`가 활성 목록에 둘 있다. 지정 침대를 다시 찾을 때는 나중에 켜진(새로 만든) 쪽을 골라야 한다(`FindRespawnPoint`).
- **시체 가방**: 새 구성요소 없이 `StorageContainer`를 확장했다(이름, 칸 제한 없음=`slotCount` 0, 비면 사라짐). `corpse_bag`은 건축 DB에는 있지만 플레이어의 건축 메뉴 목록에는 없다(저장/불러오기가 id로 프리팹을 찾기 때문에 DB 등록이 필요하다). 죽는 순간 `StorageTransfer.MoveAll`로 인벤토리 전부를 옮기므로 방어구는 `Equipment`가 자동으로 풀고, 무기는 자동으로 풀리지 않아 `WeaponEquipment.Unequip`을 먼저 부른다. 가방은 시간이 지나도 사라지지 않는다.
- **발견한 버그와 수정**: Unity 객체는 파괴되면 `== null`이 참이라, 열려 있던 보관함이 파괴되면(`openStorage == null`) HUD의 닫기 검사가 첫 줄에서 빠져나가 패널과 커서 해제가 남았다. `storageIsOpen` 플래그를 따로 두어 해결했다. 같은 함정이 다른 곳에도 있을 수 있으니 "파괴될 수 있는 참조를 null로 열림 여부에 쓰지 않는다"를 기억한다.
- **죽은 채 F9**: 불러오기가 건축물을 저장 시점으로 되돌리므로 죽은 사이 생긴 가방은 사라지고 인벤토리도 저장 시점 것이라 아이템이 중복되지 않는다. 대신 저장 이후에 얻은 것은 가방째 사라진다(저장/불러오기의 원래 성질).
- **후속 위험**: 아이템을 모두 잃고 부활하면 맨손이다(스타팅 아이템 지급은 게임 시작 때만). 가방까지 가다 다시 죽으면 가방이 겹쳐 생긴다. 가방 위치 표시가 없어 찾기 어렵다. 다음 작업 후보로 가방 위치 표시(나침반)와 가방 시간 제한을 고려한다.
- **시험 팁**: UI Toolkit `Button`은 `ClickEvent`를 `SendEvent`해도 `clicked`가 불리지 않는다(포인터 이벤트를 기다린다). 버튼 동작은 핸들러 메서드를 리플렉션으로 직접 불러 확인한다.

## 2026-10-10 시체 가방 수명, 단일 가방, 스타팅 아이템 결정

- **사용자 결정**: 가방은 현실 시간 5~10분 안에 못 찾으면 사라짐, 스타팅 아이템은 테스트용이라 실제 게임은 아이템 없이 시작, 가방은 마지막 사망 기준으로 1개만 남김.
- **해석(임시, 조절 가능)**: ① 5~10분은 상한인 **10분**(`corpse_bag` 프리팹의 `lifetimeSeconds` 600)으로 했다. 값을 바꾸려면 그 프리팹의 필드를 고친다(`StorageContainer`는 0이면 수명 없음). ② 현실 시간은 `Time.unscaledDeltaTime`으로 세며 게임을 끄고 있는 동안은 흐르지 않는다(저장에는 남은 초를 기록). ③ 인벤토리가 빈 채로 죽으면 새 가방을 만들지 않고 이전 가방을 그대로 둔다(빈손으로 가방을 찾아가다 다시 죽어도 잃지 않도록).
- **스타팅 아이템**: 에디터와 개발 빌드에서는 그대로 지급하고(테스트 편의), 릴리스 빌드에서는 컴파일 조건으로 아무것도 주지 않는다. 릴리스 동작은 빌드로 확인하지 않았다.
- **발견**: 가방이 만료되면 `contents.Clear()`가 `Modified`를 내서 `destroyWhenEmpty`가 먼저 `Destroy`를 부르지만 같은 프레임에 중복 호출은 무해했다. 만료는 `DropContents`를 거치지 않아서 전리품이 남지 않는다(철거나 부서짐은 거친다).
- **후속 위험**: 가방 위치 표시가 없다. 빈손 부활 + 수명 10분이라 멀리서 죽으면 가방까지 가는 동안 위험하다. 만료 1분 전 경고도 없다.

## 2026-10-10 가방 나침반 마커 구현 결과

- **구조**: `CompassMarker`(월드 컴포넌트, 활성 목록, 색, 임박 기준)는 가방 프리팹에만 붙였다. HUD의 `UpdateCompassMarkers`가 활성 마커마다 요소(마름모 + 거리 글자)를 만들어 `compass-viewport`에 넣고 매 프레임 위치를 정한다. 플레이어 기준 수평 거리와 방위각(`atan2(dx, dz)`, 카메라 yaw와 같은 기준)을 쓴다.
- **시야 밖 처리**: 시야는 ±53°(320px / 3px)라 그보다 벗어나면 양끝에 붙이고 `< 20m` / `20m >`로 방향을 알린다. 정확히 뒤(180°)는 오른쪽으로 붙는다.
- **글리프**: 기본 폰트에 `◆`, `◀` 같은 기호가 없을 수 있어서 마름모는 45도 회전한 정사각형 `VisualElement`로 그리고 화살표는 `<`, `>` 글자로 했다.
- **수명 임박**: 이전 후속이던 "만료 1분 전 경고"를 마커 깜빡임으로 대신했다(`urgentBelowSeconds` 60). 기준 시간은 마커 필드로 조절한다.
- **함정**: `Dictionary`의 키로 쓴 마커가 파괴되면 `== null`이 참이 되지만 해시는 유효해서 그대로 지울 수 있다. 정리는 활성이 아닌(`isActiveAndEnabled` 거짓) 마커와 파괴된 마커를 모두 대상으로 한다.
- **후속**: 침대, 동물, 작업대 같은 다른 대상에도 같은 컴포넌트를 붙이면 나침반에 뜬다(필요하면 라벨/색을 구분). 미니맵, 월드 공간 3D 마커는 만들지 않았다.

## 2026-10-10 플레이 맵(폐허 산업지대) 제작 결과와 발견

- **재현**: `ZGV01/Map/Build All`(지형 → 소품 → 스폰너/NavMesh → 경계/빌드 설정)이 같은 시드로 씬을 처음부터 만든다. 씬이 없으면 `Test_Combat`을 복사하고(플레이어, HUD, 시스템 유지) 바닥/훈련 표적/시험 자원만 걷어 낸다. 지형 수치와 구역/도로 좌표는 `MapLayout`에 모여 있다. 개별 단계는 메뉴 1~4, 외부 에셋 목록은 `Write Asset Manifest`.
- **하이트맵**: 원본 `All In One - Heightmaps/.../ValleyLandscape/T_HeightMap4k.png`(4033², 16비트, 19MB)는 저장소에 넣지 않았다(계속 무시). `MapHeightmapReader`가 PNG를 직접 해석해 (700,700)부터 2049칸을 1025칸으로 줄여 쓰고, 결과를 `TerrainData`(`Data/Map/Terrain_Industrial.asset`)에 구워 추적한다. 텍스처 임포터는 16비트를 8비트로 줄여 계단이 생기므로 쓰지 않았다. 원본이 없으면 지형을 다시 만들 수 없고 이미 구운 에셋은 그대로 쓴다.
- **Tree Creator 나무는 URP에서 분홍색**이다(`Hidden/Nature/Tree Creator*`). 같은 메시에 URP Lit 재질(잎은 알파 컷오프 + 양면)을 입힌 어댑터 프리팹으로 해결했다. 새 셰이더 변형이 처음 컴파일될 때 한두 프레임은 잎이 안 그려져 앙상한 나무처럼 보인다(오래 안 간다). 지형 나무(`TreePrototype`)는 `Tree` 컴포넌트가 없는 일반 프리팹이라 **빌보드 거리를 컬링 거리와 같게** 해야 먼 나무가 검은 사각형이 되지 않는다.
- **지형 가공 순서가 중요하다**: 테두리 산등성이(`RaiseRim`) → 구역 평탄화 → 도로 정지. 순서가 틀리면 남쪽 야영지 바닥이 7m 기울었고(테두리가 평탄화 뒤에 올라옴), 도로 정지가 구역 가장자리를 기울여 울타리가 떴다. 도로는 구역 안에서 구역 바닥 높이로 고정하고, 구역 사각형 안은 도로 보정이 지형을 건드리지 않는다.
- **피벗 함정**: 산업/학교 프리팹은 피벗이 바운즈 중심이라 `MapPropPlacer`가 렌더 바운즈로 바닥(`minY`)과 중심 오프셋을 계산해 놓는다. 겹침은 회전한 직사각형 분리축 검사로 막는다.
- **스폰너 보강**: 기존 `ZombieSpawner`/`AnimalSpawner`는 후보 높이를 스폰너/플레이어 높이로 고정해서 높낮이 있는 지형에서 NavMesh 표본이 실패했다. `GroundSnap.OnGround`로 지형 높이에 맞춘다(지형이 없는 테스트 씬은 영향 없음). 좀비는 스폰너 반경 안에서 플레이어 22~45m 고리에만 태어나서, 구역마다 스폰너를 두면 그 구역에 들어갔을 때만 좀비가 생긴다. 낮(9시 무렵) 밀도 곡선이 0.36이라 일부러 적다.
- **`GameObject.Find`는 비활성 오브젝트를 못 찾는다**: 스폰너 원본 폴더가 중복돼 복제가 비는 버그가 있었다. 씬 루트를 직접 훑도록 고쳤다. 스폰너 원본(`SpawnerTemplates`)은 비활성으로 씬에 남아 있어야 `Build All`이 복제할 수 있다.
- **NavMesh**: 지형과 물리 콜라이더를 모아 복셀 0.25로 구웠다(삼각형 약 1.4만, 2MB). 굽는 동안 플레이어는 꺼서 구멍이 나지 않게 한다. 경계 벽은 굽기 뒤에 만든다.
- **추적 방식**: `.gitignore`의 부분 허용 대신 `Data/Map/MapAssets.txt`(씬이 쓰는 외부 에셋 목록)를 만들어 `git add -f --pathspec-from-file`로 추적했다(새 외부 파일 약 408개, 264MB; 가장 큰 파일 12MB). 에셋을 더하면 `Write Asset Manifest` 후 같은 명령으로 올린다. 제외한 큰 에셋: `FurnishedCabin/PFB_Building_Full`(182MB), 8k 하이트맵.
- **시험 팁**: 큰 씬에서 임시 `Camera`로 `Render()`해 PNG로 저장하면 UI Toolkit 없이도 3D 구도를 눈으로 볼 수 있다(첫 렌더는 셰이더 컴파일로 5초 제한에 걸릴 수 있어 한 번 더 부른다). 텔레포트 직후 Cinemachine 카메라가 한동안 지형 밑이나 내려다보는 각도로 보이는 것은 일시적이다.
- **후속**: 건물 내부/문 출입, 밤 가로등, 라이트맵, 대형 건물 오클루전, 지형 풀, 좀비 밀도·자원 밀도 밸런스, 시작 야영지 안내(튜토리얼). `Hud` 나침반에 구역 이름 마커를 더하면 길찾기에 도움이 된다.

## 2026-10-10 시간대별 하늘(SkySeries Freebie) 구현 결과와 발견

- **구조**: `GameClock.Hour`(0~24)를 `SkyCycle`이 읽어 키프레임(밤 → 새벽 노을 → 낮 → 저녁 노을 → 밤)을 따라 `ZGV/SkyBlend` 셰이더의 큐브맵 A/B와 섞는다. 하늘 재질은 실행 중 복제본이라 에셋이 더러워지지 않는다. 쓰는 하늘은 `SunlessCirruscover`(낮), `CloudedSunGlow`(노을), `CoriolisNight4k`(밤)이고 나머지 SkySeries는 쓰지 않는다. 태양 색은 `SkyCycle`의 정적 그라디언트다(키가 최대 8개).
- **해/달 정렬**: SkySeries 큐브맵의 해/달은 모두 방위 약 0°, 고도 약 40°에 구워져 있다. 낮은 태양 없는 하늘을 써서 어긋남을 없앴고, 노을은 `_Rotation`으로 빛 쪽을 현재 태양 방위에 맞추고(`태양방위 - 구워진 방위`), 밤은 달을 태양 반대편으로 돌린다. 고도는 구워진 값(약 40°)이라 노을 빛과 달이 시각에 따라 오르내리지는 않는다.
- **함정**: ① 셰이더 `[Gamma]` float 속성은 `SetFloat` 값을 선형 공간 값으로 바꿔 버려 회전이 틀어졌다(회전 같은 각도에는 쓰지 않는다). ② 직렬화한 `Gradient` 필드는 씬에 기본(흰색) 값이 저장돼 코드 기본값이 덮이고, 색 키가 9개면 `SetKeys`가 조용히 무시돼 기본값이 남는다(최대 8개). ③ 새 셰이더 변형은 처음 렌더할 때 컴파일돼 에디터 도구의 5초 제한에 걸린다(한 번 더 부르면 된다). ④ 큐브맵 스카이박스는 아래 반구까지 들어 있어 지평선 아래 먼 곳이 하늘 그라디언트로 채워진다(테두리 능선이 대부분 가린다).
- **환경광**: 환경광 모드가 스카이박스라서 `DynamicGI.UpdateEnvironment()`를 0.75초마다 부른다. `GameClock`의 환경광 배율 곡선은 그대로 곱해진다.
- **추적**: 새 외부 에셋은 큐브맵 3장(약 55MB). `Write Asset Manifest` 후 `git add -f`로 올렸다.
- **후속**: 날씨(비, 폭풍: `DarkStorm` 하늘 사용 가능), 달 위상, 밤하늘 별 이동, 해/달 고도를 시각에 맞추려면 구워진 큐브맵 대신 절차 하늘이나 실제 회전(3D)이 필요하다. 저장된 시각(`timeOfDay`)은 이미 저장/복원된다.

## 2026-10-10 조명(휴대 랜턴, 설치형 조명, 가로등 점등) 구현 결과와 발견

- **사용자 결정**: 휴대 랜턴(허리춤에 차서 총과 함께 씀), 설치형 조명, 가로등 점등을 한다. 연료·점화(모닥불 조명)는 하지 않는다 → 랜턴은 연료 없이 켜고 끈다.
- **구조**: 시간표는 순수 규칙 `NightLightSchedule`(18.5~19.5시 켜짐, 5.5~6.5시 꺼짐, 부드러운 전환)이고 `NightLight`가 읽어 조명 밝기와 발광 유리 재질을 바꾼다. 가로등은 Tirgames의 켜진 변형(`RoadLamp1aNight`, `RoadLamp2On`)을 래핑한 `MapLamp_Post`/`MapLamp_Small` 프리팹이고, 꺼진 재질은 꺼진 변형의 같은 렌더러/칸에서 가져온다. 건축 조명은 `BuildableAssetBuilder`의 `lightRange` 등 사양으로 모델 꼭대기에 점광원을 붙인다(`raise`로 벽 높이에 띄움).
- **발견(리그 함정)**: 휴머노이드 `Hips` 본이 발치 높이의 `root`로 매핑돼 있어서(실제 골반은 그 자식 `pelvis`) 허리 본을 그대로 쓰면 랜턴이 몸 한가운데에 숨는다. `Spine`의 부모(`pelvis`)에 달고 위치/회전은 로컬 값(`(0.064, 0.01, -0.249)`, 오일러 `(87.7, 189.1, 280)`)으로 맞췄다. 이 리그는 골반 로컬 축이 눕혀져 있어 값이 낯설다. 캐릭터의 왼쪽은 플레이어 `-right`다.
- **직렬화 함정**: 컴포넌트를 프리팹에 붙인 뒤 필드 기본값을 코드에서 바꿔도 프리팹에 저장된 옛 값이 이긴다. 빌더가 값을 명시적으로 덮어쓴다.
- **정적 배칭**: 밤에 재질을 바꾸는 가로등은 `BatchingStatic`에서 뺀다(`MapPropLayout.MarkStatic`).
- **성능**: 점광원은 가로등 14개 + 랜턴 + 설치물이고 모두 그림자 없음. URP 추가 조명 한도(오브젝트당 8개) 안이다. 건물이 많은 구역에 가로등을 더 늘릴 때는 한도와 프레임 시간을 다시 본다.
- **후속**: 랜턴을 야간에 쓰면 좀비가 더 잘 알아채는 규칙(`NoiseSystem`/시야 연동), 모닥불 조명과 연료, 손전등(총기 장착), 가로등을 일부러 몇 개 꺼 두는 "폐허" 연출, 랜턴을 보급 상자 전리품에 넣기.

## 2026-10-10 폐허 가로등과 보급 상자 랜턴

- **고장 가로등**: 배치 단계(`MapPropLayout.BreakLamps`)가 시드 40으로 학교 가로등에 꺼짐 40%/깜빡임 25%를 배정해 `NightLight.fault`에 저장한다. 야영지 가로등은 처음 밤의 길잡이라 정상으로 둔다. 깜빡임은 램프 시드로 위상이 어긋난 11초 주기 안의 1.6초 동안만 일어나고 마지막에 켜진 채 끝난다(순수 규칙 `LampFaultPattern`).
- **보급 상자 랜턴**: 자원 노드에 일반 보너스 드랍(`bonusItem/bonusChance/bonusSkipIfOwned`)을 추가했다. 보급 상자는 채집당 15%, 이미 가졌으면 제외(300초마다 재생돼도 랜턴이 쌓이지 않게). 스타팅 아이템의 랜턴은 테스트용이며 실제 게임은 빈손 시작이라 상자가 랜턴의 주된 입수처가 된다.
- **후속**: 합선 불꽃/소리, 가로등 수리 건축, 깜빡임 가로등 근처 좀비 반응.

## 2026-10-10 콘솔 경고·오류 처리 방침

- **Devion `WriteInputManager` 정적 생성자 오류**: 외부 에셋 에디터 스크립트가 에디터 로드 때 한 번 내는 오류다. 게임과 무관하고 외부 에셋은 개조하지 않는 원칙이라 무시한다.
- **`Nature/Soft Occlusion` 나무 경고**: 지형 나무 프리팹이 Tree Creator 셰이더가 아니라서 나는 경고다. URP에서는 일반 메시 나무로 정상 렌더링되므로 그대로 둔다(셰이더를 바꾸면 URP에서 분홍색이 된다). 먼 나무가 이상하게 보이면 그때 `treeBillboardDistance`/`treeMaximumFullLODCount`를 조정한다.

## 2026-10-10 가로등 밝기와 깜빡임 패턴

- **밝기 값 위치**: 가로등 프리팹 조명 값은 `LightingAssetBuilder`의 배율(1.75/2)과 `OldFluorescentColor`로 정의하고 프리팹에도 같은 값을 직접 반영했다. `Build Lantern And Lamps` 전체를 돌리면 아이템/벨트/레시피까지 재생성되므로 가로등만 고칠 때는 프리팹을 직접 수정한다.
- **깜빡임 패턴 변경 이유**: 사용자가 "깜빡임 후 꺼짐/켜짐을 번갈아" 반복하길 원했다. 이전의 "대부분 켜짐" 규칙은 폐기했고 `Dead`(항상 꺼짐)는 유지한다.

## 2026-10-10 낮 좀비 소멸

- **그늘/실내도 불탄다**: 사용자가 명시했다. 햇빛 가림 판정을 만들지 않는다(구현이 단순해지고 낮에 안전지대가 생기지 않는다).
- **불 때문에 죽을 때만 드랍 제거**: 낮에 불로 죽은 좀비만 드랍이 없다. 플레이어가 죽인 좀비는 낮에도 드랍한다(불타는 중에 쏴 죽인 경우 포함).
- **불 시작 지연**: 새벽에 모든 좀비가 한꺼번에 죽지 않게 좀비마다 1~30초 무작위 지연을 둔다.

## 2026-10-10 하늘 이동

- **해/달은 셰이더 원반**: 큐브맵을 3D로 돌리는 대신 원반을 그린다(아래 반구가 회색이라 3D 회전이 불가능). 구워진 달은 마스크로 가린다. 마스크 값은 프로브 카메라로 큐브맵의 가장 밝은 점을 찾아 측정했다(밤 방위 -2.9°/고도 24.5°, 노을 방위 -11.5°/고도 23.4°).
- **노을은 그대로**: 노을 큐브맵의 구워진 빛은 고도 약 23°라 해(지평선)와 정확히 겹치지 않지만, 노을 시간대에는 해 원반을 그리지 않고 큐브맵의 빛에 맡긴다.

## 2026-10-10 하늘 비중은 해의 고도로

- **시계 키프레임 폐기**: 시각 표(키프레임) 대신 해의 고도로 세 하늘의 비중을 정한다. 이유는 키프레임 크로스페이드가 좁은 구간에서 서로 다른 구름 모양을 한꺼번에 바꿔 뚝 끊겨 보였기 때문이다. 고도 기반이면 해 위치(조명, 원반)와 하늘이 항상 같은 기준으로 움직인다.
- **한계**: SkySeries Freebie에는 하늘 3장뿐이라 구름 모양이 다른 하늘을 섞는 방식이다. 중간 시간대(해 고도 20~40°)에는 두 구름층이 겹쳐 보일 수 있다. 더 부드럽게 하려면 구름 모양이 비슷한 하늘을 더 쓰거나 셰이더에서 구름 밝기로 디졸브하는 방법이 있다.

## 2026-10-10 가로등 배치 방침

- **기존 배치 유지**: 가로등을 구역 단계에서 더 놓으면 공유 난수 순서가 밀려 맵 전체 소품이 바뀐다. 그래서 새 가로등은 모든 소품 뒤(`BuildLamps`)에 빈 곳에만 놓는다. 야영지/학교 가로등은 구역 단계 그대로다.
- **고장은 거리 기반**: 도로 가로등은 야영지에서 멀수록 더 많이 고장 난다(시작 구역은 안전하고 멀수록 위험하다는 신호). 구역 가로등은 구역별 고정 비율이다.
- **큰 가로등 높이 절반**: 사용자가 너무 높다고 해서 줄였다. 지형은 다시 만들지 않고 소품만 다시 배치해도 된다(`2 Place Props` 이후 `3`, `4`).

## 2026-10-10 탈것(말) 결정

- **말을 고른 이유**: 프로젝트에 바퀴 달린 탈것 에셋이 없다. 말은 `Animal pack deluxe v2`(6종, 걷기/달리기/점프 애니메이션)와 `Ultimate Animal Sounds/Horse`(발굽/질주/울음/안장)로 바로 만들 수 있다. 차량 에셋을 구하면 같은 `HorseRider` 구조 위에 얹을 수 있다.
- **플레이어가 이동을 맡는다**: 말을 따로 조종하면 카메라/입력/충돌을 다시 만들어야 한다. 외부 StarterAssets 컨트롤러를 그대로 쓰고 속도와 점프만 바꾸는 편이 안전하다(외부 코드 수정 금지 원칙).
- **야생 말은 도망가지 않는다**: 말의 `alertDistance`를 0으로 해 다가가도 도망가지 않게 한다(맞으면 도망). 사냥감 스폰너를 건드리지 않으려고 말 전용 스포너를 둔다.
- **테스트 순간이동은 런타임 자체 생성**: 씬에 오브젝트를 두면 맵 재생성(Build All)마다 사라지거나 중복되므로, 개발 빌드에서만 스스로 만든다.

- **안장 높이 0.95**: 처음 1.35로 했더니 말 등 위에 "서 있는" 모습이었다. 앉는 애니메이션이 없어서, 서 있는 자세의 다리가 말 몸통(폭 약 0.7m)에 가려지도록 낮췄다. 앉는 클립이 생기면 `HorseRider`에서 그 클립을 틀고 모델을 안장 위에 맞추면 된다.
- **말 콜라이더는 탑승 중 끈다**: 플레이어 CharacterController와 겹치면 매 프레임 밀어낸다. 시체 콜라이더는 다시 켜지 않는다.

## 2026-10-10 월드 보강 결정

- **드롭은 아이콘 빌보드가 기본**: 아이템 29종에 모델이 없다. 모든 아이템 아이콘(128x128)이 이미 있으므로 아이콘을 세워 보여 주고, 3D 모델이 있는 무기만 모델을 쓴다(`ItemDefinition.worldPrefab`).
- **풀은 코드로 만든 텍스처**: 프로젝트에 풀 에셋이 없다. 외부 에셋을 새로 구하기보다 풀잎 텍스처를 생성해 쓰고, 나중에 더 좋은 에셋이 생기면 디테일 프로토타입만 바꾼다.
- **물은 거점 루프로**: 웅덩이에서 반복 채집하는 대신 집수통을 짓고 기다리는 방식으로 바꾼다(사용자 선택). 연못은 맵을 돌아다니는 보조 수단이다.

- **지형 풀 디테일은 쓰지 않는다**: 이 프로젝트(Unity 6 URP)에서 지형 디테일(풀 빌보드, 버텍스 라이트, 메시)이 어떤 설정에서도 그려지지 않았다(원인 미확인). 지형 디테일 데이터는 밀도 저장용으로만 쓰고 `GrassField`가 직접 그린다. 디테일 산란 모드는 인스턴스 개수 모드여야 한다(커버리지 모드에서는 한 자릿수 값이 거의 보이지 않는다).
- **맵 재생성은 `Build All`**: 풀/나무/채집 노드가 모두 지형 빌드와 소품 배치에서 나오므로 값을 바꾸면 `Build All`을 다시 돌린다(결정적 시드라 같은 맵이 나온다).

- **학교 내부는 건물 메시를 고치지 않고 안쪽에 지었다**: 원본이 한쪽 면만 그린 껍데기라 안쪽에서는 비어 보인다. 안쪽 벽(두께 0.3m)과 바닥판으로 막고, 입구 자리만 본체 메시 사본에서 삼각형을 걷어냈다(원본 에셋은 수정하지 않는다). 평면 치수는 원본 메시 측정값이다(층 높이 2.9m, 1층 바닥 25.9 = 건물 피벗 -3.3m, 정면 창 가로 위치 6곳).
- **실내 조명은 항상 켜짐(`NightLight.alwaysOn`)**: 실내는 낮에도 어두워 시간표와 무관하게 켠다. 고장/깜빡임 패턴은 가로등과 같은 규칙을 쓴다. 디버그 순간이동의 가로등 순회에서는 뺀다.
- **게임 중 입력이 섞일 수 있다**: 에디터에서 사용자가 Game 뷰를 조작하면(W/Shift) 시험 중 플레이어가 저절로 움직인다. 시험은 컨트롤러 입력을 흉내 내기보다 위치와 충돌을 직접 보는 방식이 안정적이다.

- **1인칭은 같은 카메라의 거리 0으로**: Cinemachine 3인칭 팔로우 카메라의 거리를 0으로 줄이면 머리 위치에서 본다. 별도 1인칭 카메라를 만들지 않았다. 캐릭터 모델에는 FPS 팔(`SK_Miliary_FPS_Arms`)이 이미 있지만 지금은 몸과 함께 숨긴다(손에 든 무기는 보인다). 1인칭 팔이 필요하면 그 렌더러만 켜면 된다.
- **가구 방향은 모델마다 다르다**: 방향은 추측하지 말고 위에서 찍어 확인하고(`YawFor`), 의자처럼 알기 어려운 것은 등받이 정점 분포로 구한다.

- **광산 입구는 지형 구멍으로 연다**: 광장을 한 칸 절벽으로 파고 입구 폭(5.6m)만큼 절벽 칸에 `TerrainData.SetHoles` 구멍을 뚫는다. 구멍 뒤 산 지면(약 46m)은 갱도 천장(최대 44.7m) 위에 그대로 남는다. 지형 콜라이더는 한쪽 면만 막아 지면 아래 갱도 안에서는 박스 바닥/벽이 충돌을 맡는다.
- **광산 오브젝트 이름 주의**: `MapSceneBuilder`가 이름이 `Floor`인 오브젝트를 `GameObject.Find`로 지워서 광산 바닥을 `MineFloor`로 지었다. 같은 이름(`Floor`, `Tree`, `FoodAndWater`, `TrainingDummy`)을 맵 소품에 쓰지 않는다.
- **속 빈 부속 건물**: 원본 재질이 한쪽 면만 그려 안이 비어 보였다. 원본 에셋은 건드리지 않고 `Assets/_Project/Materials/Map/DoubleSided/` 아래에 컬링을 끈 재질 사본을 만들어 쓰고 메시 콜라이더를 붙인다.
- **광산 정면은 바위 덩어리로 막는다**: 바위 판(Wall_Caves)만으로는 판 사이로 지형 절벽/구멍/하늘이 보여 `FacadeMass` 상자(통로 폭만 비움)와 상인방으로 뒤를 채우고, 판의 콜라이더는 지운다(입구 판 콜라이더가 통로를 좁혀 막았다). 연못은 원판이 3cm라 비탈에 묻혔다. 평탄 검사를 반지름 4.8m/오차 0.15m로 조이고 0.17m 띄운다.

- **구역 밀도 작업 범위(2026-10-10)**: 새 에셋을 들이지 않고 있는 에셋(산업 팩의 배관/지지대/팬/자루 팔레트, 학교 팩의 화단/벤치/울타리, 광산 팩의 광석/상자/널빤지)을 조합한다. 차량/텐트/모래주머니는 에셋에 없어 쓰지 않는다. 바닥은 지형 알파맵 칠하기 규칙만 바꾸고 텍스처는 그대로다.
- **광산 광장 예약 순서**: `MapPropLayout.Build`에서 광장 `Reserve`가 구역 소품보다 뒤에 있어 채석장 소품이 광장(채석장 구역 북쪽과 겹침)을 막을 수 있었다. 구역 소품 앞으로 옮겼다. 배치가 바뀌므로 이 순서를 되돌리지 않는다.
- **큰 건물은 먼저 놓는다**: 낡은 교사동처럼 발자국이 큰 건물은 작은 소품보다 뒤에 놓으면 겹침 검사에 걸려 조용히 빠진다(`Place`가 null을 돌려준다). 구역 함수 앞쪽에서 놓는다.

- **거점 판정과 10일 유예(2026-10-10)**: 거점은 설치/복원된 `BuildableInstance`(시체 가방 제외)가 하나라도 있는 것이다. 유예 10일은 게임 시작부터 게임 시간으로 센다(사용자 확인: 마지막 거점을 잃은 순간부터가 아니다). 10일이 지나면 거점이 없어도 밤에 추적한다(요청 문장에 없어 그렇게 해석했다). 일 수는 `GameClock`에 날 수가 없어 `BaseThreat`가 흐른 게임 시간(시)으로 센다.
- **정찰의 한계**: 정찰 중에도 시야에 들어오면 평소대로 추격한다("정찰"이 시야 감지를 끈다는 뜻은 아니라고 봤다). 낮에는 좀비가 나오지 않으므로 정찰은 사실상 밤에만 보인다.

- **좀비 셰이더는 Lit을 감싼다(2026-10-10)**: URP Lit의 `LitForwardPass`를 `#define LitPassFragment LitPassFragmentBase`로 이름만 바꿔 포함하고 그 뒤에 효과를 입힌다. 그림자/깊이/깊이 법선/메타 패스는 `UsePass "Universal Render Pipeline/Lit/..."`로 재사용하므로 디졸브 중에도 그림자는 온전하다(짧은 시간이라 받아들였다). 효과 속성은 `UnityPerMaterial` 밖이라 SRP 배처가 이 재질을 묶지 못한다(좀비 수십 마리 수준이라 괜찮다).
- **원본 몸 재질의 발광은 꺼져 있었다**: `ZombieBB_Body_URP`는 발광 맵과 색(3.0)이 있지만 `_EMISSION` 키워드가 꺼져 있어 눈이 안 빛났다. 맥동은 키워드와 상관없이 발광 맵을 직접 읽어 눈/핏줄을 박동에 맞춰 밝힌다. 옷 재질은 발광 맵이 없어(기본 흰색) 맥동을 걸면 전체가 빛나므로 발광 맵이 있는 렌더러에만 건다.
- **좀비 프리팹을 다시 만들면 적용을 다시 한다**: `ZombieAssetBuilder`가 프리팹을 새로 만들면 재질과 `ZombieShaderFX`가 사라지므로 `ZGV01/Zombie/Apply Shader FX`를 다시 실행한다.

- **GPU Resident Drawer와 셰이더 속성 위치(2026-10-10)**: 이 프로젝트는 GPU Resident Drawer를 쓰므로 정적 메시 셰이더의 모든 재질 속성이 `UnityPerMaterial` 상수 버퍼 안에 있어야 한다. 효과 속성을 버퍼 밖에 두면 분홍색으로 그려지고 "not SRP Batcher compatible" 오류가 난다. 그래서 오염 셰이더는 URP `LitInput.hlsl`/`SimpleLitInput.hlsl`을 복사해(`ZGVGrimeLitInput.hlsl`, `ZGVGrimeSimpleLitInput.hlsl`) 버퍼 안에 속성을 넣고 모든 패스(그림자 포함)가 그 복사본을 쓰게 했다. 좀비 셰이더는 스킨드 메시라 GPU Resident Drawer 대상이 아니어서 버퍼 밖 속성으로도 문제없다.
- **오염 셰이더 생성 방식**: URP `Lit.shader`/`SimpleLit.shader` 텍스트를 스크립트로 변형해 만들었다(생성 스크립트는 저장소에 두지 않았다). URP 버전을 올리면 다시 만들어야 한다. 에디터에서 새 셰이더 변형은 비동기로 컴파일돼 처음에는 회색/분홍으로 보일 수 있으니 잠시 기다린다. `unity_ObjectToWorld`는 DOTS 변형에서 직접 못 쓰므로 `GetObjectToWorldMatrix()`를 쓴다.
- **런타임 셰이더는 Resources 재질로 참조한다**: 건축 고스트(`Resources/BuildGhost.mat`)와 반짝임(`Resources/ResourceSparkle.mat`)은 `Shader.Find`가 아니라 Resources 재질로 불러야 빌드에서 셰이더가 빠지지 않는다.

- **지형 나무 채집은 대역 방식(2026-10-10)**: 나무 1만여 그루를 오브젝트로 바꾸면 `ResourceNode.Update`와 상호작용 검색이 전부 돌아 무거우므로, 플레이어 둘레 5그루에만 보이지 않는 `ResourceNode`(트리거 캡슐) 대역을 붙인다. 숨기기는 `TerrainData.SetTreeInstance`로 크기를 0으로 하는 것이고, 종료할 때(`OnDestroy`) 숨긴 나무를 모두 복구해 에디터에서 지형 에셋이 바뀐 채 남지 않게 한다. 채집 정의는 씬의 `TerrainTreeGather`에 직접 연결한다(Resources에 두지 않는다).

- **옵션 적용 위치(2026-10-10)**: 마우스 감도/반전은 입력 값이 만들어지는 `StarterAssetsInputs.LookInput`에서 곱한다(`ThirdPersonController`는 `_input.look`을 그대로 쓴다). 외부 샘플 코드 한 줄을 고친 것이므로 샘플을 다시 가져오면 이 줄을 다시 넣는다. 시야각은 상태별 목표 시야각(맨손/무장/조준/실내)에 배율을 곱해 상태 전환이 그대로 부드럽다.
- **일시정지 방식**: `Time.timeScale = 0`과 `AudioListener.pause`로 멈춘다. HUD의 코루틴/스케줄은 영향이 없고, 홀드 링은 `unscaledDeltaTime`이라 괜찮다. 닫을 때와 `HudController` 파괴 시 이전 값으로 되돌린다.
- **UI 검증 한계**: `capture_game_view`/`screenshot` 도구는 UI Toolkit 오버레이를 담지 못한다(기존 HUD도 안 찍힌다). UI는 `resolvedStyle`/`worldBound`와 컨트롤 값 조작으로 확인한다. 레이아웃 계산은 열린 직후 한 프레임 늦어서 바로 읽으면 이전 값이 나온다.

- **분류별 음량은 믹서 없이 재생 지점에서 곱한다(2026-10-10)**: AudioMixer 에셋은 스크립트로 그룹/노출 파라미터를 만들 공개 API가 없고, 오디오 소스를 전부 라우팅해야 해서 `AudioBus.Volume(분류)`를 각 재생 지점(`PlayOneShot` 배율, `source.volume`)에 곱했다. 새 소리를 재생하는 코드를 추가하면 자기 분류의 `AudioBus.Volume`을 곱해야 옵션이 먹는다. 마스터는 `AudioListener.volume`이다.
- **그래픽 품질 구현 한계(2026-10-10)**: URP 에셋의 `supportsSoftShadows`, `supportsAdditionalLightShadows`는 읽기 전용이라 그림자 해상도/거리/추가 조명 수/렌더 스케일/SSAO(리플렉션)로 조절한다. RP 에셋은 `Instantiate`한 복제본을 `QualitySettings.renderPipeline`에 넣고 `Application.quitting`에서 원본으로 되돌린다. 커스텀 셰이더는 전역 `_ZGV_Cut`(0 높음, 1 중간, 2 낮음)을 읽는다. 값을 안 넣은 곳(에디터 편집 화면)은 0이라 전체 효과가 나온다. 전역 변수는 재질 속성이 아니라서 `UnityPerMaterial` 밖에 둬도 GPU Resident Drawer와 호환된다.
- **UI 크기는 패널 기준 해상도로 조절한다(2026-10-10)**: `PanelSettings`가 ScaleWithScreenSize(1200x800)라 배율로 나눈 값을 기준 해상도에 넣는다. 에셋을 직접 바꾸므로 `HudController.OnDestroy`에서 원래 값을 되돌린다.

- **키 설정 구조(2026-10-10)**: 액션 에셋 키는 `PlayerInput.actions`의 바인딩 인덱스(키보드 그룹 첫 번째, 이동은 복합 조각 이름)에 `ApplyBindingOverride`로 반영하고, 코드가 읽는 키는 `KeyBindings.Pressed/Held`가 설정의 경로로 `InputSystem.FindControl`해 읽는다. 새 키 입력을 코드에서 읽을 때는 `Keyboard.current.xKey`를 직접 쓰지 말고 `KeyBindings`에 항목을 더한다. 액션 에셋은 종료 때 `RemoveAllBindingOverrides`로 되돌린다(에디터에서 에셋이 바뀐 채 남지 않게).
- **키 설정 한계**: 첫 조회 때 `EnsureAttached`로 액션 에셋을 찾는다(HUD가 `AfterSceneLoad`보다 먼저 키 이름을 읽기 때문). 기본 인벤토리 키는 액션 에셋의 `Inventory` 액션이 아니라 `HudController`가 Tab을 직접 읽던 것이라 코드 키로 다룬다. `Crouch` 액션(C)은 쓰는 코드가 없어 목록에 넣지 않았다.

- **아이템 버리기 구조(2026-10-10)**: 버리기는 `ItemDropper.Drop`이 인벤토리 차감과 전리품 생성을 함께 하는 단일 진입점이다(멀티 때 서버 권위 요청으로 바꿀 자리). 버린 직후 바로 다시 줍지 않도록 `LootPickup`에 줍기 지연을 두었다. 비소비 아이템의 "사용"은 막아 두었다(장비는 V 창에서 장착).
- **낮음 품질 하늘(2026-10-10)**: 세 장 블렌딩 `ZGV/SkyBlend` 대신 내장 `Skybox/Cubemap`에 색조만 입힌다. 내장 셰이더는 빌드에서 빠질 수 있어 `SkyCycle.simpleSkyShader`로 직렬화해 둔다.

- **고철 공급(2026-10-10)**: 전에는 좀비 드랍만이 유일한 공급이라 부족했다. 고철 더미 노드를 구역과 길가에 더했다(채집 1회 1개, 최대 3개, 240초 재생). 수치는 임시 밸런스이고, 맵을 다시 만들 때는 `MapPropLayout`이 구역 안 노드를 같이 놓는다(길가 30개는 `Place Scrap Piles In Scene`이 담당).
- **씬 로드와 자동 생성 컴포넌트(2026-10-10)**: `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`는 첫 씬 한 번만 불린다. 첫 씬이 메인 메뉴가 되면 게임 씬에는 아무것도 만들어지지 않으므로, 이런 시작 훅은 `SceneManager.sceneLoaded`에도 걸어 씬마다 다시 한다. 새 자동 생성 컴포넌트를 만들 때도 같은 패턴(`Hook` + `OnSceneLoaded` + 메뉴 씬 가드)을 따른다.
- **메인 메뉴 구조(2026-10-10)**: 이어하기는 게임 씬으로 넘어간 뒤 `GameSession.ContinueLoader`가 3프레임 뒤 `SaveLoadDebugController.LoadNow`를 불러 적용한다(씬이 시작을 마친 뒤여야 플레이어/DB가 준비돼 있다). 일시정지 패널은 `PausePanel.uxml` 템플릿으로 분리했고, 템플릿이 들어가는 `TemplateContainer`는 크기가 0이라 `.pause-host`(절대 위치 전체 화면, 클릭 통과)로 감싼다.

- **저장 슬롯(2026-10-10)**: 슬롯은 파일 이름(`save_slot{n}.json`)만 다르고 저장 포맷은 같다. `CurrentSlot`은 정적 값이라 메뉴에서 고른 슬롯이 게임 씬에서도 이어진다. 새 게임은 고른 슬롯의 파일을 먼저 지운다(그래야 F9가 옛 저장을 불러오지 않는다). 멀티플레이를 붙일 때는 서버가 저장을 맡으므로 이 정적 슬롯은 싱글(로컬 권위) 전용이다.
- **메뉴 3D 배경의 소품(2026-10-10)**: 게임 프리팹을 씬에 놓을 때 `UnpackPrefabInstance` 후 MonoBehaviour/콜라이더/NavMeshAgent 등을 떼어 낸다(안 떼면 `CraftingStation` 같은 정적 등록 컴포넌트가 메뉴에서 돌아간다). 배경을 바꾸려면 `MainMenuSceneBuilder`의 `BuildBackdrop`을 고치고 `ZGV01/Menu/Build Main Menu Scene`을 다시 실행한다.
- **메뉴 배경 개편(2026-10-12)**: 메뉴 글자가 왼쪽에 있으므로 주인공(`PlayerArmature`)을 모닥불 옆 오른쪽에 세우고(Idle은 컨트롤러 기본 상태, 스크립트·카메라·AudioListener는 `Place`에서 제거), 나무 34그루(고정 시드 7)와 좀비 8마리로 숲 속 야영지를 채웠다. `Place`는 `[RequireComponent]`로 다른 컴포넌트가 요구하는 것을 건너뛰고 의존하는 쪽부터 지운다(이전에는 순서 때문에 콘솔에 'Can't remove' 오류가 났다). 깊이 보정(`DepthScale`)은 소품 좌표를 실제 거리로 쓰도록 없앴다. 에디트 모드 캡처에서는 Animator가 안 돌아 T자 포즈로 보이니 Play 모드(`source=screen`)로 확인한다.

- **HUD 숨기기는 visibility로 한다(2026-10-10)**: `HudController`가 많은 HUD 요소의 `style.display`를 직접 바꾸므로(인라인 스타일이 USS보다 우선) USS의 `display: none`으로는 안 감춰진다. 요소별 `hide-이름` 클래스(`HudController.HudHideClasses`)가 `visibility: hidden`을 걸어 감춘다. 새 HUD 요소를 더하면 `HudElement`(끝에 추가), `Hud.uss`의 `.hide-*` 규칙, `PausePanel.uxml`의 HUD 탭 토글, `PauseMenu.HudToggleNames`, `HudController.HudHideClasses`에 모두 넣는다. 또 USS를 고친 뒤 Play 중 반영이 안 보이면 에디터 포커스가 없어 재임포트가 늦은 것이니 `AssetDatabase.ImportAsset`으로 강제한다.

- **멀티플레이 1단계 구조(2026-10-10)**: 씬에 있는 `PlayerArmature`를 로컬 플레이어로 그대로 쓰고, 접속자마다 모양만 있는 `NetworkPlayerAvatar`(소유자 권위 `NetworkTransform`/`NetworkAnimator`)를 `SpawnAsPlayerObject`로 소환한다. 게임 규칙은 아직 서버 권위가 아니다(이동만 소유자 권위, 월드는 접속자마다 따로 돈다). 다음 단계에서 도메인(전투→인벤토리→건축→AI 순)을 서버 권위로 옮기고 월드 오브젝트를 네트워크 오브젝트로 바꾼다. 아바타는 `Player` 태그가 없어야 한다(AI/상호작용이 로컬 플레이어만 찾도록).
- **NetworkManager는 프리팹 + 자동 생성(2026-10-10)**: `Resources/NetworkRoot`를 `MultiplayerSession.Bootstrap`이 시작 전에 만들어 유지한다. 씬마다 두면 메뉴로 돌아올 때 중복 인스턴스가 생긴다. 플레이어 프리팹은 자동 소환을 쓰지 않고(`PlayerPrefab = null`) 게임 씬이 열린 뒤 직접 소환한다(씬 전환에 오브젝트가 휩쓸리지 않게). 아바타 프리팹은 `Assets/DefaultNetworkPrefabs.asset`에도 등록되므로 `AddNetworkPrefab` 전에 `Contains`로 확인한다(중복 등록은 오류).
- **멀티 시험 방법(2026-10-10)**: 에디터는 호스트, 개발 빌드는 클라이언트. 빌드는 `ZGV01/Network/Build Test Client`(첫 빌드는 셰이더 컴파일로 한 시간 가까이 걸리고 이후는 빠르다). 에디터 창에 포커스가 없으면 Play가 느려진다.

## 2026-10-11 건축 서버 권위
- 건축물을 NetworkObject로 만들지 않고 worldId 기반 복제본을 쓴다. 이유: 건축물 프리팹이 40여 종이고 저장 불러오기 경로(`Restore`)가 이미 같은 일을 하며, 서버가 전부 소유해도 참가자는 상호작용 대상이 아니라 충돌체/표시만 필요하기 때문이다. 대신 이후 보관함 같은 상태 시설은 별도 동기화가 필요하다.
- 설치 검증을 위해 서버에서 임시 고스트(프리팹 인스턴스, 충돌체 끔)를 만들어 로컬 고스트와 같은 `EvaluatePlacement`를 돌린다. 규칙을 두 군데에 쓰지 않으려는 선택이다. 임시 고스트는 만든 즉시 `SetActive(false)` 후 `Destroy`한다.
- `BuildableInstance.Initialize` 직후 바로 알리지 않고 한 프레임 뒤에 알린다. 설치 코드가 등록 뒤에 받침(`SetSupport`)을 정하던 순서도 등록 전으로 바꿨다.
- 시험 함정: 한 번 접속했던 개발 빌드를 강제 종료하고 곧바로 다시 붙이면 "Failed to connect to server."가 났다(이전 연결 정리 전으로 추정, 에디터 플레이를 껐다 켜서 해결). 늦은 접속 시험은 플레이를 새로 시작해 건축물을 먼저 놓고 클라이언트를 붙였다.
- 수리 시험이 처음엔 반응이 없었는데, 서버의 거리 검사(수리 범위 4.5m + 3m)에 8.65m라서 정상 거부였다.

## 2026-10-12 멀티플레이 6단계: 동물/맹수 동기화

- **좀비와 같은 방식, 인터페이스 하나로 묶음**: 초식(`AnimalAI`)과 맹수(`PredatorAI`)가 `IAnimalNetworkSource`를 구현하고 `NetworkAnimal` 하나가 둘을 동기화한다. 서버가 내보내는 것은 상태 정수(`NetworkVariable`), 재생 애니메이션 이름(RPC + 늦은 접속용 변수), 피해(`Health.ApplyReplicatedDamage`), 경계/공격 사건(소리용)이다. 참가자의 `Motion`/`CanCallIdly`는 동기화된 상태에서 로컬로 계산하므로 발소리와 울음이 서버와 같은 규칙을 쓴다.
- **애니메이션은 이름으로 동기화한다**: 동물은 `Animator.CrossFade(상태 이름)`으로 구동해서 파라미터가 없다. 같은 상태를 다시 재생해야 하는 경우(공격 A 연속)가 있어 변수 하나로는 부족하고 RPC로 `force`를 함께 보낸다. 사망은 꼭두각시가 `Health.Died`로 스스로 쓰러지므로 죽은 뒤 도착한 서버의 사망 애니메이션은 무시한다.
- **목표 선택 변경(싱글에도 영향)**: 동물/맹수가 `FindGameObjectWithTag("Player")` 대신 `PlayerTargets.Nearest`(0.4초마다 재선택)를 쓴다. 죽은 플레이어는 후보가 아니라서 초식 동물도 시체를 위협으로 보지 않는다(이전에는 거리만 봤다). 맹수 피해는 로컬 플레이어면 `Health.ApplyDamage(양, 방향, false)`, 참가자 아바타(Health 없음)면 `PlayerTargets.Damage`로 소유자에게 전달한다.
- **네트워크 오브젝트는 부모가 없어야 한다**: `AnimalSpawner`는 호스트에서 부모 없이 소환하고, 저장 불러오기의 `Adopt`는 이미 소환된 오브젝트의 부모를 바꾸지 않는다. 호스트의 `LoadAnimals`는 불러온 동물을 `Spawn(true)`한다.
- **말(`Mountable`)**: 참가자의 꼭두각시 말은 탈 수 없게 막았다(`CanBeMounted`). 호스트가 탔을 때 걸음 애니메이션(`Mountable.PlayGait`이 애니메이터를 직접 구동)은 동기화하지 않는다.
- **`NetworkRoot.prefab`의 `NetworkPrefabsLists` 참조**: `Assets/DefaultNetworkPrefabs.asset`은 `.gitignore`(`/Assets/*`)로 추적되지 않아 컴퓨터마다 GUID가 다르다. `Build Network Assets`를 실행하면 이 컴퓨터의 GUID로 덮어써지므로 커밋 전에 되돌린다(`f17b4357...`). 이 목록은 우연히 생긴 참조이고 실제 소환 프리팹은 `NetworkRoot`의 자체 배열에 들어간다.
- **`Build Network Assets` 콘솔 오류**: `BuildAvatar`의 컴포넌트 삭제가 의존 순서 때문에 "Can't remove" 오류를 여러 번 남기지만 재시도 끝에 성공한다(기존 현상, `MainMenuSceneBuilder.Place`는 이번에 순서를 고쳤다).

## 2026-10-12 이어하기로 땅 밑 자유낙하 수정

- **원인**: `save_slot1.json`(10월 7일, 옛 테스트 씬 시절)에 플레이어 위치가 `(0, -0.01, 0)`으로 저장돼 있었다. 슬롯 1에 저장이 있으면 이어하기와 멀티 호스트(`GameSession.StartHost`)가 그 위치를 적용하는데, 지금 `Main_Map`의 지면은 그 자리에서 약 24m라 플레이어가 지형 아래에서 자유낙하했다. `Main_Map`을 바로 실행하면 저장을 읽지 않아 정상이다.
- **수정**: `SavedPositionRule.IsBuried`(순수, 테스트 대상). 저장된 높이가 지면보다 1m 넘게 아래이고 그 아래 3m 안에 받쳐 주는 콜라이더도 없으면 묻힌 위치로 보고 저장 위치를 적용하지 않고 현재(시작) 위치를 유지한다. 광산처럼 지형 아래에 바닥이 있는 실내는 받침이 있으므로 영향이 없다. 그 외 저장 내용(체력, 인벤토리 등)은 그대로 적용한다.
- **한계**: 맵이 바뀌어 옛 좌표가 공중이나 건물 안이면 알 수 없다(공중은 떨어져 착지한다). 컴퓨터마다 저장 파일이 다르므로(저장은 git 밖) 이 증상은 해당 컴퓨터의 옛 저장에서만 나타난다.

## 2026-10-12 멀티플레이 7단계: 보관함, 침대, 시체 가방

- **보관함 복제본은 열 때만 채운다**: 참가자 월드의 보관함은 건축물 복제본이라 내용이 비어 있고, 열면 서버가 그 접속자를 보는 사람으로 기록해 스냅샷을 보낸다. 이후 내용이 바뀔 때마다(호스트가 열든 다른 참가자가 넣든) 보는 사람에게만 다시 보낸다. 모든 보관함의 내용을 상시 동기화하면 가방과 상자가 많을 때 낭비라서다.
- **스냅샷 형식 공유**: 인벤토리 스냅샷의 "id:수량;…" 문자열(`NetworkInventory.EncodeSnapshot/DecodeSnapshot`)을 보관함에도 그대로 쓴다.
- **복제본은 지우지 않는다**: 복제본 `StorageContainer`는 수명 타이머를 표시용으로만 돌리고, 비었을 때 스스로 사라지는 규칙과 수명 만료 처리와 전리품 드랍을 모두 건너뛴다. 서버가 가방을 지우면 `BuildSync.RemovedRpc`가 복제본을 파편 없이 지운다(가방만. 다른 건축물은 기존대로 무너지는 연출).
- **가방은 접속자별 하나**: `PlayerRespawn.CreateCorpseBag(…, ownerClientId)`와 `StorageContainer.OwnerClientId`로 "마지막 사망 기준 하나"를 접속자별로 지킨다(이전에는 서버가 다른 접속자의 가방까지 지웠을 것이다). 다른 접속자의 가방 만료 알림은 `StorageContainer.OwnerExpired` → `StorageSync`가 소유자에게 보낸다(호스트 가방은 기존 `Expired`).
- **침대는 소유자 쪽 상태**: 부활이 소유자가 자기 플레이어를 옮기는 기존 방식이라 침대 지정(`PlayerRespawn.SetBed`)도 복제본 `RespawnPoint`로 소유자 쪽에서 한다. 서버 검증과 저장은 하지 않는다(위치 이동 자체가 소유자 권위인 1단계 설계와 같은 수준).
- **시험 방법**: 개발 빌드에 `-mp-storage-log -mp-storage-test`. 호스트(에디터)가 `BuildableInstance.Initialize`로 참가자 옆에 건축물을 두고 `NetworkInventory.ServerGrant`로 재료를 주면 참가자가 열기/넣기/꺼내기/모두 가져오기/침대 지정을 요청한다. 가방은 호스트가 `NetworkCombat.DamagePlayer`로 참가자를 죽이면 이어서 부활 후 가방 비우기까지 진행한다.

## 2026-10-12 멀티플레이 8단계: 참가자 인벤토리와 침대 저장

- **식별은 참가자 컴퓨터가 만든 id로 한다**: 접속마다 바뀌는 `clientId` 대신 `LocalPlayerIdentity`가 `player_id.txt`에 보관한 GUID를 아바타 소환 직후 서버에 알린다(`NetworkInventory.IdentifyRpc`). 계정이나 인증이 아니라 식별이다.
- **서버 기록부 `ParticipantStore`**: 접속 중인 참가자는 서버 인벤토리를 그대로 읽고, 나간 참가자는 마지막 상태를 id별로 보관한다. 저장(`CollectForSave`)은 둘을 합치므로 나가 있는 참가자의 데이터도 지워지지 않고, 싱글플레이로 이어서 저장해도 유지된다. 호스트가 불러오면(`LoadFromSave`) 기록부를 저장 내용으로 바꾸고 이미 접속한 참가자에게도 적용한다. 그래서 접속이 불러오기보다 먼저여도(`ContinueLoader`가 3프레임 뒤에 불러온다) 순서 문제가 없다.
- **침대 복원은 지연 적용**: 서버는 침대 월드 id만 알리고, 소유자는 건축물 복제본(`BuildSync`가 늦게 도착한다)이 생길 때까지 기다렸다가 `RestoreBed`로 조용히 지정한다(알림과 서버 재전송이 없다). 저장 때 존재하지 않는 침대 id는 비운다.
- **시험 방법의 함정(기존 문제, 이번 변경과 무관)**: 개발 빌드 참가자를 `taskkill /F`로 강제 종료하면 호스트가 이후 새 접속을 전혀 받지 못하고 남은 참가자까지 끊겼다(UDP 포트는 열려 있고 `Listening=True`인데 연결 실패, 호스트 콘솔에 오류 없음). 같은 세션에서 참가자 둘이 접속하는 것은 정상이고, 창을 정상 종료(`CloseMainWindow`)하면 그 참가자만 즉시 빠지고 다른 참가자는 유지되며 재접속도 정상이다. 원인은 확정하지 못했고 Windows UDP 소켓의 연결 재설정(ICMP) 오류로 추정한다. 참가자 시험은 정상 종료로 한다. 실제 플레이에서 프로세스가 비정상 종료(충돌, 작업 관리자 종료)되면 같은 현상이 생길 수 있으나 확인하지 않았다.
- **시험 슬롯**: 저장 시험은 사용자 저장이 있는 슬롯 1이 아니라 슬롯 3으로 하고 끝나면 `save_slot3.json`을 지운다.

## 2026-10-12 멀티플레이 9단계: 참가자 장착 상태와 생존 수치 저장

- **원본은 참가자 쪽**: 장착(`WeaponEquipment`, `Equipment`)과 허기/갈증(`SurvivalStats`)은 참가자 소유 컴포넌트가 가진 상태라 서버가 읽을 수 없다. 참가자가 2초마다 바뀐 때만 JSON 하나(`ParticipantSaveData`의 상태 필드)로 서버에 보고하고(`StateRpc`), 서버는 `ParticipantStateRule.Sanitize`로 범위와 길이만 걸러 기록한다. 서버가 값을 의미까지 검증하지는 않는다(참가자 본인의 상태이고 다른 사람에게 영향이 없다. 위치/체력과 같은 수준의 소유자 권위).
- **복원 순서가 중요하다**: 장착은 아이템을 가지고 있어야 되므로 서버가 인벤토리 스냅샷을 먼저 보내고 침대, 상태를 잇달아 보낸다(RPC 순서 보장). 소유자는 서버의 식별 응답(`IdentifiedRpc`)을 받은 뒤에야 상태를 적용하고 보고를 시작한다. 이 응답이 없으면(응답 전에 보고하면) 접속 직후의 기본 상태(허기 100, 맨손)가 저장된 상태를 덮어쓴다.
- **불러오기도 같은 경로**: 호스트가 불러오면 접속 중인 참가자에게 `ServerRestoreClient`로 같은 순서로 다시 보내고, `IdentifiedRpc`가 `reporting`을 끄고 다시 적용하게 한다.
- **저장 코드 공용화**: 호스트의 장착/생존 저장·불러오기 코드를 `GameSaveSystem`의 공용 정적 함수로 빼서 참가자 보고와 복원이 같은 코드를 쓴다. 호스트 싱글 저장/불러오기는 리팩터링 전후로 같은 결과인지 확인했다.

## 2026-10-12 다른 접속자 아바타의 무기 모습

- **기존 코드 재사용**: 무기 자세(총 모델, 양손 2본 IK, 손가락, 반동, 화염)는 `WeaponEquipment.LateUpdate`가 만들고 카메라와 입력에 의존한다. 아바타용으로 따로 만들지 않고 같은 컴포넌트에 보기 전용 모드를 더했다. 이 모드는 `Update`(재장전 진행, 탄약), 몸 회전, `Camera.main`을 쓰지 않고 소유자가 보낸 yaw/pitch를 카메라 방향 대신 쓴다. 조준점은 어깨 중심에서 그 방향으로 `aimDistance` 앞이다.
- **설정 복사**: 아바타의 무기 컴포넌트는 플레이어 프리팹의 직렬화 값(자세 오프셋, 손 모양, 무기 목록)을 `EditorUtility.CopySerialized`로 복사한다. 플레이어 쪽 자세를 조정하면 `Build Network Assets`를 다시 실행해야 아바타에도 반영된다.
- **아바타의 무기 컴포넌트가 늘어난 부작용 점검**: `FindFirstObjectByType<WeaponEquipment>()`를 쓰는 곳(`NetworkCombat`, `NetworkInventory`)은 무기 목록 조회(`FindById`)용이라 아바타의 같은 목록이 잡혀도 결과가 같다. 나머지는 `GetComponent`(플레이어 자신)를 쓴다.
- **전송 방식**: 소유자 쓰기 `NetworkVariable`(무기 id, 조준, 재장전, yaw, pitch, 발사 횟수)이다. 늦게 들어온 접속자에게 현재 값이 저절로 가고, 발사 횟수 변화로 화염을 낸다. yaw/pitch는 0.5도 이상 바뀔 때만 보낸다.
- **시험 방법**: 임시 카메라를 아바타 옆에 만들어 `capture_game_view source=camera camera=이름`으로 그 카메라 화면을 찍는다(호스트 카메라는 어깨 너머 시점이라 다른 사람을 보기 어렵다).

## 2026-10-12 다른 접속자 아바타의 사망/부활과 맨손 공격 모습

- **사망 연출은 새로 보내지 않는다**: `NetworkCombat.IsDead`(소유자 쓰기)가 이미 모든 접속자에게 가므로, 아바타 쪽 `AvatarActionSync`가 그 값만 보고 `PlayerRespawn.Fall`과 같은 연출(`Geometry`/`Skeleton`을 0.5초에 -85도 눕힘, 부활하면 즉시 원위치)을 만든다. 눕히는 대상 이름(`Geometry`, `Skeleton`)은 플레이어 프리팹의 `PlayerRespawn.modelRoots` 두 루트와 같다. 이름이 바뀌면 `AvatarActionSync.FindModelRoots`도 바꿔야 한다.
- **펀치는 소유자 로컬 권위 그대로**: 피해 판정은 소유자 쪽 `UnarmedPunch`가 하고, 아바타의 같은 컴포넌트는 `PlayRemote`로 팔과 상체 곡선만 재생한다(피해, 소음, 소리 없음). 시작은 소유자 쓰기 `NetworkVariable`(펀치 횟수) 변화로 전달한다. 서버 권위 펀치는 이번 범위가 아니다.
- **시험 함정**: 호스트 자신의 플레이어와 참가자 아바타가 시작 위치에서 겹쳐 서 있어 캡처가 누가 누군지 헷갈릴 수 있다(호스트 아바타는 숨겨지고 호스트 플레이어가 그 자리에 보인다). 사망 연출 캡처는 시험 코드의 부활 지연(12초)이 짧으면 캡처 전에 부활해 놓친다.

## 2026-10-12 맨손 공격 서버 권위와 아바타 충돌체

- **이전에는 참가자 펀치가 서버에 닿지 않았다**: 참가자 월드의 좀비와 동물은 AI를 끈 복제본이라 거기에 준 피해는 서버 월드에 반영되지 않았고 소음도 참가자 월드에만 났다. 이제 타격 순간에 `PunchRpc`로 서버가 호스트 월드에서 판정하고 소음을 낸다. 사격(`FireRpc`)과 같은 검증 패턴(소유자 전용 RPC, 유한한 값, 거리, 간격)이다.
- **판정 코드는 한 곳**: `UnarmedPunch.ApplyHits`가 로컬(싱글, 호스트)과 서버가 같이 쓰는 구 범위 판정이고, 서버는 아바타에 복사된 보기 전용 `UnarmedPunch`의 거리/반경/높이/피해 설정을 쓴다(플레이어 프리팹 설정을 바꾸면 `Build Network Assets`로 아바타에 다시 복사해야 한다).
- **피해 배율은 신뢰하되 0~1로 제한**: 허약 상태가 줄이는 값이라 클라이언트가 속여도 더 약해질 뿐이다.
- **충돌체는 `Avatar` 레이어에 둔다**: 아바타 몸은 서로 막지만 사격, 좀비 시야와 소음 가림, 건축 조준, 좀비 화살의 레이캐스트에서는 지금처럼 보이지 않아야 한다. 레이캐스트는 층 충돌 행렬을 따르지 않으므로 그 호출들에 `AvatarBody.ExcludeMask`를 넘겨 뺀다. 새 레이캐스트가 "맞는 것"을 찾는 코드(사격, 시야, 조준)를 추가하면 이 마스크를 써야 한다. 접지 판정은 `GroundLayers`가 Default뿐이라 걸리지 않고, `BuildingSystem.FindGroundHeight`는 이미 아바타를 건너뛴다.
- **소유자 자신의 아바타 충돌체는 끈다**: 아바타가 로컬 플레이어 위치를 따라 겹치므로 켜 두면 자기 플레이어가 막힌다. 다른 접속자의 아바타만 켠다.
- **`TagManager` 변경 주의**: `Avatar` 레이어(10번)가 `ProjectSettings/TagManager.asset`에 추가됐다. 벤더 에셋이 에디터에서 `Fire`, `FxTemporaire` 태그를 자동으로 추가해 이 파일이 같이 바뀌어 보이지만 이번 변경과 무관해서 커밋에서 뺐다.

## 2026-10-12 좀비 화살과 참가자별 시작 위치 분산

- **궁수가 멀티에서 빠져 있던 이유**: `ZombieSpawnTable`이 투사체 동기화가 없어서 멀티플레이 중에는 궁수 좀비를 뽑지 않았다. 이번에 화살을 서버가 날리고 판정하며 참가자에게는 모습만 보내는 방식으로 동기화하고 제외를 없앴다. 궁수 프리팹은 네트워크 오브젝트가 됐고 `NetworkRoot`에 등록됐다(`Build Network Assets`).
- **화살은 아바타를 맞힌다(12단계의 `AvatarBody.ExcludeMask`를 화살에서만 되돌림)**: 사격, 시야, 조준은 아바타를 계속 무시하지만 화살은 몸에 맞아야 하므로 `~0`이다. 아바타에 맞으면 `PlayerTargets.Damage`가 소유자에게 전달하고, 죽은 아바타는 피해 없이 박힌다.
- **보기 전용 화살**: 서버가 날릴 때 시작 위치와 속도만 `NetworkZombie.ArrowRpc`로 보내고, 참가자 쪽 꼭두각시 궁수(컴포넌트는 꺼져 있다)가 `SpawnVisualArrow`로 같은 프리팹을 `LaunchVisual`(피해 없음)로 날리고 활 소리를 낸다. 궁수의 공격 모션은 기존 `Attacked` 알림이 이미 낸다.
- **시작 위치 분산은 고정 기준이어야 한다**: 처음 구현은 "지금 위치 + 오프셋"이라 시작 직후 충돌체에 먼저 밀려난 위치가 기준이 돼 칸이 어긋났다. 기준을 씬 시작 위치(`PlayerRespawn.StartPosition`, 바뀌지 않는다)로 고정했다. 또 서버가 아바타를 호스트 위치에 겹쳐 소환하면 호스트가 먼저 밀리므로 아바타를 처음부터 접속자의 칸에 소환한다(`MultiplayerSession.SpawnAvatar`). 번호는 비어 있는 가장 작은 양수이고 아바타가 사라질 때 반납돼 재접속하면 같은 칸을 다시 받는다. 칸은 호스트 시작 위치 둘레 2m 고리에 8명, 넘치면 1.8m씩 바깥 고리다(`StartSlotLayout`).
- **`Build Network Assets` 뒤 되돌릴 것**: `NetworkRoot.prefab`은 이 컴퓨터 전용 `NetworkPrefabsLists` GUID 한 줄만 되돌린다(통째로 되돌리면 새 프리팹 등록까지 사라진다 — 이번에 한 번 그렇게 실수했다). `NetworkWorld.prefab`은 해시만 바뀌므로 통째로 되돌려도 된다.

## 2026-10-12 참가자 위치 저장

- **보고 없이 서버가 읽는다**: 장착/생존 수치와 달리 위치는 소유자 권위 `NetworkTransform`으로 서버의 아바타에 이미 있으므로 참가자가 따로 보고하지 않는다. 저장과 접속 종료(`ParticipantStore.MakeData`)에서 아바타의 위치와 y축 회전을 읽는다. 죽어 있는 동안은 기록하지 않아 시체 자리가 아니라 시작 칸에서 시작하게 한다.
- **복원 순서**: `StartSlotRpc`(시작 칸) → `RestorePositionRpc`(저장 위치) → 침대 → `IdentifiedRpc`(장착, 생존 수치). 모두 서버에서 소유자로 가는 같은 아바타의 RPC라 순서가 지켜진다. 그래서 저장된 위치가 시작 칸 이동을 덮어쓴다.
- **땅속 좌표 방어는 싱글과 같은 규칙**: 소유자가 `GameSaveSystem.IsBuried`(`SavedPositionRule`)로 걸러, 맵이 바뀌기 전의 옛 좌표면 무시하고 시작 칸에 남는다.
- **시험 함정**: `-mp-move`의 대기를 보고 타이머(`nextStateReport`)에 걸었더니 그 값이 2초마다 갱신돼 영원히 이동하지 않았다. 시험 코드는 자체 타이머를 쓴다.

## 2026-10-12 참가자 체력 저장과 보관함 UI 조작 확인

- **체력은 상태 보고에 얹었다**: 체력 원본이 참가자 컴퓨터의 `Health`라서 9단계의 `StateRpc` 보고에 `hasHealth/health`를 더했다. 복원은 `Health.RestoreState`(이벤트 없이 값만). 죽어 있는 동안은 보고하지 않아(`hasHealth` 거짓) 다시 접속하면 가득 찬 체력이다. 사망 상태를 저장하면 접속할 때마다 죽어 시체 가방이 반복될 수 있어서이고, 대가로 "죽은 채 나가면 사망 페널티를 피한다"는 허점이 있다(인벤토리는 이미 시체 가방에 있어 잃은 것은 없다).
- **UI 조작 시험의 한계와 방법**: 이 환경에서는 별도 창의 플레이어를 마우스로 누를 수 없고 스크린샷도 찍지 못한다. 대신 `HudController`의 개발 전용 시험 코드가 실제 UI 이벤트(칸 `ClickEvent`, 버튼 `NavigationSubmitEvent`)를 보내 등록된 핸들러(`MoveStorageItem`, `TakeAllFromStorage`)와 서버 스냅샷으로 인한 화면 갱신을 검증하고 칸 수/제목/내용을 로그로 남긴다. 물리 입력, Shift+클릭, 툴팁은 이 방법으로 확인되지 않는다.
- **발견한 동작**: 인벤토리 칸을 빠르게 연달아 누르면 서버 응답 전의 화면이 같은 칸을 가리켜 두 번째 클릭이 같은 묶음을 다시 요청한다(서버가 이미 옮긴 뒤라 0개가 옮겨진다). 결과는 안전하지만 사용자에게는 "두 번째 클릭이 무시된 것"처럼 보인다.

## 2026-10-12 보관함 요청 중 칸 잠금과 멀티플레이 중 불러오기 잠금

- **잠금은 안전 문제가 아니라 사용감 문제**: 서버는 어차피 보유량만큼만 옮기므로 같은 칸을 두 번 눌러도 잘못된 상태가 되지 않는다. 다만 화면이 서버 스냅샷으로만 바뀌어 두 번째 클릭이 "무시된 것처럼" 보이던 것을, 응답 전에는 같은 아이템 칸을 흐리게 하고 중복 요청을 보내지 않는 것으로 정리했다(`PendingRequests`, 키는 아이템 `StableId`와 "모두 가져오기").
- **해제 시점**: 인벤토리나 보관함 스냅샷이 도착하면(`Modified`) 모든 잠금을 풀고 화면을 다시 그린다(`OnStorageDataChanged`). 두 스냅샷(인벤토리, 보관함)이 따로 와서 첫 도착 직후 반대쪽 격자는 잠깐 낡은 상태일 수 있으나 서버가 검증하므로 안전하다. 응답이 없으면 1.5초 뒤 스스로 풀려 영구 잠금이 되지 않는다.
- **불러오기는 사용자 경로만 막는다**: 멀티플레이 중 F9와 일시정지 메뉴의 불러오기는 호스트도 막는다(월드와 접속 중인 참가자 상태를 되돌려 건축물이 참가자 화면에서 무너졌다 다시 생기는 등 맞지 않는다). 호스트가 저장을 이어하기로 시작하는 `ContinueLoader`는 `LoadNow(player, true)`로 막지 않는다. 그래서 `ParticipantStore.LoadFromSave`의 "접속 중인 참가자에게 적용" 경로는 이어하기 시작 때 먼저 접속한 참가자에게만 쓰인다. 저장(F5)은 호스트에게 계속 허용한다.

## 2026-10-12 멀티플레이 중 저장 슬롯 흐름 정리와 자동 저장

- **슬롯 흐름은 원래 맞았고 안내가 부족했다**: 슬롯은 메인 메뉴의 새 게임/이어하기/호스트 시작(`GameSession`)에서만 정해지고 게임 중에는 바꾸는 UI가 없다. 멀티플레이 세션 동안 슬롯은 고정이며 수동 저장, 자동 저장, 참가자 데이터(참가자 id별)가 모두 호스트가 정한 슬롯 파일에 들어간다. 참가자는 슬롯이 없다. 슬롯을 바꾸려면 호스트가 메인 메뉴로 나가 다시 시작해야 하고 그러면 세션이 끝난다. 이 흐름을 일시정지 메뉴와 메인 메뉴 문구로 알려 주게만 했다(동작 변경 없음).
- **자동 저장은 새 슬롯을 만들지 않고 같은 슬롯 파일에 쓴다**(18단계에서 변경됨: 슬롯별 별도 파일 `autosave_slotN.json`으로 분리): 슬롯 3개와 이어하기 UI를 단순하게 두려고 별도 자동 저장 슬롯을 만들지 않았다. 대가로 죽음이나 실수로 망가진 진행이 10분 뒤 덮어써질 수 있다. 이를 줄이려고 죽어 있거나 플레이어가 없을 때는 타이머를 새로 시작하지 않고 조건이 맞을 때까지 미룬다(`AutoSaveRule.CanSave`).
- **참가자는 자동 저장을 하지 않는다**: 참가자 쪽에도 `AutoSave`가 만들어지지만 `IsClientOnly`면 건너뛴다(수동 저장도 막혀 있다). 시험은 참가자 빌드의 주기를 5초로 줄이고 같은 컴퓨터의 저장 파일이 생기지 않는 것으로 확인했다(호스트와 같은 `persistentDataPath`를 쓰므로 참가자가 잘못 저장하면 파일이 생긴다).
- **시험 방법**: `AutoSave.Interval`을 올릴 수 있어 에디터에서 `FindFirstObjectByType<AutoSave>().Interval = 5f`로 줄이고, 개발 빌드는 `-mp-autosave-seconds=N`을 쓴다. 실제 600초 대기는 하지 않았다.

## 2026-10-12 별도 자동 저장 파일
- 17단계의 "같은 파일 덮어쓰기" 한계를 없애려고 슬롯마다 `autosave_slot{n}.json`을 둔다. 슬롯 수와 수동 저장 흐름은 그대로이고, 자동 저장만 새 파일에 쓰며 새 게임은 두 파일을 모두 지운다. 이어하기는 메인 메뉴의 전환 버튼으로 어느 쪽을 읽을지 고른다(이유: 어느 쪽이 최신인지 자동으로 고르면 수동 저장이 숨겨진다).

## 2026-10-13 런타임 성능 최적화 시작
- 최적화는 추측하지 않고 측정 후 상위 병목만 고친다(`context-notes.md` 앞부분의 프로파일링 우선 원칙). 예산 수치는 기준선 측정 후 사용자와 확정한다.
- `ProjectSettings/QualitySettings.asset`은 작업 시작 전부터 `git status`에 수정(M)으로 보였지만 `git diff`(공백·줄바꿈 무시 포함)에는 내용 차이가 없다. 줄바꿈 또는 파일 속성 차이로 보이며 사용자 변경으로 간주해 커밋하지 않고 그대로 둔다. 품질 설정을 실제로 바꿔야 하면 먼저 사용자에게 알린다.

## 2026-10-13 성능 기준선(에디터, Main_Map, 단일 플레이)
- 측정 도구 `PerfProbe`(개발 전용)로 같은 PC 에디터에서 6초 워밍업 + 20초 측정. 에디터 수치는 빌드보다 느리고 Scene 뷰 렌더가 섞일 수 있어 **상대 비교용**이다. 셰이더 캐시가 채워진 뒤의 값이다(재시작 직후 값은 셰이더 컴파일이 섞여 버렸다).
- `world`는 플레이어를 출발점 둘레 300m 정사각형 경로로 10m/s로 옮긴다(지면은 위에서 쏜 레이로 구하고 끝나면 출발점 복귀). `horde`는 좀비 40마리를 8~35m 고리에 만든다(측정 동안 플레이어 체력은 채운다). `base`는 건축물 프리팹 300개를 격자로 놓는다.

| 상황 | 프레임 평균 | 상위 1% 최악 | 스크립트(Update) | 물리 | 애니메이터 | 드로우콜 | GC 할당/프레임 |
|---|---|---|---|---|---|---|---|
| idle (기준) | 24.1ms (41fps) | 49~53ms | 4.0ms | 0.63ms | 0.8ms | 약 4630 | 약 47KB |
| world(이동) | 24.9ms (40fps) | 52ms | 3.9ms | 1.67ms | 0.9ms | 약 5570 | 약 52KB |
| base(건축물 300) | 24.1ms (41fps) | 51ms | 4.3ms | 0.67ms | 0.8ms | 약 4735 | 약 53KB |
| horde(좀비 40) | 31.0ms (32fps) | 61ms | 5.3ms | 0.99ms | 1.4ms | 약 4737 | 약 60KB |
| mp(호스트+참가자) | 미측정 | | | | | | |

- 재현성: idle 두 번이 24.08ms/24.14ms로 거의 같다.
- 관찰(가설, 확정 아님): 아무것도 없는 idle도 24ms이고 스크립트는 4ms뿐이라 나머지(약 20ms)는 렌더 쪽으로 보인다. 드로우콜/배치가 약 4600~5500으로 매우 많다(에디터 측정이라 Scene 뷰 포함 가능). 좀비 40마리는 +7ms인데 Update/LateUpdate/물리/애니메이터 합이 약 +2.4ms이고 나머지 +4~5ms는 아직 구분 못 했다(스킨드 메시 렌더/그림자로 추정).
- 한계: `renderMs`(`Camera.Render`)는 계속 0이라 마커 이름/카테고리가 맞지 않다(렌더 비중을 직접 못 읽는다). base 시나리오는 플레이어 앞쪽에 놓아 카메라 시야에 많이 안 들어와 비용이 거의 안 늘었을 수 있다. mp는 개발 빌드(호스트+참가자)가 필요해 못 했다.

## 2026-10-13 렌더 병목 조사(에디터, 런타임에만 값을 바꿔 측정, 코드 변경 없음)
- `PerfProbe`에 마커를 추가했다: `RenderLoop.DrawSRPBatcher`(드로우 제출), `Terrain.Trees.Render`, URP 메인 카메라, 그림자 패스, 스키닝, UI Toolkit, vsync 대기. 기존 `Camera.Render`/`WaitForTargetFPS` 등은 이름이 맞지 않아 항상 0이었다.
- idle 24.1ms 중 `PlayerLoop` 21.2ms(에디터 루프/대기 아님). 드로우 제출 12.8ms, 지형 나무 3.8ms, URP 카메라 3.8ms, 스크립트 Update 4.1ms, vsync 대기 약 0.
- 렌더러 구성: 활성 MeshRenderer 약 3950개(그림자 드리움 3913개), 메인 카메라 시야 안 2366개 중 200m 안은 17개, 오클루전 베이크 데이터 없음(umbraDataSize=0), 카메라 far 5000, 점광원 53개(6개만 그림자, 카메라 근처 0), 스킨드 113개(좀비 0일 때). 지형: 1000x1000m, 나무 12555그루, treeDistance 450, billboardStart 450(나무 프로토타입 셰이더가 URP Lit로 대체돼 빌보드는 사실상 동작하지 않음: 씬을 열 때 "must use Nature/Soft Occlusion shader" 경고).
- 풀(`GrassField`)은 배치 7개, 인스턴스 234개로 비용이 작다.
- 카메라 far 5000→300으로 줄여도 프레임/드로우콜이 거의 안 변했다(24.9→25.3ms, 4650→4661). 그림자 거리 0은 약 1ms 감소. 그래서 "먼 건물이 원인" 가설과 "오클루전 베이크 우선"은 지지되지 않는다.
- 단독 끄기(깨끗한 기준 24.5ms): 지형 나무/풀 끔 17.1ms(-7.5ms, 드로우콜 -1060), `MapProps` 끔 25.3ms(변화 없음, 드로우콜 -3520, 드로우 제출 -11ms). `MapProps`는 CPU 드로우 제출이 크지만 이 환경에서는 프레임을 제한하는 병목이 아니다(GPU/렌더 스레드 쪽으로 추정, 미확인).
- 나무 거리 효과(`MapProps` 끈 상태, 기준 25.3ms): treeDistance 300 → 21.7ms, 200 → 17.3ms, 120 → 14.1ms. 시각 변화(멀리 있는 나무가 일찍 사라짐)가 있어 사용자 승인이 필요하다.
- URP 에셋은 `GraphicsPresets`가 실행 중 복제(`Instantiate`)해서 `QualitySettings.renderPipeline`으로 쓴다. GPU Resident Drawer는 에셋상 InstancedDrawing + Forward+ 이지만 `MapProps`가 SRP Batcher 경로로 3500 드로우를 만들어 실제로 작동하는지 확인하지 못했다(복제/원본 비교는 `MapProps`가 꺼진 상태에서 해 무효).
- 실험 중 함정: 런타임 실험의 복구 코드(`GameObject.Find("MapProps")`)는 비활성 오브젝트를 못 찾아 복구되지 않는다. 실험 뒤에는 Play를 종료해 상태를 초기화해야 한다.
- 에디터가 재시작되면 `Packages/manifest.json`의 `com.unity.ide.vscode`가 1.2.1에서 1.2.5로 자동 변경된다(내 변경이 아니며 커밋하지 않는다).

## 2026-10-13 지형 나무 거리 450m → 250m (최적화 1호)
- 사용자 결정: 450m를 250m 안팎(±20)으로 줄인다. 값은 `MapTerrainBuilder`(`terrain.treeDistance`)와 `Main_Map.unity`의 지형(`m_TreeDistance`, `m_TreeBillboardDistance`) 두 곳에 있어 둘 다 250으로 바꿨다(빌더만 고치면 씬이 안 바뀌고 씬만 고치면 다음 빌드에서 450으로 돌아간다). 빌보드 시작 거리는 같은 값으로 유지한다(빌보드가 되면 검은 사각형이 되는 기존 이유).
- 에디터 측정(단일 플레이, 같은 PC): idle 평균 24.1~24.9ms(상위 1% 49~53ms) → 19.1ms(35.8ms). world(이동) 평균 24.9ms(52ms) → 21.6ms(43.9ms). 지형 나무 렌더 3.9ms → 1.7ms. 230/250/270은 19.3/19.1/19.3ms로 사실상 같아서 ±20 범위는 성능 차이가 없다(250 채택).
- 시각 변화: 250m보다 먼 나무가 사라진다. 화면 캡처 비교는 저장하지 못했다(`capture_game_view`에 저장 경로 인자가 없다). 사용자가 직접 확인한다.
- 검증: EditMode 287개 통과(`unity test`로 에디터를 닫고 실행. 에디터 안에서 `run_tests`를 돌리면 에디터가 멈춘다). Play 중 게임 스크립트 예외 0건, 로그의 `InvalidOperationException ... moved past all properties`는 에디터 Inspector(IMGUI/UI Toolkit)에서 나는 기존 오류(세션 시작 때부터 있던 Devion `WriteInputManager` 계열)다. 호스트 + 참가자 실행은 이 항목에서 하지 않았다(지형 렌더 거리만 바꿨고 네트워크 코드는 변경 없음).
- 함정: 에디터가 Play가 아닐 때 `PerfProbe.Begin`을 부르면 `PerfProbe` 오브젝트가 씬에 저장된다. 씬에서 제거했고 `Begin`은 Play 중에만 시작하고 `HideFlags.DontSave`를 쓰도록 고쳤다.

## 2026-10-13 성능 예산 확정과 병목 순위
- 사용자 결정: 목표 60fps = 평균 프레임 시간 16.7ms 이하(`spec.md`와 `design.md` 반영). 기준 환경은 이 PC의 개발 빌드, 에디터 수치는 상대 비교용.
- 현재(에디터, 나무 250m 적용 후) 예산 대비:

| 상황 | 현재 | 예산 | 부족분 |
|---|---|---|---|
| idle | 19.1ms (52fps) | 16.7ms | 약 -2.4ms |
| world(이동) | 21.6ms (46fps) | 16.7ms | 약 -4.9ms |
| horde(좀비 40) | 약 26ms (약 38fps, 추정: 나무 최적화 전 31.0ms에서 idle 개선분 약 5ms를 뺀 값, 재측정 안 함) | 16.7ms | 약 -9ms |
| base(건축물 300) | idle과 비슷(건물이 시야 밖이라 비용이 안 보였다) | 16.7ms | 재측정 필요 |
| mp(호스트+참가자) | 미측정(개발 빌드 필요) | 16.7ms | 미정 |

- 병목 순위(에디터 단독 끄기/측정 근거): 1) 지형 나무(-7.5ms 끌 때, 250m로 약 -5ms 완료), 2) `MapResources` 약 6ms(원인 미확인, 드로우콜은 약 144개뿐), 3) 좀비 40마리 +7ms(스크립트·물리·애니메이터 약 2.4ms + 렌더 추정 4~5ms), 4) 건물(`MapProps`) 드로우 제출 약 11ms(CPU, 끄고 측정해도 프레임은 안 줄어 이 환경에서는 직접 병목이 아님, GPU/렌더 스레드 추정), 5) 그림자 거리 약 1ms.
- idle은 나무 외에 약 2.4ms만 더 줄이면 예산에 들어온다. world와 horde는 추가 최적화가 필요하다.

## 2026-10-13 MapResources 조사 결과(결론: 6ms 병목은 확인되지 않음)
- 구성: 오브젝트 1060개, `ResourceNode` 550개(덤불 260, 보급 상자 92, 고철 더미 87, 소나무 42, 물통 37, 웅덩이 14, 나무 10, 연못 8), 메시 렌더러 558개, 콜라이더 약 1030개(Box 677, Mesh 297, Capsule 52), `GrassClearing` 22개. 광원/파티클은 없다. `ResourceNode.Update`는 조건 하나만 확인하는 가벼운 코드이고 `ResourceSparkleField`는 0.4초마다 14m 안 노드만 갱신하므로 비용이 작다.
- 종류별로 하나씩 끄기(덤불/상자/고철/물통/연못/웅덩이/소나무): 모두 31.9~34.3ms로 기준(32.2ms)과 오차 범위. 복원 후 기준이 35.3ms로 같은 세션 안에서도 3ms 흔들린다.
- `MapResources` 전체를 켜고/끄기를 번갈아 반복: 켬 32.6/36.4/35.1ms(평균 약 34.7), 끔 30.8/31.8ms(평균 약 31.3). 평균 차이는 약 3ms이지만 같은 상태끼리도 3.8ms 흔들려 실제 효과인지 구분하지 못한다. 드로우콜 차이는 약 100개.
- 결론: 앞서 적은 "`MapResources`를 끄면 6ms 감소"는 측정 흔들림이 섞인 값이라 신뢰하지 않는다. 순위 2번 병목에서 뺀다. 코드 변경은 하지 않았다.
- 더 큰 교훈: 에디터 측정은 세션마다 값이 크게 달라진다(같은 idle이 한 세션에서 19ms, 에디터를 다시 연 다음 세션에서 32ms). 같은 세션 안에서 바로 번갈아 비교하면 큰 효과(나무 거리 450→120에서 25→14ms)는 믿을 수 있지만 5ms 이하의 효과는 에디터에서 구분하기 어렵다. 정확한 판단을 하려면 개발 빌드 측정이 필요하다.

## 2026-10-13 개발 빌드 기준선(이 PC, 지형 나무 250m 적용 상태)
- 방법: `ZGV01 > Network > Build Test Client`와 같은 옵션(Windows 64, Development)으로 `D:/work/ZGV01_PerfBuild/`에 빌드(프로젝트 밖). 메뉴 항목은 `E:/Unity/...`로 고정돼 이 PC에서는 실패해서 같은 설정의 빌드를 직접 호출했다(저장소 코드는 바꾸지 않았다). 에디터를 닫고 실행. 단일 상황은 1920x1080 창 모드, 멀티플레이는 한 PC에서 호스트와 참가자를 동시에(각 960x540). vsync는 측정 동안 끔(`PerfProbe`가 플레이어에서만 끔), 워밍업 후 20~35초 측정.
- 실행 예: `ZGV01.exe -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile 로그 -perf-scenario=idle|world|base|horde -perf-seconds=20 -perf-warmup=8 [-perf-count=N] -perf-quit`, 멀티는 `-mp-host`/`-mp-join=127.0.0.1:7777`와 `-perf-scenario=mp`를 함께(호스트 측정 구간이 참가자 접속 구간과 겹치게 호스트 워밍업을 길게).

| 상황 | 평균 프레임 | 평균 fps | 상위 1% 최악 | 최악 fps | 60fps 예산(16.7ms) |
|---|---|---|---|---|---|
| idle | 8.54ms | 117fps | 18.5ms | 54fps | 충족 |
| world(이동) | 9.66ms | 104fps | 21.3ms | 47fps | 충족 |
| base(건축물 300) | 8.85ms | 113fps | 19.5ms | 51fps | 충족 |
| horde(좀비 40) | 11.45ms | 87fps | 24.1ms | 42fps | 충족 |
| mp 호스트(참가자 1명 접속) | 10.49ms | 95fps | 24.8ms | 40fps | 충족 |
| mp 참가자 | 9.78ms | 102fps | 19.2ms | 52fps | 충족 |

- 결론 1: 이 PC의 개발 빌드에서는 평균 기준 모든 상황이 60fps 예산을 넘는다(가장 느린 horde도 87fps). 에디터 측정(idle 19~32ms)은 빌드보다 약 2.5~3배 느려서 에디터 수치로 병목을 판단하면 과대평가한다. 앞서 에디터에서 본 나무 거리 효과(-5ms)는 빌드에서는 더 작을 것이다(450m 빌드와의 직접 비교는 하지 않았다).
- 결론 2: 남은 문제는 평균이 아니라 **끊김**이다. 상위 1% 최악 프레임이 모든 상황에서 18~25ms(40~54fps)이고 최대 프레임 시간은 25~55ms까지 튄다(예: `mainMs` 최대 54ms, `updateMs` 최대 45ms). GC 할당은 프레임당 5~10KB로 작아 원인으로 보기 어렵다(미확인). 스트리밍/로딩이나 한 번에 몰리는 작업이 후보다.
- 한계: 이 PC 한 대, 한 해상도 기준이다. 더 낮은 사양, 낮은 그래픽 품질 프리셋, 좀비가 더 많은 상황은 측정하지 않았다. 호스트와 참가자를 같은 PC에서 돌려 서로 자원을 나눠 쓴 보수적 조건이다. `PerfProbe`의 `drawSrpMs` 마커는 플레이어에서 쓰레기 값(음수 거대값)이 나와 신뢰하지 않는다. 플레이어는 포커스를 못 받으면 멈추므로 `Application.runInBackground`를 켰다.

## 2026-10-13 끊김 원인: SkyCycle 환경광 갱신(0.75초 → 3초)
- 방법: `PerfProbe`가 20ms 이상인 프레임을 시각/위치/구간별 시간과 함께 기록하고(최대 80개) 16.7/20/33.3/50ms 초과 프레임 수를 낸다. 개발 빌드, 각 상황 60초.
- 발견: 거의 모든 끊김 프레임에서 `Update.ScriptRunBehaviourUpdate`가 11~19ms(평소 약 2ms)로 늘었고 렌더/물리/애니메이션/지형/UI/GC 수집은 정상이었다. 발생 간격이 약 0.75초(예: 6.8s, 7.6s, 8.3s, 18.1s, 18.9s, 19.6s)이고 117fps에서 0.75초는 약 88프레임이라 "가장 느린 1%"와 일치한다.
- 원인: `SkyCycle`이 `ambientRefreshSeconds`(0.75초)마다 `DynamicGI.UpdateEnvironment()`를 불러 큐브맵 3장을 섞는 하늘에서 환경광을 다시 계산한다. 한 번에 약 10~15ms가 걸린다. 값이 `Main_Map.unity`에도 직렬화돼 있어 코드 기본값과 씬 값을 둘 다 바꿨다.
- 조치: 0.75초 → 3초(하루가 현실 1200초이므로 게임 시간 약 4분 간격). 시각 영향: 환경광(하늘에서 오는 빛) 갱신이 덜 잦아 노을/새벽에 밝기 변화가 조금 단계적으로 보일 수 있다(직접 확인은 못 했다).
- 결과(60초, 1920x1080, vsync 끔, 전 → 후): 가만히 가장 느린 1% 18.5ms(54fps) → 14.5ms(69fps), 16.7ms 초과 54 → 17프레임, 20ms 초과 11 → 7. 이동 21.3ms(47fps) → 18.2ms(55fps), 62 → 33, 17 → 13. 좀비 40마리 24.1ms(42fps) → 18.4ms(54fps), 112 → 31, 80 → 17. 평균 프레임은 8.54 → 8.48ms, 9.66 → 9.28ms, 11.45 → 11.18ms로 거의 같다.
- 남은 끊김: 3초마다의 갱신이 여전히 10~13ms라 갱신 프레임은 20ms를 넘는다(가만히 7프레임/60초). 더 줄이려면 하늘이 변하지 않는 구간(한낮/한밤)에는 갱신을 건너뛰거나 간격을 더 늘릴 수 있다. 호스트 + 참가자 실행으로 이 변경을 따로 확인하지는 않았다.
- 검증: EditMode 287개 통과, 개발 빌드 세 상황 로그에 예외 0건.
