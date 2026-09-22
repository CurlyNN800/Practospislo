using UnityEngine;

namespace CursedMansion
{
    /// <summary>
    /// Заглушка для старых сцен: раньше на объекте висел автор пути.
    /// Маршрут делается только в окне редактора: меню <b>CursedMansion → Path → Vehicle spline tool</b>.
    /// Этот компонент можно безопасно удалить с объекта.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class VehiclePathAuthor : MonoBehaviour
    {
    }
}
