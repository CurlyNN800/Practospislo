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
// Во время игры (Update): брошенный пистолет, провалившийся под пол или улетевший далеко от игрока, сразу
// возвращается в кобуру; такие же магазины — со стола обратно на стол, остальные удаляются.
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

    [Header("Улетевшие предметы (во время игры)")]
    [Tooltip("Брошенный предмет ниже этой высоты считается провалившимся под пол")]
    [SerializeField] float fallY = -1f;
    [Tooltip("Брошенный предмет дальше этого расстояния от игрока (м) считается улетевшим")]
    [SerializeField] float maxDistance = 10f;
    [Tooltip("Как часто проверять (с)")]
    [SerializeField] float checkInterval = 0.25f;

    // Насколько предмет может сдвинуться от своего места на столе и всё ещё считаться «лежащим на столе»
    const float HomeTolerance = 1f;
    float nextLostCheckTime;

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
        // Брошен на пол, улетел или лежит на столе — в кобуру, иначе на исходное место на столе.
        // В руке или уже в кобуре — не трогаем.
        ReturnFirearm(firearm, rig, manager);

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

    // Возвращает брошенный пистолет: в кобуру, а если она занята или не принимает — на исходное место на столе.
    // Пистолет в руке или в сокете не трогаем. Возвращает false, если вернуть было некуда.
    bool ReturnFirearm(Firearm firearm, Transform rig, XRInteractionManager manager)
    {
        var grab = firearm.GetComponent<XRGrabInteractable>();
        if (grab == null || grab.isSelected)
            return true;

        XRSocketInteractor holster = rig != null ? FindSocket(rig, HolsterSocketName) : null;
        if (holster != null && !holster.hasSelection &&
            (holster.interactionLayers.value & grab.interactionLayers.value) != 0)
        {
            // Сначала переносим пистолет к кобуре и гасим скорость: иначе с пола / из-под пола он «летит» к сокету
            // (при Velocity Tracking — сквозь стены и пол) и может не долететь
            Transform attach = holster.GetAttachTransform(grab);
            MoveTo(firearm.gameObject, new StartPose { position = attach.position, rotation = attach.rotation });
            manager.SelectEnter((IXRSelectInteractor)holster, (IXRSelectInteractable)grab);
            if (grab.isSelected)
                return true;
            Debug.LogWarning($"[LevelLoadout] Кобура не приняла {firearm.name} — кладём на стол.", firearm);
        }

        if (firearmPoses.TryGetValue(firearm, out StartPose pose))
        {
            MoveTo(firearm.gameObject, pose);
            return true;
        }

        Debug.LogWarning($"[LevelLoadout] Некуда вернуть {firearm.name}: кобура недоступна, исходного места на столе нет.", firearm);
        return false;
    }

    // --- Улетевшие предметы во время игры ---

    // Раз в checkInterval: брошенный пистолет ниже fallY или дальше maxDistance от игрока — сразу в кобуру;
    // брошенный магазин — со стола возвращаем на стол, остальные удаляем.
    void Update()
    {
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsPlaying)
            return;

        nextLostCheckTime -= Time.deltaTime;
        if (nextLostCheckTime > 0f)
            return;
        nextLostCheckTime = checkInterval;

        Camera head = Camera.main;
        if (head == null)
            return;
        Vector3 playerPosition = head.transform.position;

        XRInteractionManager manager = null;
        Transform rig = null;

        foreach (Firearm firearm in FindObjectsByType<Firearm>(FindObjectsSortMode.None))
        {
            var grab = firearm.GetComponent<XRGrabInteractable>();
            if (grab == null || grab.isSelected)
                continue;
            firearmPoses.TryGetValue(firearm, out StartPose home);
            if (!IsLost(firearm.transform.position, playerPosition, firearmPoses.ContainsKey(firearm), home))
                continue;

            if (manager == null)
            {
                manager = FindFirstObjectByType<XRInteractionManager>();
                rig = FindRigRoot();
                if (manager == null)
                    return;
            }
            Debug.Log($"[LevelLoadout] {firearm.name} улетел ({firearm.transform.position}) — возвращаем.", firearm);
            ReturnFirearm(firearm, rig, manager);
        }

        foreach (Magazine magazine in FindObjectsByType<Magazine>(FindObjectsSortMode.None))
        {
            var grab = magazine.GetComponent<XRGrabInteractable>();
            if (grab == null || grab.isSelected)
                continue;
            bool fromTable = tableMagazinePoses.TryGetValue(magazine, out StartPose home);
            if (!IsLost(magazine.transform.position, playerPosition, fromTable, home))
                continue;

            if (fromTable)
            {
                MoveTo(magazine.gameObject, home);
            }
            else
            {
                Debug.Log($"[LevelLoadout] Магазин {magazine.name} улетел ({magazine.transform.position}) — удалён.", magazine);
                Destroy(magazine.gameObject);
            }
        }
    }

    // Ниже пола — потерян всегда. Далеко от игрока — потерян, если это не предмет, спокойно лежащий у себя на столе
    // (игрок просто мог отойти от стола дальше maxDistance).
    bool IsLost(Vector3 position, Vector3 playerPosition, bool hasHome, StartPose home)
    {
        if (position.y < fallY)
            return true;
        float maxSqr = maxDistance * maxDistance;
        if ((position - playerPosition).sqrMagnitude <= maxSqr)
            return false;
        return !hasHome || (position - home.position).sqrMagnitude > HomeTolerance * HomeTolerance;
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
