using UnityEngine;
using System.Collections;

public class ParticleTriggerEffect : MonoBehaviour
{
    public ParticleSystem particleSystem;
    public ParticleSystem secondaryParticleSystem;

    public float timeDelay = 4;

    [Header("Target Settings")]
    public float targetSimulationSpeed = 1f;
    public Vector3 targetNoiseStrength = Vector3.one;

    [Header("Animation Durations")]
    public float enterDuration = 2f;
    public float exitDuration = 2f;

    private ParticleSystem.NoiseModule noiseModule;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.Particle[] particles;

    private float initialSize;
    private float initialSimulationSpeed;
    private Vector3 initialNoiseStrength;
    private Coroutine settingsRoutine;
    private bool isInTrigger = false;

    void Start()
    {
        if (particleSystem == null || secondaryParticleSystem == null) return;

        noiseModule = particleSystem.noise;
        mainModule = particleSystem.main;
        initialSize = mainModule.startSize.constant;
        initialSimulationSpeed = mainModule.simulationSpeed;
        initialNoiseStrength = new Vector3(noiseModule.strengthX.constant, noiseModule.strengthY.constant, noiseModule.strengthZ.constant);

        particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isInTrigger)
        {
            isInTrigger = true;

            if (settingsRoutine != null) StopCoroutine(settingsRoutine);
            settingsRoutine = StartCoroutine(AnimateEnter());

            secondaryParticleSystem.Stop();
            ModifyParticleColor(secondaryParticleSystem, new Color(1f, 1f, 1f, 0f));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && isInTrigger)
        {
            isInTrigger = false;

            if (settingsRoutine != null) StopCoroutine(settingsRoutine);
            settingsRoutine = StartCoroutine(AnimateExit());

            secondaryParticleSystem.Play();
            ModifyParticleColor(secondaryParticleSystem, new Color(1f, 1f, 1f, 1f));
        }
    }

    private IEnumerator AnimateEnter()
    {
        yield return new WaitForSeconds(timeDelay); 
        float elapsed = 0f;

     
        var originalStartSize = mainModule.startSize.constant;
        var originalStartColor = mainModule.startColor.color;
        mainModule.startSize = 0f;
        mainModule.startColor = new Color(1f, 1f, 1f, 0f);

        while (elapsed < enterDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / enterDuration);

            int count = particleSystem.GetParticles(particles);
            float sizeValue = Mathf.Lerp(initialSize, 0f, t);
            float alpha = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < count; i++)
            {
                particles[i].startSize = sizeValue;
                Color c = particles[i].startColor;
                particles[i].startColor = new Color(c.r, c.g, c.b, alpha);
            }

            particleSystem.SetParticles(particles, count);

        
            noiseModule.strengthX = Mathf.Lerp(initialNoiseStrength.x, targetNoiseStrength.x, t);
            noiseModule.strengthY = Mathf.Lerp(initialNoiseStrength.y, targetNoiseStrength.y, t);
            noiseModule.strengthZ = Mathf.Lerp(initialNoiseStrength.z, targetNoiseStrength.z, t);
            mainModule.simulationSpeed = Mathf.Lerp(initialSimulationSpeed, targetSimulationSpeed, t);

            yield return null;
        }
    }

    private IEnumerator AnimateExit()
    {
        float elapsed = 0f;

    
        mainModule.startSize = initialSize;
        mainModule.startColor = new Color(1f, 1f, 1f, 1f);

      
        Vector3 fromNoise = targetNoiseStrength;
        Vector3 toNoise = initialNoiseStrength;

        float fromSpeed = targetSimulationSpeed;
        float toSpeed = initialSimulationSpeed;

        while (elapsed < exitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);

            noiseModule.strengthX = Mathf.Lerp(fromNoise.x, toNoise.x, t);
            noiseModule.strengthY = Mathf.Lerp(fromNoise.y, toNoise.y, t);
            noiseModule.strengthZ = Mathf.Lerp(fromNoise.z, toNoise.z, t);
            mainModule.simulationSpeed = Mathf.Lerp(fromSpeed, toSpeed, t);

            yield return null;
        }
    }

    private void ModifyParticleColor(ParticleSystem ps, Color color)
    {
        var main = ps.main;
        main.startColor = color;
    }
}
