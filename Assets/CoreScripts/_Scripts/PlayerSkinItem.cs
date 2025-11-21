//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using LootLocker.Requests;

///// <summary>
///// The UI representing a player skin item from LootLocker. Manages setting the color of the image and storing the value in LootLocker when the player selects a skin.
///// </summary>
//public class PlayerSkinItem : MonoBehaviour, IPointerClickHandler
//{
//    private string colorName;

//    /// <summary>
//    /// Called when the player clicks on the skin image. Sets the new player preferred skin in the LootLocker server.
//    /// </summary>
//    /// <param name="eventData">Information about the click event.</param>
//    public void OnPointerClick(PointerEventData eventData)
//    {
//        LootLockerSDKManager.UpdateOrCreateKeyValue("skin", colorName, (response) =>
//        {
//            if (response.success)
//            {
//                Debug.Log("Set player skin color key successfully.");
//            }
//            else
//            {
//                Debug.Log("Set player skin color key unsuccessfully.");
//            }
//        });
//    }

//    /// <summary>
//    /// Sets the color of the image to match the name stored in LootLocker.
//    /// </summary>
//    /// <param name="color">The color of the image/skin.</param>
//    public void SetColor(string color) {
//        if (ColorUtility.TryParseHtmlString(color, out Color newColor)) {
//            GetComponent<Image>().color = newColor;
//        }
//        colorName = color;
//    }
//}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// The UI representing a player skin item. Previously talked to online service
/// to store the selected skin; now it only updates the local UI.
/// </summary>
public class PlayerSkinItem : MonoBehaviour, IPointerClickHandler
{
    private string colorName;

    /// <summary>
    /// Called when the player clicks on the skin image.
    /// online service integration has been removed, so this now only logs locally.
    /// </summary>
    /// <param name="eventData">Information about the click event.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"PlayerSkinItem clicked. Selected color key = {colorName}. online service integration has been removed.");
        // If you want to persist this choice locally, you could use:
        // PlayerPrefs.SetString("selected_skin", colorName);
    }

    /// <summary>
    /// Sets the color of the image to match the given color string.
    /// </summary>
    /// <param name="color">The color of the image/skin.</param>
    public void SetColor(string color)
    {
        if (ColorUtility.TryParseHtmlString(color, out Color newColor))
        {
            GetComponent<Image>().color = newColor;
        }

        colorName = color;
    }
}
