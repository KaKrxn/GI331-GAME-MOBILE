//using System.Collections;
//using UnityEngine;
//using LootLocker.Requests;
//using UnityEngine.Events;

///// <summary>
///// Manages the creation of the LootLocker instance.
///// </summary>
//public class GameManager : MonoBehaviour
//{
//    [SerializeField, Tooltip("Called when the LootLocker instance is created.")]
//    private UnityEvent playerConnected;

//    private IEnumerator Start() {
//        bool connected = false;
//        LootLockerSDKManager.StartGuestSession((response) =>
//        {
//            if (!response.success)
//            {
//                Debug.Log("Error starting LootLocker session");
//                return;
//            }
//            Debug.Log("Successfully LootLocker Session");
//            connected = true;
//        });
//        yield return new WaitUntil(() => connected);
//        playerConnected.Invoke();
//    }
//}
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages basic game initialization. online service integration has been removed.
/// </summary>
public class GameManagers : MonoBehaviour
{
    [SerializeField, Tooltip("Called when the game has finished initial setup.")]
    private UnityEvent playerConnected;

    private IEnumerator Start()
    {
        // Previously this would wait for a online service guest session.
        // Now we simply invoke the event after one frame.
        yield return null;

        if (playerConnected != null)
        {
            playerConnected.Invoke();
        }
    }
}

