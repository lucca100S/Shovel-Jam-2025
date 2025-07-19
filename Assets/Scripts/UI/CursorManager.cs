using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] Texture2D cursorSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.SetCursor(cursorSprite, new Vector2(cursorSprite.width / 2, cursorSprite.height / 2), CursorMode.Auto);
    }
}
