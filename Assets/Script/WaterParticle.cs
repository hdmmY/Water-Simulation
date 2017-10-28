using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu()]
public class WaterParticle: ScriptableObject
{
    public float m_radial;

    public float m_amplitude;

    public Vector3 m_originPos;

    public float m_orignTime;

    public float m_nowTime;

    public float m_waveSpeed;

    public float m_dispersionAngle;

    // Propergation direction
    public Vector3 m_propagationDir;
                                  
    public Vector3 CurrentPosition
    {
        get
        {
            return m_originPos + 
                   m_waveSpeed * m_propagationDir.normalized * 
                   (m_nowTime - m_orignTime);
        }
    }               

    public float CurrentLength
    {
        get
        {
            return m_dispersionAngle * (m_nowTime - m_orignTime) * m_waveSpeed;
        }
    }

    public float Deviation(Vector3 nowPos, float nowTime)
    {
        m_nowTime = nowTime;
        nowPos.y = 0;

        float distance = (nowPos - CurrentPosition).sqrMagnitude;

        float result =  m_amplitude * 0.5f *
                        (Mathf.Cos(Mathf.PI * distance / m_radial) + 1) *
                        MathUtitly.Rectangle(distance / (2 * m_radial));

        return (result < 0.05f) ? 0 : result;
    }

}
