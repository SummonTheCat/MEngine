
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIElemAssetItem : MonoBehaviour
{
    // References
    public TMP_Text txtAssetID;
    public Image imgAssetType;
    public TMP_Text txtAssetPath;
    public Button buttonEdit;
    public Button buttonDelete;

    public void Init(ResConfigModuleAsset asset)
    {
        if (asset == null) return;

        txtAssetID.text = asset.AssetID;
        txtAssetPath.text = asset.AssetPath;
        imgAssetType.sprite = UIPanelModKitCore.Instance.GetIconForAssetType(asset.Type);
        imgAssetType.color = UIPanelModKitCore.Instance.GetColorForAssetType(asset.Type);

        buttonEdit.onClick.RemoveAllListeners();
        buttonEdit.onClick.AddListener(() => UIPanelModKitCore.Instance.OnButtonEditClicked(asset));

        buttonDelete.onClick.RemoveAllListeners();
        buttonDelete.onClick.AddListener(() => UIPanelModKitCore.Instance.OnButtonDeleteClicked(asset));

        // Enable GameObject and components
        gameObject.SetActive(true);

        // And in the children
        foreach (var comp in GetComponentsInChildren<Behaviour>())
        {
            comp.enabled = true;
        }

    }

}