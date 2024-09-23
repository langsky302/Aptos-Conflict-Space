using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;

public class OpenWebPage : MonoBehaviour
{
    // Đường dẫn URL bạn muốn mở
    public string url = "https://www.aptosfaucet.com/";

    // Tham chiếu đến Button trong UI
    public Button yourButton;

    void Start()
    {
        // Gán sự kiện khi click vào nút
        yourButton.onClick.AddListener(OpenURL);
    }

    void OpenURL()
    {
        // Mở trang web
        Application.OpenURL(url);
    }
}
