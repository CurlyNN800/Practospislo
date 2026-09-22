using CursedMansion;
using UnityEngine;

/// <summary>
/// Legacy-триггер на HouseScene (ковёр у входа). Использует общий SceneTransitionRunner.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NextScene : MonoBehaviour
{
    [Header("Куда грузить")]
    public string InsideHouse = GameScenes.InsideHouse;

    [Header("Спавн на новой сцене")]
    public string spawnId = "FromHouseScene";

    [Header("Затемнение")]
    public float fadeOutSeconds = 1.5f;
    public float fadeInSeconds = 1.5f;

    bool _loading;

    void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_loading || !SceneTransfer.IsPlayerCollider(other))
            return;

        if (string.IsNullOrEmpty(InsideHouse))
        {
            Debug.LogError("[NextScene] Имя сцены не указано.");
            return;
        }

        _loading = true;

        var gp = GameProgress.Instance;
        if (gp != null)
            gp.EnterMansionInterior();

        Debug.Log($"[NextScene] ? {InsideHouse}");
        SceneTransitionRunner.TransitionTo(InsideHouse, spawnId, fadeOutSeconds, fadeInSeconds);
    }
}
