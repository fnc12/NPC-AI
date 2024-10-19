using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformMovement : MonoBehaviour
{
	public int range = 10;//max height of Box's movement
	public float xCenter = 6f;
	public float speed = 10;
	public float xLimit = 30;
	public bool isInverted = false;

	Rigidbody rb;
	private void Start() {
		rb = GetComponent<Rigidbody>();
		speed = Random.Range(8f, 12f);
	}
	
	void FixedUpdate()
	{
		//	TODO: change 30 to anything desired
		if (isInverted) {
			if (transform.position.x < -xLimit) {
				transform.position = new Vector3(transform.position.x + xLimit * 2, transform.position.y, transform.position.z);
			}
		} else {
			if (transform.position.x > xLimit) {
				transform.position = new Vector3(transform.position.x - xLimit * 2, transform.position.y, transform.position.z);
			}
		}
		float speedRatio = isInverted ? -1 : 1;
		rb.MovePosition(new Vector3(transform.position.x + speedRatio * speed * Time.deltaTime, transform.position.y, transform.position.z));

	}
}
