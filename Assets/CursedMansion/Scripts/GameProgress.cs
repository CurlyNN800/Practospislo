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
            if (GetComponent<PlayerDamageVfx>() == null)
                gameObject.AddComponent<PlayerDamageVfx>();
            if (GetComponent<ExtraMagazinesOnTables>() == null)
                gameObject.AddComponent<ExtraMagazinesOnTables>();
            if (GetComponent<LightSourceVisuals>() == null)
                gameObject.AddComponent<LightSourceVisuals>();
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
