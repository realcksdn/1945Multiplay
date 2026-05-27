using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 보스의 행동과 공격 패턴을 제어하는 클래스
/// 좌우 이동, 미사일 발사, 원형 탄막 공격 등을 구현합니다
/// </summary>
public class BossController : NetworkBehaviour
{
    #region 변수 선언
    [Header("이동 설정")]
    // 보스의 좌우 이동 방향 (1: 오른쪽, -1: 왼쪽)
    //private int movementDirection = 1;

    // 네트워크 동기화 필요한 변수들
    NetworkVariable<int> movementDirection = new NetworkVariable<int>(1);
    NetworkVariable<float> positionX = new NetworkVariable<float>();

    [Tooltip("보스의 이동 속도")]
    private int moveSpeed = 2;

    [Header("미사일 설정")]
    [Tooltip("보스가 발사하는 일반 미사일 프리팹")]
    public GameObject normalMissilePrefab;

    [Tooltip("보스가 발사하는 원형 탄막 미사일 프리팹")]
    public GameObject circularMissilePrefab;

    [Header("발사 위치")]
    [Tooltip("첫 번째 미사일 발사 위치")]
    public Transform firstFirePoint;

    [Tooltip("두 번째 미사일 발사 위치")]
    public Transform secondFirePoint;
    #endregion

    /// <summary>
    /// 보스 초기화: 경고 텍스트 숨기기 및 공격 패턴 시작
    /// </summary>
    // void Start()
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 1초 후에 보스 등장 경고 텍스트를 숨김
        Invoke("HideBossWarning", 1f);

        // 일반 미사일 발사 코루틴 시작
        StartCoroutine(FireNormalMissiles());

        // 원형 탄막 발사 코루틴 시작
        StartCoroutine(FireCircularPattern());
    }

    /// <summary>
    /// 보스 경고 텍스트를 숨기는 메서드 (서버 측 호출)
    /// </summary>
    private void HideBossWarning()
    {
        // 현재 실행 환경이 서버인지 확인
        if (IsServer)
        {
            // 모든 클라이언트에게 보스 경고 텍스트를 숨기라는 RPC 호출
            HideBossWarningTextClientRpc();
        }
    }

    /// <summary>
    /// 보스 등장 경고 텍스트를 비활성화하는 클라이언트 RPC
    /// 서버가 호출하면 모든 클라이언트(서버 포함)에서 실행됨
    /// </summary>
    // HideBossWarning()
    [ClientRpc] // 이 어트리뷰트는 서버에서 호출하면 모든 클라이언트에서 실행되도록 함
    void HideBossWarningTextClientRpc()
    {
        // Scene 계층 구조에서 "TextBossWarning" 이름을 가진 게임 오브젝트를 검색
        GameObject warningText = GameObject.Find("TextBossWarning");

        // 오브젝트를 찾았는지 null 체크 (오브젝트가 없으면 에러 방지)
        if (warningText != null)
        {
            // 찾은 경고 텍스트 오브젝트를 비활성화하여 화면에서 숨김
            warningText.SetActive(false);
        }
    }

    /// <summary>
    /// 일반 미사일을 두 지점에서 동시에 발사하는 코루틴
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    IEnumerator FireNormalMissiles()
    {
        // 무한 반복
        while (IsServer) // 서버에서만 실행
        {
            SpawnMissile(firstFirePoint.position);  // 첫 번째 발사 위치에서 미사일 생성
            SpawnMissile(secondFirePoint.position);  // 두 번째 발사 위치에서 미사일 생성

            // 0.5초 대기 후 다시 발사
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 일반 미사일을 생성하고 네트워크에 스폰하는 메서드
    /// </summary>
    /// <param name="Position">미사일이 생성될 위치 (현재 사용되지 않음)</param>
    private void SpawnMissile(Vector3 spawnPosition)
    {
        // 서버가 아닌 경우 함수 종료 (오직 서버만 네트워크 오브젝트를 생성할 수 있음)
        if (!IsServer) return;

        // 일반 미사일 프리팹을 첫 번째 발사 지점 위치에 회전값 없이 인스턴스화
        // spawnPosition: 미사일이 생성될 위치
        // Quaternion.identity: 회전 없음 (기본 회전값 0, 0, 0)
        GameObject missileObj = Instantiate(normalMissilePrefab, spawnPosition, Quaternion.identity);

        // 생성된 미사일 오브젝트에서 NetworkObject 컴포넌트를 가져옴
        NetworkObject networkObject = missileObj.GetComponent<NetworkObject>();

        // NetworkObject 컴포넌트가 존재하는지 확인 (null 체크로 에러 방지)
        if (networkObject != null)
        {
            // 네트워크에 오브젝트를 스폰하여 모든 클라이언트에 동기화
            networkObject.Spawn();
        }
    }

    /// <summary>
    /// 원형 탄막 패턴으로 미사일을 발사하는 코루틴
    /// 360도 방향으로 일정 간격의 미사일을 발사합니다
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    IEnumerator FireCircularPattern()
    {
        float attackInterval = 3f;  // 공격 주기 (초 단위)
        int projectileCount = 30;   // 한 번에 생성할 발사체 개수

        // 각 발사체 사이의 각도 간격 (360도를 발사체 개수로 나눔)
        float angleBetweenProjectiles = 360f / projectileCount;

        // 발사 시작 각도 (회전하는 탄막을 만들기 위한 변수)
        float startAngleOffset = 0f;

        while (IsServer)  // 서버에서만 실행
        {
            // 지정된 개수만큼 발사체 생성
            for (int i = 0; i < projectileCount; ++i)
            {
                // 현재 발사체의 각도 계산 (시작 각도 + 간격 * 인덱스)
                float currentAngle = startAngleOffset + angleBetweenProjectiles * i;

                // 발사 위치에 미사일 생성
                GameObject missile = Instantiate(circularMissilePrefab, transform.position, Quaternion.identity);

                //네트워크 오브젝트로 스폰
                NetworkObject networkObject = missile.GetComponent<NetworkObject>();

                if (networkObject != null)
                {
                    networkObject.Spawn();
                }

                // 각도를 라디안으로 변환하여 X, Y 방향 벡터 계산
                float xDirection = Mathf.Cos(currentAngle * Mathf.Deg2Rad);  // Cos(각도): X축 방향 성분
                float yDirection = Mathf.Sin(currentAngle * Mathf.Deg2Rad);  // Sin(각도): Y축 방향 성분

                // 생성된 미사일에 이동 방향 설정
                missile.GetComponent<BossProjectile>().SetDirection(new Vector2(xDirection, yDirection));
            }

            // 다음 발사 시 패턴이 회전하도록 시작 각도를 1도씩 증가
            startAngleOffset += 1f;

            // 지정된 공격 주기만큼 대기
            yield return new WaitForSeconds(attackInterval);
        }
    }

    /// <summary>
    /// 매 프레임 보스의 좌우 이동 처리
    /// 화면 경계에 도달하면 방향을 반전시킵니다
    /// </summary>
    void Update()
    {
        if (IsServer)
        {
            // 보스가 오른쪽 경계(0.75)에 도달하면 방향 반전
            if (transform.position.x >= 0.75f)
                movementDirection.Value *= -1;

            // 보스가 왼쪽 경계(-0.75)에 도달하면 방향 반전
            if (transform.position.x <= -0.75f)
                movementDirection.Value *= -1;

            // 현재 방향으로 보스 이동 (프레임 독립적)
            transform.Translate(movementDirection.Value * moveSpeed * Time.deltaTime, 0, 0);

            // 위치 동기화
            positionX.Value = transform.position.x;
        }
        else if (IsClient && !IsServer)  // 순수 클라이언트만 실행
        {
            // 클라이언트에서 서버의 위치 값을 따라감
            
            Vector3 targetPos = transform.position;  // 현재 오브젝트의 위치를 목표 위치로 복사
            targetPos.x = positionX.Value;  // 서버로부터 동기화된 X축 위치 값을 목표 위치의 X에 적용

            // 현재 위치에서 목표 위치로 부드럽게 이동 (선형 보간)
            // Time.deltaTime * 10f: 초당 10의 속도로 보간
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ItemDropServerRpc()
    {
        // 서버에서 아이템 드롭 처리
        DropItem();
    }

    /// <summary>
    /// 보스 처치 시 아이템 드롭 (현재 미구현)
    /// </summary>
    public void DropItem()
    {
        if (!IsServer) return;
        
        // TODO: 아이템 드롭 로직 구현
        
    }
}




