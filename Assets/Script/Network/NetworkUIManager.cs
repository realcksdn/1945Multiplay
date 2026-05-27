using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 네트워크 UI를 관리하는 클래스
/// UI 입력 처리 및 시각적 피드백 담당
/// </summary>
public class NetworkUiManager : MonoBehaviour
{
    #region 변수 선언
    [Header("UI 버튼 참조")]
    [SerializeField] private Button startHostButton;      // 호스트 시작 버튼
    [SerializeField] private Button startClientButton;    // 클라이언트 시작 버튼

    [Header("IP 주소 UI 참조")]
    [SerializeField] private TextMeshProUGUI ipText;          // 현재 IP 주소 표시 텍스트
    [SerializeField] private TMP_InputField  ipAddressInput;  // IP 주소 입력 필드

    [Header("버튼 텍스트 참조")]
    [SerializeField] private TextMeshProUGUI hostButtonText;    // 호스트 버튼의 텍스트
    [SerializeField] private TextMeshProUGUI clientButtonText;  // 클라이언트 버튼의 텍스트

    // 연결 상태를 추적하는 플래그 (이중 클릭 방지)
    private bool isConnecting = false;

    // 콜백이 이미 등록되었는지 확인하는 플래그 (중복 등록 방지)
    private bool callbacksRegistered = false;
    #endregion

    #region Unity 생명주기 메서드
    /// <summary>
    /// 스크립트가 활성화될 때 호출
    /// UI 초기화 및 이벤트 리스너 등록
    /// </summary>
    private void Start()
    {
        // NetworkConnectionManager 존재 여부 확인
        if (NetworkConnectionManager.Instance == null)
        {
            Debug.LogError("NetworkUiManager: NetworkConnectionManager를 찾을 수 없습니다!" +
                " 씬에 NetworkConnectionManager를 추가해주세요.");
            enabled = false;  // 스크립트 비활성화
            return;
        }

        // NetworkManager 존재 여부 확인
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkUiManager: NetworkManager.Singleton이 null입니다!" +
                " 씬에 NetworkManager를 추가해주세요.");
            enabled = false;  // 스크립트 비활성화
            return;
        }

        InitializeUI();              // UI 초기화
        RegisterButtonEvents();      // 버튼 이벤트 등록
        RegisterNetworkCallbacks();  // 네트워크 콜백 등록
    }

    /// <summary>
    /// 오브젝트가 파괴될 때 호출
    /// 등록한 콜백을 제거하여 메모리 누수 방지
    /// </summary>
    private void OnDestroy()
    {
        UnregisterButtonEvents();      // 버튼 이벤트 제거
        UnregisterNetworkCallbacks();  // 네트워크 콜백 제거
    }
    #endregion

    #region 초기화 메서드
    /// <summary>
    /// UI 요소들을 초기화하는 메서드
    /// 초기 상태 설정 및 기본값 지정
    /// </summary>
    private void InitializeUI()
    {
        // 로컬 IP 주소 가져오기
        string localIP = NetworkConnectionManager.Instance.GetLocalIPAddress();

        // IP 입력 필드 초기화
        if (ipAddressInput != null)
        {
            // 기본값으로 localhost 설정 (같은 컴퓨터에서 테스트용)
            ipAddressInput.text = localIP;

            // Placeholder 텍스트 설정
            if (ipAddressInput.placeholder is TextMeshProUGUI placeholder)
            {
                placeholder.text = "Enter IP Address...";
            }
        }

        // IP 표시 텍스트에 로컬 IP 주소 표시
        if (ipText != null)
        {
            ipText.text = $"IP: {localIP}";  // 변경된 부분
        }

        // 버튼 텍스트 스타일 초기화
        ResetTextStyle(hostButtonText);
        ResetTextStyle(clientButtonText);
    }

    /// <summary>
    /// 버튼 클릭 이벤트를 등록하는 메서드
    /// 각 버튼에 해당하는 메서드를 연결
    /// </summary>
    private void RegisterButtonEvents()
    {
        // 호스트 버튼 이벤트 등록
        if (startHostButton != null)
        {
            startHostButton.onClick.RemoveAllListeners();           // 기존 리스너 제거하여 중복 방지
            startHostButton.onClick.AddListener(OnClickStartHost);  // 클릭 시 OnClickStartHost 호출
        }
        else
        {
            Debug.LogError("NetworkUiManager: StartHostButton이 할당되지 않았습니다!");
        }

        // 클라이언트 버튼 이벤트 등록
        if (startClientButton != null)
        {
            startClientButton.onClick.RemoveAllListeners();             // 기존 리스너 제거하여 중복 방지
            startClientButton.onClick.AddListener(OnClickStartClient);  // 클릭 시 OnClickStartClient 호출
        }
        else
        {
            Debug.LogError("NetworkUiManager: StartClientButton이 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// 버튼 클릭 이벤트를 제거하는 메서드
    /// OnDestroy에서 메모리 누수 방지를 위해 호출
    /// </summary>
    private void UnregisterButtonEvents()
    {
        if (startHostButton != null)
        {
            startHostButton.onClick.RemoveAllListeners();    // 호스트 버튼 이벤트 제거
        }

        if (startClientButton != null)
        {
            startClientButton.onClick.RemoveAllListeners();  // 클라이언트 버튼 이벤트 제거
        }
    }

    /// <summary>
    /// 네트워크 연결 관련 콜백을 등록하는 메서드
    /// 클라이언트 연결/해제 이벤트 처리
    /// </summary>
    private void RegisterNetworkCallbacks()
    {
        // 아직 콜백이 등록되지 않은 경우에만 등록
        if (!callbacksRegistered)
        {
            // 클라이언트 연결 성공 시 호출될 콜백 등록
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // 클라이언트 연결 해제 시 호출될 콜백 등록
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // 콜백 등록 완료 플래그 설정 (중복 등록 방지)
            callbacksRegistered = true;
        }
    }

    /// <summary>
    /// 네트워크 콜백을 제거하는 메서드
    /// OnDestroy에서 메모리 누수 방지를 위해 호출
    /// </summary>
    private void UnregisterNetworkCallbacks()
    {
        // NetworkManager가 존재하고 콜백이 등록되어 있는 경우에만 제거
        if (NetworkManager.Singleton != null && callbacksRegistered)
        {
            // 등록된 콜백 제거
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            // 콜백 등록 플래그 초기화
            callbacksRegistered = false;
        }
    }
    #endregion

    #region 버튼 클릭 이벤트 처리
    /// <summary>
    /// 호스트 시작 버튼 클릭 시 호출되는 메서드
    /// NetworkConnectionManager를 통해 호스트 모드 시작
    /// </summary>
    private void OnClickStartHost()
    {
        // 이미 연결 중이면 무시 (이중 클릭 방지)
        if (isConnecting) return;

        // 연결 시도 플래그 설정
        isConnecting = true;

        // NetworkConnectionManager를 통해 호스트 시작
        bool success = NetworkConnectionManager.Instance.StartHost();

        // 호스트 시작 성공 시
        if (success)
        {
            // UI 업데이트: 버튼 비활성화 및 텍스트 강조
            DisableBothButtons();
            MakeTextBold(hostButtonText);

            // IP 주소 표시 업데이트
            UpdateIPDisplay();
        }
        else
        {
            // 시작 실패 시 플래그 초기화
            isConnecting = false;
        }
    }

    /// <summary>
    /// 클라이언트 시작 버튼 클릭 시 호출되는 메서드
    /// 입력된 IP 주소로 호스트에 연결 시도
    /// </summary>
    private void OnClickStartClient()
    {
        // 이미 연결 중이면 무시 (이중 클릭 방지)
        if (isConnecting) return;

        // IP 입력 필드에서 입력된 IP 주소 가져오기
        string ipAddress = ipAddressInput != null ? ipAddressInput.text.Trim() : "127.0.0.1";

        // IP 주소가 비어있는지 확인
        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogError("NetworkUiManager: IP 주소를 입력해주세요!");
            return;
        }

        // IP 주소 형식 유효성 검사 (NetworkConnectionManager에 위임)
        if (!NetworkConnectionManager.Instance.IsValidIPAddress(ipAddress))
        {
            Debug.LogError($"NetworkUiManager: 유효하지 않은 IP 주소입니다: {ipAddress}");
            return;
        }

        // 연결 시도 플래그 설정
        isConnecting = true;

        // NetworkConnectionManager를 통해 클라이언트 시작
        bool success = NetworkConnectionManager.Instance.StartClient(ipAddress);

        // 클라이언트 시작 성공 시
        if (success)
        {
            // UI 업데이트: 버튼 비활성화 및 텍스트 강조
            DisableBothButtons();
            MakeTextBold(clientButtonText);

            // IP 표시를 "연결 중..."으로 업데이트
            if (ipText != null)
            {
                ipText.text = $"IP: {ipAddress}";
            }
        }
        else
        {
            // 시작 실패 시 플래그 초기화
            isConnecting = false;
        }
    }
    #endregion

    #region 네트워크 콜백 처리
    /// <summary>
    /// 클라이언트가 서버에 연결되었을 때 호출되는 콜백
    /// 연결 성공 시 로그 출력 및 UI 상태 업데이트
    /// </summary>
    /// <param name="clientId">연결된 클라이언트의 고유 ID</param>
    private void OnClientConnected(ulong clientId)
    {
        // NetworkManager가 null인 경우 안전장치
        if (NetworkManager.Singleton == null) return;

        // 로컬 클라이언트(자신)가 연결된 경우만 처리
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"클라이언트 연결 성공 (ClientId: {clientId})");

            // 클라이언트로 연결된 경우 (호스트가 아닌 경우)
            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                if (ipText != null)
                {
                    ipText.text = "IP: Connected";
                }
            }
        }
    }

    /// <summary>
    /// 클라이언트가 서버에서 연결 해제되었을 때 호출되는 콜백
    /// 연결 종료 시 UI를 초기 상태로 복원
    /// </summary>
    /// <param name="clientId">연결 해제된 클라이언트의 고유 ID</param>
    private void OnClientDisconnected(ulong clientId)
    {
        // NetworkManager가 null인 경우 안전장치
        if (NetworkManager.Singleton == null) return;

        // 로컬 클라이언트(자신)가 연결 해제된 경우만 처리
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"클라이언트 연결 해제 (ClientId: {clientId})");

            // 연결 플래그 초기화
            isConnecting = false;

            // UI 복원: 버튼 다시 활성화 및 텍스트 스타일 초기화
            EnableBothButtons();
            ResetTextStyle(hostButtonText);
            ResetTextStyle(clientButtonText);

            // IP 표시 초기화
            if (ipText != null)
            {
                ipText.text = "IP: Disconnected";
            }
        }
    }
    #endregion

    #region UI 업데이트 메서드
    /// <summary>
    /// IP 주소 표시를 업데이트하는 메서드
    /// 호스트가 시작되면 로컬 IP 주소를 표시
    /// </summary>
    private void UpdateIPDisplay()
    {
        // 필수 컴포넌트 확인
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        // 호스트로 실행 중인 경우에만 IP 주소 표시
        if (NetworkManager.Singleton.IsHost)
        {
            // NetworkConnectionManager에서 로컬 IP 주소 가져오기
            string localIP = NetworkConnectionManager.Instance.GetLocalIPAddress();

            // IP 표시 텍스트 업데이트
            if (ipText != null)
            {
                ipText.text = $"IP: {localIP}";
            }

            // IP 입력 필드에도 호스트 IP 주소 표시
            if (ipAddressInput != null)
            {
                ipAddressInput.text = localIP;
            }
        }
    }

    /// <summary>
    /// 두 버튼을 모두 비활성화하는 메서드
    /// 이중 클릭 방지 및 선택 후 변경 불가 처리
    /// </summary>
    private void DisableBothButtons()
    {
        if (startHostButton != null)
        {
            startHostButton.interactable = false;  // 호스트 버튼 비활성화
        }

        if (startClientButton != null)
        {
            startClientButton.interactable = false;  // 클라이언트 버튼 비활성화
        }
    }

    /// <summary>
    /// 두 버튼을 모두 활성화하는 메서드
    /// 연결 해제 시 다시 선택 가능하도록 복원
    /// </summary>
    private void EnableBothButtons()
    {
        if (startHostButton != null)
        {
            startHostButton.interactable = true;  // 호스트 버튼 활성화
        }

        if (startClientButton != null)
        {
            startClientButton.interactable = true;  // 클라이언트 버튼 활성화
        }
    }

    /// <summary>
    /// 텍스트를 진하게(Bold) 만드는 메서드
    /// 선택된 버튼을 시각적으로 강조
    /// </summary>
    /// <param name="textComponent">진하게 만들 TextMeshProUGUI 컴포넌트</param>
    private void MakeTextBold(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            textComponent.fontStyle = FontStyles.Bold;  // 텍스트를 진하게 표시
        }
    }

    /// <summary>
    /// 텍스트 스타일을 기본값(Normal)으로 복원하는 메서드
    /// 연결 해제 시 원래 상태로 되돌림
    /// </summary>
    /// <param name="textComponent">복원할 TextMeshProUGUI 컴포넌트</param>
    private void ResetTextStyle(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            textComponent.fontStyle = FontStyles.Normal;  // 일반 텍스트로 복원
        }
    }
    #endregion
}

