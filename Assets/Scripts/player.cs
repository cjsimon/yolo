using UnityEngine;
using System.Collections;

public class player:MonoBehaviour {
	/* Movement */
	// The horizontal and verticle axises
	float hAxis;
	float absHAxis;
	// The max speed of the player
	public float maxSpeed;
	// The x and y velocities of the player
	float xVelocity = 0;
	float yVelocity = 0;
	
	/* Jumping */
	// The jumpForce of the player
	public float jumpForce;
	// The hoverForce of the player applied when holding down jump
	public float hoverForce;
	// The number of times that the player is allowed to jump
	public int totalJumpCount;
	// The current jumpCount
	int jumpCount = 0;
	// Single or double jump allowed on falling
	public bool removeFirstJump;
	// Is the player allowed to change directions in air
	public bool flipInAir;
	// Will the player change directions on jump
	public bool flipOnJump;
	// The totalHoldTime allowed in frames
	public float totalHoldTime;
	// The currentHoldTime frame
	float currentHoldTime;
	
	/* Direction */
	// Is the player facing left or right
	bool facingRight = false;

	/* Animator Properties */
	private Animator anim;

	/* Sprite Renderer */
	private SpriteRenderer sprite;
	private BoxCollider2D collider;
	private Rigidbody2D rigidbody;
	/* Ground Properties */
	// Is the player grounded
	bool grounded = false;
	// Was the player grounded last frame
	bool wasGrounded = false;
	// The ground detector Transforms
	public Transform groundCheckLeft;
	public Transform groundCheckRight;
	// The set in and out positions for both ground detectors
	Vector2 inLeft;
	Vector2 inRight;
	Vector2 outLeft;
	Vector2 outRight;
	// The layers that are considered as ground
	public LayerMask groundLayer;

	/* Input Properties */
	// Jump Key
	bool holdingJump = false;
	// Was the jump held last frame
	bool jumpWasHeld = false;

	/// <summary>
	/// Runs when the game starts
	/// </summary>
	void Awake() {
		/* Setup */
		sprite = GetComponent<SpriteRenderer>();
		collider = GetComponent<BoxCollider2D>();
		rigidbody = GetComponent<Rigidbody2D>();
		/* Ground Detection */
		// The y localPosition of the ground detectors
		Vector2 groundLeft = groundCheckLeft.localPosition;
		Vector2 groundRight = groundCheckRight.localPosition;
		// The in/out positions of both ground detectors
		// Make sure to maintain the y position as is
		// The original positions as inward
		inLeft = groundLeft;
		inRight	= groundRight;
		// The modified positions as outward
		// The only difference is the x component,
		// So reuse the in variables and modify the x component accordingly
		outLeft = groundLeft;
		outRight = groundRight;
		outLeft.x = outLeft.x + 0.33f;
		outRight.x = outRight.x - 0.33f;
		// Additionally, new vectors can be used all together,
		// however this method might be slightly less efficient due to the number of instantiations
		// outLeft = new Vector2(groundLeft.x + 0.33f, groundLeft.y);
		// outRight = new Vector2(groundRight.x - 0.33f, groundRight.y);
	}

	/// <summary>
	/// Runs when script is made active
	/// </summary>
	void Start() {
		// Animation
		// Get the animator component from the object
		anim = GetComponent<Animator>();
	}

	void Update() {}

	/// <summary>
	/// Runs in between physics calls
	/// </summary>
	void FixedUpdate() {
		//Controls
		//Map the Inputs to their respective variables
		holdingJump = Input.GetButton("Jump");

		//Axises
		//Get the respective axis values
		hAxis = Input.GetAxis("Horizontal");
		//Get the abs values of the axis for animation purposes
		absHAxis = Mathf.Abs(hAxis);

		// Check if the player is grounded
		// Using area overlap
		//grounded = Physics2D.OverlapArea(groundCheckLeft.position, groundCheckRight.position, groundLayer);
		// Using 3 points of raycasting
		Vector2 bottomRight  = new Vector2(transform.position.x + collider.bounds.extents.x + collider.offset.x, transform.position.y - collider.bounds.extents.y + collider.offset.y);
		Vector2 bottomLeft   = new Vector2(transform.position.x - collider.bounds.extents.x + collider.offset.x, transform.position.y - collider.bounds.extents.y + collider.offset.y);
		Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y + collider.offset.y);
		grounded = (
			Physics2D.Raycast(bottomLeft, -Vector2.up, 0.2f, groundLayer)   ||
			Physics2D.Raycast(bottomCenter, -Vector2.up, 0.2f, groundLayer) ||
			Physics2D.Raycast(bottomRight, -Vector2.up, 0.2f, groundLayer)
		);

		#if UNITY_EDITOR
			print(grounded);
			Debug.DrawRay(bottomRight, -Vector2.up * 0.2f, Color.red);
			Debug.DrawRay(bottomLeft, -Vector2.up * 0.2f, Color.red);
			Debug.DrawRay(bottomCenter, -Vector2.up * 0.2f, Color.red);
		#endif

		// Reset properties when grounded
		groundReset();
		// Reposition Ground Detectors
		//groundDetectors();
		// Allow horizontal movement
		movePlayer();
		// If the player is allowed to be flipped in the air
		if(flipInAir || grounded) {
			//Flip the player's direction if nesissarry
			flipCheck();
		}
		// Allow verticle jumpping
		jumpCheck();
	}

	//Reset properties when grounded
	void groundReset() {
		if(grounded) {
			//Restore jump ability and currentHoldTime when grounded if either properties have not been restored yet
			if(currentHoldTime != 0) {
				//Reset availableHoldTime
				currentHoldTime = 0;
			}
			if(jumpCount != 0) {
				//No jumps have been used
				jumpCount = 0;
			}
		}
	}

	//Fix ground detection based on ground state to prevent collision box issues
	void groundDetectors() {
		//If grounded and player was not grounded last frame
		if(grounded && !wasGrounded) {
			//Set the groundChecks outward
			groundCheckLeft.localPosition = outLeft;
			groundCheckRight.localPosition = outRight;
			//Otherwise if not grounded and player was grounded last frame
		} else if(!grounded && wasGrounded) {
			//Set the ground checks inward
			groundCheckLeft.localPosition = inLeft;
			groundCheckRight.localPosition = inRight;
		}
	}

	/// <summary>
	/// Move the character along the x axis and flip the sprite when necesarry
	/// </summary>
	void movePlayer() {
		//If walking on the ground and the animation speed has not been changed yet
		if(grounded && hAxis != 0 && anim.speed != absHAxis*1.6f) {
			//Set the speed of the animation to the hAxis while not stopped
			anim.speed = absHAxis*1.6f;
		} else {
			//Set the animation speed back to the original speed
			anim.speed = 1;
		}
		//Set the horizontal speed for the animation 
		anim.SetFloat("speed", absHAxis);
		//Get the y Velocity of the player
		yVelocity = rigidbody.velocity.y;
		//Move the player faster if grounded
		if(grounded) {
			//Apply the move value * the maxSpeed to the object velocity
			//The axis range is from -1 to 1, hence why the maxSpeed is the maximum
			//Keep the y velocity the same
			rigidbody.velocity = new Vector2(hAxis * maxSpeed, yVelocity);
		} else {
			//Dampen the hAxis value in air
			//Get the hAxis value of jumpFrame
			rigidbody.velocity = new Vector2((hAxis) * maxSpeed, yVelocity);
		}
	}

	/// <summary>
	/// Allow the player to jump along the y axis with frame sensitive jumpHolding height
	/// </summary>
	void jumpCheck() {
		// If the jump button being held
		if(holdingJump) {
			// If all jumps are used
			if(jumpCount >= totalJumpCount) {
				// Cap the jump count at the limit
				jumpCount = totalJumpCount;
				// Stop the jumpCheck
				return;
			}

			// If this is the first jump frame
			// when the currentHoldTime is still 0
			if(currentHoldTime == 0) {
				// Add this frame to the currentHoldTime amount
				currentHoldTime++;
				// Set the y velocity to 0 to allow jumping in mid air
				rigidbody.velocity = new Vector2(rigidbody.velocity.x, 0);
				// Give the player an extra jumpForce boost on the first frame of jumpping to create a poppy jump
				// as opposed to a gradual rising effect, like a rocketship
				rigidbody.AddForce(new Vector2(0, jumpForce));
				// Allow the player to flip directions on each jump if allowed to do so
				// flipInAir already takes care of this if it is true
				if(!flipInAir && flipOnJump) {
					flipCheck();
				}
			//Otherwise if not the first jump frame
			} else if(currentHoldTime < totalHoldTime) {
				//The user is holding the jump button for a higher jump
				//Add this frame to the currentHoldTime amount
				currentHoldTime++;
				//And push the player upwards by hoverForce/currentHoldTime
				//Division by currentHoldTime allows for a gratual finish in rising
				GetComponent<Rigidbody2D>().AddForce(new Vector2(0, hoverForce/currentHoldTime));
			}
		// Otherwise, did the player stop holding the jump button mid jump
		} else if(!holdingJump && jumpWasHeld) {
			// The player has stopped holding jump
			// Take away the current hover ability and the current jump
			currentHoldTime = 0;
			jumpCount++;
		}

		// Check if a jump should be removed when the player falls off of a platform before jumpping
		if(removeFirstJump) {
			// If the player is now in the air and has not used the first jump yet
			if(!grounded && wasGrounded && !holdingJump && jumpCount == 0) {
				// Then the first jump has been used
				jumpCount++;
			}
		}
		
	}


	/// <summary>
	/// Check if the player needs to be flipped
	/// </summary>
	void flipCheck() {
		// If facing right but not yet indicated as so
		if(hAxis > 0 && !facingRight) {
			// Flip so that the player faces right
			flip();
			// Otherwise if facing left but not yet indicated as so
		} else if(hAxis < 0 && facingRight) {
			// Flip so that the player faces left
			flip();
		}
	}

	/// <summary>
	/// Invert the player's local x scale
	/// </summary>
	void flip() {
		// Invert the side being faced
		facingRight = !facingRight;
		// The current vector3 scale value retrieved from the local scale
		Vector3 currentLocalScale = transform.localScale;
		// Invert the current local scale.x value
		currentLocalScale.x *= -1;
		// Then apply the inversion value to the localScale
		transform.localScale = currentLocalScale;
	}

	/// <summary>
	/// Called before frame end
	/// </summary>
	void LateUpdate() {
		// Update the wasGrounded and jumpWasHeld states
		// from the current frame that is ending
		wasGrounded = grounded;
		jumpWasHeld = holdingJump;
		// Pass the current grounded status to the animator
		anim.SetBool("grounded", grounded);
		// Pass the x and y velocity to the animaor
		// anim.SetFloat("xVelocity", xVelocity);
		// anim.SetFloat("yVelocity", yVelocity);
	}
}