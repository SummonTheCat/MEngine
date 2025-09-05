using UnityEngine;

public class SysLevelPhysics : MonoBehaviour
{
    // ---------- References ---------- //
    public static SysLevelPhysics Instance { get; private set; }

    // ---------- Data ---------- //

    // ---------- Lifecycle ---------- //

    public void Init()
    {
        InitSingleton();
    }

    public void Tick()
    {

    }

    private void InitSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


}