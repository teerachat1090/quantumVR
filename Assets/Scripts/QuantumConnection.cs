using UnityEngine;

public class QuantumWaveConnection : MonoBehaviour
{
    [Header("Connection Points")]
    public Transform alicePoint;
    public Transform bobPoint;
    
    [Header("Line Settings")]
    public Color lineColor = new Color(0, 1, 1, 0.8f); // Cyan
    public float lineWidth = 0.1f;
    public Material lineMaterial;
    
    [Header("Wave Settings")]
    public int waveResolution = 50;
    public float waveAmplitude = 0.5f;
    public float waveFrequency = 2f;
    public float waveSpeed = 2f;
    
    [Header("Effect Settings")]
    public bool useGlow = true;
    public float glowSpeed = 2f;
    public float minGlow = 0.3f;
    public float maxGlow = 1f;
    
    [Header("Wave Direction")]
    public bool useVerticalWave = true;
    
    [Header("Particle Settings")]
    public bool enableParticles = true;
    public int particleCount = 20;
    public float particleSize = 0.1f;
    public float particleSpeed = 1f;
    public float particleSpread = 0.5f; // รัศมีการกระจาย
    public Color particleColor = new Color(0, 1, 1, 0.8f);
    public Material particleMaterial;
    
    private LineRenderer lineRenderer;
    private ParticleSystem particleSystem;
    private ParticleSystem.Particle[] particles;
    private float glowValue = 1f;
    private bool glowingUp = false;
    private float waveOffset = 0f;

    void Start()
    {
        SetupLineRenderer();
        if (enableParticles)
        {
            SetupParticleSystem();
        }
    }

    void SetupLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = waveResolution;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        
        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = lineColor;
        }
        
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
    }

    void SetupParticleSystem()
    {
        // สร้าง GameObject ใหม่สำหรับ Particle System
        GameObject particleObj = new GameObject("ConnectionParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        particleSystem = particleObj.AddComponent<ParticleSystem>();
        
        // ตั้งค่า Main Module
        var main = particleSystem.main;
        main.startLifetime = 2f;
        main.startSpeed = 0f; // เราจะควบคุมการเคลื่อนที่เอง
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.maxParticles = particleCount;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // ตั้งค่า Emission
        var emission = particleSystem.emission;
        emission.enabled = false; // เราจะสร้าง particle เอง
        
        // ตั้งค่า Renderer
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        if (particleMaterial != null)
        {
            renderer.material = particleMaterial;
        }
        else
        {
            // สร้าง Material แบบ Glow
            Material defaultMat = new Material(Shader.Find("Particles/Standard Unlit"));
            defaultMat.SetColor("_Color", particleColor);
            renderer.material = defaultMat;
        }
        
        // สร้าง array สำหรับเก็บ particles
        particles = new ParticleSystem.Particle[particleCount];
        
        // สร้าง particles เริ่มต้น
        InitializeParticles();
    }

    void InitializeParticles()
    {
        if (alicePoint == null || bobPoint == null) return;
        
        for (int i = 0; i < particleCount; i++)
        {
            // สุ่มตำแหน่งเริ่มต้นตามเส้น
            float t = Random.Range(0f, 1f);
            Vector3 pos = Vector3.Lerp(alicePoint.position, bobPoint.position, t);
            
            // เพิ่มการกระจายแบบสุ่ม
            pos += Random.insideUnitSphere * particleSpread;
            
            particles[i].position = pos;
            particles[i].startLifetime = 2f;
            particles[i].remainingLifetime = Random.Range(0.5f, 2f);
            particles[i].startSize = particleSize * Random.Range(0.5f, 1.5f);
            particles[i].startColor = particleColor;
            
            // สุ่มความเร็ว
            particles[i].velocity = Random.insideUnitSphere * particleSpeed * 0.5f;
        }
        
        particleSystem.SetParticles(particles, particleCount);
    }

    void Update()
    {
        if (alicePoint != null && bobPoint != null)
        {
            UpdateWave();
            
            if (useGlow)
            {
                AnimateGlow();
            }
            
            if (enableParticles && particleSystem != null)
            {
                UpdateParticles();
            }
        }
    }

    void UpdateWave()
    {
        waveOffset += Time.deltaTime * waveSpeed;
        
        Vector3 startPos = alicePoint.position;
        Vector3 endPos = bobPoint.position;
        Vector3 direction = endPos - startPos;
        
        Vector3 perpendicular;
        if (useVerticalWave)
        {
            perpendicular = Vector3.up;
        }
        else
        {
            perpendicular = Vector3.Cross(direction.normalized, Vector3.up);
            if (perpendicular.magnitude < 0.1f)
            {
                perpendicular = Vector3.Cross(direction.normalized, Vector3.forward);
            }
        }
        perpendicular = perpendicular.normalized;
        
        for (int i = 0; i < waveResolution; i++)
        {
            float t = i / (float)(waveResolution - 1);
            Vector3 basePosition = Vector3.Lerp(startPos, endPos, t);
            float waveValue = Mathf.Sin((t * waveFrequency * Mathf.PI * 2) + waveOffset) * waveAmplitude;
            float fadeMultiplier = Mathf.Sin(t * Mathf.PI);
            waveValue *= fadeMultiplier;
            Vector3 wavePosition = basePosition + (perpendicular * waveValue);
            
            lineRenderer.SetPosition(i, wavePosition);
        }
    }

    void UpdateParticles()
    {
        Vector3 startPos = alicePoint.position;
        Vector3 endPos = bobPoint.position;
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);
        
        particleSystem.GetParticles(particles);
        
        for (int i = 0; i < particleCount; i++)
        {
            // ลด lifetime
            particles[i].remainingLifetime -= Time.deltaTime;
            
            // ถ้า particle หมดอายุ ให้สร้างใหม่
            if (particles[i].remainingLifetime <= 0)
            {
                RespawnParticle(ref particles[i]);
            }
            else
            {
                // หาจุดที่ใกล้ที่สุดบนเส้น
                Vector3 particlePos = particles[i].position;
                Vector3 toParticle = particlePos - startPos;
                float projectionLength = Vector3.Dot(toParticle, direction);
                projectionLength = Mathf.Clamp(projectionLength, 0, distance);
                
                Vector3 closestPoint = startPos + direction * projectionLength;
                
                // คำนวณตำแหน่งบนเส้นคลื่น
                float t = projectionLength / distance;
                float waveValue = Mathf.Sin((t * waveFrequency * Mathf.PI * 2) + waveOffset) * waveAmplitude;
                float fadeMultiplier = Mathf.Sin(t * Mathf.PI);
                waveValue *= fadeMultiplier;
                
                Vector3 perpendicular;
                if (useVerticalWave)
                {
                    perpendicular = Vector3.up;
                }
                else
                {
                    perpendicular = Vector3.Cross(direction, Vector3.up);
                    if (perpendicular.magnitude < 0.1f)
                    {
                        perpendicular = Vector3.Cross(direction, Vector3.forward);
                    }
                }
                perpendicular = perpendicular.normalized;
                
                Vector3 wavePoint = closestPoint + perpendicular * waveValue;
                
                // ดึง particle เข้าหาเส้นคลื่นเบาๆ
                Vector3 toWave = wavePoint - particlePos;
                particles[i].velocity += toWave * particleSpeed * Time.deltaTime;
                
                // เพิ่มการเคลื่อนที่แบบสุ่ม
                particles[i].velocity += Random.insideUnitSphere * particleSpeed * 0.1f * Time.deltaTime;
                
                // จำกัดความเร็ว
                if (particles[i].velocity.magnitude > particleSpeed)
                {
                    particles[i].velocity = particles[i].velocity.normalized * particleSpeed;
                }
                
                // อัพเดทตำแหน่ง
                particles[i].position += particles[i].velocity * Time.deltaTime;
                
                // Fade out ตอนใกล้หมดอายุ
                float lifetimePercent = particles[i].remainingLifetime / particles[i].startLifetime;
                Color color = particleColor;
                color.a = particleColor.a * lifetimePercent;
                particles[i].startColor = color;
            }
        }
        
        particleSystem.SetParticles(particles, particleCount);
    }

    void RespawnParticle(ref ParticleSystem.Particle particle)
    {
        // สุ่มตำแหน่งใหม่บนเส้น
        float t = Random.Range(0f, 1f);
        Vector3 pos = Vector3.Lerp(alicePoint.position, bobPoint.position, t);
        pos += Random.insideUnitSphere * particleSpread;
        
        particle.position = pos;
        particle.startLifetime = 2f;
        particle.remainingLifetime = Random.Range(1f, 2f);
        particle.startSize = particleSize * Random.Range(0.5f, 1.5f);
        particle.startColor = particleColor;
        particle.velocity = Random.insideUnitSphere * particleSpeed * 0.5f;
    }

    void AnimateGlow()
    {
        if (glowingUp)
        {
            glowValue += Time.deltaTime * glowSpeed;
            if (glowValue >= maxGlow)
            {
                glowValue = maxGlow;
                glowingUp = false;
            }
        }
        else
        {
            glowValue -= Time.deltaTime * glowSpeed;
            if (glowValue <= minGlow)
            {
                glowValue = minGlow;
                glowingUp = true;
            }
        }
        
        Color currentColor = lineColor;
        currentColor.a = glowValue;
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
    }
}