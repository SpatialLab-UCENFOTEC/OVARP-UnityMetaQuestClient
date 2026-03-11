using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class AnimController : MonoBehaviour
{
    Animator anim;
    public bool think = false;
    public bool notThink = false;
    public int t = 1, s = 2;
    void Start()
    {
        anim = gameObject.GetComponent<Animator>();
    }
    void Update()
    {
        if (think)
        {

            anim.SetTrigger("Think");
            think = false;

        }
        else if(notThink)
        {
            anim.SetTrigger("StopThinking");
            notThink = false;
        }
    }

    public void StartThinking()
    {
        think = true;
    }


    public void StopThinking()
    {
        notThink = true;
    }
}
