using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput instance;

    private PlayerInput playerInput;

    private PlayerInputActions inputActions;

    #region INPUTS
    public float pitch;
    public float roll;
    public float yaw;
    public bool isThrottlePressed;
    public bool isBrakePressed;
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
        inputActions.Player.Throttle.performed += Throttle_Performed;
        inputActions.Player.Throttle.canceled += Throttle_Canceled;
        inputActions.Player.Brake.performed += Brake_Performed;
        inputActions.Player.Brake.canceled += Brake_Canceled;
    }

    private void Brake_Performed(InputAction.CallbackContext obj)
    {
        isBrakePressed = true;
    }

    private void Brake_Canceled(InputAction.CallbackContext context)
    {
        isBrakePressed = false;
    }

    private void Throttle_Performed(InputAction.CallbackContext obj)
    {
        isThrottlePressed = true;
    }

    private void Throttle_Canceled(InputAction.CallbackContext obj)
    {
        isThrottlePressed = false;
    }

    private void Update()
    {
        pitch = inputActions.Player.Pitch.ReadValue<float>();
        roll = inputActions.Player.Roll.ReadValue<float>();
        yaw = inputActions.Player.Yaw.ReadValue<float>();
    }
}
