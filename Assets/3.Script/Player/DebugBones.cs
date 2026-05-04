using UnityEngine;

public class DebugBones : MonoBehaviour
{
    private void Start()
    {
        Animator anim = GetComponent<Animator>();
        if (anim == null) return;

        foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone == HumanBodyBones.LastBone) continue;
            Transform t = anim.GetBoneTransform(bone);
            if (t != null)
                Debug.Log($"{bone} ¡æ {t.name} (path: {GetPath(t)})");
        }
    }

    private string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}