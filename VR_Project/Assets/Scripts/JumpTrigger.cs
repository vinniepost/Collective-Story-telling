using UnityEngine;

using System.Collections;


public class JumpTrigger : MonoBehaviour

{

    public AudioSource Scream;

    public GameObject FlashImg;


    // NIEUW: De reference naar je monster GameObject

    public GameObject Monster;


    void OnTriggerEnter(Collider other)

    {

        // Optioneel: Controleer of het de speler is die de trigger raakt

        // if (other.CompareTag("Player")) 

        // {


        // 1. Speel het geluid af

        Scream.Play();


        // 2. Schakel de flits in

        FlashImg.SetActive(true);


        // 3. Schakel het monster in

        Monster.SetActive(true);


        // 4. Start de coroutine om de effecten uit te schakelen

        StartCoroutine(EndJump());

        // }

    }


    IEnumerator EndJump()

    {

        // Wacht de duur van de flits en de monster-zichtbaarheid

        yield return new WaitForSeconds(2.03f);


        // 1. Schakel de flits uit

        FlashImg.SetActive(false);


        // 2. Schakel het monster uit

        Monster.SetActive(false);


        // 3. (Optioneel) Schakel deze trigger uit zodat het niet opnieuw gebeurt

        gameObject.SetActive(false);

    }

}