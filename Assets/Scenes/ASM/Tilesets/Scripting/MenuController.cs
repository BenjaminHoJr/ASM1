using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public VisualElement ui;
    public Button play;
    public Button options;
    public Button quit;

    void Start()
    {
        ui = GetComponent<UIDocument>().rootVisualElement;
        play = ui.Q<Button>("Play");
        play.clicked += Play_clicked;
    }
    private void Play_clicked()
    {
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }
}
