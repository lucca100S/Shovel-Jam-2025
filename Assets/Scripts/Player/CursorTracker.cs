using UnityEngine;
using UnityEngine.InputSystem;

public class CursorTracker : MonoBehaviour
{
    public static CursorTracker Instance { get; private set; }

    public Vector3 cursorPos { get; private set; }

    CursorTypes activeCursor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Adicionar logica para detectar controle
        activeCursor = CursorTypes.Mouse;
    }

    // Update is called once per frame
    void Update()
    {
        switch (activeCursor)
        {
            case CursorTypes.Mouse:
                cursorPos = Mouse.current.position.ReadValue();
                break;
            case CursorTypes.ControllerRightStick:
                break;
        }
    }
}
