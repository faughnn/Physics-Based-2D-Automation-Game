using System.Collections;
using UnityEngine;

namespace FallingSand
{
    public class WorldItem : MonoBehaviour
    {
        [SerializeField] private ToolType toolType = ToolType.Shovel;

        public ToolType ToolType => toolType;

        private void Awake()
        {
            // Validate and configure collider
            var collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogError($"[WorldItem] No Collider2D attached to {gameObject.name}! Item won't be collectible.");
                return;
            }
            collider.isTrigger = true;
        }

        /// <summary>
        /// Called when the item is collected. Plays feedback and destroys the world object.
        /// </summary>
        public void Collect()
        {
            // Play pickup sound (procedural beep)
            PlayPickupSound();

            // Play scale pop animation then destroy
            StartCoroutine(CollectAnimation());
        }

        private IEnumerator CollectAnimation()
        {
            float duration = 0.15f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Quick scale up then down
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f;
                transform.localScale = startScale * scale;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void PlayPickupSound()
        {
            // Create temporary audio source for one-shot sound
            GameObject audioObj = new GameObject("PickupSound");
            AudioSource source = audioObj.AddComponent<AudioSource>();

            // Generate simple beep (A5 note, 0.1s duration)
            int sampleRate = 44100;
            float frequency = 880f;
            float duration = 0.1f;

            AudioClip clip = AudioClip.Create("Beep", (int)(sampleRate * duration), 1, sampleRate, false);
            float[] samples = new float[(int)(sampleRate * duration)];

            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - (t / duration);  // Fade out
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.3f;
            }

            clip.SetData(samples, 0);
            source.clip = clip;
            source.Play();

            Destroy(audioObj, duration + 0.1f);
        }
    }
}
