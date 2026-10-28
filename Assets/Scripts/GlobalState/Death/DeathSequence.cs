using System.Collections;
using UnityEngine;

public class DeathSequence : MonoBehaviour
{
    [SerializeField] private HallwayFire fire;
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float blackHoldDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 1.5f;

    public bool IsDying { get; private set; }

    private void Awake()
    {
        if (fire == null)
            fire = GetComponent<HallwayFire>();
    }

    public void Begin()
    {
        if (IsDying) return;

        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        IsDying = true;

        GameManager gameManager = GameManager.Instance;
        GameObject player = gameManager != null ? gameManager.CurrentPlayer : null;
        LockInteraction(player);

        Stage stage = gameManager != null ? gameManager.CurrentStage : null;

        if (fire != null && stage != null)
            yield return fire.Burn(stage);

        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.FadeOut(fadeOutDuration);

        yield return new WaitForSeconds(fadeOutDuration);

        if (fire != null)
            fire.Stop();

        if (gameManager != null)
            gameManager.ResetCurrentStage();

        yield return new WaitForSeconds(blackHoldDuration);

        if (ScreenFadeManager.Instance != null)
            ScreenFadeManager.Instance.FadeIn(fadeInDuration);

        IsDying = false;
    }

    private void LockInteraction(GameObject player)
    {
        if (player == null) return;

        PlayerInteractor interactor = player.GetComponentInChildren<PlayerInteractor>();
        if (interactor != null)
            interactor.enabled = false;

        PlayerMovement movement = player.GetComponentInChildren<PlayerMovement>();
        if (movement != null)
            movement.Frozen = false;

        PlayerLook look = player.GetComponentInChildren<PlayerLook>();
        if (look != null)
            look.Frozen = false;
    }
}