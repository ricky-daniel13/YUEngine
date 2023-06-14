using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Footsetps : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource source;
    public AudioClip[] stepSounds;
    public AudioClip Jump, Land, Spin, Brake, SpinDash, SpinDashGo;
    void Step(AnimationEvent evt)
    {
        if (evt.animatorClipInfo.weight < 0.5f)
            return;
        source.PlayOneShot(stepSounds[Random.Range(0, stepSounds.Length)]);
    }
}
