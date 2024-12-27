using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput instance;

    private PlayerInput playerInput;

    private PlayerInputActions inputActions;

    #region INPUTS

    [Range(-1, 1)]
    public float pitch;
    [Range(-1, 1)]
    public float roll;
    [Range(-1, 1)]
    public float yaw;
    [Range(0, 1)]
    public bool isThrottleUpPressed;
    public bool isThrottleDownPressed;
    public bool flapDeployed;
    public bool brakeDeployed;
    public bool isTrimActive;
    #endregion

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            Debug.LogError("Multiple instances of GameInput found");
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        inputActions = new PlayerInputActions();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        inputActions.Player.ThrottleUp.performed += ThrottleUp_Performed;
        inputActions.Player.ThrottleUp.canceled += ThrottleUp_Canceled;
        inputActions.Player.ThrottleDown.performed += ThrottleDown_Performed;
        inputActions.Player.ThrottleDown.canceled += ThrottleDown_Canceled;
        inputActions.Player.Flap.performed += Flap_Toggle;
        inputActions.Player.Brake.performed += Brake_Toggle;
        inputActions.Player.Trim.performed += Trim_Toggle;
    }

    private void ThrottleDown_Performed(InputAction.CallbackContext obj)
    {
        isThrottleDownPressed = true;
    }

    private void ThrottleDown_Canceled(InputAction.CallbackContext context)
    {
        isThrottleDownPressed = false;
    }

    private void ThrottleUp_Performed(InputAction.CallbackContext obj)
    {
        isThrottleUpPressed = true;
    }

    private void ThrottleUp_Canceled(InputAction.CallbackContext obj)
    {
        isThrottleUpPressed = false;
    }

    private void Flap_Toggle(InputAction.CallbackContext obj)
    {
        flapDeployed = !flapDeployed;
    }

    private void Brake_Toggle(InputAction.CallbackContext obj)
    {
        brakeDeployed = !brakeDeployed;
    }

    private void Trim_Toggle(InputAction.CallbackContext obj)
    {
        isTrimActive = !isTrimActive;
    }

    private void Update()
    {
        pitch = inputActions.Player.Pitch.ReadValue<float>();
        roll = inputActions.Player.Roll.ReadValue<float>();
        yaw = inputActions.Player.Yaw.ReadValue<float>();
    }
}
