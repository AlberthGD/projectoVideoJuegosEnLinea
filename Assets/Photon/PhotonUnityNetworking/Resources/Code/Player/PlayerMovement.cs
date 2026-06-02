using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviourPun, IPunObservable 
{
    [Header("Movimiento")]
    public float baseClimbSpeed = 5f;
    private float slowTimer = 0f;
    private bool isSlowed = false;
    public float climbSpeed;    
    public float swingForce = 35f;   
    public float initialBurstMultiplier = 3f; 
    public float maxSwingSpeed = 15f;
    public float velocidadCaida = 8f;
    
    
    public float resistenciaAlViento = 0.4f;

    private int hitCount = 0;
    private bool canClimb = true;
    private float disableTimer = 0f;

    [Header("Límites de Pantalla y Rebote")]
    public float maxHorizontalDistance = 3f; 
    public float maxSwingAngle = 20f; 
    public float wallBounceForce = 40f; 
    public float bounceCooldown = 1.5f; 

    [Header("Estructura de la Cuerda")]
    public int linksCount = 15;        
    public float linkLength = 1f;    
    public float ropeWidth = 0.15f; 
    public Material ropeMaterial;    
    public Transform puntoDeAgarreHand;

    [Header("Transición Elevador")]
    public float distanciaEscaladaFisica = 3f; 
    
   public float totalClimbedDistance = 0f; 

    private List<Rigidbody> ropeLinks = new List<Rigidbody>();
    private float startX; 
    private GameObject ropePivot; 
    private float leftCooldownTimer = 0f;
    private float rightCooldownTimer = 0f;
    private Animator anim; 

    private float currentClimbOffset = 0f; 
    private float initialPivotY; 
    
    private Vector3 lastHandPos = Vector3.zero;
    private float smoothedVelX = 0f;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(totalClimbedDistance);
        }
        else
        {
            totalClimbedDistance = (float)stream.ReceiveNext();
        }
    }

    void Start()
    {

        climbSpeed = baseClimbSpeed;

        if (GetComponent<Rigidbody>() != null) GetComponent<Rigidbody>().isKinematic = true;
        //if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
        
        anim = GetComponentInChildren<Animator>();

        CameraFollow cam1 = null;
        CameraFollow cam2 = null;
        GameObject c1Obj = GameObject.Find("Camara_J1");
        GameObject c2Obj = GameObject.Find("Camara_J2");
        if (c1Obj != null) cam1 = c1Obj.GetComponent<CameraFollow>();
        if (c2Obj != null) cam2 = c2Obj.GetComponent<CameraFollow>();

        if (photonView.IsMine)
        {
            if (PhotonNetwork.IsMasterClient && cam1 != null) cam1.target = this.transform;
            else if (!PhotonNetwork.IsMasterClient && cam2 != null) cam2.target = this.transform;
            startX = transform.position.x;
        }
        else
        {
            if (PhotonNetwork.IsMasterClient && cam2 != null) cam2.target = this.transform;
            else if (!PhotonNetwork.IsMasterClient && cam1 != null) cam1.target = this.transform;
        }

        BuildStaticPendulumRope();
        initialPivotY = ropePivot.transform.position.y;
    }

    void BuildStaticPendulumRope()
    {
        ropePivot = new GameObject("RopePivot_" + photonView.ViewID);
        ropePivot.transform.position = transform.position + new Vector3(0, linksCount * linkLength, 0); 
        
        Rigidbody pivotRb = ropePivot.AddComponent<Rigidbody>();
        pivotRb.isKinematic = true; 

        Vector3 spawnPos = ropePivot.transform.position; 
        Rigidbody previousLink = pivotRb; 

        for (int i = 0; i < linksCount; i++)
        {
            GameObject link = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            link.name = "EslabonFisico_" + i;
            link.GetComponent<Collider>().isTrigger = true;
            link.transform.localScale = new Vector3(ropeWidth, linkLength / 2f, ropeWidth);
            spawnPos.y -= linkLength; 
            link.transform.position = spawnPos;

            if (ropeMaterial != null) link.GetComponent<Renderer>().material = ropeMaterial;

            Rigidbody linkRb = link.AddComponent<Rigidbody>();
            ropeLinks.Add(linkRb);

            if (photonView.IsMine)
            {
                linkRb.mass = 1f; 
                linkRb.linearDamping = 0.5f; 
                linkRb.angularDamping = 15f; 
                
                // Esto es lo que realmente evita el "helicóptero" sin romper las físicas
                linkRb.maxAngularVelocity = 10f; 

                linkRb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

                HingeJoint hinge = link.AddComponent<HingeJoint>();
                hinge.connectedBody = previousLink;
                hinge.axis = Vector3.forward;
                hinge.anchor = new Vector3(0, 1f, 0); 
                hinge.connectedAnchor = new Vector3(0, -1f, 0); 

                // Solo le ponemos límite estricto al eslabón superior
                if (i == 0) 
                {
                    hinge.useLimits = true;
                    JointLimits limits = hinge.limits;
                    limits.min = -maxSwingAngle; 
                    limits.max = maxSwingAngle;  
                    limits.bounciness = 0.2f; 
                    hinge.limits = limits;
                }
            }
            else
            {
                linkRb.isKinematic = true; 
            }
            previousLink = linkRb;
        }
    }

    void DisableClimbing()
    {
        canClimb = false;
        disableTimer = 2f;

        if (totalClimbedDistance > 0f)
        {
            if (anim != null) anim.SetBool("cayendo", true);
        }

        Debug.Log("Climbing disabled for 2 seconds!");
    }

    void TakeHit()
    {
        
        climbSpeed = baseClimbSpeed * 0.25f; 
        isSlowed = true;
        slowTimer = 1f;

        hitCount++;

        Debug.Log("Hit! Count: " + hitCount);

        if (hitCount >= 5)
        {
            DisableClimbing();
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (!photonView.IsMine) return;

        if (collision.gameObject.CompareTag("obs1"))
        {
            PlayerInventory miInventario = GetComponent<PlayerInventory>();
            
            if (miInventario != null && miInventario.tieneEscudoActivo)
            {
                Debug.Log("¡Golpe bloqueado por el escudo!");
                miInventario.RecibirGolpeEscudo();
                return; 
            }

            TakeHit();
            Debug.Log("Golpeado por un obstáculo");
        }

        if (collision.gameObject.CompareTag("LAVA"))
        {
            TakeHit();
            Debug.Log("Golpeado por LAVA");
        }

    }

    void Update()
    {
        if (ropeLinks.Count == 0 || ropePivot == null) return;

        if (photonView.IsMine)
        {
            if (isSlowed)
            {
                slowTimer -= Time.deltaTime;

                if (slowTimer <= 0f)
                {
                    climbSpeed = baseClimbSpeed;
                    isSlowed = false;
                }
            }
            
            if (!canClimb)
            {
                disableTimer -= Time.deltaTime;
                if (totalClimbedDistance > 0f)
                {
                    totalClimbedDistance -= velocidadCaida * Time.deltaTime;
                    
                    if (totalClimbedDistance <= 0f)
                    {
                        totalClimbedDistance = 0f;
                        if (anim != null) anim.SetBool("cayendo", false);
                    }
                }
                
                if (disableTimer <= 0f)
                {
                    canClimb = true;
                    hitCount = 0; 
                    if (anim != null) anim.SetBool("cayendo", false); 
                }

            }
            else
            {
                float verticalInput = 0f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) verticalInput = 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) verticalInput = -1f;

                if (anim != null) anim.SetFloat("velocidadAnim", (verticalInput != 0) ? 1f : 0f);

                if (verticalInput != 0)
                {
                    totalClimbedDistance += verticalInput * climbSpeed * Time.deltaTime;
                    if (totalClimbedDistance < 0) totalClimbedDistance = 0f;
                }
            }
        }

        currentClimbOffset = Mathf.Min(totalClimbedDistance, distanciaEscaladaFisica);
        float elevacionTecho = Mathf.Max(0f, totalClimbedDistance - distanciaEscaladaFisica);
        
        ropePivot.transform.position = new Vector3(ropePivot.transform.position.x, initialPivotY + elevacionTecho, ropePivot.transform.position.z);

        if (!photonView.IsMine)
        {
            Vector3 handPos = puntoDeAgarreHand.position;
            Vector3 ceilingPos = ropePivot.transform.position;
            
            if (lastHandPos == Vector3.zero) lastHandPos = handPos;
            float currentVelX = (handPos.x - lastHandPos.x) / Time.deltaTime;
            smoothedVelX = Mathf.Lerp(smoothedVelX, currentVelX, Time.deltaTime * 5f);
            lastHandPos = handPos;

            float totalRopeLength = (linksCount - 1) * linkLength;
            float distFromTop = totalRopeLength - currentClimbOffset;

            int activeLinks = Mathf.Max(1, Mathf.FloorToInt(distFromTop / linkLength));
            int looseLinks = linksCount - activeLinks;

            Vector3[] ropeNodes = new Vector3[linksCount + 1];
            
            Vector3 topP0 = ceilingPos;
            Vector3 topP2 = handPos;
            Vector3 topP1 = (topP0 + topP2) / 2f;
            topP1.x -= smoothedVelX * 0.22f;
            topP1.y -= 0.5f;

            for(int i = 0; i <= activeLinks; i++)
            {
                float t = (float)i / activeLinks;
                ropeNodes[i] = GetBezierPoint(topP0, topP1, topP2, t);
            }
            ropeNodes[activeLinks] = handPos; 
            if (looseLinks > 0)
            {
                Vector3 tailStart = handPos;
                Vector3 tailEnd = handPos + Vector3.down * (looseLinks * linkLength);
                tailEnd.x -= smoothedVelX * 0.25f; 
                
                Vector3 tailControl = (tailStart + tailEnd) / 2f;
                tailControl.x -= smoothedVelX * 0.15f; 

                for(int i = 1; i <= looseLinks; i++)
                {
                    float t = (float)i / looseLinks;
                    ropeNodes[activeLinks + i] = GetBezierPoint(tailStart, tailControl, tailEnd, t);
                }
            }

            for (int i = 0; i < ropeLinks.Count; i++)
            {
                if (i >= ropeNodes.Length - 1) break;

                Vector3 startNode = ropeNodes[i];
                Vector3 endNode = ropeNodes[i+1];
                
                ropeLinks[i].transform.position = (startNode + endNode) / 2f;
                
                Vector3 dir = endNode - startNode;
                if (dir != Vector3.zero) 
                {
                    ropeLinks[i].transform.up = dir.normalized;
                }

                float dist = dir.magnitude;
                ropeLinks[i].transform.localScale = new Vector3(ropeWidth, dist / 2f, ropeWidth);
            }
            return; 
        }

        if (leftCooldownTimer > 0) leftCooldownTimer -= Time.deltaTime;
        if (rightCooldownTimer > 0) rightCooldownTimer -= Time.deltaTime;

        AttachPlayerToHybridRope();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || ropeLinks.Count == 0) return;

        float horizontalInput = 0f;

        if (canClimb) 
        {
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontalInput = 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontalInput = -1f;
        }

        if (horizontalInput > 0 && rightCooldownTimer > 0) horizontalInput = 0f;
        if (horizontalInput < 0 && leftCooldownTimer > 0) horizontalInput = 0f;

        float totalRopeLength = (linksCount - 1) * linkLength;
        float distFromTop = totalRopeLength - currentClimbOffset;
        int currentIndex = Mathf.FloorToInt(distFromTop / linkLength);
        currentIndex = Mathf.Clamp(currentIndex, 0, ropeLinks.Count - 1);
        
        Rigidbody targetRb = ropeLinks[currentIndex];
        
        float currentX = targetRb.transform.position.x;
        float distanceFromCenter = currentX - startX;

        if (distanceFromCenter > maxHorizontalDistance)
        {
            if (horizontalInput > 0) horizontalInput = 0; 
            if (targetRb.linearVelocity.x > 0)
            {
                targetRb.linearVelocity = new Vector3(0, targetRb.linearVelocity.y, targetRb.linearVelocity.z);
                targetRb.AddForce(Vector3.left * wallBounceForce, ForceMode.VelocityChange);
                rightCooldownTimer = bounceCooldown; 
            }
        }
        else if (distanceFromCenter < -maxHorizontalDistance)
        {
            if (horizontalInput < 0) horizontalInput = 0; 
            if (targetRb.linearVelocity.x < 0)
            {
                targetRb.linearVelocity = new Vector3(0, targetRb.linearVelocity.y, targetRb.linearVelocity.z);
                targetRb.AddForce(Vector3.right * wallBounceForce, ForceMode.VelocityChange);
                leftCooldownTimer = bounceCooldown; 
            }
        }

        float windDir = 0f;
        if (WindManager.Instance != null && WindManager.Instance.direccionVientoActual != 0)
        {
            windDir = WindManager.Instance.direccionVientoActual;
            
            targetRb.AddForce(Vector3.right * windDir * WindManager.Instance.fuerzaViento, ForceMode.Acceleration);
        }

        if (horizontalInput != 0)
        {
            float speedInDesiredDirection = targetRb.linearVelocity.x * horizontalInput;
            float appliedForce = swingForce;

            if (speedInDesiredDirection < (maxSwingSpeed * 0.4f)) appliedForce *= initialBurstMultiplier;

            if (windDir != 0 && Mathf.Sign(horizontalInput) != Mathf.Sign(windDir))
            {
                appliedForce *= resistenciaAlViento; 
            }

            targetRb.AddForce(Vector3.right * horizontalInput * appliedForce, ForceMode.Acceleration);

            if (targetRb.linearVelocity.magnitude > maxSwingSpeed)
            {
                targetRb.linearVelocity = targetRb.linearVelocity.normalized * maxSwingSpeed;
            }
        }
    }

    void AttachPlayerToHybridRope()
    {
        float totalRopeLength = (linksCount - 1) * linkLength;
        float distFromTop = totalRopeLength - currentClimbOffset; 
        
        int index = Mathf.FloorToInt(distFromTop / linkLength);
        float t = (distFromTop % linkLength) / linkLength;

        if (index < 0) { index = 0; t = 0; }
        if (index >= ropeLinks.Count - 1) { index = ropeLinks.Count - 2; t = 1f; }

        Vector3 currentLinkPos = ropeLinks[index].transform.position;
        Vector3 nextLinkPos = ropeLinks[index + 1].transform.position;

        Vector3 ropeCenter = Vector3.Lerp(currentLinkPos, nextLinkPos, t);
        Vector3 offsetDinamico = transform.position - puntoDeAgarreHand.position;
        
        transform.position = ropeCenter + offsetDinamico;
        transform.rotation = ropeLinks[index].transform.rotation;
    }

    Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t) 
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0; 
        p += 2f * u * t * p1; 
        p += tt * p2; 
        return p;
    }
}