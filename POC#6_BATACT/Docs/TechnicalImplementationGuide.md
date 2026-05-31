# POC#6_BATACT 기술 구현 가이드

작성일: 2026-06-01  
목적: 현재 프로토타입 구현 구조를 정리하고, 정식 버전을 처음부터 다시 만들 때 참고할 기술 구현 방향을 정의한다.

## 1. 문서 목적

현재 `POC#6_BATACT`는 전투 아이디어를 빠르게 검증하기 위한 프로토타입이다. 따라서 지금 코드는 기능 검증과 빠른 연결을 우선한 구조이며, 정식 개발 단계에서는 그대로 확장하기보다 핵심 규칙과 플레이 감각을 가져가되 구조는 새로 설계하는 것이 좋다.

이 문서는 두 가지를 목표로 한다.

- 현재 프로토타입이 어떤 기술 구조로 구현되어 있는지 정리한다.
- 정식 프로젝트를 처음부터 만들 때 어떤 순서와 구조로 구현하면 좋은지 제안한다.

## 2. 현재 프로토타입 기술 구조

### 2.1 프로젝트 환경

- 엔진: Unity 6000.3.9f1
- 렌더 파이프라인: URP
- 장르 구조: 2D 사이드뷰 액션
- 입력: 현재는 `Input.GetKey`, `Input.GetAxisRaw`, `Input.mouseScrollDelta` 등 레거시 입력 API 중심
- 씬: `Assets/Scenes/SampleScene.unity`
- 주요 프리팹:
  - `Assets/Prefab/Player.prefab`
  - `Assets/Prefab/Enemy.prefab`
  - `Assets/Prefab/EnemyFast.prefab`
  - `Assets/Prefab/EnemyRanged.prefab`
  - `Assets/Prefab/EnemyProjectile.prefab`
  - `Assets/Prefab/DualBladeBoomerangProjectile.prefab`

### 2.2 런타임 핵심 구조

현재는 대부분의 기능이 `Assets/Scripts`에 MonoBehaviour 단위로 직접 구현되어 있다.

| 영역 | 핵심 스크립트 | 역할 |
| --- | --- | --- |
| 플레이어 이동 | `PlayerTopDownMovement` | 이동, 점프, 대시, 넉백, 방향 전환 |
| 플레이어 체력 | `PlayerHealth` | 체력, 무적, 사망, 리스폰 |
| 무기 제어 | `DualBladeWeaponController` | 무기 형태 전환, 공격 입력, 부메랑 투척/회수 |
| 공격 판정 | `WeaponHitbox2D`, `WeaponHitboxProfile` | 근접 히트박스와 공격 데이터 |
| 부메랑 | `DualBladeBoomerangProjectile` | 투척체 이동, 회수, 타격 |
| 지형 보조 | `WeaponTerrainProbe2D`, `SpearTerrainMovementAssist`, `ScissorsTerrainGripAssist`, `WeaponTerrainContactLimiter2D`, `WeaponVerticalEscapeAssist` | 무기와 지형 접촉 감지 및 보정 |
| 적 이동 | `EnemySideViewChaser` | 추격, 거리 유지, 장애물 hop, 적 간 분리 |
| 적 공격 | `EnemyMeleeAttack`, `EnemySelfDestruct`, `EnemyRangedShooter`, `EnemyProjectile2D` | 근접, 자폭, 원거리 공격 |
| 적 체력 | `PrototypeEnemyHitReceiver`, `EnemyHealthBar2D` | 피격, 사망, 체력바 |
| 웨이브 | `EnemySpawner` | 웨이브 정의, 스폰, 생존 적 관리 |
| UI | `PlayerHealthUI`, `WeaponModeSliderUI`, `WaveStatusUI`, `GameRestartUI`, `GameResultUI` | HUD, 무기 표시, 웨이브 상태, 재시작, 결과 |
| 목표 지점 | `StageGoalTrigger` | 스테이지 클리어 트리거 |

### 2.3 현재 구현 방식의 특징

현재 프로토타입은 다음 방식으로 구현되어 있다.

- 각 기능이 MonoBehaviour 안에서 직접 입력, 상태, 물리, UI를 처리한다.
- ScriptableObject는 무기 히트박스 프로필에 사용되고 있다.
- 적 웨이브는 씬 안의 `EnemySpawner` 직렬화 데이터로 관리된다.
- 일부 레퍼런스는 인스펙터 연결, 일부는 `FindFirstObjectByType`, `FindGameObjectWithTag` fallback으로 해결한다.
- 에디터 메뉴 스크립트로 프리팹/씬 구성 자동화를 일부 제공한다.

빠르게 POC를 만들기에는 적합하지만, 정식 프로젝트에서는 기능 간 의존성이 커지고 테스트가 어려워질 수 있다.

## 3. 현재 구조에서 주의할 점

### 3.1 입력 처리와 게임 로직이 붙어 있음

`DualBladeWeaponController`와 `PlayerTopDownMovement`가 직접 입력을 읽고 바로 행동을 실행한다. 정식 프로젝트에서는 입력 처리와 행동 실행을 분리하는 것이 좋다.

권장 방향:

- `PlayerInputReader`가 입력만 수집한다.
- `PlayerMotor`, `WeaponController`는 입력 결과를 받아 행동한다.
- 키보드/패드/모바일 입력을 바꾸더라도 전투 로직은 건드리지 않는다.

### 3.2 씬 탐색 의존성이 많음

`FindFirstObjectByType`, `FindGameObjectWithTag`는 프로토타입에서는 편하지만 정식 구조에서는 씬이 커질수록 불안정해진다.

권장 방향:

- 주요 참조는 프리팹/씬 조립 시 명시적으로 연결한다.
- 스테이지 시작 시 `GameContext` 또는 `SceneInstaller`가 주요 서비스를 바인딩한다.
- 태그 fallback은 디버그 편의용으로만 둔다.

### 3.3 전투 규칙이 여러 스크립트에 흩어져 있음

현재 피해, 넉백, 히트스톱, 스턴, 무적, 사망 처리가 여러 컴포넌트에 나뉘어 있다.

권장 방향:

- 피해 요청 데이터 `DamageRequest`
- 피격 결과 데이터 `DamageResult`
- 공통 인터페이스 `IDamageable`, `IHitReceiver`
- 히트스톱/카메라 쉐이크/이펙트는 별도 피드백 시스템으로 분리

### 3.4 승패 규칙이 아직 명확하지 않음

현재는 리스폰과 게임오버가 동시에 존재한다. 정식 프로젝트에서는 `GameSessionController`가 승패 상태를 단일하게 관리해야 한다.

권장 방향:

- `Playing`
- `Paused`
- `GameOver`
- `StageClear`
- `Restarting`

같은 명시적 게임 상태를 둔다.

## 4. 정식 프로젝트 구현 방향

정식 버전은 현재 프로토타입을 그대로 이어서 확장하기보다, 아래 구조로 새 프로젝트를 만드는 것을 권장한다.

### 4.1 추천 폴더 구조

```text
Assets/
  _Project/
    Art/
    Audio/
    Prefabs/
      Player/
      Enemies/
      Weapons/
      UI/
      Stage/
    Scenes/
    ScriptableObjects/
      Weapons/
      Enemies/
      Waves/
      Stages/
    Scripts/
      Core/
      Player/
      Combat/
      Weapons/
      Enemies/
      Stage/
      UI/
      Camera/
      Utilities/
    Tests/
      EditMode/
      PlayMode/
```

프로토타입 에셋과 정식 에셋을 섞지 않기 위해 `_Project` 루트 아래에 새 구조를 만드는 것이 좋다.

### 4.2 어셈블리 분리

정식 프로젝트에서는 asmdef를 나누는 것이 좋다.

```text
Game.Core
Game.Player
Game.Combat
Game.Weapons
Game.Enemies
Game.Stage
Game.UI
Game.Editor
Game.Tests
```

장점:

- 컴파일 속도 개선
- 의존성 방향 확인 쉬움
- 테스트 대상 분리 쉬움
- 에디터 코드가 런타임에 섞이는 문제 방지

### 4.3 의존성 방향

정식 구조에서는 아래 방향을 유지한다.

```text
UI -> GameSession / ViewModel
Stage -> Spawner / GameSession
Enemies -> Combat / Core
Weapons -> Combat / Core
Player -> Weapons / Combat / Core
Combat -> Core
Core -> Unity 최소 의존
```

금지에 가까운 방향:

- UI가 직접 적을 생성한다.
- 무기가 직접 결과 UI를 띄운다.
- 적이 직접 씬을 재시작한다.
- 히트박스가 특정 적 클래스를 직접 참조한다.

## 5. 정식 버전 주요 시스템 설계

### 5.1 GameSessionController

게임 전체 진행 상태를 관리한다.

책임:

- 스테이지 시작
- 일시정지
- 게임오버
- 스테이지 클리어
- 재시작
- 플레이어 사망 정책
- 웨이브 완료와 골 활성화 정책

예상 상태:

```csharp
public enum GameSessionState
{
    Booting,
    Playing,
    Paused,
    GameOver,
    StageClear,
    Restarting
}
```

정식 구현에서는 `PlayerHealth`가 직접 게임오버 UI를 띄우지 않고, 사망 이벤트를 발생시키면 `GameSessionController`가 해석한다.

### 5.2 PlayerInputReader

입력을 읽어 플레이어/무기에 전달한다.

책임:

- 이동축
- 점프 입력
- 대시 입력
- 공격 입력
- 무기 전환 입력
- 조준 방향

권장:

- 정식 버전은 Unity Input System Action Asset을 사용한다.
- 키보드/마우스와 패드를 같은 액션으로 묶는다.
- 입력은 매 프레임 구조체로 전달한다.

예시:

```csharp
public struct PlayerInputFrame
{
    public float MoveX;
    public bool JumpPressed;
    public bool JumpHeld;
    public bool DashPressed;
    public bool AttackPressed;
    public bool AttackHeld;
    public WeaponForm RequestedForm;
    public Vector2 AimDirection;
}
```

### 5.3 PlayerMotor

플레이어 물리 이동만 담당한다.

책임:

- 좌우 이동
- 점프
- 대시
- 외부 넉백
- 지면 체크
- 방향 전환

분리해야 할 것:

- 공격 입력
- 무기 상태
- UI 표시
- 게임오버 처리

### 5.4 WeaponController

무기 상태와 공격 실행을 담당한다.

추천 상태:

```csharp
public enum WeaponForm
{
    Spear,
    Boomerang,
    Scissors
}

public enum WeaponRuntimeState
{
    Ready,
    Morphing,
    Attacking,
    Thrown,
    Recovering,
    Disabled
}
```

책임:

- 무기 형태 전환
- 공격 가능 여부 판단
- 공격 커맨드 실행
- 공격 중 입력 버퍼
- 부메랑 투척/회수
- 무기 시각 상태 갱신

정식 버전에서는 `DualBladeWeaponController`처럼 모든 로직을 하나에 몰아넣기보다 아래처럼 나누는 것이 좋다.

```text
WeaponController
WeaponFormPresenter
MeleeAttackRunner
BoomerangThrowRunner
WeaponFormData
WeaponAttackData
```

### 5.5 WeaponFormData

무기 폼별 수치를 ScriptableObject로 관리한다.

포함할 데이터:

- 폼 이름
- 시각 프리셋
- 기본 공격 데이터
- 콤보 데이터
- 이동 보조 데이터
- 쿨다운
- 히트박스 크기
- 피해량
- 넉백
- 스턴 시간

예시:

```csharp
[CreateAssetMenu(menuName = "Game/Weapons/Weapon Form Data")]
public sealed class WeaponFormData : ScriptableObject
{
    public WeaponForm Form;
    public WeaponAttackData[] ComboAttacks;
    public float AutoAttackInterval;
    public Color UiColor;
}
```

### 5.6 Combat System

전투는 공격 판정과 피해 처리를 분리한다.

추천 구성:

```text
HitboxCaster
HitboxShapeData
DamageRequest
DamageResult
IDamageable
IHitReceiver
HitFeedbackDispatcher
```

흐름:

1. 공격자가 `HitboxCaster`로 충돌 후보를 찾는다.
2. 후보에서 `IHitReceiver` 또는 `IDamageable`을 찾는다.
3. `DamageRequest`를 전달한다.
4. 대상이 체력/무적/방어 상태를 계산한다.
5. `DamageResult`가 반환된다.
6. 성공한 경우 피드백 시스템이 이펙트, 사운드, 히트스톱을 실행한다.

프로토타입의 `IWeaponHitReceiver`, `HitReactionData`, `WeaponHitboxProfile`은 이 구조의 출발점으로 삼을 수 있다.

### 5.7 Enemy Architecture

적은 타입별 스크립트를 완전히 따로 만들기보다 공통 골격 위에 행동 모듈을 얹는 구조가 좋다.

추천 구성:

```text
EnemyController
EnemyMotor
EnemyHealth
EnemyPerception
EnemyAttackRunner
EnemyBrain
EnemyData
```

적 타입은 데이터와 행동 조합으로 만든다.

| 타입 | Brain | Attack |
| --- | --- | --- |
| Basic | ChaseBrain | MeleeAttack |
| Kamikaze | ChaseBrain | SelfDestructAttack |
| Ranged | KeepDistanceBrain | ProjectileAttack |

장점:

- 적 타입 추가가 쉬워진다.
- 이동/체력/피격 로직을 중복하지 않는다.
- 밸런스는 `EnemyData`에서 조정한다.

### 5.8 Wave and Stage System

현재 `EnemySpawner`의 웨이브 배열은 정식 버전에서 ScriptableObject로 빼는 것이 좋다.

추천 데이터:

```text
StageDefinition
WaveDefinition
EnemySpawnEntry
SpawnPointGroup
```

예시 구조:

```csharp
[CreateAssetMenu(menuName = "Game/Stage/Stage Definition")]
public sealed class StageDefinition : ScriptableObject
{
    public WaveDefinition[] Waves;
    public bool EnableGoalAfterAllWaves;
    public bool LoopForDebug;
}
```

웨이브 흐름:

1. StageDefinition 로드
2. WaveDefinition 순서대로 실행
3. SpawnEntry 기준으로 적 생성
4. 생존 적 수 추적
5. 웨이브 완료 이벤트 발행
6. 모든 웨이브 완료 시 골 활성화 또는 클리어 처리

### 5.9 UI Architecture

정식 버전에서는 UI가 게임 오브젝트를 직접 찾지 않도록 한다.

권장:

- UI는 `GameSessionController`, `PlayerHealth`, `WeaponController`, `WaveDirector`의 이벤트를 구독한다.
- UI 텍스트 갱신은 전용 Presenter가 담당한다.
- 결과 UI는 `GameSessionState`만 보고 표시한다.

예시:

```text
HealthHudPresenter
WeaponHudPresenter
WaveHudPresenter
ResultScreenPresenter
RestartPresenter
```

### 5.10 Camera System

현재는 기본 추적 카메라 구조다. 정식 버전에서는 다음 기능을 분리해서 관리한다.

- 플레이어 추적
- 이동 방향 look-ahead
- 전투 중 카메라 흔들림
- 보스/웨이브 이벤트 카메라 연출
- 스테이지 경계 제한

추천:

- Cinemachine 사용
- 카메라 흔들림은 Combat Feedback에서 이벤트로 호출

## 6. 물리/레이어 정책

정식 프로젝트 시작 시 레이어 정책을 먼저 고정한다.

추천 레이어:

| Layer | 용도 |
| --- | --- |
| Player | 플레이어 몸체 |
| PlayerWeapon | 플레이어 무기 시각/보조 콜라이더 |
| PlayerHitbox | 플레이어 공격 판정 |
| Enemy | 적 몸체 |
| EnemyHitbox | 적 공격 판정 |
| EnemyProjectile | 적 투사체 |
| Ground | 지형 |
| TriggerZone | 골, 이벤트 트리거 |
| Pickup | 아이템 |

정책:

- 공격 판정은 가능하면 trigger로 둔다.
- 몸체 충돌과 공격 판정을 분리한다.
- 무기 시각 콜라이더와 실제 공격 히트박스를 분리한다.
- 레이어 매트릭스는 문서화하고 초기화 에디터 툴로 자동 설정한다.

## 7. 데이터 기반 구현

정식 개발에서는 수치를 코드에 박아두기보다 ScriptableObject로 분리한다.

필요 데이터:

- `PlayerMovementData`
- `PlayerHealthData`
- `WeaponFormData`
- `WeaponAttackData`
- `EnemyData`
- `ProjectileData`
- `WaveDefinition`
- `StageDefinition`
- `UiThemeData`

장점:

- 밸런스 수정이 빠르다.
- 코드 변경 없이 튜닝 가능하다.
- 테스트용/실게임용 데이터를 분리할 수 있다.
- 디자이너가 인스펙터에서 조정하기 쉽다.

## 8. 정식 개발 순서 제안

### 1단계: 프로젝트 골격

- 새 Unity 프로젝트 또는 `_Project` 폴더 구조 생성
- URP/2D 설정
- 레이어/태그 정책 설정
- asmdef 구성
- 기본 씬과 테스트 씬 생성

### 2단계: 플레이어 조작

- Input System 액션 정의
- PlayerInputReader 구현
- PlayerMotor 구현
- 지면 체크, 점프, 대시 구현
- 카메라 추적 연결

완료 기준:

- 공격 없이도 이동이 기분 좋게 느껴진다.
- 지면/벽/플랫폼에서 물리 튐이 적다.

### 3단계: 전투 기반

- DamageRequest/DamageResult 정의
- IDamageable/IHitReceiver 정의
- HitboxCaster 구현
- HealthComponent 구현
- 피격/무적/넉백/사망 이벤트 구현

완료 기준:

- 더미 적을 공격하면 피해와 넉백이 안정적으로 적용된다.

### 4단계: 무기 3폼

- WeaponController 구현
- WeaponFormData 설계
- 창 공격 구현
- 부메랑 투척/회수 구현
- 가위 공격 구현
- 폼 전환 UI 연결

완료 기준:

- 세 폼의 역할 차이가 플레이어에게 바로 느껴진다.

### 5단계: 적 3종

- EnemyController/EnemyMotor 구현
- Basic Melee 구현
- Kamikaze 구현
- Ranged 구현
- EnemyData로 수치 분리

완료 기준:

- 각 적 타입이 서로 다른 대응을 요구한다.

### 6단계: 웨이브/스테이지

- StageDefinition/WaveDefinition 구현
- WaveDirector 구현
- SpawnPointGroup 구현
- 웨이브 완료 이벤트 구현
- 골 활성화/클리어 조건 구현

완료 기준:

- 1개 스테이지를 시작부터 클리어까지 플레이할 수 있다.

### 7단계: UI/피드백

- 체력 UI
- 무기 UI
- 웨이브 UI
- 결과 UI
- 재시작/일시정지
- 타격 이펙트/사운드/카메라 흔들림

완료 기준:

- 플레이어가 현재 상황을 UI와 피드백만으로 이해할 수 있다.

### 8단계: 테스트와 안정화

- EditMode 테스트
- PlayMode 테스트
- 프리팹 레퍼런스 검증 툴
- 씬 빌드 검증
- 콘솔 오류 없는 플레이 테스트

완료 기준:

- 핵심 루프를 반복 실행해도 오류가 없다.

## 9. 프로토타입에서 정식 버전으로 가져갈 것

가져갈 것:

- 무기 3폼 컨셉
- 창/부메랑/가위의 역할 구분
- 적 3종 역할 구분
- 웨이브 기반 전투 구조
- 현재 수치의 초깃값
- 지형 접촉 보조 아이디어
- UI 구성 방향

그대로 가져가지 않는 것이 좋은 것:

- `FindFirstObjectByType` 중심 참조 구조
- 입력과 행동이 한 클래스에 붙어 있는 구조
- 거대한 단일 `DualBladeWeaponController`
- 씬 직렬화에 직접 박힌 웨이브 데이터
- 디버그 로그가 기본으로 켜진 공격 판정
- 리스폰/게임오버 규칙이 섞인 결과 처리

## 10. 정식 버전의 MVP 구현 기준

정식 버전 첫 MVP는 아래 기준을 만족하면 된다.

- 플레이어가 이동/점프/대시를 자연스럽게 할 수 있다.
- 창/부메랑/가위 3폼을 전환하고 각각 공격할 수 있다.
- 기본 적, 자폭 적, 원거리 적이 등장한다.
- 3개 웨이브가 순서대로 진행된다.
- 모든 웨이브 클리어 후 골이 활성화된다.
- 골 도달 시 클리어 화면이 나온다.
- 사망 시 게임오버 또는 정해진 리스폰 규칙이 일관되게 적용된다.
- 재시작이 가능하다.
- 콘솔 오류 없이 3회 이상 반복 플레이 가능하다.

## 11. 구현 시 우선 결정해야 할 사항

정식 프로젝트 시작 전에 아래 결정이 필요하다.

1. 사망 시 즉시 게임오버인가, 리스폰인가?
2. 웨이브를 모두 잡아야 클리어인가, 골 도달이 클리어인가?
3. 무기 전환은 즉시 전환인가, 전환 딜레이가 있는가?
4. 부메랑을 던진 동안 다른 무기 전환을 예약할 수 있는가?
5. 창/가위의 지형 보조는 핵심 액션인가, 보조 편의 기능인가?
6. 적은 물리 기반으로 움직일 것인가, 경로/상태 기반으로 움직일 것인가?
7. 테스트용 무한 웨이브와 실제 스테이지 웨이브를 어떻게 분리할 것인가?

이 결정이 내려지면 코드 구조가 훨씬 안정적으로 잡힌다.

## 12. 권장 구현 원칙

- MonoBehaviour는 Unity 연결과 생명주기만 담당하고, 핵심 규칙은 작은 클래스로 분리한다.
- 수치는 ScriptableObject로 뺀다.
- 공격 판정, 피해 계산, 피드백 실행을 분리한다.
- UI는 게임 오브젝트를 직접 찾지 않고 이벤트를 구독한다.
- 태그/레이어 정책은 코드와 문서로 동시에 관리한다.
- 프로토타입 코드는 참고 자료로 사용하고, 정식 버전에는 필요한 로직만 재설계해서 옮긴다.
- 먼저 1개 스테이지를 끝까지 완성하고, 그 다음 확장한다.

## 13. 요약

현재 프로토타입은 아이디어 검증에는 충분한 구현량을 갖고 있다. 하지만 정식 게임으로 확장하려면 새 구조로 다시 만드는 것이 더 안전하다.

정식 버전의 핵심은 다음 네 가지다.

- 입력, 이동, 전투, UI, 진행 상태를 분리한다.
- 무기/적/웨이브 수치를 데이터화한다.
- GameSessionController가 승패와 진행 흐름을 단일하게 관리한다.
- 프로토타입에서 검증된 플레이 감각만 가져오고 코드는 필요한 부분만 재구현한다.

이 방향으로 시작하면 현재 POC의 장점은 살리면서, 나중에 스테이지/적/무기/연출을 확장해도 무너지지 않는 구조를 만들 수 있다.
