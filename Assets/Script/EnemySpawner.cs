using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 적 우주선과 보스를 생성하는 스포너 클래스
/// 게임 진행에 따라 1차 웨이브, 2차 웨이브, 보스 단계로 구성됩니다
/// </summary>
public class EnemySpawner : NetworkBehaviour
{
    #region 변수 선언
    [Header("스폰 영역 설정")]
    [Tooltip("적 생성 X 좌표 최소값")]
    public float spawnMinX = -2f;

    [Tooltip("적 생성 X 좌표 최대값")]
    public float spawnMaxX = 2f;

    [Header("스폰 타이밍 설정")]
    [Tooltip("첫 적 생성까지의 대기 시간 (초)")]
    public float startDelay = 1f;

    [Tooltip("1차 웨이브 지속 시간 (초)")]
    public float firstWaveDuration = 30f;

    [Header("적 프리팹")]
    [Tooltip("1차 웨이브 적 프리팹")]
    public GameObject firstWaveEnemyPrefab;

    [Tooltip("2차 웨이브 적 프리팹")]
    public GameObject secondWaveEnemyPrefab;

    [Tooltip("보스 프리팹")]
    public GameObject bossPrefab;

    // 1차 웨이브 스폰 활성화 플래그
    private bool isFirstWaveActive = true;

    // 2차 웨이브 스폰 활성화 플래그
    private bool isSecondWaveActive = true;

    [Header("보스 등장 UI")]
    [Tooltip("보스 등장 경고 텍스트 오브젝트")]
    [SerializeField]
    private GameObject bossWarningText;
    #endregion

    // NetworkBehaviour의 가상 함수를 재정의, 이 네트워크 오브젝트가 네트워크에 스폰될 때 자동으로 호출되는 생명주기 함수,
    // public은 외부 접근 가능, override는 부모 클래스의 함수를 덮어쓴다는 의미
    // OnNetworkSpawn() 설명 링크 : http://bit.ly/4gVMFop
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        // 보스 등장 텍스트가 할당되어 있는지 확인
        if (bossWarningText != null)
        {
            // 보스 등장 텍스트를 초기에는 비활성화
            bossWarningText.SetActive(false);
        }
        else
        {
            Debug.LogWarning("EnemySpawner: bossWarningText가 할당되지 않았습니다!");
        }

        // 1차 웨이브 스폰 코루틴 시작
        StartCoroutine("SpawnFirstWaveEnemies");

        // firstWaveDuration 초 후에 1차 웨이브 종료 함수 호출
        Invoke("StopFirstWave", firstWaveDuration);
    }

    /// <summary>
    /// 1차 웨이브를 종료하고 2차 웨이브를 시작합니다
    /// </summary>
    void StopFirstWave()
    {
        // 1차 웨이브 스폰 중지
        isFirstWaveActive = false;

        // 1차 웨이브 코루틴 중지
        StopCoroutine("SpawnFirstWaveEnemies");

        // 2차 웨이브 스폰 코루틴 시작
        StartCoroutine("SpawnSecondWaveEnemies");

        // (firstWaveDuration + 20)초 후에 2차 웨이브 종료 함수 호출
        Invoke("StopSecondWave", firstWaveDuration + 20f);
    }

    /// <summary>
    /// 2차 웨이브를 종료하고 보스를 등장시킵니다
    /// </summary>
    void StopSecondWave()
    {
        // 2차 웨이브 스폰 중지
        isSecondWaveActive = false;

        // 2차 웨이브 코루틴 중지
        StopCoroutine("SpawnSecondWaveEnemies");

        // 보스 등장 위치 설정 (화면 상단 중앙)
        Vector3 bossSpawnPosition = new Vector3(0, 2.76f, 0);

        // 보스 경고 텍스트 활성화
        if (bossWarningText != null)
        {
            //bossWarningText.SetActive(true);
            SetBossWarningClientRpc(true);
        }

        // 보스 프리팹이 할당되어 있는지 확인 후 생성
        if (bossPrefab != null)
        {
            GameObject bossObj = Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
            bossObj.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            Debug.LogError("EnemySpawner: bossPrefab이 할당되지 않았습니다!");
        }
    }

    // [ClientRpc] 어트리뷰트: 서버에서 호출하면 모든 클라이언트에서 실행되는 RPC 메서드
    // 서버 → 모든 클라이언트로 명령을 브로드캐스트합니다
    [ClientRpc]
    // 메서드 선언: 보스 경고 UI를 활성화/비활성화하는 클라이언트 RPC 메서드
    // active 매개변수: true면 경고 표시, false면 경고 숨김
    private void SetBossWarningClientRpc(bool active)
    {
        // null 체크: bossWarningText 오브젝트가 존재하는지 확인
        // 오브젝트가 삭제되었거나 할당되지 않은 경우를 방지
        if (bossWarningText != null)
        {
            // SetActive 메서드: GameObject의 활성화 상태를 변경
            // active가 true면 UI를 화면에 표시, false면 숨김
            // 모든 클라이언트에서 동시에 동일한 UI 상태가 설정됩니다
            bossWarningText.SetActive(active);
        }
    }

    /// <summary>
    /// 1차 웨이브 적을 랜덤한 X 위치에 생성하는 코루틴
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    IEnumerator SpawnFirstWaveEnemies()
    {
        // 1차 웨이브가 활성화된 동안 반복
        while (isFirstWaveActive)
        {
            // startDelay 초 대기 (기본 1초)
            yield return new WaitForSeconds(startDelay);

            // 프리팹이 할당되어 있는지 확인
            if (firstWaveEnemyPrefab != null)
            {
                // X 좌표를 랜덤하게 결정 (spawnMinX ~ spawnMaxX 범위)
                float randomX = Random.Range(spawnMinX, spawnMaxX);

                // 스폰 위치 생성 (X는 랜덤, Y는 스포너의 Y 좌표)
                Vector2 spawnPosition = new Vector2(randomX, transform.position.y);

                // 1차 웨이브 적 생성
                //Instantiate(firstWaveEnemyPrefab, spawnPosition, Quaternion.identity);

                GameObject enemyObj = Instantiate(firstWaveEnemyPrefab, spawnPosition, Quaternion.identity);
                enemyObj.GetComponent<NetworkObject>().Spawn();

            }
        }
    }

    /// <summary>
    /// 2차 웨이브 적을 랜덤한 X 위치에 생성하는 코루틴
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    IEnumerator SpawnSecondWaveEnemies()
    {
        // 2차 웨이브가 활성화된 동안 반복
        while (isSecondWaveActive)
        {
            // (startDelay + 2)초 대기 (기본 3초, 1차보다 천천히 생성)
            yield return new WaitForSeconds(startDelay + 2f);

            // 프리팹이 할당되어 있는지 확인
            if (secondWaveEnemyPrefab != null)
            {
                // X 좌표를 랜덤하게 결정
                float randomX = Random.Range(spawnMinX, spawnMaxX);

                // 스폰 위치 생성
                Vector2 spawnPosition = new Vector2(randomX, transform.position.y);

                // 2차 웨이브 적 생성
                GameObject SecondEnemy = Instantiate(secondWaveEnemyPrefab, spawnPosition, Quaternion.identity);
                SecondEnemy.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}





