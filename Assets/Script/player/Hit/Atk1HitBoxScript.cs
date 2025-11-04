using Supercyan.AnimalPeopleSample;
using UnityEngine;

public class Atk1HitBoxScript : MonoBehaviour
{
    public PlayerController playerController;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    private void OnTriggerEnter(Collider other)
    {
        playerController.OnAtk1ColliderEnter(other);
    }
}
