---
name: clean-code-style
description: Unity/C# 클린 코드 스타일 규칙. 코드 작성 및 리뷰 시 이 규칙을 따른다.
reference: .claude/references/2022_WritingCleanerCodeThatScales_EBook-v6-Final.pdf
---

# Unity/C# 클린 코드 스타일

> 원본 참고: `.claude/references/2022_WritingCleanerCodeThatScales_EBook-v6-Final.pdf`

---

## 1. C# 명명 규칙

| 대상 | 형식 | 예시 |
|------|------|------|
| 클래스, 메서드, Public 필드 | PascalCase | `PlayerController`, `CalculateTrajectory()` |
| 변수, 매개변수 | camelCase | `healthPoints`, `targetPosition` |
| Private 멤버 필드 | `_` + camelCase | `_maxHealth`, `_isDead` |
| Boolean | `is/has/can` 접두사 | `isDead`, `hasHealthPotion`, `canJump` |
| Interface | `I` 접두사 | `IDamageable`, `IInteractable` |
| Enum | 단수형 PascalCase | `WeaponType { Knife, Gun }` |
| 이벤트 | 과거형 동사 | `DoorOpened`, `PointsScored` |
| 이벤트 핸들러 | `On` + 이벤트명 | `OnDoorOpened`, `OnPointsScored` |

**금지:**

```csharp
// Bad — 약어, 한 글자, 불명확한 이름
float hp;
Vector3 pos;
GameObject obj;
string temp;

// Good
float healthPoints;
Vector3 targetPosition;
GameObject enemyObject;
string playerName;
```

---

## 2. MonoBehaviour 클래스 구성 순서

```csharp
public class PlayerController : MonoBehaviour
{
    // 1. Public Fields
    public float DamageMultiplier = 1.5f;

    // 2. [SerializeField] Private Fields
    [SerializeField] private float _maxHealth;

    // 3. Private Fields
    private bool _isDead;

    // 4. Properties
    public float MaxHealth => _maxHealth;
    public bool IsDead => _isDead;

    // 5. Events / Delegates
    public event Action<int> PointsScored;
    public event Action OnDied;

    // 6. MonoBehaviour 생명주기 (Awake → OnEnable → Start → Update → OnDisable → OnDestroy)
    private void Awake() { }
    private void Start() { }
    private void Update() { }

    // 7. Public Methods
    public void InflictDamage(float damage) { }

    // 8. Private Methods
    private void Die() { }
}
```

---

## 3. 포맷팅

### Allman 스타일 — 중괄호 항상 별도 줄

```csharp
// Good
if (!showMouse)
{
    Cursor.lockState = CursorLockMode.Locked;
}

// Bad
if (!showMouse) Cursor.lockState = CursorLockMode.Locked;
if (!showMouse) { Cursor.lockState = CursorLockMode.Locked; }
```

### 수평 간격

```csharp
// Good — 연산자 전후 공백, 콤마 뒤 공백
float result = a + b * c;
Vector3 position = new Vector3(0f, 1f, 0f);

// Bad
float result=a+b*c;
Vector3 position=new Vector3(0f,1f,0f);
```

### 수직 간격

```csharp
// Good — 관련 있는 코드는 묶고, 논리 단위 사이에 빈 줄 1개
private void Update()
{
    HandleInput();

    UpdateMovement();

    CheckGroundState();
}
```

---

## 4. Properties & Serialization

### 단일 표현식 프로퍼티

```csharp
// Good — 읽기 전용 프로퍼티는 => 사용
public float MaxHealth => _maxHealth;
public bool IsAlive => _currentHealth > 0f;
```

### Inspector 어트리뷰트

```csharp
[Header("이동 설정")]
[SerializeField, Tooltip("좌우 이동 속도 (m/s)")]
[Range(1f, 20f)]
private float _moveSpeed = 5f;

[Header("점프 설정")]
[SerializeField, Tooltip("점프 힘")]
private float _jumpForce = 10f;
```

**규칙:**
- Inspector 설명은 코드 주석 대신 `[Tooltip]` 사용
- 수치 범위가 있으면 `[Range]` 필수
- 섹션 구분은 `[Header]` 사용

---

## 5. 메서드 설계

### 파라미터 개수 제한

```csharp
// Bad — 파라미터 3개 이상
public void SetupEnemy(float health, float speed, float damage, bool isBoss) { }

// Good — 구조체/클래스로 묶기
public void SetupEnemy(EnemyData data) { }
```

### Flag 파라미터 금지

```csharp
// Bad — true/false가 무슨 의미인지 호출부에서 모름
public float GetAngle(bool inDegrees) { }
GetAngle(true);

// Good — 의도가 명확한 별도 메서드
public float GetAngleInDegrees() { }
public float GetAngleInRadians() { }
```

---

## 6. 주석 규칙

### WHY를 설명, WHAT은 코드로

```csharp
// Bad — 코드가 이미 말하는 내용을 반복
int count = 0; // 카운트를 0으로 초기화

// Good — 코드만으로 알 수 없는 이유를 설명
// 물리 엔진이 FixedUpdate 이후에 적용되므로, 한 프레임 지연 후 체크
private IEnumerator CheckGroundNextFrame() { }
```

### Public API — XML 문서 주석

```csharp
/// <summary>
/// 플레이어에게 데미지를 적용하고 사망 여부를 반환합니다.
/// </summary>
/// <param name="damage">적용할 데미지 양 (0 이상)</param>
/// <returns>이 데미지로 사망했으면 true</returns>
public bool ApplyDamage(float damage) { }
```

### [Tooltip] vs 주석 선택 기준

| 상황 | 사용 |
|------|------|
| Inspector에 보이는 필드 설명 | `[Tooltip]` |
| 코드 실행 이유, 알고리즘 설명 | `//` 주석 |
| Public 메서드/클래스 API 문서 | XML `///` |

---

## 7. 피해야 할 코드 스멜

| 스멜 | 설명 | 해결 |
|------|------|------|
| 불명확한 이름 | `data`, `info`, `temp`, `manager2` | 역할을 명확히 표현하는 이름 |
| 과도한 주석 | 나쁜 코드를 주석으로 설명 | 코드 개선이 우선 |
| Flag 파라미터 | `DoSomething(true, false)` | 별도 메서드로 분리 |
| 매직 넘버 | `if (health < 30f)` | `const float LowHealthThreshold = 30f;` |
| 중첩 조건문 | 3단계 이상 if 중첩 | 조기 반환(guard clause)으로 평탄화 |

---

## 8. 나의 코드 스타일 (개인 취향)

> 이 프로젝트는 개인 작업이므로, 아래 스타일을 위 규칙과 함께 적용한다.
> 가독성을 높이기 위한 선택으로, 널리 사용되는 스타일이다.

### 필드 열 정렬 (Column Alignment)

같은 modifier 그룹 내에서 타입명을 공백으로 맞춰 **변수명 열을 정렬**한다.

```csharp
// Good — 타입명 뒤 공백으로 변수명 열 맞춤
[SerializeField] private GridPool               _gridPool;
[SerializeField] private CameraScrollController _cameraScroll;
[SerializeField] private FloorGrid              _startGrid;
[SerializeField] private float                  _gridHeight = 10f;
[SerializeField] private int                    _totalFloors = 10;

private int  _currentFloor = 1;
private bool _isTransitioning;

// Bad — 열 정렬 없이 나열
[SerializeField] private GridPool _gridPool;
[SerializeField] private CameraScrollController _cameraScroll;
[SerializeField] private float _gridHeight = 10f;

private int _currentFloor = 1;
private bool _isTransitioning;
```

**규칙:**
- 정렬 단위는 **같은 modifier 그룹** (`[SerializeField] private` / `private` / `private readonly` 등)
- 그룹이 달라지면 정렬 기준 리셋 (빈 줄로 구분)
- `const`, `readonly` 등 키워드가 다르면 별도 그룹으로 분리

```csharp
// 그룹별 정렬 예시
private readonly Dictionary<int, FloorGrid> _floorGridMap = new();

private int  _currentFloor = 1;
private bool _isTransitioning;

private const int PreloadCount = 2;
```
