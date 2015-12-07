﻿using UnityEngine;
using System.Collections;

public enum PlayerState
{
    Idle,
    TurnRight,
    TurnLeft,
    Jump,
    Slide
}


public class PlayerBase : MonoBehaviour
{
    //WORKABLE
    protected float runSpeed = 2f; //tiles per second.
    protected float jumpForce = 1f;
    protected float gravity = 2f;

    //DONT TOUCH
    protected float deceleration;
    protected float acceleration;
    protected float turnSpeed;
    protected float crntSpeed;
    protected bool isJumping = false;
    protected bool isFalling = false;
    protected Rigidbody rb;
    protected float rotationTarget;
    protected float rotationLast;
    protected Quaternion qTo = Quaternion.identity;
    protected int turnPhase = 0;
    protected bool turnRotation;
    protected bool isSliding = false;
    protected float crntSlideLength;
    protected BoxCollider bc;
    protected Animator anim;
    protected bool isFirstFrame = true;
    protected PlayerState nextAction = PlayerState.Idle;
    protected bool isActionActive = false;
    protected float crntJumpForce;


    // Use this for initialization
    void Awake ()
    {
        crntSpeed = runSpeed;
        turnSpeed =  runSpeed * 90;
        deceleration =  runSpeed;
        acceleration =  runSpeed * runSpeed;
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
        crntSlideLength = 1 / runSpeed;
        crntJumpForce = jumpForce;
        rotationTarget = World.Instance.StartDirection.y;
        isFirstFrame = false;
        anim = gameObject.GetComponentInChildren<Animator>();
        anim.Play("Run");
    }

    protected void UpdateRun()
    {
        //Run forward
        transform.Translate(transform.forward * 2.0f * crntSpeed * Time.deltaTime, Space.World);
    }

    protected void TurnLeft()
    {
        //ACTIVATION PHASE

        anim.Play("TurnLeft90");

        rotationLast = rotationTarget;
        rotationTarget -= 90.0f;
        turnPhase = 1;
        turnRotation = true;
        crntSpeed = runSpeed;

        if (rotationTarget == 360 || rotationTarget == -360) rotationTarget = 0.0f;
        if (rotationTarget == -270) rotationTarget = 90;
        if (rotationTarget == -180) rotationTarget = 180;
        if (rotationTarget == -90) rotationTarget = 270;
        qTo = Quaternion.Euler(0.0f, rotationTarget, 0.0f);

    }
    protected void TurnRight()
    {
        //ACTIVATION PHASE

        anim.Play("TurnRight90");

        rotationLast = rotationTarget;
        rotationTarget += 90.0f;
        turnPhase = 1;
        turnRotation = true;
        crntSpeed = runSpeed;

        if (rotationTarget == 360 || rotationTarget == -360) rotationTarget = 0.0f;
        if (rotationTarget == -270) rotationTarget = 90;
        if (rotationTarget == -180) rotationTarget = 180;
        if (rotationTarget == -90) rotationTarget = 270;
        qTo = Quaternion.Euler(0.0f, rotationTarget, 0.0f);

    }
    private void UpdateTurn()
    {
        //BRAKING PHASE
        if (turnPhase == 1)
        {
            if (crntSpeed > 0) crntSpeed -= deceleration * Time.deltaTime;
            else crntSpeed = 0;

            Vector3 tempVec = transform.position;
            if (tempVec.z < 0) tempVec.z *= -1;
            if (tempVec.x < 0) tempVec.x *= -1;
            if (rotationLast == 0 && tempVec.z % 2 >= 0 && tempVec.z % 2 < 1) turnPhase = 2;
            else if (rotationLast == 90 && tempVec.x % 2 >= 0 && tempVec.x % 2 < 1) turnPhase = 2;
            else if (rotationLast == 180 && tempVec.z % 2 <= 2 && tempVec.z % 2 > 1) turnPhase = 2;
            else if (rotationLast == 270 && tempVec.x % 2 <= 2 && tempVec.x % 2 > 1) turnPhase = 2;
        }

        //TURNING PHASE
        if (turnRotation)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, qTo, turnSpeed * Time.deltaTime);
            if (transform.rotation == qTo) turnRotation = false;
        }


        //AXELERATION PHASE
        if (turnPhase == 2)
        {
            transform.rotation = qTo;
            turnRotation = false;

            crntSpeed += acceleration * Time.deltaTime;
            if (crntSpeed >= runSpeed)
            {
                crntSpeed = runSpeed;
                turnPhase = 0;
            }
        }
    }

    protected void Slide()
    {
        //Slide start
        isSliding = true;
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.5f, transform.localScale.z);
        
    }
    private void UpdateSlide()
    {
        if (isSliding)
        {
            if (crntSlideLength <= 0.1)
            {
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 2, transform.localScale.z);
                crntSlideLength = 1  / runSpeed;
                isSliding = false;
            }
            else
            {
                crntSlideLength -= 1.0f * Time.deltaTime;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Floor"))
        {
            isJumping = false;
            isFalling = false;
            transform.position = new Vector3(transform.position.x, other.transform.position.y, transform.position.z);
        }

        if (other.gameObject.CompareTag("Wall"))
        {
            Death();
        }

        if (other.gameObject.CompareTag("Goal"))
        {
            GoalFunc();
        }

        if (other.gameObject.CompareTag("Hole"))
        {
            isFalling = true;
        }
    }

    protected void Jump()
    {
        isJumping = true;
    }
    private void UpdateJump()
    {
        if (isJumping)
        {
            //Going up
            if (!isFalling)
            {
                Debug.Log(crntJumpForce);
                crntJumpForce -= gravity * Time.deltaTime;
                transform.Translate(transform.up * crntJumpForce * Time.deltaTime, Space.World);
                if (crntJumpForce <= 0)
                {
                    crntJumpForce = jumpForce;
                    isFalling = true;
                }
            }
        }
    }
    private void UpdateFalling()
    {
        if (isFalling)
        {
            transform.Translate(transform.up * -gravity * Time.deltaTime, Space.World);
        }
    }

    protected void SetNextAction(PlayerState input)
    {
        nextAction = input;
    }

    private void nextActionActivation()
    {
        if (turnPhase == 0 || turnPhase == 2)
        {
            Vector3 tempVec = transform.position;
            if (tempVec.z < 0) tempVec.z *= -1;
            if (tempVec.x < 0) tempVec.x *= -1;

            if (rotationTarget <= 1 && rotationTarget >= -1)
            {
                rotationTarget = 0;
            }

            if (rotationTarget == 0)
            {

                if (tempVec.z % 2 <= 1 && !isJumping && !isSliding)
                {
                    isActionActive = false;
                }

                if (tempVec.z % 2 >= 1 && !isActionActive)
                {
                    ActivateNextAction();
                    isActionActive = true;
                }
            }
            if (rotationTarget == 90)
            {
                if (tempVec.x % 2 <= 1 && !isJumping && !isSliding)
                {
                    isActionActive = false;
                }

                if (tempVec.x % 2 >= 1 && !isActionActive)
                {
                    ActivateNextAction();
                    isActionActive = true;
                }
            }
            if (rotationTarget == 180)
            {

                if (tempVec.z % 2 >= 1 && !isJumping && !isSliding)
                {
                    isActionActive = false;
                }

                if (tempVec.z % 2 < 1 && !isActionActive)
                {
                    ActivateNextAction();
                    isActionActive = true;
                }
            }
            if (rotationTarget == 270)
            {

                if (tempVec.x % 2 >= 1 && !isJumping && !isSliding)
                {
                    isActionActive = false;
                }
                if (tempVec.x % 2 <= 1 && !isActionActive)
                {
                    ActivateNextAction();
                    isActionActive = true;
                }
            }
        }      
    }

    virtual protected void ActivateNextAction()
    {
        if (nextAction != PlayerState.Idle)
        {
            if (nextAction == PlayerState.TurnLeft) TurnLeft();
            if (nextAction == PlayerState.TurnRight) TurnRight();
            if (nextAction == PlayerState.Jump) Jump();
            if (nextAction == PlayerState.Slide) Slide();

            nextAction = PlayerState.Idle;
        }
    }

   
    protected virtual void Death()
    {
        Destroy(gameObject);
    }

    protected virtual void GoalFunc()
    {

    }

    protected void MovementUpdate()
    {
        nextActionActivation();
        UpdateJump();
        UpdateSlide();
        UpdateFalling();
        UpdateTurn();
        UpdateRun();
    }
}
