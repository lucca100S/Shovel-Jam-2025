using UnityEngine;

[CreateAssetMenu(fileName = "JointParameters", menuName = "Scriptable Objects/Joint")]
public class JointParameters : ScriptableObject
{
    public float maxDistanceModifier;
    public float minDistanceModifier;
    public float spring;
    public float damper;
    public float massScale;
}
