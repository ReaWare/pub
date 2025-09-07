using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.8f;
    [SerializeField] float acceleration = 30f;
    [SerializeField] float deceleration = 40f;
    [SerializeField] float inputDeadZone = 0.1f;
    [SerializeField] bool lockMovementDuringActions = true;

    [Header("Animation")]
    [SerializeField] bool use2DBlend = false;     // Blend Tree 2D con MoveX/MoveY?
    [SerializeField] float moveEnter = 0.05f;     // isteresi
    [SerializeField] float moveExit = 0.02f;
    [SerializeField] float animBaseSpeed = 1.15f; // moltiplicatore clip walk

    [Header("Actions & Tags")]
    [SerializeField] KeyCode interactKey = KeyCode.E; // cassa
    [SerializeField] KeyCode smokeKey = KeyCode.F; // lampione
    [SerializeField] string registerTag = "CashRegisterZone";
    [SerializeField] string lampTag = "SmokeZone";

    [Header("Checking Behaviour")]
    [SerializeField] bool holdToCheck = true;        // tieni premuto E per restare in checking
    [SerializeField] bool allowCancelWithMove = true;
    [SerializeField] bool requireReadyForPose = true;// mano su solo se cliente pronto (anti-spam)
    [SerializeField] bool autoUncheckOnSuccess = true;
    [SerializeField] float checkAutoCancelTime = 0.4f; // auto-abbassa mano se nessuno pronto (toggle)

    [Header("Checkout Cooldown")]
    [SerializeField] float cashRepeat = 0.15f; // antispam E tenuta
    float cashCd;

    [Header("Animator States")]
    [SerializeField] string checkoutState = "Player_Checkout";
    [SerializeField] string movementState = "Movement";

    // 🔧 NEW: trigger e piccolo lock per far partire la clip di servizio
    [Header("Checkout Anim (One-shot)")]
    [SerializeField] string checkoutTrigger = "DoCheckout"; // Trigger nell'Animator
    [SerializeField] float postCheckoutHold = 0.25f;        // tieni la posa per X secondi
    float checkoutAnimLockUntil = -1f;
    bool pendingAutoUncheck = false;

    // Refs
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;

    // Runtime
    Vector2 input, desiredVel, currentVel;
    bool isMoving, inRegisterZone, inLampZone;
    bool isChecking, isSmoking;
    int facing = 1; // 1=right, -1=left
    int registerContacts = 0;

    CashRegister currentRegister;
    float checkNoReadyTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        // --- INPUT & MOVIMENTO ---
        bool actionLock = lockMovementDuringActions && (isChecking || isSmoking);
        if (!actionLock)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            if (input.sqrMagnitude > 1f) input.Normalize();
        }
        else input = Vector2.zero;

        desiredVel = input * moveSpeed;

        // Flip orizzontale
        if (Mathf.Abs(input.x) > inputDeadZone)
        {
            facing = input.x > 0 ? 1 : -1;
            sr.flipX = facing < 0;
        }

        // --- CASSA / CHECKING ---
        float ax = Input.GetAxisRaw("Horizontal");
        float ay = Input.GetAxisRaw("Vertical");
        bool wantsToMove = Mathf.Abs(ax) > 0.1f || Mathf.Abs(ay) > 0.1f;

        bool pressE = Input.GetKeyDown(interactKey);
        bool holdE = Input.GetKey(interactKey);
        cashCd -= Time.deltaTime;

        bool ready = currentRegister && currentRegister.HasServeableCustomer();

        // Entrata/uscita checking (pose mano)
        if (holdToCheck)
        {
            
            // mantieni la posa se sei già in checking, anche se 'ready' lampeggia
            bool wantPose = inRegisterZone && holdE && (!requireReadyForPose || ready || isChecking);



            // 🔧 NEW: durante il mini-lock post incasso, tieni comunque la posa alzata
            if (Time.time < checkoutAnimLockUntil) wantPose = false;

            SetChecking(wantPose);
        }
        else
        {
            // Toggle: al press entro se in zona e (se richiesto) c'è pronto
            if (inRegisterZone && pressE)
            {
                if (isChecking) SetChecking(false);
                else if (!requireReadyForPose || ready) SetChecking(true);
            }
        }

        // Auto-cancel di sicurezza
        if (isChecking)
        {
            if (!inRegisterZone) SetChecking(false);
            else if (!holdToCheck && !ready)
            {
                checkNoReadyTimer += Time.deltaTime;
                if (checkNoReadyTimer >= checkAutoCancelTime) SetChecking(false);
            }
            else checkNoReadyTimer = 0f;
        }

        // Tentativo incasso (chiama sempre la cassa; antispam con cashCd)
        // NON chiamiamo più la cassa da qui: lo fa l'Interactor.
        // Qui teniamo solo cooldown e (se vuoi) la posa.

        if (isChecking && inRegisterZone && currentRegister)
        {
            if (pressE || (holdToCheck && holdE && cashCd <= 0f))
            {
                // nessuna chiamata a TryCheckout qui
                cashCd = cashRepeat;
                // niente animazione condizionata a "ok" (non la conosciamo qui)
            }
        }
        else if (!isChecking && inRegisterZone && pressE && currentRegister && cashCd <= 0f)
        {
            // nessuna chiamata a TryCheckout qui
            cashCd = cashRepeat;
        }


        // 🔧 NEW: se richiesto, abbassa la mano automaticamente dopo il breve lock
        if (pendingAutoUncheck && Time.time >= checkoutAnimLockUntil)
        {
            pendingAutoUncheck = false;
            SetChecking(false);
        }

        // --- LAMPIONE / SMOKING ---
        if (holdToCheck) SetSmoking(inLampZone && Input.GetKey(smokeKey));
        else if (inLampZone && Input.GetKeyDown(smokeKey)) SetSmoking(!isSmoking);

        if (allowCancelWithMove && isSmoking && (wantsToMove || Input.GetKeyDown(KeyCode.Escape)))
            SetSmoking(false);

        // --- ANIMATOR PARAMS ---
        float speedForAnim = (rb.bodyType == RigidbodyType2D.Dynamic) ? rb.velocity.magnitude : desiredVel.magnitude;
        anim.SetFloat("Speed", speedForAnim);

        if (isMoving) isMoving = speedForAnim > moveExit; else isMoving = speedForAnim > moveEnter;
        anim.SetBool("IsMoving", isMoving);

        if (use2DBlend)
        {
            Vector2 dir = isMoving ? desiredVel.normalized : new Vector2(facing, 0f);
            anim.SetFloat("MoveX", dir.x);
            anim.SetFloat("MoveY", dir.y);
        }

        // Scala leggera della walk (se lo usi come Speed Multiplier dello state Movement)
        float speedNorm = Mathf.InverseLerp(0f, moveSpeed, speedForAnim);
        anim.SetFloat("AnimSpeed", animBaseSpeed * Mathf.Lerp(0.9f, 1.2f, speedNorm));
    }

    void FixedUpdate()
    {
        float ramp = (desiredVel.sqrMagnitude > currentVel.sqrMagnitude) ? acceleration : deceleration;

        if (rb.bodyType == RigidbodyType2D.Kinematic)
        {
            currentVel = Vector2.MoveTowards(currentVel, desiredVel, ramp * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + currentVel * Time.fixedDeltaTime);
        }
        else
        {
            currentVel = Vector2.MoveTowards(rb.velocity, desiredVel, ramp * Time.fixedDeltaTime);
            rb.velocity = currentVel;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(registerTag))
        {
            registerContacts++;
            inRegisterZone = registerContacts > 0;
            // prendi la cassa solo la prima volta che entri

            if (currentRegister == null)
            {
                currentRegister = other.GetComponentInParent<CashRegister>();
                if (currentRegister) currentRegister.OnCheckoutSuccess += OnRegisterCheckoutSuccess;
            }
        }
        if (other.CompareTag(lampTag)) inLampZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(registerTag))
        {
            registerContacts = Mathf.Max(0, registerContacts - 1);
            inRegisterZone = registerContacts > 0;
            if (!inRegisterZone)
            {
                currentRegister.OnCheckoutSuccess -= OnRegisterCheckoutSuccess;
                currentRegister = null;
                SetChecking(false);
                checkoutAnimLockUntil = -1f;
                pendingAutoUncheck = false;
            }
        }
        if (other.CompareTag(lampTag)) { inLampZone = false; SetSmoking(false); }
    }


    void OnRegisterCheckoutSuccess()
    {
        if (anim && !string.IsNullOrEmpty(checkoutTrigger))
            anim.SetTrigger(checkoutTrigger);

        checkoutAnimLockUntil = Time.time + postCheckoutHold;
        if (autoUncheckOnSuccess) pendingAutoUncheck = true;
    }


    // --- Helpers ---
    void SetChecking(bool value)
    {
        if (isChecking == value) return;
        isChecking = value;
        anim.SetBool("IsChecking", value);

        if (value)
        {
            StopInstant();
            CrossFadeSafe(checkoutState, 0.02f);
        }
        else
        {
            checkNoReadyTimer = 0f;
            CrossFadeSafe(movementState, 0.02f);
        }
    }

    void SetSmoking(bool value)
    {
        if (isSmoking == value) return;
        isSmoking = value;
        anim.SetBool("IsSmoking", value);
        if (value) StopInstant();
    }

    void StopInstant()
    {
        desiredVel = Vector2.zero;
        currentVel = Vector2.zero;
        rb.velocity = Vector2.zero;
    }

    void CrossFadeSafe(string stateName, float duration = 0.02f)
    {
        if (anim && anim.isActiveAndEnabled && gameObject.activeInHierarchy)
            anim.CrossFade(stateName, duration, 0, 0f);
    }

    void OnDisable()
    {
        if (!anim) return;
        anim.SetBool("IsChecking", false);
        anim.SetBool("IsSmoking", false);
        isChecking = false;
        isSmoking = false;

        // 🔧 NEW: reset fix flags
        checkoutAnimLockUntil = -1f;
        pendingAutoUncheck = false;
    }
}
