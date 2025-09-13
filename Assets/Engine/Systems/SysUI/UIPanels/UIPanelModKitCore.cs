using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelModKitCore : UIPanelCore
{
    // --------- References --------- //

    public static UIPanelModKitCore Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private GameObject templateAssetListEntry;
    [SerializeField] private Sprite[] assetTypeIcons = new Sprite[6]; // Match AssetType enum
    [SerializeField] private Color[] assetTypeColors = new Color[6];  // Match AssetType enum
    [SerializeField] private Color[] statusColors    = new Color[4];  // 0 = normal, 1 = success, 2 = error, 3 = warning

    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown dropdownTargetMod;

    [SerializeField] private TMP_InputField inputFieldModName;
    [SerializeField] private TMP_InputField inputFieldModVersion;
    [SerializeField] private TMP_Text       txtSaveStatus;
    [SerializeField] private Button         buttonSaveModInfo;

    [SerializeField] private TMP_InputField inputFieldAssetSortString;
    [SerializeField] private TMP_Dropdown   dropdownAssetSortType;
    [SerializeField] private Transform      contentAreaAssetList;

    [SerializeField] private TMP_InputField inputFieldNewAssetID;
    [SerializeField] private TMP_InputField inputFieldNewAssetPath;
    [SerializeField] private TMP_Dropdown   dropdownNewAssetType;
    [SerializeField] private Button         buttonAddNewAsset;

    // --------- Data --------- //

    [Header("Data")]
    [SerializeField] private string currentModID = "";
    [SerializeField] private ResConfigModuleAsset[] displayedAssets = new ResConfigModuleAsset[0];

    private readonly List<UIElemAssetItem> assetItemPool = new List<UIElemAssetItem>();

    // --------- Lifecycle --------- //

    public override void Init()
    {
        if (SysGame.Instance.GetGameMode() != GameMode.ModKit) return;

        base.Init();
        InitSingleton();
    }

    public override void Tick()
    {
        if (SysGame.Instance.GetGameMode() != GameMode.ModKit) return;
        base.Tick();
    }

    private void InitSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // --------- UI Management --------- //

    private void UpdateUIElements()
    {
        ResConfigGame gameConfig = SysResource.Instance.GameConfig;
        ResConfigModule[] moduleMaps = SysResource.Instance.LoadedModuleMaps;

        UpdateModuleSelection(moduleMaps);
        UpdateUIModuleDropdown(moduleMaps);

        var currentModule = SysResource.Instance.LoadedModuleMaps
            .FirstOrDefault(m => m.ModuleName == currentModID);

        UpdateUIModuleDetails(currentModule);

        // Build pool only once per module
        BuildAssetPool(currentModule);

        // Initial filter display
        UpdateUIAssetList(currentModule);

        HookSearchListeners(currentModule);
    }

    private void UpdateModuleSelection(ResConfigModule[] modules)
    {
        if (modules == null || modules.Length == 0) return;

        if (string.IsNullOrEmpty(currentModID))
            currentModID = modules[0].ModuleName;
    }

    private void UpdateUIModuleDropdown(ResConfigModule[] modules)
    {
        dropdownTargetMod.ClearOptions();
        if (modules == null || modules.Length == 0) return;

        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var module in modules)
            options.Add(new TMP_Dropdown.OptionData(module.ModuleName));

        dropdownTargetMod.AddOptions(options);

        int selectedIndex = 0;
        if (!string.IsNullOrEmpty(currentModID))
        {
            selectedIndex = modules.ToList()
                .IndexOf(modules.FirstOrDefault(m => m.ModuleName == currentModID));
            if (selectedIndex < 0) selectedIndex = 0;
        }
        dropdownTargetMod.value = selectedIndex;
        dropdownTargetMod.RefreshShownValue();

        dropdownTargetMod.onValueChanged.RemoveAllListeners();
        dropdownTargetMod.onValueChanged.AddListener(OnDropdownTargetModChanged);
    }

    private void UpdateUIModuleDetails(ResConfigModule module)
    {
        if (module == null) return;

        inputFieldModName.text    = module.ModuleName;
        inputFieldModVersion.text = module.Version;

        buttonSaveModInfo.onClick.RemoveAllListeners();
        buttonSaveModInfo.onClick.AddListener(() => OnButtonSaveModDetailsClicked(module));
    }

    private void HookSearchListeners(ResConfigModule module)
    {
        if (module == null) return;

        inputFieldAssetSortString.onValueChanged.RemoveAllListeners();
        inputFieldAssetSortString.onValueChanged.AddListener(_ => UpdateUIAssetList(module));

        dropdownAssetSortType.onValueChanged.RemoveAllListeners();
        dropdownAssetSortType.onValueChanged.AddListener(_ => UpdateUIAssetList(module));
    }

    // Create all child entries once per module to avoid runtime Instantiate in filter
    private void BuildAssetPool(ResConfigModule module)
    {
        if (module == null || module.ModuleAssets == null) return;

        // Clear old pool
        foreach (var item in assetItemPool)
        {
            if (item != null) Destroy(item.gameObject);
        }
        assetItemPool.Clear();

        if (templateAssetListEntry != null)
            templateAssetListEntry.SetActive(false);

        foreach (var asset in module.ModuleAssets)
        {
            var entry = Instantiate(templateAssetListEntry, contentAreaAssetList);
            entry.SetActive(false);
            var item = entry.GetComponent<UIElemAssetItem>();
            assetItemPool.Add(item);
        }
    }

    private void UpdateUIAssetList(ResConfigModule module)
    {
        if (module == null || module.ModuleAssets == null) return;
        if (assetItemPool.Count == 0) return;

        string searchString = inputFieldAssetSortString != null
            ? inputFieldAssetSortString.text.Trim().ToLowerInvariant()
            : string.Empty;

        int typeIndex = dropdownAssetSortType != null ? dropdownAssetSortType.value : 0;
        bool filterByType = typeIndex > 0; // 0 = All
        AssetType selectedType = filterByType ? (AssetType)(typeIndex - 1) : default;

        List<ResConfigModuleAsset> visible = new List<ResConfigModuleAsset>();

        for (int i = 0; i < module.ModuleAssets.Length; i++)
        {
            var asset = module.ModuleAssets[i];
            bool match = true;

            if (!string.IsNullOrEmpty(searchString))
            {
                match &= (asset.AssetID?.ToLowerInvariant().Contains(searchString) ?? false)
                         || (asset.AssetPath?.ToLowerInvariant().Contains(searchString) ?? false);
            }

            if (filterByType)
                match &= asset.Type == selectedType;

            var item = assetItemPool[i];
            if (item != null)
            {
                // Always refresh data so item is current
                item.Init(asset);
                item.gameObject.SetActive(match);
            }

            if (match) visible.Add(asset);
        }

        displayedAssets = visible.ToArray();
    }

    // ---------- UI Element Events ---------- //

    private void OnDropdownTargetModChanged(int index)
    {
        var gameConfig = SysResource.Instance.GameConfig;
        if (gameConfig == null || gameConfig.ModulePaths.Length == 0) return;

        currentModID = gameConfig.ModulePaths[index];

        var selectedModule = SysResource.Instance.LoadedModuleMaps
            .FirstOrDefault(m => m.ModuleName == currentModID);

        UpdateUIModuleDetails(selectedModule);
        BuildAssetPool(selectedModule);
        UpdateUIAssetList(selectedModule);
        HookSearchListeners(selectedModule);
    }

    private void OnButtonSaveModDetailsClicked(ResConfigModule module)
    {
        if (module == null) return;

        module.ModuleName = inputFieldModName.text;
        module.Version    = inputFieldModVersion.text;

        string modulePath = System.IO.Path.Combine(
            SysResource.Instance.gameDataPath,
            "Modules",
            module.ModuleName,
            "Module.json"
        );
        Debug.Log("Saving module to: " + modulePath);

        ModKitResIO.SaveModuleToFile(modulePath, module);

        SetSaveStatus($"Module \"{module.ModuleName}\" saved successfully.", 1);

        UpdateUIElements();
    }

    // --------- Utility Functions --------- //

    private void SetSaveStatus(string message, int statusType = 0)
    {
        if (txtSaveStatus == null) return;

        txtSaveStatus.text = message;
        if (statusColors != null && statusColors.Length > statusType)
            txtSaveStatus.color = statusColors[statusType];
    }

    public void OnButtonEditClicked(ResConfigModuleAsset asset)
    {
        Debug.Log($"Edit asset: {asset.AssetID}");
    }

    public void OnButtonDeleteClicked(ResConfigModuleAsset asset)
    {
        Debug.Log($"Delete asset: {asset.AssetID}");
    }

    public Sprite GetIconForAssetType(AssetType type)
    {
        int index = (int)type;
        if (assetTypeIcons != null && index >= 0 && index < assetTypeIcons.Length)
            return assetTypeIcons[index];
        return null;
    }

    public Color GetColorForAssetType(AssetType type)
    {
        int index = (int)type;
        if (assetTypeColors != null && index >= 0 && index < assetTypeColors.Length)
            return assetTypeColors[index];
        return Color.white;
    }

    // --------- Public API --------- //

    public void OpenModKit()
    {
        if (SysGame.Instance.GetGameMode() != GameMode.ModKit) return;
        UpdateUIElements();
        SetPanelIsActive(true);
    }
}
