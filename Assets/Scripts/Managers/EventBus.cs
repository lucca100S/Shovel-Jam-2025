using System;
using UnityEngine;

public class EventBus : MonoBehaviour
{
    public static EventBus Instance { get; private set; }

    public Action landedOnGround = () => { };
    public Action hookAttached = () => { };
    public Action hookReleased = () => { };
    public Func<bool> checkOnGround = () => false;

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
    }
}
