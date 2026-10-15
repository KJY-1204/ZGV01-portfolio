# system.md

이 문서는 Unity 6 TPS 좀비 생존 프로젝트의 기술 아키텍처와 시스템 경계를 정의한다. 실제 패키지 버전과 현재 구현은 저장소의 `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, 코드와 씬을 최종 근거로 한다.

## 1. 기술 기준

- 엔진은 Unity 6 계열을 사용한다.
- 제공된 TPS 자료의 기준 버전은 Unity 6000.4.3f1이지만 실제 프로젝트 버전을 임의로 변경하지 않는다.
- 렌더링은 URP를 기본으로 한다.
- 신규 입력은 Input System을 기본으로 한다.
- 카메라는 실제 패키지 상태를 확인한 뒤 Cinemachine을 우선 검토한다.
- 런타임 UI는 UI Toolkit을 기본 후보로 한다.
- GameObject/MonoBehaviour 기반 멀티플레이는 Netcode for GameObjects를 우선 검토한다.
- Lobby, Relay, Matchmaker, Dedicated Server 같은 서비스는 필요한 단계에서만 도입한다.
- Addressables는 콘텐츠 스트리밍이나 원격 콘텐츠가 실제로 필요할 때 도입한다.

패키지 추가, 제거, 버전 업그레이드는 실제 `manifest.json`과 호환성을 먼저 확인하고 별도 논리 커밋으로 수행한다.

## 2. 아키텍처 핵심 원칙

게임 상태 변경과 화면 표현을 분리한다.

```text
Input
  ↓
Intent / Request
  ↓
Gameplay Rule / Authority Validation
  ↓
Authoritative State Change
  ↓
Domain Event / Replicated State
  ↓
Presentation, VFX, Audio, UI
```

싱글플레이에서는 같은 게임 규칙을 로컬 권위가 실행한다. 멀티플레이에서는 서버 권위가 실행한다. 네트워크 유무 때문에 데미지, 인벤토리, 제작, 건축 규칙을 복제하지 않는다.

## 3. 책임 계층

### 3.1 Data Definition

변하지 않는 게임 정의를 관리한다.

예시는 ItemDefinition, WeaponDefinition, ZombieDefinition, RecipeDefinition, BuildableDefinition, DropTable이다.

- ScriptableObject 사용을 우선 검토한다.
- 안정적인 문자열 또는 GUID 기반 ID를 가진다.
- 런타임 수량, 체력, 내구도, 소유권을 정의 에셋에 기록하지 않는다.
- 네트워크와 저장에서는 에셋 참조 자체보다 안정적인 ID를 전달한다.

### 3.2 Gameplay Domain

게임 규칙과 상태 전이를 담당한다.

- 데미지 계산.
- 체력과 사망.
- 인벤토리 트랜잭션.
- 드랍 결정.
- 자원 채집.
- 제작.
- 건축 배치 검증.
- 저장 가능한 월드 상태.

가능한 한 순수 C# 로직으로 분리해 EditMode 테스트가 가능하게 만든다.

### 3.3 Authority

어떤 요청이 실제 게임 결과가 될 수 있는지 검증한다.

멀티플레이에서 다음 결과는 서버가 확정한다.

- 데미지와 사망.
- 탄약 소모의 최종 결과.
- 아이템 드랍 RNG와 획득.
- 인벤토리 이동과 수량 변경.
- 자원 노드 채집 결과.
- 제작과 재료 차감.
- 건축물 생성, 파괴, 소유권.
- 좀비 AI 최종 판단.
- 플레이어 성장과 월드 저장.

싱글플레이에서는 같은 검증 규칙을 로컬에서 실행한다.

### 3.4 Networking Adapter

게임 규칙을 네트워크 전송 방식과 분리한다.

- 클라이언트 요청을 권위 계층에 전달한다.
- 권위 상태를 클라이언트로 복제한다.
- 지속 상태와 순간 이벤트를 구분한다.
- NetworkVariable은 장시간 유지되어야 하는 동기화 상태에 사용한다.
- RPC는 발사 피드백, 알림처럼 순간 이벤트에 사용한다.
- 네트워크 콜백 안에 핵심 게임 규칙을 직접 묻어두지 않는다.

### 3.5 Presentation

카메라, 애니메이션, VFX, 오디오, UI를 담당한다.

- 로컬 플레이어 입력과 카메라.
- 조준점과 화면 흔들림.
- 총구 화염과 피격 시각 효과.
- UI 애니메이션과 알림.
- 원격 캐릭터 표시와 보간.

Presentation은 권위 상태를 읽고 보여주며 직접 게임 결과를 확정하지 않는다.

## 4. 권장 프로젝트 구조

실제 저장소 구조가 이미 존재하면 먼저 읽고 최소 변경으로 적용한다.

```text
Assets/
  _Project/
    Art/
    Audio/
    Data/
      Items/
      Weapons/
      Zombies/
      Recipes/
      Building/
      DropTables/
    Prefabs/
      Characters/
      Weapons/
      World/
      Building/
      Effects/
      UI/
      Prototypes/
    Scenes/
      Main/
      Prototype/
      Test/
    Scripts/
      Core/
      Input/
      Player/
      Combat/
      AI/
      Items/
      Inventory/
      Survival/
      Crafting/
      Building/
      World/
      Save/
      Networking/
      UI/
      Utilities/
    UI/
      UXML/
      USS/
      Themes/
    VFX/
    Materials/
    Tests/
      EditMode/
      PlayMode/
  ThirdParty/
```

외부 에셋은 가능한 한 `ThirdParty` 또는 공급자 전용 폴더에 두고 직접 수정하지 않는다. 프로젝트 코드에서 어댑터 또는 래퍼로 연결한다.

## 5. 플레이어 시스템

### 입력

Input Actions를 중심으로 Move, Look, Aim, Fire, Reload, Sprint, Interact, Inventory, BuildMode 같은 액션을 구성한다.

UI 또는 건축 모드에서는 Action Map을 분리하거나 입력 컨텍스트를 명확히 전환한다. 원격 플레이어 오브젝트는 로컬 `PlayerInput`을 읽지 않는다.

### 이동

입력 수집과 실제 이동 처리를 분리한다. 카메라 기준 이동 벡터와 캐릭터 상태를 계산하되 네트워크 패키지 API에 이동 규칙 전체를 묶지 않는다.

### 카메라

카메라는 로컬 플레이어 전용이다. 원격 플레이어 프리팹에서 카메라, AudioListener, HUD 입력은 비활성화되어야 한다.

## 6. 전투 시스템

### 사격 흐름

```text
Local Input
→ Fire Request
→ Local Immediate Feedback
→ Authority Validation
→ Hit Evaluation
→ Damage Application
→ Confirmed State/Event
→ Replicated Hit/VFX Feedback
```

- 클라이언트가 `TakeDamage`를 직접 호출해 결과를 확정하지 않는다.
- 히트스캔 무기는 카메라 조준점과 총구에서 실제 목표까지의 장애물을 함께 확인한다.
- 발사 가능 상태, 연사 간격, 탄약, 재장전 상태는 권위 측에서 검증할 수 있어야 한다.
- 총구 화염과 반동은 예측 표현으로 즉시 재생할 수 있다.

### 체력

체력 변경 진입점을 제한한다. 여러 코드가 `CurrentHP -= value`처럼 필드를 직접 수정하지 않는다.

`ApplyDamage`, `Heal`, `Kill`처럼 의미 있는 경로를 통해 변경하고 이벤트를 발행한다.

## 7. 아이템과 인벤토리

### 데이터 모델

```text
ItemDefinition
  StableId
  DisplayName
  Icon
  Category
  MaxStack
  BaseProperties

ItemStack
  DefinitionId
  Quantity
  OptionalInstanceData
```

무기 내구도나 랜덤 옵션이 필요해지면 ItemInstance를 별도 도입한다. 모든 아이템을 처음부터 복잡한 인스턴스로 만들지 않는다.

### 인벤토리 연산

Add, Remove, Move, Split, Merge, Swap을 한 시스템에서 검증한다.

UI가 슬롯 배열을 직접 수정하지 않는다. 멀티플레이 클라이언트는 요청만 보내고 서버 확정 결과를 표시한다.

## 8. 드랍과 전리품

드랍 테이블은 데이터로 정의한다.

```text
DropTable
  Entries[]
    ItemDefinitionId
    Weight
    MinQuantity
    MaxQuantity
    Conditions
```

좀비 사망 흐름은 다음을 따른다.

```text
Zombie Death Confirmed
→ Authority Rolls DropTable
→ Loot State Created
→ Network/Local World Spawn
→ Player Pickup Request
→ Authority Validates Distance/Ownership/Capacity
→ Inventory Transaction
→ Loot Removed
```

멀티플레이에서 클라이언트 RNG 결과를 신뢰하지 않는다.

## 9. 좀비 AI

- NavMeshAgent와 명시적인 상태 머신을 기본 후보로 한다.
- 감지, 목표 선택, 경로 선택, 공격 시도, 사망을 분리한다.
- 멀티플레이에서는 서버가 AI 상태 머신과 NavMesh 의사결정을 실행한다.
- 클라이언트는 위치, 회전, 애니메이션, 피격 반응을 표시한다.
- 대규모 좀비 수가 필요해질 때 감지 주기, AI tick, LOD, 비활성화 거리, 스폰 디렉터를 별도로 최적화한다.

## 10. 자원 채집과 제작

자원 노드는 Definition과 RuntimeState를 분리한다.

```text
ResourceNodeDefinition
  ResourceType
  MaxYield
  RespawnRule

ResourceNodeState
  StableWorldId
  RemainingYield
  RespawnAt
```

제작 요청은 RecipeId와 수량을 전달하고 권위 측에서 재료, 조건, 공간을 다시 검증한다.

## 11. 건축 시스템

배치 프리뷰는 클라이언트 로컬 표현이다. 최종 설치는 권위 결과다.

```text
Select Buildable
→ Local Ghost Preview
→ Snap/Rotate
→ Build Request
→ Authority Placement Validation
→ Resource Transaction
→ Spawn Building State
→ Replicate/Save
```

건축물 저장 상태에는 최소한 StableWorldId, BuildableId, Transform, Health, Ownership 정보가 필요하다.

초기에는 복잡한 구조 안정도나 연결 그래프를 만들지 않는다.

## 12. 저장 시스템

정적 정의, 런타임 상태, 저장 DTO를 분리한다.

- ScriptableObject를 런타임 세이브 파일처럼 사용하지 않는다.
- Unity 인스턴스 ID를 영속 ID로 사용하지 않는다.
- 세이브 루트에 `schemaVersion`을 둔다.
- 역직렬화 시 존재하지 않는 DefinitionId를 안전하게 보고한다.
- 싱글플레이 저장은 로컬 권위가 수행한다.
- 멀티플레이 저장은 서버 또는 호스트 권위가 수행한다.
- 클라이언트 로컬 저장은 그래픽, 오디오, 키 설정 같은 개인 옵션 중심으로 제한한다.

## 13. UI Toolkit 구조

UI는 Model/ViewModel 또는 Presenter/View 식의 중간 계층을 둔다. 이름은 실제 코드 스타일에 맞춘다.

```text
Gameplay State
→ UI Data Source / ViewModel
→ Runtime Binding
→ UXML Visual Tree
→ USS Theme
```

- 게임 도메인 오브젝트를 UI가 직접 수정하지 않는다.
- 빈번한 변환은 데이터 소스에서 미리 계산하고 DataConverter는 가볍게 유지한다.
- Inventory, Storage, Crafting, PlayerList는 ListView를 우선 검토한다.
- 공통 색, 간격, 글꼴 크기는 USS 변수와 클래스에 모은다.
- 인라인 스타일 남발을 피한다.
- 화면 전체를 매 프레임 강제 갱신하지 않는다.

## 14. URP와 렌더링

- URP 설정은 타깃 플랫폼과 성능 예산을 기준으로 관리한다.
- LOD, 정적/동적 오클루전, GPU Resident Drawer, GPU 오클루전은 실제 씬에서 Profiler와 Rendering Debugger로 검증한다.
- GPU 오클루전은 항상 성능을 높인다고 가정하지 않는다.
- GPU Resident Drawer 호환성을 깨는 MaterialPropertyBlock 사용과 렌더러 조건을 확인한다.
- APV, 데칼, 후처리, 볼류메트릭은 품질 목표와 성능 측정이 있을 때 도입한다.

## 15. VFX와 오디오 이벤트

권위 시스템은 `ZombieDied`, `ShotConfirmed`, `BuildPlaced` 같은 의미 이벤트를 만든다. 표현 시스템은 이 이벤트로 파티클, VFX Graph, 사운드를 재생한다.

- 네트워크로 파티클 개별 위치를 계속 전송하지 않는다.
- 반복 VFX는 풀링을 우선 검토한다.
- VFX Graph bounds, capacity, 출력 복잡도를 프로파일링한다.
- 화면 밖 또는 멀리 있는 효과는 비용을 줄인다.

## 16. 월드 스트리밍

첫 수직 슬라이스는 단일 작은 플레이 영역으로 만든다.

월드가 커졌을 때만 다음 구조를 검토한다.

- Bootstrap/Persistent scene.
- POI 또는 청크 additive scenes.
- Addressables 기반 비동기 로드.
- 서버 권위 월드 상태 레지스트리.
- 플레이어 주변 interest 영역에 따른 오브젝트 활성화와 네트워크 스폰.

스트리밍 시스템을 게임 초기부터 완성하려 하지 않는다.

## 17. 테스트 하네스

### EditMode

- 데미지 계산.
- 드랍 가중치와 조건.
- 인벤토리 Add/Remove/Move/Split/Merge.
- 제작 재료 검증.
- 건축 배치 순수 검증 함수.
- 저장 DTO 직렬화와 버전 마이그레이션.

### PlayMode

- 플레이어 이동과 입력 연결.
- 사격과 총구 장애물.
- 좀비 감지, 추적, 공격, 사망.
- 드랍 생성과 획득.
- UI 바인딩과 주요 화면 열기/닫기.
- 건축 프리뷰와 실제 설치.

### Multiplayer

- Host + Client 또는 지원되는 Multiplayer Play Mode로 최소 2 인스턴스를 테스트한다.
- 소유하지 않은 플레이어가 로컬 입력을 읽지 않는지 확인한다.
- 클라이언트가 직접 데미지, 아이템, 건축 상태를 조작할 수 없는지 확인한다.
- 접속, 중도 이탈, 재접속 시 핵심 상태가 깨지지 않는지 단계적으로 검증한다.

## 18. 테스트 씬

초기에는 목적별 작은 테스트 씬을 유지한다.

```text
Test_Input
Test_Camera
Test_Combat
Test_ZombieAI
Test_Loot
Test_Inventory
Test_Crafting
Test_Building
Test_UI
Test_Network
Prototype_CoreLoop
```

모든 기능을 메인 월드에서만 테스트하지 않는다.

## 19. 성능 예산 운영

초기에는 임의의 숫자를 품질 기준으로 고정하지 않는다. 타깃 플랫폼이 정해지면 다음을 문서화한다.

- 목표 FPS와 프레임 시간.
- CPU Main Thread 예산.
- GPU 예산.
- 메모리 예산.
- 활성 좀비 수.
- 활성 네트워크 오브젝트 수.
- VFX 파티클과 오버드로우 범위.

최적화는 Profiler, Profile Analyzer, Frame Debugger, Rendering Debugger, Network Simulator 같은 측정 결과를 기반으로 한다.

## 20. 의존성 방향

권장 의존성 방향은 다음과 같다.

```text
Definitions → Domain → Authority → Networking
                    ↘ Events/Snapshots → Presentation → UI
```

- Domain은 UI Toolkit을 몰라야 한다.
- Domain은 NGO RPC를 직접 몰라도 동작할 수 있어야 한다.
- UI는 Save 파일 포맷을 직접 수정하지 않는다.
- VFX는 데미지 계산의 원인이 되지 않는다.
- 네트워크 전송 형식이 ItemDefinition의 내부 구조 전체를 노출하지 않게 한다.

이 경계를 유지하면 싱글플레이와 멀티플레이를 같은 게임으로 유지하면서 네트워크, UI, 표현 방식을 교체하기 쉬워진다.
