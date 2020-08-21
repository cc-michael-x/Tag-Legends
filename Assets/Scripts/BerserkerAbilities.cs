﻿using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BerserkerAbilities : MonoBehaviourPunCallbacks
{
    [Header("Config")]
    public Animator animator;
    public Rigidbody rig;
    public string berserkerAbilityResourceLocation = "Character/Berserker/";

    [Header("Shout Ability Config")]
    public string shoutActiveAnimFloatVar = "ShoutActive";
    public float shoutDurationEffect = 10f;
    public AudioSource leapAudioSource;
    public AudioSource axeThrowAudioSource;
    public AudioSource groundSlamAudioSource;
    public AudioSource shoutAudioSource;

    [Header("Axe Ability Config")]
    public float axeDurationEffect = 5f;

    public static BerserkerAbilities instance;

    private void Awake()
    {
        instance = this;
    }

    public void Leap()
    {
        leapAudioSource.Play();

        // Lift character up in ther air before applying velocity, I think friction occurs if this is not done and prevents velocity from being applied
        rig.transform.position = new Vector3(rig.transform.position.x, rig.transform.position.y + 0.5f, rig.transform.position.z);
        
        // Leap
        rig.velocity = new Vector3(transform.forward.x * 10f, 10f, transform.forward.z * 10.0f);
    }

    public void AxeThrow()
    {
        axeThrowAudioSource.Play();

        PhotonNetwork.Instantiate(
            berserkerAbilityResourceLocation + "Axe",
            transform.position + Vector3.up,
            gameObject.transform.rotation);
    }

    public void GroundSlam()
    {
        groundSlamAudioSource.Play();
        animator.SetTrigger("GroundSlam");
        PhotonNetwork.Instantiate(
            berserkerAbilityResourceLocation + "GroundSlam",
            transform.position,
            Quaternion.identity);
    }

    public void Shout()
    {
        shoutAudioSource.Play();
        animator.SetTrigger("Shout");
        // Set all other players feared active state
        AbilityManager.instance.photonView.RPC("BerserkerShout", RpcTarget.Others);
        PhotonNetwork.Instantiate(
            berserkerAbilityResourceLocation + "ShoutParticles",
            transform.position,
            Quaternion.identity);
    }
}
