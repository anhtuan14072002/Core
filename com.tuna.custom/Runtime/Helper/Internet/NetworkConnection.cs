using System.Collections;
using UnityEngine;
using System;
using UnityEngine.Networking;

namespace Core
{
    public class NetworkConnection
    {
        private readonly string SERVER_URL = "https://google.com";

        public NetworkConnection(string url)
        {
            SERVER_URL = url;
        }

        public IEnumerator CheckConnection(Action<bool> onConnectionChecked)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                yield return new WaitForSecondsRealtime(0.5f);

                onConnectionChecked?.Invoke(false);

                yield break;
            }

            bool result;
            using (UnityWebRequest request = UnityWebRequest.Head(SERVER_URL))
            {
                request.timeout = 5;

                yield return request.SendWebRequest();

                result = request.result != UnityWebRequest.Result.ConnectionError  && request.result != UnityWebRequest.Result.ProtocolError && request.responseCode == 200;
            }

            onConnectionChecked?.Invoke(result);
        }
    }
}