using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// 네트워크 연결을 관리하는 클래스
/// IP 주소 설정 및 호스트/클라이언트 시작을 담당
/// </summary>
public class NetworkConnectionManager : MonoBehaviour
{
    #region 1. 변수 선언
    /// <summary>
    /// 싱글톤 인스턴스
    /// 다른 스크립트에서 NetworkConnectionManager.Instance로 접근 가능
    /// </summary>
    public static NetworkConnectionManager Instance { get; private set; }

    /// <summary>
    /// Unity Transport 컴포넌트 참조
    /// IP 주소와 포트 설정에 사용됨
    /// </summary>
    private UnityTransport transport;

    /// <summary>
    /// 네트워크 통신에 사용할 기본 포트 번호
    /// 호스트와 클라이언트 모두 동일한 포트를 사용해야 함
    /// </summary>
    private const ushort DEFAULT_PORT = 8888;

    [Header("플레이어 스폰 위치 설정")]
    [Tooltip("호스트 플레이어가 스폰될 위치")]
    [SerializeField] private Vector3 hostSpawnPosition = new Vector3(-1.5f, -3f, 0f);

    [Tooltip("클라이언트 플레이어가 스폰될 위치")]
    [SerializeField] private Vector3 clientSpawnPosition = new Vector3(1.5f, 3f, 0f);
    #endregion

    #region 2. Unity 생명주기 메서드
    /// <summary>
    /// 오브젝트가 생성될 때 호출되는 메서드
    /// 싱글톤 패턴을 설정하고 Transport 컴포넌트를 초기화함
    /// </summary>
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance != null && Instance != this)
        {
            // 이미 Instance가 존재하고 현재 오브젝트가 아닌 경우
            // 중복된 오브젝트를 삭제하여 싱글톤 보장
            Destroy(gameObject);
            return;
        }

        // 현재 오브젝트를 싱글톤 인스턴스로 설정
        Instance = this;

        // 씬이 전환되어도 이 오브젝트가 파괴되지 않도록 설정
        // 게임 전체에서 네트워크 연결 관리자를 유지
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeTransport();  // Transport 컴포넌트 초기화
    }

    /// <summary>
    /// 오브젝트가 파괴될 때 호출
    /// 싱글톤 인스턴스 정리 및 네트워크 콜백 해제
    /// </summary>
    private void OnDestroy()
    {
        // 네트워크 콜백 해제 (메모리 누수 방지)
        if (NetworkManager.Singleton != null)
        {
            // ConnectionApprovalCallback을 null로 설정하여 등록 해제
            // 이 콜백이 남아있으면 오브젝트가 파괴된 후에도 호출될 수 있어 에러 발생
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
        }

        // 현재 인스턴스가 싱글톤 인스턴스인 경우에만 null로 설정
        if (Instance == this)
        {
            Instance = null;
        }
    }
    #endregion

    #region 3. 초기화 메서드
    /// <summary>
    /// Unity Transport 컴포넌트를 초기화하는 메서드
    /// NetworkManager에서 UnityTransport 컴포넌트를 찾아 참조를 저장
    /// </summary>
    private void InitializeTransport()
    {
        // NetworkManager.Singleton이 존재하는지 확인
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkConnectionManager: NetworkManager.Singleton이 null입니다!" +
                                                   " 씬에 NetworkManager를 추가해주세요.");
            return;
        }

        // NetworkManager에서 UnityTransport 컴포넌트를 가져옴
        // 이 컴포넌트를 통해 IP 주소와 포트를 설정할 수 있음
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Transport 컴포넌트를 찾지 못한 경우
        if (transport == null)
        {
            Debug.LogError("NetworkConnectionManager: UnityTransport 컴포넌트를 찾을 수 없습니다!");
        }

        // Connection Approval 기능을 활성화
        // 이 설정을 true로 하면 클라이언트가 연결을 시도할 때
        // ConnectionApprovalCallback이 호출되어 연결 승인 여부와 스폰 위치를 제어할 수 있음
        // false인 경우 모든 클라이언트가 자동으로 승인되고 기본 위치에 스폰됨
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
    }
    #endregion

    #region 4. 네트워크 시작 메서드
    /// <summary>
    /// 호스트로 네트워크를 시작하는 메서드
    /// 서버이면서 동시에 플레이어로 참여하는 역할
    /// 다른 클라이언트들이 이 호스트에 연결할 수 있음
    /// </summary>
    /// <returns>호스트 시작 성공 시 true, 실패 시 false</returns>
    public bool StartHost()
    {
        // NetworkManager.Singleton이 존재하는지 확인
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkConnectionManager: NetworkManager.Singleton이 null입니다!");
            return false;
        }

        // 연결 승인 기능 활성화 (클라이언트 연결 시 ApprovalCheck 호출됨)
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

        // 연결 승인 콜백 등록
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        // NetworkManager를 통해 호스트 모드로 시작
        bool success = NetworkManager.Singleton.StartHost();

        // 호스트 시작 성공 여부 확인
        if (success)
        {
            // 현재 컴퓨터의 로컬 IP 주소 가져오기
            string localIP = GetLocalIPAddress();
        }
        else
        {
            // 호스트 시작 실패 시 에러 로그 출력
            Debug.LogError("NetworkConnectionManager: 호스트 시작 실패!");
        }

        return success;  // 시작 성공 여부 반환
    }

    /// <summary>
    /// 클라이언트로 네트워크를 시작하는 메서드 / 지정된 IP 주소의 호스트에 연결을 시도함
    /// 연결 성공 시 해당 호스트의 게임 세션에 참여
    /// </summary>
    /// <param name="ipAddress">연결할 호스트의 IP 주소 (예: "192.168.0.100")</param>
    /// <returns>클라이언트 시작 성공 시 true, 실패 시 false</returns>
    public bool StartClient(string ipAddress)
    {
        // NetworkManager.Singleton이 존재하는지 확인
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkConnectionManager: NetworkManager.Singleton이 null입니다!");
            return false;
        }

        // Transport 컴포넌트가 존재하는지 확인
        if (transport == null)
        {
            Debug.LogError("NetworkConnectionManager: UnityTransport가 null입니다!");
            return false;
        }

        // IP 주소가 null이거나 빈 문자열인지 확인
        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogError("NetworkConnectionManager: IP 주소가 비어있습니다!");
            return false;
        }

        // IP 주소 형식이 유효한지 검증
        if (!IsValidIPAddress(ipAddress))
        {
            Debug.LogError($"NetworkConnectionManager: 유효하지 않은 IP 주소입니다: {ipAddress}");
            return false;
        }

        // Transport에 연결할 호스트의 IP 주소와 포트 설정
        // 이 설정이 없으면 기본값(localhost)으로만 연결 시도하여 다른 컴퓨터와 연결 불가
        transport.SetConnectionData(ipAddress, DEFAULT_PORT);

        Debug.Log($"NetworkConnectionManager: {ipAddress}:{DEFAULT_PORT}에 연결 시도");

        // NetworkManager를 통해 클라이언트 모드로 시작
        bool success = NetworkManager.Singleton.StartClient();

        // 클라이언트 시작 성공 여부 확인
        if (success)
        {
            Debug.Log($"클라이언트 시작 성공! {ipAddress}:{DEFAULT_PORT}에 연결 중...");
        }
        else
        {
            Debug.LogError("NetworkConnectionManager: 클라이언트 시작 실패!");
        }

        return success;  // 시작 성공 여부 반환
    }

    /// <summary>
    /// 네트워크 연결을 종료하는 메서드
    /// 호스트 또는 클라이언트 모드를 종료하고 네트워크 세션에서 나감
    /// </summary>
    public void Disconnect()
    {
        // NetworkManager.Singleton이 존재하는지 확인
        if (NetworkManager.Singleton != null)
        {
            // NetworkManager의 Shutdown 메서드를 호출하여 네트워크 종료
            NetworkManager.Singleton.Shutdown();

            Debug.Log("NetworkConnectionManager: 네트워크 연결 종료");
        }
    }
    #endregion

    #region 5. IP 주소 관련 메서드
    /// <summary>
    /// 현재 컴퓨터의 로컬 네트워크 IP 주소를 반환하는 메서드
    /// 호스트가 다른 플레이어에게 알려줘야 할 IP 주소
    /// 같은 LAN(로컬 네트워크)에 있는 다른 컴퓨터에서 이 IP로 접속 가능
    /// </summary>
    /// <returns>로컬 IP 주소 문자열 (예: "192.168.0.100")</returns>
    public string GetLocalIPAddress()
    {
        try
        {
            // DNS를 통해 현재 컴퓨터의 호스트 이름을 가져옴
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

            // 호스트가 가진 모든 IP 주소를 순회
            foreach (var ip in host.AddressList)
            {
                // IP 주소가 IPv4 형식인지 확인
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // IPv4 주소를 문자열로 변환하여 반환
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            // IP 주소를 가져오는 중 예외 발생 시 에러 로그 출력
            Debug.LogError($"NetworkConnectionManager: IP 주소 가져오기 실패 - {e.Message}");
        }

        // IPv4 주소를 찾지 못한 경우 localhost 반환
        return "127.0.0.1";
    }

    /// <summary>
    /// IP 주소 형식이 유효한지 검사하는 메서드
    /// 잘못된 형식의 IP 주소 입력을 방지
    /// </summary>
    /// <param name="ipAddress">검사할 IP 주소 문자열</param>
    /// <returns>유효한 IP 형식이면 true, 아니면 false</returns>
    public bool IsValidIPAddress(string ipAddress)
    {
        // System.Net.IPAddress.TryParse를 사용하여 IP 주소 형식 검증
        // 반환값: 파싱 성공 시 true (유효한 IP), 실패 시 false (잘못된 IP)
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
    #endregion

    #region 6. 스폰 위치 관리
    /// <summary>
    /// 클라이언트 연결 승인 및 스폰 위치를 설정하는 콜백 메서드
    /// 서버(호스트)에서만 실행되며, 클라이언트가 연결을 시도할 때 호출됨
    /// 각 플레이어의 역할(호스트/클라이언트)에 따라 다른 위치에 스폰시킴
    /// </summary>
    /// <param name="request">연결 요청 정보 (ClientNetworkId 포함)</param>
    /// <param name="response">연결 응답 정보 (승인 여부, 스폰 위치 등 설정)</param>
    private void ApprovalCheck(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log($"[ApprovalCheck 호출됨] ClientNetworkId: {request.ClientNetworkId}");  // ← 추가

        // 연결 승인 (true: 연결 허용, false: 연결 거부)
        response.Approved = true;

        // 플레이어 오브젝트 자동 생성 활성화
        // true로 설정하면 NetworkManager가 자동으로 플레이어 프리팹을 생성함
        response.CreatePlayerObject = true;

        // 연결을 요청한 클라이언트가 호스트 자신인지 확인
        // LocalClientId: 서버(호스트) 자신의 클라이언트 ID
        // request.ClientNetworkId: 연결을 요청한 클라이언트의 ID
        // 두 값이 같으면 호스트, 다르면 외부 클라이언트
        bool isHost = request.ClientNetworkId == NetworkManager.Singleton.LocalClientId;

        // 스폰 위치 결정 (삼항 연산자 사용)
        // isHost가 true면 hostSpawnPosition, false면 clientSpawnPosition 사용
        response.Position = isHost ? hostSpawnPosition : clientSpawnPosition;

        // 스폰 시 회전값 설정 (Quaternion.identity는 회전 없음, 즉 정면을 바라봄)
        response.Rotation = Quaternion.identity;

        // 디버그 로그 출력: 어떤 클라이언트가 어느 위치에 스폰되었는지 확인
        Debug.Log($"플레이어 스폰 승인: ClientId={request.ClientNetworkId}, IsHost={isHost}, Position={response.Position}");
    }
    #endregion
}























