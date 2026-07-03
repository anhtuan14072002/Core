using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Core
{
    public class InternetServices : MonoBehaviour
    {
        [SerializeField] private GameObject _popupNoInterNet;

        [SerializeField] private Button _tryAgain;

        [SerializeField] private bool _isOpenSetting;

        [SerializeField] private float _checkInterval = 2f;

        [SerializeField] private string _checkUrl = "https://google.com/";

        private Coroutine _checkInternetCoroutine;

        private bool _isChecking;

        private void Awake()
        {
            if (_tryAgain != null)
            {
                _tryAgain.onClick.RemoveListener(OnTryAgainClicked);
                _tryAgain.onClick.AddListener(OnTryAgainClicked);
            }

            SetPopupActive(false);
        }

        private void OnEnable()
        {
            _checkInternetCoroutine = StartCoroutine(CheckInternetLoop());
        }

        private void OnDisable()
        {
            if (_checkInternetCoroutine != null)
            {
                StopCoroutine(_checkInternetCoroutine);
                _checkInternetCoroutine = null;
            }

            if (_tryAgain != null)
            {
                _tryAgain.onClick.RemoveListener(OnTryAgainClicked);
            }
        }

        private IEnumerator CheckInternetLoop()
        {
            while (true)
            {
                yield return CheckInternetAndUpdatePopup();

                yield return new WaitForSecondsRealtime(_checkInterval);
            }
        }

        private void OnTryAgainClicked()
        {
            if (_isOpenSetting)
            {
                OpenSetting();
                return;
            }

            if (!_isChecking)
            {
                StartCoroutine(CheckInternetAndUpdatePopup());
            }
        }

        private IEnumerator CheckInternetAndUpdatePopup()
        {
            _isChecking = true;

            bool hasInternet = false;

            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                using (UnityWebRequest request = UnityWebRequest.Head(_checkUrl))
                {
                    request.timeout = 5;

                    yield return request.SendWebRequest();

                    hasInternet = request.result == UnityWebRequest.Result.Success;
                }
            }

            SetPopupActive(!hasInternet);

            _isChecking = false;
        }

        private void SetPopupActive(bool isActive)
        {
            if (_popupNoInterNet != null && _popupNoInterNet.activeSelf != isActive)
            {
                _popupNoInterNet.SetActive(isActive);
            }
        }

        private void OpenSetting()
        {
            DeviceSettingsOpener.OpenWifiSettings();
        }
    }
}