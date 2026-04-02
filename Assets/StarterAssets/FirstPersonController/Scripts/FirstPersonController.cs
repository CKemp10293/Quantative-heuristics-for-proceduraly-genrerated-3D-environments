using UnityEngine;
using UnityEngine.Rendering;



#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		// swimming
		[Header("Ocean Settings")]
		public bool oceanEnabled = true;
    	public float waterLevel = 12f; // EXACTLY matches your HDRP Ocean Y-position
    	public float waterDrag = 2f;   // Slows the player down in water
    	public float buoyancy = 1f;    // Pushes the player up to the surface
		public float swimSpeed = 3f;

		[Header("Audio Settings")]
		public AudioSource ambientAudioSource;

		[Header("Dynamic Footstep Audio")]
    	public AudioSource footstepAudioSource;
    	public float baseStepRate = 0.5f; // How long between steps at normal walking speed
    	private float stepTimer = 0f;

		[HideInInspector] public float sandMaxHeight;
    	[HideInInspector] public float grassMaxHeight;
    	[HideInInspector] public AudioClip[] snowFootsteps; 
    	[HideInInspector] public AudioClip[] grassFootsteps;
    	[HideInInspector] public AudioClip[] sandFootsteps;

    	[Header("Surface Audio Clips")]
    	public AudioClip[] swimStrokes;

		[Header("VFX Settings")]
		public ParticleSystem windParticleSystem;

		[Header("Swimming Effects")]
        public float swimBobSpeed = 2f;
        public float swimBobAmount = 0.08f;
        public Volume underwaterVolume; // Link to our HDRP FX
        private float defaultCameraY;

		[Header("Animation Settings")]
    	public Animator playerAnimator; 
    	private CharacterController controller;

    	private Vector3 velocity;
    	private bool isGrounded;
    	private bool isSwimming;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

	
#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			controller = GetComponent<CharacterController>();
			defaultCameraY = CinemachineCameraTarget.transform.localPosition.y;
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
		}

		private void Update()
		{
			float currentSpeed = 0f;
			isSwimming = oceanEnabled && transform.position.y < (waterLevel - 0.2f);

			if (isSwimming)
			{
				HandleSwimming();
			}
			else
			{
				JumpAndGravity();
				GroundedCheck();
				Move();
			};

			if (playerAnimator != null && controller != null)
        	{
            	if (isSwimming) 
            	{
                	// In water, bypass standard physics and force a steady swim speed
                	currentSpeed = _input.move.magnitude > 0.1f ? swimSpeed : 0f; 
            	}
            	else
            	{
               		// On land, read the actual physics velocity
                	Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
                	currentSpeed = horizontalVelocity.magnitude;
            	}

            	playerAnimator.SetFloat("Speed", currentSpeed);
            
            	// Only trigger the swim animation if the player is physically submerged
            	playerAnimator.SetBool("IsSwimming", isSwimming);
        	}

			HandleDynamicAudio(currentSpeed, isSwimming);
		}

		private void LateUpdate()
		{
			CameraRotation();
			SwimmingVisuals();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}
		void HandleSwimming()
    	{
        	_verticalVelocity = Mathf.Lerp(_verticalVelocity, 0f, waterDrag * Time.deltaTime);

			// 2. 3D Swimming Movement using the New Input System
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
			Vector3 move = transform.right * inputDirection.x + transform.forward * inputDirection.z;

			// Allow the player to swim up (Jump) or dive down (Sprint)
			if (_input.jump) move.y = 1f;
			if (_input.sprint) move.y = -1f; // Re-using sprint button to dive!

			_controller.Move(move * swimSpeed * Time.deltaTime);

			// 3. Gentle Buoyancy (Float to the top if not pressing anything)
			if (transform.position.y < waterLevel - 1f)
			{
				_verticalVelocity += buoyancy * Time.deltaTime;
			}

			// Apply the vertical math
			_controller.Move(new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    	}
		private void SwimmingVisuals()
        {
            // 1. Handle the Camera Bobble
            Vector3 targetLocalPos = CinemachineCameraTarget.transform.localPosition;
            if (isSwimming)
            {
                // Create a smooth up-and-down wave based on time
                float bobOffset = Mathf.Sin(Time.time * swimBobSpeed) * swimBobAmount;
                targetLocalPos.y = Mathf.Lerp(targetLocalPos.y, defaultCameraY + bobOffset, Time.deltaTime * 3f);
            }
            else
            {
                // Snap back to the normal neck height when walking
                targetLocalPos.y = Mathf.Lerp(targetLocalPos.y, defaultCameraY, Time.deltaTime * 5f);
            }
            CinemachineCameraTarget.transform.localPosition = targetLocalPos;

            // 2. Handle the Post-Processing Fade
            if (underwaterVolume != null)
            {
                float targetWeight = 0f;
				if (oceanEnabled)
				{
					float cameraWorldY = CinemachineCameraTarget.transform.position.y;
                    float depth = waterLevel - cameraWorldY;
                    targetWeight = Mathf.Clamp01(depth / 0.15f);
				}
				underwaterVolume.weight = Mathf.Lerp(underwaterVolume.weight, targetWeight, Time.deltaTime * 15f);
            }
        }

		public void HandleDynamicAudio(float speed, bool isSwimming)
        {
            // Fact Check: If we aren't moving, do not play any movement sounds.
        if(speed < 0.1f) return;

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            if (isSwimming)
            {
                PlayRandomClip(swimStrokes);
                stepTimer = baseStepRate * 4f;
            }
            else if (controller.isGrounded)
            {
                // Shoot the raycast down to find exactly what we are standing on
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2.0f))
                {
                    
                    // Extract the exact mathematical height of the ground we hit
                    float groundHeight = hit.point.y;

                    // Compare the height to our defined biomes
                    if (groundHeight <= sandMaxHeight)
                    {
                        PlayRandomClip(sandFootsteps);
                    }
                    else if (groundHeight <= grassMaxHeight)
                    {
                        PlayRandomClip(grassFootsteps);
                    }
                    else
                    {
                        // If they are higher than the grass limit, it must be snow/rock
                            PlayRandomClip(snowFootsteps);
                    }
                    
                }
                
                // Speed calculation for how fast the steps play
                stepTimer = baseStepRate / (speed / MoveSpeed);
            }
        }
	    }

		private void PlayRandomClip(AudioClip[] clips)
        {
            if (clips.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, clips.Length);
            footstepAudioSource.PlayOneShot(clips[index]);
        }
        }

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}