using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAdventure", menuName = "Sonic/Player/AdventureSettings", order = 1)]
public class AdventurePlayerSettings : ScriptableObject
{
    [Header("Collision")]
    public float tryGroundDistance = 0.5f;
    public float tryGroundDistanceFast = 0.25f;
    public LayerMask InteractionLayer;

    [Header("Movement")]
    public float acc = 10;
    public float dcc = 112, frc = 8, air = 26, topSpeed = 30, runSpeed = 20, slopeFactor = 20;
    public AnimationCurve accOverSpeed;
    public AnimationCurve maxSpeedOverPush;
    [Header("Roll")]
    public float rollSlopeUpFactor = 17.5f;
    public float rollSlopeDownFactor = 70, rollFrc = 6, rollDcl = 28, rollStopSpeed = 1, rollRotaSpeed = 45;

    [Header("Input Modes")]
    public AnimationCurve tangOverSpeed;
    public float inputBehindAngle = 135;
    public float inputBehindSlowAngle = 179, maxGroundSpeed = 30, tangentDrag = 110, jumpTangentDrag = 15, rotaModeSpeed = 300, rotaModeSpeedFast = 90, rotaModeSpeedMax = 30, rotaModeSpeedMin = 15, rotaModeSpdMul = 2;

    [Header("Actions")]
    public float jumpForce = 15;
    public float lowJumpSpeed = 6, quickForce = 30;

    [Header("Gravity")]
    public float gravityForce = 35;
    public float upGravityForce = 20;
}
