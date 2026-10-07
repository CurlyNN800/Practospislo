using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRFPSKit;

// Полное восстановление снаряжения на каждый запуск уровня: старт из меню, «Заново», «Следующий уровень»
// (все три идут через GameStateManager.StartLevel → RestoreLoadout).
//  - пистолет: если брошен (его никто не держит) — в кобуру на плитоноске, а если кобура занята — на его место на столе;
//    в пистолете полный магазин, патрон в патроннике, затвор закрыт, курок взведён — готов стрелять;
//  - подсумки Magazine Socket 1..3: полные магазины (новые или дозаряженные);
//  - магазины на столе (те, что лежали в сцене на старте) возвращаются на стол полными;
//    все остальные брошенные магазины (пустые и нет) удаляются.
// Магазины кладутся в сокеты как в CursedMansion.PlayerSpawnLoadout: Instantiate в точке сокета
// и XRInteractionManager.SelectEnter. Предметы в руке игрока не отбираем, только дозаряжаем.
public class LevelLoadout : MonoBehaviour
{
    const string HolsterSocketName = "Pistol Holster Socket";
    static readonly string[] MagazineSocketNames =
    {
        "Magazine Socket 1",
        "Magazine Socket 2",
        "Magazine Socket 3",
    };

    // M17 17rd Magazine
    [SerializeField] GameObject magazinePrefab;

    struct StartPose
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    // Где пистолеты и магазины лежали при загрузке сцены (стол)
    readonly Dictionary<Firearm, StartPose> firearmPoses = new();
    readonly Dictionary<Magazine, StartPose> tableMagazinePoses = new();

    // Start сцены, до первого запуска уровня: запоминаем раскладку стола
    void Start()
    {
        foreach (Firearm firearm in FindObjectsByType<Firearm>(FindObjectsSortMode.None))
            firearmPoses[firearm] = PoseOf(firearm.transform);

        foreach (Magazine magazine in FindObjectsByType<Magazine>(FindObjectsSortMode.None))
        {
            var grab = magazine.GetComponent<XRGrabInteractable>();
            if (grab != null && !grab.isSelected)
                tableMagazinePoses[magazine] = PoseOf(magazine.transform);
        }
    }

    public void RestoreLoadout()
    {
        XRInteractionManager manager = FindFirstObjectByType<XRInteractionManager>();
        if (manager == null)
        {
            Debug.LogWarning("[LevelLoadout] Не найден XRInteractionManager — снаряжение не восстановлено.");
            return;
        }

        Transform rig = FindRigRoot();
        if (rig == null)
            Debug.LogWarning("[LevelLoadout] Не найден риг игрока (XROrigin) — кобура и подсумки пропущены.");

        int pistols = 0;
        foreach (Firearm firearm in FindObjectsByType<Firearm>(FindObjectsSortMode.None))
        {
            RestoreFirearm(firearm, rig, manager);
            pistols++;
        }

        int created = 0, refilled = 0;
        if (rig != null)
        {
            foreach (string socketName in MagazineSocketNames)
            {
                XRSocketInteractor socket = FindSocket(rig, socketName);
                if (socket == null)
                {
                    Debug.LogWarning($"[LevelLoadout] На риге нет сокета '{socketName}'.");
                    continue;
                }

                if (socket.hasSelection)
                {
                    // В подсумке уже что-то есть: магазин дозаряжаем, чужой предмет не трогаем
                    Magazine existing = socket.interactablesSelected[0].transform.GetComponent<Magazine>();
                    if (existing != null)
                    {
                        Refill(existing);
                        refilled++;
                    }
                    continue;
                }

                if (PutNewMagazine(socket, manager) != null)
                    created++;
            }
        }

        // В самом конце: к этому моменту выброшенный из пистолета магазин уже отсоединён и тоже считается брошенным
        int removed = CleanUpLooseMagazines(out int returned, out int refilledInHands);

        Debug.Log($"[LevelLoadout] Снаряжение восстановлено: пистолетов {pistols}; подсумки — новых магазинов {created}, " +
                  $"дозаряжено {refilled}; на стол возвращено {returned}; в руках дозаряжено {refilledInHands}; удалено брошенных {removed}.");
    }

    // --- Пистолет ---

    void RestoreFirearm(Firearm firearm, Transform rig, XRInteractionManager manager)
    {
        var grab = firearm.GetComponent<XRGrabInteractable>();

        // Брошен на пол (или лежит на столе) — в кобуру, иначе на исходное место на столе.
        // В руке или уже в кобуре — не трогаем.
        if (grab != null && !grab.isSelected)
        {
            XRSocketInteractor holster = rig != null ? FindSocket(rig, HolsterSocketName) : null;
            if (holster != null && !holster.hasSelection &&
                (holster.interactionLayers.value & grab.interactionLayers.value) != 0)
            {
                manager.SelectEnter((IXRSelectInteractor)holster, (IXRSelectInteractable)grab);
            }
            else if (firearmPoses.TryGetValue(firearm, out StartPose pose))
            {
                MoveTo(firearm.gameObject, pose);
            }
        }

        // Магазин в пистолете
        Magazine magazine = null;
        var magazineSocket = firearm.GetComponentInChildren<MagazineInteractor>(true);
        if (magazineSocket != null)
        {
            if (magazineSocket.hasSelection)
            {
                // Магазин мог быть «выброшен» кнопкой: ещё висит в сокете, но пистолет его уже не использует.
                // Такой отсоединяем (он станет брошенным и удалится) и ставим новый.
                if (firearm.magazine == null)
                    magazineSocket.DetachMagazine();
                else
                    magazine = magazineSocket.GetAttachedMagazine();
            }

            if (magazine == null && !magazineSocket.hasSelection)
                magazine = PutNewMagazine(magazineSocket, manager);
        }
        else
        {
            magazine = firearm.magazine;
        }

        if (magazine != null)
            Refill(magazine);
        else
            Debug.LogWarning($"[LevelLoadout] Не удалось вставить магазин в {firearm.name}.", firearm);

        // Патрон в патроннике, затвор закрыт, курок взведён
        Caliber caliber = magazine != null ? magazine.caliber : PrefabCaliber();
        firearm.chamberCartridge = new Cartridge(caliber, BulletType.FMJ);
        firearm.isHammerCocked = true;
        firearm.isActionOpen = false;

        var action = firearm.GetComponent<FirearmCyclingAction>();
        if (action != null)
        {
            action.isLockedBack = false;
            action.actionPosition01 = 0f;
        }
    }

    // --- Магазины ---

    // Создаёт полный магазин и вставляет его в сокет (подсумок или пистолет)
    Magazine PutNewMagazine(XRSocketInteractor socket, XRInteractionManager manager)
    {
        if (magazinePrefab == null)
        {
            Debug.LogWarning("[LevelLoadout] Не назначен префаб магазина.", this);
            return null;
        }

        Transform attach = socket.attachTransform != null ? socket.attachTransform : socket.transform;
        GameObject instance = Instantiate(magazinePrefab, attach.position, attach.rotation);
        var grab = instance.GetComponent<XRGrabInteractable>();
        var magazine = instance.GetComponent<Magazine>();
        if (grab == null || magazine == null)
        {
            Debug.LogWarning($"[LevelLoadout] У {magazinePrefab.name} нет XRGrabInteractable или Magazine.");
            Destroy(instance);
            return null;
        }

        // Заполняем сразу, не дожидаясь Magazine.Start (он потом ничего не добавит: магазин уже полный)
        Refill(magazine);
        manager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)grab);
        return magazine;
    }

    static void Refill(Magazine magazine)
    {
        magazine.cartridges.Clear();
        magazine.AddCartridgeToTop(new Cartridge(magazine.caliber, BulletType.FMJ), magazine.capacity);
    }

    // Магазины-предметы, которые никто не держит (не в руке, не в пистолете, не в подсумке):
    // стартовые магазины стола — полными обратно на стол, остальные удаляем.
    // Магазин в руке игрока дозаряжаем.
    int CleanUpLooseMagazines(out int returned, out int refilledInHands)
    {
        int removed = 0;
        returned = 0;
        refilledInHands = 0;
        foreach (Magazine magazine in FindObjectsByType<Magazine>(FindObjectsSortMode.None))
        {
            var grab = magazine.GetComponent<XRGrabInteractable>();
            if (grab == null)
                continue;

            if (grab.isSelected)
            {
                if (grab.interactorsSelecting[0] is XRBaseInputInteractor)
                {
                    Refill(magazine);
                    refilledInHands++;
                }
                continue;
            }

            if (tableMagazinePoses.TryGetValue(magazine, out StartPose pose))
            {
                Refill(magazine);
                MoveTo(magazine.gameObject, pose);
                returned++;
            }
            else
            {
                Destroy(magazine.gameObject);
                removed++;
            }
        }
        return removed;
    }

    Caliber PrefabCaliber()
    {
        Magazine prefabMagazine = magazinePrefab != null ? magazinePrefab.GetComponent<Magazine>() : null;
        return prefabMagazine != null ? prefabMagazine.caliber : default;
    }

    // --- Общее ---

    static StartPose PoseOf(Transform t) => new StartPose { position = t.position, rotation = t.rotation };

    static void MoveTo(GameObject go, StartPose pose)
    {
        var body = go.GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        go.transform.SetPositionAndRotation(pose.position, pose.rotation);
        if (body != null)
        {
            body.position = pose.position;
            body.rotation = pose.rotation;
        }
    }

    static Transform FindRigRoot()
    {
        XROrigin origin = FindFirstObjectByType<XROrigin>();
        if (origin != null)
            return origin.transform;
        return Camera.main != null ? Camera.main.transform.root : null;
    }

    static XRSocketInteractor FindSocket(Transform rig, string socketName)
    {
        foreach (XRSocketInteractor socket in rig.GetComponentsInChildren<XRSocketInteractor>(true))
        {
            if (socket.name == socketName)
                return socket;
        }
        return null;
    }
}
