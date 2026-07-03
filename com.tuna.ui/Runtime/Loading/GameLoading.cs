using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class GameLoading : MonoBehaviour
    {
        private const float MINIMUM_LOADING_TIME = 2.0f;

        private static GameLoading gameLoading;

        [SerializeField] Initializer initializer;
        [SerializeField] LoadingGraphics loadingGraphics;

        [Space]
        [SerializeField] bool useManualControl;
        [SerializeField] bool checkNetworkConnection = true;
        
        private static AsyncOperation loadingOperation;

        private static bool isReadyToHide;

        private static string loadingMessage;

        private Coroutine initCoroutine;

        public static int LoadingSceneBuildIndex = -1;

        private void Awake()
        {
            gameLoading = this;

            DontDestroyOnLoad(gameObject);
            
            loadingGraphics.Init(this);

            initCoroutine = StartCoroutine(BootstrapCoroutine());
        }

        private IEnumerator BootstrapCoroutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            initializer.Init();

            yield return ConnectionCheckCoroutine();
        }

        public void RetryConnection()
        {
            if(initCoroutine == null)
            {
                initCoroutine = StartCoroutine(ConnectionCheckCoroutine());
            }
        }

        private IEnumerator ConnectionCheckCoroutine()
        {
            loadingGraphics.HideErrorMessage();
            loadingGraphics.SetLoadingState(0.0f, "Checking connection..");

            if(checkNetworkConnection)
            {
                bool isConnected = false;

                NetworkConnection networkConnection = new NetworkConnection("https://google.com/");
                IEnumerator connectionCheck = networkConnection.CheckConnection((state) => isConnected = state);

                yield return connectionCheck;

                if (!isConnected)
                {
                    loadingGraphics.ShowErrorMessage("Connection error");

                    initCoroutine = null;

                    yield break;
                }
            }
            
            loadingGraphics.SetLoadingState(0.1f, "Loading..");
            

            initializer.InitModules();
            initializer.InitSDKs();
            
            yield return null;

            float realtimeSinceStartup = Time.realtimeSinceStartup;

            int sceneIndex = LoadingSceneBuildIndex;
            if(sceneIndex == -1)
            {
                sceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
                if (SceneManager.sceneCount < sceneIndex)
                    Debug.LogError("[Loading]: First scene is missing!");
            }

            float minimumFinishTime = realtimeSinceStartup + MINIMUM_LOADING_TIME;

            loadingOperation = SceneManager.LoadSceneAsync(sceneIndex);

            yield return null;

            loadingMessage = "Loading..";

            while (!loadingOperation.isDone || realtimeSinceStartup < minimumFinishTime)
            {
                yield return null;

                realtimeSinceStartup = Time.realtimeSinceStartup;

                loadingGraphics.SetLoadingState(Mathf.Lerp(0.2f, 0.9f, loadingOperation.progress), loadingMessage);
            }

            loadingGraphics.SetLoadingState(1.0f, "Completed");

            if (useManualControl)
            {
                while (!isReadyToHide)
                {
                    yield return null;
                }
            }

            loadingGraphics.OnLoadingFinished();

            Destroy(gameObject);
        }
        
    }
}