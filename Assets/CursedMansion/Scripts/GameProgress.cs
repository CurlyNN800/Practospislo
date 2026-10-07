using UnityEngine;

namespace CursedMansion
{
    public enum GamePhase
    {
        OpeningRoad,
        MansionExterior,
        MansionInterior,
        EndingRoad
    }

    public enum FinaleOutcome
    {
        None,
        Victory,
        Defeat
    }

    /// <summary>
    /// Состояние курсового прохвала между сценами. DontDestroyOnLoad.
    /// </summary>
    public class GameProgress : MonoBehaviour
    {
        public static GameProgress Instance { get; private set; }

        [SerializeField] GamePhase phase = GamePhase.OpeningRoad;
        [SerializeField] FinaleOutcome lastFinale = FinaleOutcome.None;

        public GamePhase Phase => phase;
        public FinaleOutcome LastFinale => lastFinale;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (GetComponent<PlayerDeathToGameProgress>() == null)
                gameObject.AddComponent<PlayerDeathToGameProgress>();
            if (GetComponent<LightSourceVisuals>() == null)
                gameObject.AddComponent<LightSourceVisuals>();
            // PlayerDamageVfx и ExtraMagazinesOnTables зависят от сцены — см. ConfigureForScene
        }

        /// <summary>
        /// Вызывается из GameLifetimeHooks при каждой загрузке сцены.
        /// На арене (SampleScene) не нужны:
        ///  - ExtraMagazinesOnTables: сразу в OnEnable клонирует 2 магазина на объект с "Table" в имени,
        ///    а магазины там раздаёт LevelLoadout;
        ///  - PlayerDamageVfx: виньетка через пост-обработку, а на камере арены она выключена ради passthrough
        ///    (вспышку урона там рисует DamageFlash).
        /// В остальных сценах компоненты добавляются (или включаются обратно), как раньше.
        /// </summary>
        public void ConfigureForScene(string sceneName)
        {
            bool needed = sceneName != GameScenes.Arena;
            SetSceneComponent<PlayerDamageVfx>(needed);
            SetSceneComponent<ExtraMagazinesOnTables>(needed);
        }

        void SetSceneComponent<T>(bool needed) where T : Behaviour
        {
            var component = GetComponent<T>();
            if (needed)
            {
                if (component == null)
                    gameObject.AddComponent<T>();
                else
                    component.enabled = true;
            }
            else if (component != null)
            {
                component.enabled = false;
            }
        }

        public void SetPhase(GamePhase newPhase) => phase = newPhase;

        public void EnterMansionExterior()
        {
            phase = GamePhase.MansionExterior;
            lastFinale = FinaleOutcome.None;
        }

        public void EnterMansionInterior() => phase = GamePhase.MansionInterior;

        public void SetFinaleVictory()
        {
            lastFinale = FinaleOutcome.Victory;
            phase = GamePhase.EndingRoad;
        }

        public void SetFinaleDefeat()
        {
            lastFinale = FinaleOutcome.Defeat;
            phase = GamePhase.EndingRoad;
        }

        /// <summary>Сброс для нового прохождения из меню / отладки.</summary>
        public void ResetProgress()
        {
            phase = GamePhase.OpeningRoad;
            lastFinale = FinaleOutcome.None;
        }
    }
}
