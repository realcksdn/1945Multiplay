using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 획득 가능한 아이템의 물리 동작을 제어하는 클래스
/// 생성 시 랜덤한 대각선 방향으로 날아가며 플레이어가 먹을 때까지 무한히 튕깁니다
/// </summary>
public class ItemPickup : NetworkBehaviour
{
    #region 변수 선언
    [Header("아이템 발사 설정")]
    [Tooltip("아이템이 발사될 때 적용될 물리적 힘의 크기")]
    public float launchForce = 5f;

    [Header("무한 튕김 설정")]
    [Tooltip("속도가 이 값 이하로 떨어지면 속도를 복원합니다")]
    public float minSpeed = 3f;

    [Tooltip("속도 복원 시 적용할 속도")]
    public float restoreSpeed = 5f;

    // 아이템의 물리 연산을 담당하는 Rigidbody2D 컴포넌트 참조
    private Rigidbody2D rigidBody2D = null;
    #endregion

    /// <summary>
    /// 아이템 생성 시 초기화 및 랜덤 대각선 방향으로 발사
    /// </summary>
    // void Start()
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // 현재 게임 오브젝트에 붙어있는 Rigidbody2D 컴포넌트를 가져와서 변수에 저장
        rigidBody2D = GetComponent<Rigidbody2D>();

        // Rigidbody2D가 있는지 확인 후 발사
        if (rigidBody2D != null)
        {
            // 랜덤한 대각선 방향으로 발사
            LaunchRandomDiagonal();
        }
        else
        {
            Debug.LogError("ItemPickup: Rigidbody2D 컴포넌트가 필요합니다!");
        }
    }

    /// <summary>
    /// 매 프레임 아이템의 속도를 확인하고 너무 느려지면 속도를 복원합니다
    /// 이를 통해 아이템이 멈추지 않고 계속 튕기도록 합니다
    /// </summary>
    void FixedUpdate()
    {
        // Rigidbody2D가 있는지 확인
        if (rigidBody2D == null) return;

        // 현재 속도의 크기를 계산 (방향 무시, 순수 속력)
        float currentSpeed = rigidBody2D.linearVelocity.magnitude;

        // 속도가 최소 속도보다 느려졌으면 속도를 복원
        if (currentSpeed < minSpeed)
        {
            // 현재 이동 방향을 유지하면서 속도만 증가
            Vector2 currentDirection = rigidBody2D.linearVelocity.normalized;

            // 방향이 유효한지 확인 (정지 상태가 아닌지)
            if (currentDirection.magnitude > 0.1f)
            {
                // 현재 방향으로 복원 속도 적용
                rigidBody2D.linearVelocity = currentDirection * restoreSpeed;
            }
            else
            {
                // 완전히 멈춘 경우 랜덤 방향으로 다시 발사
                LaunchRandomDiagonal();
            }
        }
    }

    /// <summary>
    /// 아이템을 랜덤한 대각선 방향으로 발사합니다
    /// 50% 확률로 왼쪽 위 또는 오른쪽 위로 날아갑니다
    /// </summary>
    void LaunchRandomDiagonal()
    {
        // 랜덤으로 왼쪽(-1) 또는 오른쪽(1) 선택
        float horizontalDirection = Random.Range(0, 2) == 0 ? -1f : 1f;

        // X축: 랜덤 좌우 방향, Y축: 항상 위쪽
        // launchForce를 양쪽에 곱해서 대각선 45도 방향으로 발사
        Vector2 force = new Vector2(horizontalDirection * launchForce, launchForce);

        // ForceMode2D.Impulse: 질량을 고려한 순간적인 힘 (한 번만 적용)
        rigidBody2D.AddForce(force, ForceMode2D.Impulse);

        // 아이템이 회전하며 날아가도록 랜덤 회전력 추가
        float randomTorque = Random.Range(-100f, 100f);
        rigidBody2D.AddTorque(randomTorque);
    }

    /// <summary>
    /// 화면 밖으로 나가면 자동으로 삭제
    /// </summary>
    private void OnBecameInvisible()
    {
        // 화면 밖으로 나간 아이템은 메모리 절약을 위해 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// Trigger 충돌 (플레이어 획득용)
    /// </summary>
    /// <param name="collision">충돌한 콜라이더 정보</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어와 충돌 시 획득 처리는 PlayerController에서 수행
        // 여기서는 추가 로직 없음
    }
}

/*
 * ========================================
 * 무한 튕김 원리
 * ========================================
 * 
 * 1. FixedUpdate()에서 매 물리 프레임마다 속도 확인
 * 2. 속도가 minSpeed(3) 이하로 떨어지면
 * 3. 현재 방향을 유지하면서 속도를 restoreSpeed(5)로 복원
 * 4. 이를 통해 아이템이 멈추지 않고 계속 튕김!
 * 
 * ========================================
 * Unity Inspector 설정 가이드
 * ========================================
 * 
 * === 1. ItemPickup (Script) 설정 ===
 * 
 * Launch Force: 5 ⭐
 *   - 초기 발사 힘
 * 
 * Min Speed: 3 ⭐
 *   - 이 속도 이하로 떨어지면 속도 복원
 *   - 낮을수록 자주 복원 (더 활발히 튕김)
 * 
 * Restore Speed: 5 ⭐
 *   - 속도 복원 시 적용할 속도
 *   - 높을수록 빠르게 튕김
 * 
 * ========================================
 * === 2. Rigidbody 2D 설정 (중요!) ===
 * ========================================
 * 
 * Body Type: Dynamic ⭐
 * Material: ItemBouncy ⭐
 * Mass: 1
 * Linear Drag: 0 ⭐⭐⭐ (중요! 0으로 설정!)
 * Angular Drag: 0 ⭐⭐⭐ (중요! 0으로 설정!)
 * Gravity Scale: 1
 * Collision Detection: Continuous ⭐
 * Sleeping Mode: Never Sleep ⭐⭐⭐ (중요!)
 * 
 * ⚠️ 주의:
 * - Linear Drag: 0 (공기 저항 없음)
 * - Angular Drag: 0 (회전 저항 없음)
 * - Sleeping Mode: Never Sleep (절대 멈추지 않음)
 * 
 * ========================================
 * === 3. Circle Collider 2D - 2개 필요! ===
 * ========================================
 * 
 * 【첫 번째 Collider - 물리 충돌용】
 * - Is Trigger: □ (체크 해제!) ⭐⭐⭐
 * - Radius: 0.4
 * 
 * 【두 번째 Collider - 플레이어 획득용】
 * - Is Trigger: ✓ (체크!) ⭐⭐⭐
 * - Radius: 0.6
 * 
 * ========================================
 * === 4. Physics Material 2D (ItemBouncy) ===
 * ========================================
 * 
 * Friction: 0 ⭐⭐⭐ (마찰 없음!)
 * Bounciness: 1.0 ⭐⭐⭐ (완벽한 탄성!)
 * 
 * ⚠️ 중요:
 * - Friction: 0 (마찰 없어야 에너지 손실 없음)
 * - Bounciness: 1.0 (100% 반발력)
 * 
 * ========================================
 * === 5. 벽 설정 (4개 모두 필요!) ===
 * ========================================
 * 
 * LeftWall, RightWall, TopWall, BottomWall:
 * - Box Collider 2D
 * - Is Trigger: □ (체크 해제!) ⭐⭐⭐
 * - Rigidbody 2D: 없음 (고정된 벽)
 * 
 * ⚠️ 벽이 없으면 아이템이 화면 밖으로 나갑니다!
 * 
 * ========================================
 * === 무한 튕김 조절 ===
 * ========================================
 * 
 * 【더 활발하게 튕기게】
 * Min Speed: 3 → 4
 * Restore Speed: 5 → 7
 * 
 * 【더 부드럽게 튕기게】
 * Min Speed: 3 → 2
 * Restore Speed: 5 → 4
 * 
 * 【슈퍼볼처럼 매우 빠르게】
 * Min Speed: 5
 * Restore Speed: 10
 * Launch Force: 10
 * 
 * 【천천히 계속 튕기게】
 * Min Speed: 2
 * Restore Speed: 3
 * Launch Force: 3
 * 
 * ========================================
 * === 완벽한 무한 튕김 설정 (권장) ===
 * ========================================
 * 
 * ItemPickup (Script):
 * - Launch Force: 5
 * - Min Speed: 3
 * - Restore Speed: 5
 * 
 * Rigidbody 2D:
 * - Linear Drag: 0 ⭐
 * - Angular Drag: 0 ⭐
 * - Gravity Scale: 1
 * - Sleeping Mode: Never Sleep ⭐
 * 
 * ItemBouncy:
 * - Friction: 0 ⭐
 * - Bounciness: 1.0 ⭐
 * 
 * ========================================
 * === 테스트 방법 ===
 * ========================================
 * 
 * 1. Play 버튼 클릭
 * 2. 적 처치하여 아이템 드롭
 * 3. 확인:
 *    ✅ 아이템이 대각선으로 날아감
 *    ✅ 벽에서 계속 튕김 ⭐⭐⭐
 *    ✅ 속도가 멈추지 않음 ⭐⭐⭐
 *    ✅ 무한히 튕기며 이동 ⭐⭐⭐
 *    ✅ 플레이어가 먹으면 사라짐
 *    ✅ 화면 밖으로 나가면 삭제
 * 
 * ========================================
 * === 문제 해결 ===
 * ========================================
 * 
 * 【문제】 아이템이 점점 느려지고 멈춤
 * 【원인】 Linear Drag가 0이 아님
 * 【해결】 Rigidbody 2D > Linear Drag: 0
 * 
 * 【문제】 아이템이 튕기다가 멈춤
 * 【원인】 Sleeping Mode가 Start Awake
 * 【해결】 Rigidbody 2D > Sleeping Mode: Never Sleep ⭐
 * 
 * 【문제】 아이템이 벽에서 안 튕김
 * 【원인】 Bounciness가 낮음
 * 【해결】 ItemBouncy > Bounciness: 1.0
 * 
 * 【문제】 아이템이 벽에 붙어버림
 * 【원인】 Friction이 높음
 * 【해결】 ItemBouncy > Friction: 0
 * 
 * 【문제】 아이템 회전이 멈춤
 * 【원인】 Angular Drag가 0이 아님
 * 【해결】 Rigidbody 2D > Angular Drag: 0
 * 
 * ========================================
 * === 주의사항 ===
 * ========================================
 * 
 * ⚠️ 너무 많은 아이템이 무한히 튕기면 성능 저하!
 * → 화면에 아이템이 10개 이상이면 오래된 것부터 삭제 권장
 * 
 * ⚠️ 벽이 없으면 화면 밖으로 나가서 삭제됨
 * → LeftWall, RightWall, TopWall, BottomWall 모두 생성 필수!
 */