using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

// Лучи (XRRayInteractor) на руках игрока работают только когда открыт UI: меню, выбор уровня, пауза, победа, поражение.
// Во время игры (GameState.Playing) лучи и их визуал (линия, курсор) выключены, чтобы не мешали стрелять.
// Лучи висят на тех же объектах контроллеров, что и XRDirectInteractor, поэтому выключаем компоненты, а не GameObject:
// хват ближней рукой остаётся всегда.
// Добавляется автоматически из GameStateManager.Awake.
public class MenuRayToggle : MonoBehaviour
{
    GameStateManager manager;

    void OnEnable()
    {
        manager = GameStateManager.Instance != null ? GameStateManager.Instance : GetComponent<GameStateManager>();
        if (manager == null)
        {
            Debug.LogWarning("[MenuRayToggle] Не найден GameStateManager — лучи не переключаются.", this);
            return;
        }
        manager.StateChanged += OnStateChanged;
        // Состояние могло смениться до подписки — применяем текущее сразу
        OnStateChanged(manager.State);
    }

    void OnDisable()
    {
        if (manager != null)
            manager.StateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        SetRaysEnabled(state != GameState.Playing);
    }

    static void SetRaysEnabled(bool enabled)
    {
        Transform rig = FindRigRoot();
        if (rig == null)
        {
            Debug.LogWarning("[MenuRayToggle] Не найден риг игрока (XROrigin) — лучи не переключены.");
            return;
        }

        // Ищем заново при каждой смене состояния: смена редкая, а руки могли пересоздаться
        foreach (XRRayInteractor ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
        {
            // Включаем: сначала луч, потом визуал (линии нужен рабочий луч).
            // Выключаем: сначала визуал — XRInteractorLineVisual.OnDisable сам прячет LineRenderer и курсор (reticle).
            if (enabled)
                ray.enabled = true;
            foreach (XRInteractorLineVisual visual in ray.GetComponents<XRInteractorLineVisual>())
                visual.enabled = enabled;
            if (!enabled)
            {
                var line = ray.GetComponent<LineRenderer>();
                if (line != null)
                    line.enabled = false;
                ray.enabled = false;
            }
        }
    }

    static Transform FindRigRoot()
    {
        XROrigin origin = FindFirstObjectByType<XROrigin>();
        if (origin != null)
            return origin.transform;
        return Camera.main != null ? Camera.main.transform.root : null;
    }
}
