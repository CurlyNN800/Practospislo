using UnityEngine.SceneManagement;

namespace CursedMansion
{
    public static class SceneFlow
    {
        public static void LoadRoad(string spawnId = "Default") =>
            SceneTransitionRunner.TransitionTo(GameScenes.Road, spawnId, 1.5f, 1.5f);

        public static void LoadHouseScene(string spawnId = "Default") =>
            SceneTransitionRunner.TransitionTo(GameScenes.HouseScene, spawnId, 1.5f, 1.5f);

        public static void LoadInterior(string spawnId = "Default") =>
            SceneTransitionRunner.TransitionTo(GameScenes.Interior, spawnId, 1.5f, 1.5f);

        public static void LoadInsideHouse(string spawnId = "FromHouseScene") =>
            SceneTransitionRunner.TransitionTo(GameScenes.InsideHouse, spawnId, 1.5f, 1.5f);

        /// <summary>Мгновенная загрузка без fade (меню, отладка).</summary>
        public static void LoadImmediate(string sceneName) =>
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
