using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace CHM.ChocoWater.Samples.Dynamics
{
    sealed class Spawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject circlePrefab;
        [SerializeField]
        private GameObject squarePrefab;
        public enum SpawnMode
        {
            Manual,
            Auto,
        }
        public SpawnMode spawnMode;
        [Min(0)]
        public float autoSpawnPeriod = 0.5f;
        private float nextSpawnTime = 0;
        void Update()
        {
            // Toggle manual/auto spawning.
#if ENABLE_INPUT_SYSTEM
            if(Keyboard.current.zKey.wasPressedThisFrame)
#else
            if(Input.GetKeyDown(KeyCode.Z))
#endif
            {
                if(spawnMode == SpawnMode.Manual)
                    spawnMode = SpawnMode.Auto;
                else spawnMode = SpawnMode.Manual;
            }
            // Spawning logic.
            if(spawnMode == SpawnMode.Manual)
            {
#if ENABLE_INPUT_SYSTEM
                if(Keyboard.current.xKey.wasPressedThisFrame)
#else
                if(Input.GetKeyDown(KeyCode.X))
#endif
                {
                    Spawn();
                }
            }
            else
            {
                if(Time.time >= nextSpawnTime)
                {
                    Spawn();
                    nextSpawnTime = Time.time + autoSpawnPeriod;
                }
            }
        }

        private void Spawn()
        {
            bool selectCircle = Random.value <= 0.5f;
            if (selectCircle)
            {
                var instance = Instantiate(circlePrefab);
                if (instance.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    sr.color = Random.ColorHSV(0, 0.5f, 0.75f, 1.0f, 0.5f, 1.0f);
                }
                // Biased random, so large circles are a bit rarer.
                float size = Mathf.Pow(Random.value, 4) * 2 + 1;
                if (instance.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.mass = size;
                }
                instance.transform.localScale = Vector3.one * size;
                var leftMin = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0));
                var rightMin = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
                float x = Random.Range(leftMin.x, rightMin.x);
                float y = leftMin.y + size + Random.value * 10;
                instance.transform.position = new Vector2(x, y);
            }
            else // Select square.
            {
                var instance = Instantiate(squarePrefab);
                if (instance.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    sr.color = GetRandomColor();
                }
                // Biased random, so large circles are a bit rarer.
                float sizeX = Mathf.Pow(Random.value, 2) * 2 + 0.5f;
                float sizeY = Mathf.Pow(Random.value, 2) * 2 + 0.5f;
                if (instance.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.mass = Mathf.Sqrt(sizeX * sizeY);
                    rb.rotation = Random.Range(0.0f, 360.0f);
                }
                instance.transform.localScale = new Vector3(sizeX, sizeY, 1);
                var leftMin = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0));
                var rightMin = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
                float x = Random.Range(leftMin.x, rightMin.x);
                float y = leftMin.y + Mathf.Max(sizeX, sizeY) + Random.value * 10;
                instance.transform.position = new Vector2(x, y);
            }
        }

        private static Color GetRandomColor()
        {
            // Lighter colors to look better on a dark background.
            return Random.ColorHSV(0, 0.5f, 0.75f, 1.0f, 0.5f, 1.0f);
        }
    }
}
