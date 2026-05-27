using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


/// <summary>
/// 플레이어 캐릭터의 이동, 공격, 파워업을 제어하는 클래스
/// 키보드 입력을 받아 이동하고 스페이스바로 총알/레이저를 발사합니다
/// 태그: "Player" (필수)
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [Header("애니메이션")]
    // 플레이어 애니메이터 컴포넌트
    private Animator playerAnimator;

    [Header("이동 설정")]
    [Tooltip("플레이어의 이동 속도")]
    public float moveSpeed = 5f;

    [Header("공격 설정")]
    [Tooltip("파워업 레벨별 총알 프리팹 배열 (0~3레벨)")]
    public GameObject[] bulletPrefabs;

    [Tooltip("총알 발사 위치 (플레이어 앞쪽)")]
    public Transform firePoint = null;

    [Tooltip("현재 파워업 레벨 (0~3)")]
    //public int powerLevel = 0;

    // powerfmf NetworkVariable로 동기화
    [SerializeField]
    private NetworkVariable<int> powerLevel = new NetworkVariable<int>(0);

    [SerializeField]
    private NetworkVariable<bool> isLaserActive = new NetworkVariable<bool>(false);


    [Header("레이저 차징 설정")]
    [Tooltip("레이저 차징 게이지 UI 이미지")]
    public Image chargeGauge;

    [Tooltip("현재 차징 게이지 값 (0~1)")]
    public float chargeValue = 0f;

    [Tooltip("레이저 빔 프리팹")]
    public GameObject laserBeamPrefab;

    // 레이저 활성화 상태를 네트워크로 동기화
   

    /// <summary>
    /// 초기화: 애니메이터 컴포넌트 가져오기
    /// </summary>
    void Start()
    {
        // Animator 컴포넌트를 가져와 캐싱 (애니메이션 제어용)
        playerAnimator = GetComponent<Animator>();

        // Animator가 없으면 경고 표시
        if (playerAnimator == null)
        {
            Debug.LogWarning("PlayerController: Animator 컴포넌트가 없습니다!");
        }

        // 레이저 빔이 설정되지 않았으면 자식에서 찾기
        if (laserBeamPrefab == null)
        {
            // Player의 자식 중에서 "LaserBeam" 이름을 가진 오브젝트 찾기
            Transform laserTransform = transform.Find("LaserBeam");
            if (laserTransform != null)
            {
                laserBeamPrefab = laserTransform.gameObject;
                Debug.Log("PlayerController: LaserBeam을 자식에서 자동으로 찾았습니다.");
            }
            else
            {
                Debug.LogWarning("PlayerController: LaserBeam 오브젝트를 찾을 수 없습니다!");
            }
        }

        // 시작 시 레이저 비활성화
        if (laserBeamPrefab != null)
        {
            laserBeamPrefab.SetActive(false);
        }
    }

    /// <summary>
    /// 네트워크 스폰 시 호출 - NetworkVariable 변경 이벤트 구독
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 레이저 활성화 상태가 변경될 때 호출되는 이벤트 구독
        isLaserActive.OnValueChanged += OnLaserActiveChanged;

        // 초기 상태 적용 (클라이언트가 나중에 접속한 경우를 위해)
        if (laserBeamPrefab != null)
        {
            laserBeamPrefab.SetActive(isLaserActive.Value);
        }
    }

    /// <summary>
    /// 네트워크에서 제거될 때 호출 - 이벤트 구독 해제
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // 메모리 누수 방지를 위해 이벤트 구독 해제
        isLaserActive.OnValueChanged -= OnLaserActiveChanged;
    }

    /// <summary>
    /// 레이저 활성화 상태가 변경되었을 때 호출되는 콜백
    /// NetworkVariable이 변경되면 자동으로 모든 클라이언트에서 실행됨
    /// </summary>
    /// <param name="previousValue">이전 값</param>
    /// <param name="newValue">새로운 값</param>
    private void OnLaserActiveChanged(bool previousValue, bool newValue)
    {
        // 레이저 빔 활성화/비활성화
        if (laserBeamPrefab != null)
        {
            laserBeamPrefab.SetActive(newValue);
            Debug.Log($"[Client {NetworkManager.LocalClientId}] 레이저 상태 변경: {newValue}");
        }
    }

    /// <summary>
    /// 매 프레임 플레이어 입력 처리 및 이동, 공격 업데이트
    /// </summary>
    void Update()
    {
        // 오너가 아닌 경우 이동하지 않도록 처리
        if (!IsOwner) return;

        // === 이동 입력 처리 ===
        // 수평 입력 (-1: 왼쪽, 0: 정지, 1: 오른쪽)
        float horizontalInput = Input.GetAxis("Horizontal");
        float moveX = moveSpeed * Time.deltaTime * horizontalInput;

        // 수직 입력 (-1: 아래, 0: 정지, 1: 위)
        float verticalInput = Input.GetAxis("Vertical");
        float moveY = moveSpeed * Time.deltaTime * verticalInput;

        // === 애니메이션 제어 ===
        if (playerAnimator != null)
        {
            // 오른쪽으로 이동 중 (입력값이 0.5 이상)
            if (horizontalInput >= 0.5f)
            {
                playerAnimator.SetBool("right", true);
            }
            else
            {
                playerAnimator.SetBool("right", false);
            }

            // 왼쪽으로 이동 중 (입력값이 -0.5 이하)
            if (horizontalInput <= -0.5f)
            {
                playerAnimator.SetBool("left", true);
            }
            else
            {
                playerAnimator.SetBool("left", false);
            }

            // 위로 이동 중 (입력값이 0.5 이상)
            if (verticalInput >= 0.5f)
            {
                playerAnimator.SetBool("up", true);
            }
            else
            {
                playerAnimator.SetBool("up", false);
            }
        }

        // === 공격 입력 처리 ===
        // 스페이스바를 누르는 순간 (단발 발사)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // bulletPrefabs 배열과 firePoint가 유효한지 확인
            if (bulletPrefabs != null && bulletPrefabs.Length > powerLevel.Value && firePoint != null)
            {
                // 현재 파워 레벨에 맞는 총알 발사
                if (bulletPrefabs[powerLevel.Value] != null)
                {
                    FireBulletSErverRpc();
                }
            }
        }
        // 스페이스바를 누르고 있을 때 (레이저 차징)
        else if (Input.GetKey(KeyCode.Space))
        {
            // 차징 게이지 증가 (초당 0.5씩 증가)
            chargeValue += 0.005f;

            //게이지 UI 업데이트
            if (chargeGauge != null)
            {
                chargeGauge.fillAmount = chargeValue;
            }

            // 차징이 완료되면 (게이지가 1에 도달)
            if (chargeValue >= 1f)
            {
                // 레이저가 아직 활성화되지 않았으면 활성화
                if (!isLaserActive.Value)
                {
                    // 서버에 레이저 활성화 요청
                    ActivateLaserServerRpc(true);
                }

                // 차징 게이지 유지 (레이저 지속)
                chargeValue = 1f;
            }
        }
        // 스페이스바를 떼면 (차징 취소)
        else
        {
            // 레이저가 활성화되어 있으면 비활성화
            if (isLaserActive.Value)
            {
                // 서버에 레이저 비활성화 요청
                ActivateLaserServerRpc(false);
            }

            // 차징 게이지 감소 (초당 0.5씩 감소)
            chargeValue -= 0.005f;

            // 게이지가 0 이하로 내려가지 않도록 제한
            if (chargeValue <= 0f)
                chargeValue = 0f;

            // UI 업데이트 (로컬만)
            if (chargeGauge != null)
            {
                chargeGauge.fillAmount = chargeValue;
            }
        }

        // === 플레이어 이동 적용 ===
        transform.Translate(moveX, moveY, 0);

        // === 화면 경계 제한 ===
        // 오른쪽 경계 (X 좌표 2.5 이상이면 2.5로 고정)
        if (transform.position.x >= 2.5f)
            transform.position = new Vector3(2.5f, transform.position.y, 0);

        // 왼쪽 경계 (X 좌표 -2.5 이하이면 -2.5로 고정)
        if (transform.position.x <= -2.5f)
            transform.position = new Vector3(-2.5f, transform.position.y, 0);
    }

    [ServerRpc]
    private void FireBulletSErverRpc()
    {
        GameObject go = Instantiate(bulletPrefabs[powerLevel.Value], firePoint.position, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn(true);
    }

    /// <summary>
    /// 서버에 레이저 활성화/비활성화 요청
    /// </summary>
    /// <param name="active">true: 활성화, false: 비활성화</param>
    [ServerRpc]
    private void ActivateLaserServerRpc(bool active)
    {
        // NetworkVariable 값 변경 (자동으로 모든 클라이언트에 동기화됨)
        isLaserActive.Value = active;

        Debug.Log($"[Server] 레이저 {(active ? "활성화" : "비활성화")} 요청 처리됨");
    }

    /// <summary>
    /// 다른 2D 콜라이더와 충돌 시 호출되는 Unity 콜백
    /// 아이템 획득을 처리합니다
    /// </summary>
    /// <param name="collision">충돌한 콜라이더 정보</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag("Item"))
        {
            var netObj = collision.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError("Item에 NetworkObject 없음!");
                return;
            }

            if (!netObj.IsSpawned)
            {
                Debug.LogWarning("Item이 Spawn안됌");
                return;
            }

            // 스폰된 경우 → NetworkObjectReference로 자동 변환됨
            EatItemServerRpc(netObj);
        }
    }


    // [ServerRpc] 어트리뷰트: 클라이언트에서 호출하면 서버에서 실행되는 RPC 메서드임을 나타냅니다
    // 메서드 선언: 아이템을 먹는 기능을 처리하는 서버 RPC 메서드
    [ServerRpc]
    private void EatItemServerRpc(NetworkObjectReference itemRef)
    {
        // powerLevel은 NetworkVariable<int> 타입으로, .Value를 통해 값에 접근합니다
        // 현재 파워 레벨 값을 1 증가시킵니다
        powerLevel.Value += 1;

        // if문: 파워 레벨이 3 이상인지 확인
        if (powerLevel.Value >= 3)
            // 파워 레벨이 3을 초과하지 않도록 최대값을 3으로 고정합니다
            powerLevel.Value = 3;

        // TryGet 메서드: NetworkObjectReference에서 실제 NetworkObject를 가져오려고 시도
        // 성공하면 true를 반환하고 item 변수에 NetworkObject를 할당합니다
        if (itemRef.TryGet(out NetworkObject item))
        {
            // IsSpawned 속성: 해당 오브젝트가 현재 네트워크에 스폰된 상태인지 확인
            if (item.IsSpawned)
            {
                // Despawn() 메서드: 네트워크에서 오브젝트를 제거하고 오브젝트 풀로 반환하거나 파괴합니다
                // 모든 클라이언트에서 동기화되어 아이템이 사라집니다
                item.Despawn();
            }
        }
    }
}

// -------------------------------------------
//   itemRef.TryGet(out NetworkObject item)
//   상세 설명 링크 : http://bit.ly/4oaf3Wr
// -------------------------------------------








