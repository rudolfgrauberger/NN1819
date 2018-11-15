using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlScript : MonoBehaviour {

    private Rigidbody rigid;

    public float GroundDistance, StabilizingForce, SidewaysForce, JumpForce, MinimumClearance;

    private float steeringRightForce, steeringLeftForce, jumpingForce;
        
    private bool isJumping;
    
    public ControlMode controlMode;

    Vector3 initialPosition;
    Quaternion initialRotation;

    // Use this for initialization
    void Start () {
        rigid = GetComponent<Rigidbody>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }
    
    // Update is called once per frame
    void Update () {
        if (controlMode == ControlMode.automatic)
            return;
        if (Input.GetKey(KeyCode.A))
            SteerLeft(1,true);
        if (Input.GetKey(KeyCode.D))
            SteerRight(1,true);
        if (Input.GetKeyDown(KeyCode.Space))
            Jump(1,true);
    }

    private void FixedUpdate()
    {
        RaycastHit hit;
        if(Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            float groundDistance = Vector3.Distance(transform.position, hit.point);
            float repulsionFactor = GroundDistance- groundDistance;
            if (repulsionFactor > 0)
                isJumping = false;
            rigid.AddRelativeForce(Vector3.up * StabilizingForce*repulsionFactor);
        }
            rigid.AddRelativeForce(Vector3.right * SidewaysForce*steeringRightForce);
            rigid.AddRelativeForce(Vector3.left * SidewaysForce*steeringLeftForce);
        if (!isJumping && jumpingForce-.1f>0f)
        {
            isJumping = true;
            Vector3 nVelocity = rigid.velocity;
            nVelocity.y = JumpForce*jumpingForce;
            rigid.velocity = nVelocity;
        }
        StopSteering();
    }

    public void OnCollisionEnter(Collision collision)
    {
        StartCoroutine(Reset());
    }

    public void SteerRight(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;
        steeringRightForce = Mathf.Clamp01(force);
    }

    public void SteerLeft(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;
        steeringLeftForce = Mathf.Clamp01(force);
    }

    public void Jump(float force, bool manualOverride)
    {
        if (controlMode == ControlMode.automatic && manualOverride)
            return;
        jumpingForce = Mathf.Clamp01(force);
    }
    public void StopSteering()
    {
        steeringRightForce=0;
        steeringLeftForce=0;
        jumpingForce=0;
    }

    private IEnumerator Reset()
    {
        yield return new WaitForSeconds(1);
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        rigid.angularVelocity = Vector3.zero;
        rigid.velocity = Vector3.zero;
    }
}

public enum ControlMode
{
    manual,
    automatic
};
