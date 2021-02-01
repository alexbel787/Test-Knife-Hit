using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeScript : MonoBehaviour
{
    private bool hitDone;
    private bool hitFailed;

    private GameManagerScript GMS;
    private AssetHolderScript AHS;

    private void Awake()
    {
        GMS = GameObject.Find("GameManager").GetComponent<GameManagerScript>();
        AHS = GameObject.Find("AssetHolder").GetComponent<AssetHolderScript>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!hitFailed)
        {
            if (other.collider.CompareTag("Knife") && !hitDone)
            {
                hitFailed = true;
                print("Knife hits knife");
                SoundManager.instance.PlaySingle(SoundManager.instance.knifeSounds[1], 1f);
                Vibration.VibrateNope();
                GetComponent<Rigidbody2D>().gravityScale = 1;
                transform.position += new Vector3(0, 0, -.2f);
                int n;
                if (Random.Range(0, 2) == 0) n = 1;
                else n = -1;
                GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(1500, 2000) * n, 0));
                GetComponent<Rigidbody2D>().rotation = Random.Range(45f, 90f) * n;
                StartCoroutine(GMS.GameOverCoroutine());
            }
        }
        else
        {
            foreach (Transform t in transform)
                if (t.GetComponent<BoxCollider2D>()) Destroy(t.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hitFailed)
        {
            if (other.gameObject.CompareTag("Target"))
            {
                hitDone = true;
                if (GMS.bossLevel) SoundManager.instance.PlaySingle(SoundManager.instance.knifeSounds[2], 1f);
                else SoundManager.instance.PlaySingle(SoundManager.instance.knifeSounds[0], .5f);
                GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                transform.SetParent(GMS.target.transform);
                var particles = Instantiate(AHS.hitParticles, transform.position + new Vector3(0, .16f, 0), Quaternion.identity);
                Destroy(particles, 2);
                GMS.NextKnife();
                Destroy(this);
            }
            else if (other.gameObject.CompareTag("Apple"))
            {
                SoundManager.instance.PlaySingle(SoundManager.instance.appleSound, 1f);
                List<Transform> list = new List<Transform>();
                foreach (Transform t in other.transform) list.Add(t);
                foreach (Transform t in list)
                {
                    t.gameObject.SetActive(true);
                    t.SetParent(null);
                    t.gameObject.AddComponent<Rigidbody2D>();
                    t.GetComponent<Rigidbody2D>().AddForce((t.position - other.transform.position) * Random.Range(800, 1200));
                    t.GetComponent<Rigidbody2D>().AddTorque(Random.Range(-100f, 100f));
                }
                Destroy(other.gameObject);
                GMS.apples++;
                AHS.applesText.text = GMS.apples.ToString();
            }
        }
    }
}
