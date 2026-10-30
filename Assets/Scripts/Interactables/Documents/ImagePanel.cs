using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour
{
    public static ImagePanel Instance { get; private set; }

    [SerializeField] private ImageLibrary library;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image image;
    [SerializeField] private string closePrompt = "Right click / press [B] on controller to close";

    private PlayerInputReader input;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (image != null)
            image.preserveAspect = true;
    }

    private void Start()
    {
        panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (Instance == this)
            Instance = null;
    }

    public bool Open(string id, PlayerInputReader reader)
    {
        if (library == null || !library.TryGet(id, out Sprite sprite))
        {
            Debug.LogWarning($"ImagePanel: no image with id '{id}' in the library.", this);
            return false;
        }

        image.sprite = sprite;
        panelRoot.SetActive(true);

        Unsubscribe();
        input = reader;
        if (input != null)
            input.Released += Close;

        if (GameManager.Instance != null)
            GameManager.Instance.ShowHoldPrompt(closePrompt);

        return true;
    }

    public void Close()
    {
        if (!IsOpen) return;

        Unsubscribe();
        panelRoot.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.HideHoldPrompt();
    }

    private void Unsubscribe()
    {
        if (input != null)
            input.Released -= Close;

        input = null;
    }
}
