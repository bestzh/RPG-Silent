using UnityEngine;

public sealed class TownNpcNameplate : MonoBehaviour
{
    Camera view;
    void LateUpdate()
    {
        if (view == null) view = Camera.main;
        if (view != null) transform.rotation = view.transform.rotation;
    }
}
